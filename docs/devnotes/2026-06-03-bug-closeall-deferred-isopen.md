---
Title: B-CLOSEALL — Defer IsOpen decision so taskbar "Close all windows" restores every window
Date: 2026-06-03
Owner: Jeremy
Versions Affected: 1.7.0 (regression introduced by v1.7.0 multi-window fix) → 1.7.1 (fixed)
Links:
 - Planning: docs/PLANNING.md §OPEN ITEMS & ROADMAP TO v1.0
 - Bugs: docs/BUGS.md #B-CLOSEALL
 - Related: docs/devnotes/2026-06-03-bug-label-size-multiwindow-restore.md (initial v1.7.0 multi-window fix)
---

Architecture rules live in docs/PROJECT_MEMORY.md. This note records implementation details and rationale.

## Context & Goal

v1.7.0 shipped multi-window restore with an `IsOpen` flag on `NoteWindowData`. Logic:
- `MainWindow_Closing` counted other `MainWindow` instances still in `Application.Current.Windows`.
- If others > 0 → single-window close → `IsOpen=false`.
- If others == 0 → last window closing → preserve `IsOpen=true` so app reopens it next launch.

Jeremy reported: open 2 windows, right-click SnipShottyBoard in taskbar, choose "Close all windows", reopen app — only **one** window restores. Master.json proved it: first-closed window had `isOpen=false`, last-closed had `isOpen=true`.

Goal: detect close-all reliably and preserve `IsOpen=true` on every window so Sticky-Notes-style "whatever was open at close, that's what restores" works.

## Decisions & Alternatives

**Decision: Defer the IsOpen=false decision to `DispatcherPriority.ApplicationIdle`.**

Save during `Closing` with `IsOpen=true` (current state). Schedule the flip-to-false check after the dispatcher drains the entire WM_CLOSE message burst.

**Why this is the only reliable signal:**

At the moment any one `MainWindow.Closing` runs, the system cannot distinguish:
- (a) user clicked X on this window, app continues
- (b) OS / taskbar is closing every window sequentially, app will exit

In both cases other `MainWindow` instances are still alive in `Application.Current.Windows`. The Closing handlers fire one after another on the UI thread. Inside the first handler, the second window is still listed; inside the second handler the first is already gone. That ordering trap is exactly what v1.7.0 fell into.

The only post-hoc signal that's correct: **after** every queued WM_CLOSE has been processed, is the app still alive with `MainWindow` instances? Yes → single close. No → close-all → don't touch IsOpen.

`DispatcherPriority.ApplicationIdle` work runs after all higher-priority work (Input, Background, Send, Normal, etc.) has settled — which means after the sequential Closing/Closed events have all fired and the dispatcher message pump is idle.

**Alternatives rejected:**
- *Hook `Application.SessionEnding`* — only fires for Windows logoff/shutdown, not taskbar "close all."
- *Set a static "shutdown in progress" flag from `App.OnExit`* — `OnExit` fires after all `Closing` handlers, too late to influence what got saved during them.
- *Inspect Win32 message source (SC_CLOSE)* — title-bar X and taskbar "close all" produce identical messages; not distinguishable at the message level.
- *Time-based heuristic (multiple Closing events within N ms = burst)* — fragile, slow window destruction or debugger pauses break it.

## Implementation Notes

### `UI/Views/MainWindow.xaml.cs` — `MainWindow_Closing`

Removed the synchronous sibling-count block. Replaced with:

1. Capture `WindowData` into a local (window will be destroyed before the deferred callback runs; we need a stable reference to the data object that lives on `NoteWindowManager.Instance.NoteWindows`).
2. Call `SaveApplicationData()` immediately — this writes `IsOpen=true` (the current in-memory state).
3. Schedule the deferred check:

```csharp
Application.Current?.Dispatcher.BeginInvoke(
    new Action(() =>
    {
        var stillAliveOthers = Application.Current?.Windows
            .OfType<MainWindow>()
            .Count() ?? 0;

        if (stillAliveOthers > 0)
        {
            capturedWindowData.IsOpen = false;
            NoteWindowManager.Instance.SaveNoteWindows();
        }
        // else: close-all detected → leave IsOpen=true → all windows restore.
    }),
    System.Windows.Threading.DispatcherPriority.ApplicationIdle);
```

### Why count `MainWindow`s (not "other than this") in the deferred callback?

The deferred work runs *after* `this` window has been destroyed and removed from `Application.Current.Windows`. So a simple `.Count()` of remaining `MainWindow` instances is the correct signal:
- Count > 0 → at least one MainWindow is still alive after the burst → app is still running → this was a single-window close.
- Count == 0 → every MainWindow closed in the burst → app is exiting (or already exited) → close-all.

### Why use `NoteWindowManager.Instance.SaveNoteWindows()` and not `DataManager.SaveMasterData(...)` directly?

`NoteWindowManager.Instance.SaveNoteWindows()` constructs the full `MasterData` (windows + settings) and routes through `DataManager.SaveMasterData`. That's the same path `SaveApplicationData` uses, so we stay consistent with the rest of the app's save mechanics.

## Testing & Acceptance

| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | Open 2 windows, taskbar right-click → Close all windows, reopen | Both windows restore | ✅ |
| 2 | Open 3 windows, close #2 individually, close #3 individually, close #1 (last), reopen | Only #1 restores | ✅ |
| 3 | Open 1 window, close it via X, reopen | That window restores | ✅ |
| 4 | Delete a window from NoteListWindow (IsActive=false), reopen | Deleted window stays gone | ✅ (unaffected by this fix) |

## Performance & Limits

- One extra ApplicationIdle work item per window close. Negligible.
- One extra atomic save **only** in the single-window-close path. Close-all path has zero extra work.
- No new threading. All on UI dispatcher.

## Follow-ups

- None. Behavior matches Windows Sticky Notes app spec confirmed by Jeremy.

## Graduated to CR

- None. Pattern (defer-decision-until-idle) is too specific to multi-window-close detection to warrant a general PROJECT_MEMORY rule right now. If we hit the same pattern in another part of the app, promote then.
