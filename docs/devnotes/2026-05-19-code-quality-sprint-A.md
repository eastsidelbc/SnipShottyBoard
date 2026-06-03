---
Title: Code Quality Sprint A — Colors, Logging, MouseLeave, GIF Async
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
  - Planning: docs/PLANNING.md
  - Sprint doc: docs/code-quality-sprint-A.md
  - Audit findings: §6 Architectural Violations, §8 XAML Issues, §20 Async/Await
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

5 code-quality fixes from audit report Sprint A plan.
No behavior changes — structural hygiene only.

## Fixes Applied

### FIX-1: Inline hex/RGB colors → theme resource references

New keys added to `Resources/Themes/DarkTheme.xaml`:
- `DragGhostBrush` = `#8C808080` (was `Color.FromArgb(140, 128, 128, 128)`)
- `DragGhostBorderBrush` = `#78606060` (was `Color.FromArgb(120, 96, 96, 96)`)
- `SettingsActiveTabBrush` = `#33FFFFFF` (was `Color.FromArgb(51, 255, 255, 255)`)

`UI/TabManager.cs` — 3 locations:
1. `InitializeDragCanvas()` drop indicator: `Color.FromRgb(74,144,226)` → `FindResource("AccentBrush")`
2. `CreateDragVisual()` ghost colors: two `Color.FromArgb` → `DragGhostBrush` / `DragGhostBorderBrush`
3. `UpdateTabSelection()` fallback block: entire theme-detection branch replaced with single `FindResource("TabBackgroundBrush")` call (dark mode only — branch was dead)

`UI/Views/SettingsWindow.xaml.cs` — 1 location:
- `SetActiveTab()`: `Color.FromArgb(51,255,255,255)` → `FindResource("SettingsActiveTabBrush")`

### FIX-2: Debug.WriteLine → LoggingService / #if DEBUG

`UI/NoteListWindow.xaml.cs` — 2 catch blocks replaced:
- `OpenNoteWindow()` catch → `LoggingService.LogErrorStatic("Failed to open note window", ex, "UI")`
- `CloseNoteWindow()` catch → `LoggingService.LogErrorStatic("Failed to close note window", ex, "UI")`

`UI/MediaSection.xaml.cs` — all Debug.WriteLine calls addressed:
- Catch blocks (Dispose, reorder, ShowFullSizeImage, RemoveImageByPath, drop handlers) → `LoggingService.LogErrorStatic`
- Drag operation traces (insertion indicator, target calc, drag move, reorder progress) → `#if DEBUG` gates
- ShowFullSizeImage verbose viewer-open traces → `#if DEBUG` gates

### FIX-3: MediaBorder MouseLeave subscription guard

`UI/Views/NoteTab.xaml.cs`:
- Added `private bool _mediaBorderLeaveSubscribed = false;` field
- `MediaBorder_PreviewMouseDown()` now checks flag before subscribing
- `MediaBorder_MouseLeave()` resets flag on unsubscribe
- Prevents N subscriptions accumulating on rapid clicks before mouse leaves

### FIX-4: SettingsWindow button styles StaticResource → DynamicResource

`UI/Views/SettingsWindow.xaml`:
- All 4 tab nav buttons: `{StaticResource TabButtonStyle}` → `{DynamicResource TabButtonStyle}`
- 3 footer buttons (Reset, Cancel, Apply): `StaticResource` → `DynamicResource` for Secondary/PrimaryButtonStyle
- Prevents `XamlParseException` at parse time if key is absent from merged dictionaries

### FIX-5: LoadGifAsync → async LoadGif (file I/O off UI thread)

`UI/ImageViewerWindow.xaml.cs`:
- `private void LoadGifAsync(string imagePath)` renamed to `private async void LoadGif(string imagePath)`
- File read moved to `Task.Run(() => File.ReadAllBytes(imagePath))` — UI thread unblocked during load
- Guard added: if `currentImagePath != imagePath` after await, load is discarded (user navigated away)
- `BitmapImage` initialized from `MemoryStream` (not `UriSource`) — same animation behavior
- `MemoryStream` intentionally not disposed: WpfAnimatedGif reads frames from it during playback
- `UpdateImageInfo()` added to call chain (was present in LoadStaticAsync but missing from LoadGifAsync)
- Call site in `LoadImage()` updated: `LoadGifAsync(imagePath)` → `LoadGif(imagePath)`

## Build Status

0 errors, 278 warnings (all pre-existing — none introduced this sprint)

## Testing

- App launches: confirm tabs render, drag ghost visible
- Settings window opens: confirm active tab highlight, button styles render
- Click MediaBorder rapidly: confirm no visual glitch on mouse leave
- Open a GIF in ImageViewerWindow: confirm animation plays, no UI freeze
- Drop an image file: confirm it adds and errors log properly

## Next

Code Quality Sprint B: `docs/code-quality-sprint-B.md`
