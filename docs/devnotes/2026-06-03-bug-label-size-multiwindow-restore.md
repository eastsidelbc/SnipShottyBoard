---
Title: BUG-LABELSIZE + BUG-MULTIWIN — Per-image v3 persistence and Sticky-Notes-style multi-window restore
Date: 2026-06-03
Owner: Jeremy
Versions Affected: 1.6.0 (csproj) / 1.7.0 (VERSION)
Links:
 - Planning: docs/PLANNING.md §OPEN ITEMS & ROADMAP TO v1.0
 - BUGS: docs/BUGS.md §B-LABELSIZE, §B-MULTIWIN
---

Architecture rules live in docs/PROJECT_MEMORY.md. This note records implementation details and rationale.

## Context & Goal

Two user-reported bugs were investigated together because the audit surfaced them in the same session:

1. **B-LABELSIZE** — Setting a label or thumbnail size on an image worked in-session but reset to defaults on the next launch. Same for the per-image visibility flags (label/date/time/hide).
2. **B-MULTIWIN** — Opening multiple windows and closing the app would only restore the "first" window on the next launch. User wanted Windows Sticky Notes behavior: reopen exactly the windows that were visible at last close.

## Decisions & Alternatives

### B-LABELSIZE — `[JsonIgnore]` over setter neutering

**Decision:** Add `[System.Text.Json.Serialization.JsonIgnore]` to `SavedNote.ImageFiles` and `SavedNote.ImageTimestamps`.

**Alternatives considered:**

- Make the `ImageFiles` setter a no-op. Would also fix the bug but is more surprising (the property still serializes, JSON shape stays bloated). Rejected.
- Use `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]`. The properties never have a default "empty" state at serialize time (the getter always computes from Media), so this would still write the redundant data. Rejected.
- Delete the obsolete properties entirely. They're still referenced by `MainWindow.ShowVaultAudit()` and elsewhere as a convenience accessor. Out of scope for a bug fix — flagged for Sprint R.

### B-LABELSIZE — Restore `DataVersion` and `TabOrder` in `GetSaveData()`

Surfaced during the audit even though it was not the user-visible bug. Fresh `SavedNote` objects were missing both properties on every save, so the migrator ran on every load forever. Tiny fix, big confusion savings later.

### B-MULTIWIN — `IsOpen` flag, not repurposing `IsActive`

**Decision:** Introduce a separate `IsOpen` field on `NoteWindowData`. `IsActive` keeps its meaning ("exists in user's data") and is set false only via the explicit ✕ in `NoteListWindow`. `IsOpen` tracks "was this window visible at last shutdown."

**Alternatives considered:**

- Reuse `IsActive` for both. Would break `NoteListWindow.CloseWindow` semantics — closing a window via that button would no longer fully delete its data.
- Track a per-instance "was-open" list outside `NoteWindowData`. Adds a parallel structure for no benefit; persisting on the data object is one disk write.

### B-MULTIWIN — Sticky Notes "preserve last open set" vs "always reopen all active"

The user explicitly picked Windows Sticky Notes behavior: reopen only the windows that were visible at last close. Closing a window mid-session means it stays closed across restarts until the user manually reopens it from `NoteListWindow`. The audit phase asked the user to choose between this and "reopen all active." User picked Sticky Notes.

### B-MULTIWIN — Remove `StartupUri` entirely

**Decision:** Set `ShutdownMode="OnLastWindowClose"` explicitly, drop `StartupUri`, and let `App.OnStartup` create all windows programmatically.

**Alternatives considered:**

- Keep `StartupUri` but spawn extra windows from inside the first `MainWindow` constructor. Brittle — `MainWindow` would have to know about `App` lifecycle. Rejected.
- Spawn extras inside `MainWindow.Loaded`. Same problem plus a brief flash of the first window before the others appear. Rejected.

## Implementation Notes

### Files touched

| File | Change |
|---|---|
| `Core/Models/SavedNote.cs` | `using System.Text.Json.Serialization;` + `[JsonIgnore]` on the two `[Obsolete]` properties. |
| `UI/TabManager.cs` | `GetSaveData()` now sets `DataVersion = MigrationService.CurrentNoteSchemaVersion` and `TabOrder = index`. |
| `Core/Managers/NoteWindowManager.cs` | Added `public bool IsOpen { get; set; } = true;` to `NoteWindowData`. |
| `App.xaml` | Removed `StartupUri`. Added `ShutdownMode="OnLastWindowClose"` (explicit, default behavior). |
| `App.xaml.cs` | Added `using System.Linq;` and `using SnipShottyBoard.UI.Views;`. Added `RestoreOpenWindows()` method called after `base.OnStartup(e)`. |
| `UI/Views/MainWindow.xaml.cs` | Set `WindowData.IsOpen = true` immediately after assigning `WindowData`. In `MainWindow_Closing`, count other open `MainWindow` instances and flip `IsOpen = false` only when this is not the last one. |

### Restore algorithm (in `App.OnStartup → RestoreOpenWindows`)

```
let allActive = NoteWindowManager.Instance.GetActiveWindows()
let openSet  = allActive where IsOpen == true

if openSet empty AND allActive not empty:
    openSet = [allActive[0]]                  // legacy data fallback
    log("falling back to first active window")

if openSet still empty:
    new MainWindow().Show()                   // fresh-install path
    return

for each w in openSet:
    new MainWindow(w).Show()                  // restore each saved window
```

### IsOpen state machine

```
construct MainWindow:           IsOpen ← true
close 1 of N MainWindow (N>1):  IsOpen ← false   (other windows still up)
close the last MainWindow:      IsOpen ← unchanged  (preserved for next launch)
close via NoteListWindow ✕:     IsActive ← false   (window data dismissed entirely)
```

### Defaults chosen for backward compatibility

- `IsOpen` defaults to `true` in code. Existing JSON without the field deserializes to `true`. This means the first launch after the upgrade will restore the user's one existing window — no regression on single-window users.

## Testing & Acceptance

User-driven (no automated tests in repo). Acceptance checklist:

1. Open app fresh. Set a label on an image via right-click → Rename. Change a thumbnail size (Small/Medium/Big). Toggle Hide on one image. Close app. Reopen.
   - Pass: label survives, size survives, hidden state survives.
2. Open a second window from NoteListWindow → "New Window." Position both. Close the entire app (close both windows or task manager via taskbar).
   - Pass: relaunching reopens both windows at their saved positions.
3. With two windows open, close one via its ✕. Close the other.
   - Pass: relaunch shows only the one that was still open at close.
4. Close the only existing window. Relaunch.
   - Pass: that window reopens (last-window semantics).
5. Build succeeds with zero new errors. (`dotnet build` → `Build succeeded. 0 Error(s)` ✅ verified at all three steps.)

## Performance & Limits

- Restore loop is O(windows) at startup. Realistic worst case is dozens, not thousands. Disk read is one master.json load (already on the hot path).
- `[JsonIgnore]` shrinks `master.json` by ~30–40% on notes with many images (no more redundant `imageFiles` + `imageTimestamps` blocks). Net write performance gain. No load-time penalty.

## Follow-ups

- **Sprint R candidate:** delete the `[Obsolete]` properties on `SavedNote` once all callers (`MainWindow.ShowVaultAudit`, etc.) move to `Media` directly. The `[JsonIgnore]` makes them session-only convenience accessors.
- **Sprint R candidate:** `MediaSection.ImageFiles` setter still creates fresh `MediaReference` objects with default v3 fields. Currently unused by load paths, but a future caller could hit the same bug. Consider deprecating that property too.
- **Watch:** if the user reports that closing a window via the ✕ in `NoteListWindow` also marks it `IsOpen=false`, audit the interplay between `NoteWindowManager.CloseNoteWindow` (sets `IsActive=false`) and the new `IsOpen` flag. Should be fine because `CloseNoteWindow` closes the actual `MainWindow` via `window.Close()` which fires `MainWindow_Closing` first.

## Graduated to PROJECT_MEMORY.md

None. These are bug fixes, not new patterns. The `[JsonIgnore]` discipline could become a rule ("Obsolete serialization-visible properties MUST be marked `[JsonIgnore]`") — left for a future Sprint R sweep when more examples accumulate.
