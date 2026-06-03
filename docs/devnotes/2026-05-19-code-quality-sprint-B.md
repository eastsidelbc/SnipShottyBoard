---
Title: Code Quality Sprint B — Init Order + Window Identity
Date: 2026-05-19
Owner: Jeremy
Versions Affected: current (unreleased)
Links:
  - Planning: docs/PLANNING.md
  - Sprint doc: docs/code-quality-sprint-B.md
  - Audit findings: docs/AUDIT_REPORT.md §22, §23
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

Sprint B addresses two structural issues from the deep code audit.
Sprint A must be complete and building clean first (this is Part 2 of 2).

- §22 Initialization Order: DataManager's static constructor did file I/O,
  causing any failure to surface as TypeInitializationException — a .NET wrapper
  that buries the real exception and repeats on every subsequent call.
- §23 Multi-Window Correctness: Window identity tracked via WPF Tag (untyped object).
  The primary MainWindow was never assigned a Tag, so NoteListWindow's duplicate
  detection always missed it — clicking it opened a second window instead of focusing.

## Decisions & Alternatives

FIX 1 — Considered leaving the static constructor and adding a try/catch around it.
Rejected: TypeInitializationException still wraps the inner exception even with a
catch in the static ctor. Only moving I/O out of the static ctor entirely fixes this.

FIX 2 — Considered assigning Tag to the primary window at startup.
Rejected: Tag is untyped (object), requires string round-tripping, and the primary
window is created before NoteListWindow exists so there's no clean place to set it.
A typed property on MainWindow (Guid WindowId) is the correct pattern.

## Implementation Notes

### FIX 1 — DataManager.cs + App.xaml.cs

`DataManager` static constructor now contains only a comment — no method calls.
All file I/O moved to new `public static Initialize()` method.

`App.OnStartup` calls `DataManager.Initialize()` as the VERY FIRST thing, before
LoggingService construction (logging isn't available yet — errors go to MessageBox).
Failure shows an actionable MessageBox ("Check write access to %AppData%\SnipShottyBoard")
then calls `this.Shutdown(1)` and returns. App cannot run without the data folder.

`EnsureDirectoryExists()` and `MigrateToMasterIfNeeded()` are unchanged — only
the call site moved. Static path field initializers (MasterFilePath, ImagesFolder, etc.)
remain in the static initializer; they compute strings only, no I/O.

### FIX 2 — MainWindow.xaml.cs + NoteListWindow.xaml.cs

Added to MainWindow:
```
public Guid WindowId => WindowData?.Id ?? Guid.Empty;
```

WindowData is always set in the constructor before anything reads WindowId.
Returns Guid.Empty as a safe fallback (never matches any real window ID).

NoteListWindow changes — three locations updated:
- `OpenNoteWindow()` duplicate detection: `mainWindow.WindowId == windowId`
- `OpenNoteWindow()` window creation: removed `noteWindow.Tag = windowId.ToString()`
- `RenameNoteWindow()` title update: `mainWindow.WindowId == windowId`
- `CloseNoteWindow()` window close: `mainWindow.WindowId == windowId`

NOTE: `PinButton.Tag = "Pinned"` in MainWindow is unrelated — that's a UI styling
trigger on a Button control, not window identity. Left untouched.

NOTE: `renameButton.Tag = windowData.Id` and `closeButton.Tag = windowData.Id` in
NoteListWindow are also unrelated — those pass the Guid to Click handlers via Button.Tag
on UI controls, not on MainWindow. Left untouched.

## Testing & Acceptance

Build gate: 0 errors, 0 warnings (file-lock error on rebuild with app running is expected/unrelated).

FIX 1 test: Launch app normally — no change in behavior (happy path unchanged).
FIX 1 edge test: Cannot easily test permissions failure locally; verify by code review
that the catch block would show MessageBox and call Shutdown(1).

FIX 2 test:
1. Launch app (primary MainWindow opens)
2. Click NoteListWindow button
3. Click the primary window entry in the list
4. Expected: existing window focuses — NOT a second window opening
5. Try rename from the list — primary window title updates
6. Try close from the list — primary window closes

## Performance & Limits

No performance impact. Both changes are initialization-path only (once per startup)
or property reads (Guid comparison, no allocation).

## Follow-ups

- None required from this sprint.
- Sprint C (if planned) would address remaining audit findings.
