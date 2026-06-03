---
Title: Code Quality Sprint C — Performance, Simplifications, FluentWindow, Warnings, Security
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
  - Planning: docs/PLANNING.md
  - Issues: §13, §16, §17, §20, §25, §27
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

7 targeted fixes across performance caching, simplification, FluentWindow consistency,
a nullable compiler warning, and a missing security guard. No behavior changes except
FIX-7 which adds a user-visible dialog when MaxTabs is reached.

## Fixes

### FIX-1: StatusBarManager._errorBrush cached as static frozen field
**File:** `UI/StatusBarManager.cs`
**Before:** `ShowSaveError()` created `new SolidColorBrush(Color.FromRgb(244, 67, 54))` on every call.
**After:** `_errorBrush` declared as `private static readonly SolidColorBrush`, initialized and
frozen in the static constructor alongside `_savedBrush` and `_unsavedBrush`.

### FIX-2: ImageViewerWindow GIF status timer reused via _gifStatusTimer field
**File:** `UI/ImageViewerWindow.xaml.cs`
**Before:** `DisplayImage_MouseLeftButtonUp()` created a new `DispatcherTimer` on every GIF click,
capturing a stale `prevZoomText` string.
**After:** `_gifStatusTimer` field declared on the class; timer created once on first use, stopped
and restarted on subsequent clicks. `UpdateStatusZoom()` called on tick — reads current state,
no stale capture needed.

### FIX-3: NoteWindowManager.SaveNoteWindows no longer reads master.json before writing
**File:** `Core/Managers/NoteWindowManager.cs`
**Before:** Every `SaveNoteWindows()` call (every 5s autosave) called `DataManager.LoadMasterData()`
to preserve the Settings section, then wrote back.
**After:** `_cachedSettings` field caches Settings from `LoadNoteWindows()` at startup. `SaveNoteWindows()`
builds `MasterData` directly from `NoteWindows` + `_cachedSettings` — one fewer disk read per cycle.
`Debug.WriteLine` calls replaced with `LoggingService.LogDebugStatic` and `LoggingService.LogErrorStatic`.

### FIX-4: MediaReference.FullPath caches images folder in static field
**File:** `Core/Models/MediaReference.cs`
**Before:** Every `FullPath` access called `Environment.GetFolderPath()` (P/Invoke) + 2 `Path.Combine()`.
**After:** `_imagesFolder` declared as `private static readonly string`, computed once at class load.
`FullPath` is now an expression-bodied property: `Path.Combine(_imagesFolder, Filename)`.
Path matches `DataManager.GetImagesFolder()`.

### FIX-5: WindowCornerPreference="Round" added to three FluentWindow XAML files
**Files:** `UI/ImageViewerWindow.xaml`, `UI/Views/SettingsWindow.xaml`, `UI/NoteListWindow.xaml`
**Before:** These three windows were missing `WindowCornerPreference="Round"` — the attribute
present on `MainWindow.xaml`.
**After:** Added directly on the `<ui:FluentWindow ...>` element in each file, matching MainWindow syntax.

### FIX-6: CS8620 fixed in MediaSection — .Select(path => path!) added after null filter
**File:** `UI/MediaSection.xaml.cs` — line ~1766
**Before:** `.Where(path => !string.IsNullOrEmpty(path)).ToList()` returned `List<string?>`, triggering CS8620.
**After:** `.Select(path => path!)` appended after the Where guard — safe because Where already
eliminates nulls and empty strings.

### FIX-7: TabManager.CreateNewTab() now enforces appSettings.MaxTabs
**File:** `UI/TabManager.cs`
**Before:** `MaxTabs` setting stored but never checked — unlimited tab creation possible.
**After:** Guard added at top of `CreateNewTab()`: if `tabs.Count >= appSettings.MaxTabs`, shows
`CustomDialog.ShowInformation(...)` with "Tab Limit Reached" and returns early.

## Build Status

0 errors. 276 warnings (all pre-existing — tracked for Sprint R).
No new warnings introduced by Sprint C.

## Testing & Acceptance

- App launches, tabs create and switch normally
- Tab limit: with MaxTabs=20, creating a 21st tab shows the dialog and returns
- GIF open/click toggle: status shows ⏸️/▶️ then reverts after 800ms, no new timer per click
- Image viewer, settings, note list windows: rounded corners apply on Win11
- Save error path: verify ⚠️ appears in correct red, auto-reverts after 5s

## Follow-ups

- Sprint R: clear 276 pre-existing nullable warnings across TabManager, MediaSection, MainWindow
- Sprint D (devnote template): update PLANNING.md sprint table entry
