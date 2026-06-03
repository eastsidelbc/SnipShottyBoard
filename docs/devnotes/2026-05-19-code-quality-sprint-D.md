---
Title: Code Quality Sprint D — Dead Code, Hygiene, Dependencies, Style
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
  - Planning: docs/PLANNING.md
  - Sprint Doc: docs/code-quality-sprint-D.md
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

Apply Code Quality Sprint D fixes covering dead code, codebase hygiene,
dependencies, and code style. Issues §5, §10, §23, §29 from audit report.

## Fixes Applied

### FIX 1 — Examples/ folder exclusion
SKIPPED. `Examples/` directory does not exist at repo root.

### FIX 2 — Remove duplicate XML doc comments in DataManager.cs
Four methods had two stacked `<summary>` blocks each (shorter first, detailed second).
Removed the shorter/first block in each case. Methods affected:
- `SaveNotes()`
- `SaveNoteWindows()`
- `SaveSettings()`
- `CleanupOrphanedImages()`

### FIX 3 — Version mismatch in csproj
SKIPPED. `AssemblyVersion` and `FileVersion` were already `1.7.0.0`.
`VERSION` file already says `1.7.0`. Nothing to do.

### FIX 4 — Remove CommunityToolkit.Mvvm
Zero usages of any Toolkit-specific feature found across all `.cs` files:
- No `CommunityToolkit` using directives
- No `ObservableObject`, `RelayCommand`, `ObservableProperty`, or `ObservableValidator`
- `ObservableCollection<T>` is BCL (`System.Collections.ObjectModel`) — no package needed

Removed `<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />` from csproj.
Build: 0 errors, 0 warnings.

### FIX 5 — Standardize private field naming to _camelCase
Used PowerShell word-boundary regex replacements (`\bfieldName\b`) for safety.

**TabManager.cs** — 15 fields renamed:
`tabs`, `selectedTab`, `tabHeaderPanel`, `tabContentArea`, `isDragging`,
`dragStartPoint`, `draggedTab`, `dragCanvas`, `dragVisual`, `dropIndicator`,
`draggedTabOriginalIndex`, `dropTargetIndex`, `lastDropTargetIndex`,
`appSettings`, `skipDeleteConfirmation`

**MainWindow.xaml.cs** — 11 fields renamed:
`tabManager`, `themeManager`, `statusBarManager`, `keyboardHandler`,
`helpManager`, `settingsManager`, `currentSettings`, `autoSaveTimer`,
`statusTimer`, `recoveryTimer`, `hasUnsavedChanges`

Note: `loggingService` was NOT in the sprint spec and was left as-is.
`_positionTracker` was already correctly named.
Build after each file: 0 errors.

### FIX 6 — ResourceKeys.cs with compile-time resource key constants
Created `UI/ResourceKeys.cs` with constants for 15 DarkTheme.xaml keys
used in code-behind files across the codebase.

Updated `TabManager.cs` to replace 8 raw string literals with `ResourceKeys.X`:
- `"AccentBrush"` → `ResourceKeys.AccentBrush`
- `"AccentGlowEffect"` → `ResourceKeys.AccentGlowEffect`
- `"DragGhostBrush"` → `ResourceKeys.DragGhostBrush`
- `"DragGhostBorderBrush"` → `ResourceKeys.DragGhostBorderBrush`
- `"TabButtonStyle"` → `ResourceKeys.TabButtonStyle`
- `"NativeContextMenuStyle"` → `ResourceKeys.NativeContextMenuStyle`
- `"TabBackgroundBrush"` → `ResourceKeys.TabBackgroundBrush`
- `"AppForegroundBrush"` → `ResourceKeys.AppForegroundBrush`

Other files (`MediaSection.xaml.cs`, `NoteTab.xaml.cs`, etc.) left for
incremental updates — too risky to do all in one pass per sprint rules.

## Build Status

Final: 0 errors, 0 warnings

## Testing & Acceptance

- `dotnet build` passes with 0 errors after every individual fix
- Pre-existing warnings (276) unchanged — none introduced by this sprint

## Performance & Limits

No runtime changes. All fixes are compile-time / source-level only.

## Follow-ups

- FIX 6 continued: update `MediaSection.xaml.cs`, `NoteTab.xaml.cs`,
  `SettingsWindow.xaml.cs`, `ImageViewerWindow.xaml.cs` to use ResourceKeys
- `loggingService` field in MainWindow.xaml.cs could be renamed to
  `_loggingService` in a follow-up hygiene pass
