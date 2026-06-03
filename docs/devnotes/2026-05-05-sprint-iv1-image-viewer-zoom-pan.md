---
Title: Image Viewer — Zoom, Pan, GIF Controls, and Bug Fixes
Date: 2026-05-05
Owner: Jeremy
Versions Affected: [Unreleased]
Links:
  - Planning: docs/PLANNING.md
  - PR/SHAs: pending commit
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

---

## Context & Goal

The ImageViewerWindow already had a toolbar and basic display. The goal was to add
a full photo-viewer experience: mouse-wheel zoom, click-drag pan, GIF pause toggle,
double-click to 1:1, and correct keyboard navigation. Three compile errors existed
in the handed-off code that blocked the build entirely.

---

## Compile Errors Fixed First

### 1. `MouseDoubleClick` on `<Image>` (XAML MC3072)
`MouseDoubleClick` is defined on `Control`. `Image` inherits from `FrameworkElement`,
not `Control`, so the XAML attribute does not exist.
**Fix:** Removed from XAML. Double-click detected in `MouseLeftButtonDown` via `e.ClickCount > 1`.

### 2. `ImageScrollViewer.ScrollToCenter(DisplayImage)` (CS compile error)
`ScrollViewer` has no `ScrollToCenter` method.
**Fix:** `ScrollToHorizontalOffset(ScrollableWidth / 2)` + `ScrollToVerticalOffset(ScrollableHeight / 2)`
deferred at `DispatcherPriority.Render` so layout has updated before centering.

### 3. `ImageBehavior.SetIsPaused` (CS0117)
This method does not exist in WpfAnimatedGif 2.0.2.
**Fix:** `ImageBehavior.GetAnimationController(DisplayImage)?.Pause()` / `?.Play()`
— the correct API per the library's own wiki.

---

## Zoom Implementation

Mechanism: physical `Width`/`Height` on the `<Image>` element, not `RenderTransform`.
Why: changing the element's physical size causes the parent `ScrollViewer` to
recalculate its scroll extents automatically. No matrix math required for panning.

Range: `MIN_ZOOM = 0.25` to `MAX_ZOOM = 5.0` (AppConstants candidates if needed).

### Mouse Wheel — Why `PreviewMouseWheel`
`ScrollViewer.OnMouseWheel` is a WPF *class handler*. Class handlers fire before
instance handlers in the bubbling phase. The ScrollViewer marks the event `Handled`
and swallows it — our XAML-attached `MouseWheel` handler never fires.

`PreviewMouseWheel` is a tunneling event that travels from the Window *down* to the
focused element. Our handler on the ScrollViewer fires during this tunneling phase,
before `OnMouseWheel` ever runs. We set `e.Handled = true` to prevent the scroll.

---

## Pan Implementation

Mouse capture (`CaptureMouse()`) on the `DisplayImage` on `MouseLeftButtonDown`
(single click only). `_mouseDownPos` and `_panStartScrollH/V` are recorded relative
to the `ScrollViewer` viewport (stable reference frame — does not move when content
scrolls). `MouseMove` computes the delta and calls `ScrollToHorizontalOffset` /
`ScrollToVerticalOffset`.

A 3 px threshold sets `_isMouseDragging = true` to distinguish a drag from a click.
`LostMouseCapture` resets `_isMouseDragging` in case the OS steals capture
(Alt-Tab, system dialog). Without this the next pan attempt would have corrupted
delta math.

---

## Default Zoom: 1:1 (changed from Fit)

Old behaviour: `AutoSizeWindow` sized the window, then `FitToWindow()` shrank the
image to fill the viewport. Due to chrome/padding the result was ~92–96%, showing
"Fit (94%)" in the status bar. Confusing.

New behaviour: `ApplyOneToOne()` sets `currentZoomLevel = 1.0` deferred at
`DispatcherPriority.Loaded`. Window is already sized to the image by `AutoSizeWindow`,
so 1:1 zoom shows the full image with no scaling artifact.

### `_isInFitMode` Flag
Tracks whether the image is in fit-to-window mode. Set `true` by `FitToWindow()`,
set `false` by wheel zoom, double-click, and 1:1 button.
`SizeChanged` re-runs `FitToWindow()` only when `_isInFitMode == true` —
so manual zoom survives a window resize.

---

## 1:1 Button — Window Resize Included

`FitActualButton_Click` calls `AutoSizeWindow(currentImage)` before setting zoom.
This replicates the exact window sizing used when first opening from a thumbnail.
`ApplyCurrentZoom()` is deferred at `DispatcherPriority.Loaded` so the viewport
has finished updating its dimensions before the centering scroll fires.

---

## GIF Pause Toggle

`MouseLeftButtonUp` checks:
- `e.ClickCount > 1` → skip (double-click was handled in Down)
- `wasDragging` → skip (drag, not click)
- `dist < 5.0 px` → single click → toggle via `ImageBehavior.GetAnimationController`

Status bar flashes "⏸️ GIF Paused" / "▶️ Playing" for 800 ms via `DispatcherTimer`,
then restores the zoom text.

---

## Keyboard Navigation Fix

**Root cause:** `ScrollViewer` is focusable by default. Clicking the image area moved
keyboard focus to it. The ScrollViewer handled `Left`/`Right` for scrolling,
marking the event `Handled` before it could bubble to the Window's `KeyDown` handler.
Arrow-key navigation silently broke after any click on the viewer.

**Fix:**
- `Focusable="False"` on the ScrollViewer in XAML — clicks on the image area can
  no longer steal keyboard focus.
- `this.Activated += (s, e) => this.Focus()` in `SetupWindow()` — every time the
  window becomes the active window, keyboard focus returns to it. Works after
  Alt-Tab, clicking another app, and clicking back.

---

## Files Changed

- `UI/ImageViewerWindow.xaml` — removed `MouseDoubleClick`, changed `MouseWheel` →
  `PreviewMouseWheel`, added `MouseMove`, `LostMouseCapture`, `Focusable="False"` on ScrollViewer
- `UI/ImageViewerWindow.xaml.cs` — all zoom/pan/GIF logic, `ApplyOneToOne()`,
  `_isInFitMode` flag, `LostMouseCapture` handler, `Activated` focus reset

---

## Testing & Acceptance

- [x] Build: 0 errors
- [x] Mouse wheel zooms (not scrolls)
- [x] Drag pans when zoomed in
- [x] Double-click snaps to 1:1
- [x] 1:1 button snaps to 1:1 AND resizes window
- [x] Images open at 100% (1:1) not Fit
- [x] Window resize re-fits only in fit mode
- [x] GIF single-click pauses/resumes
- [x] Arrow keys navigate after click-away and back
- [x] Alt-Tab away and back — arrow keys still work

---

## Follow-ups

- Prev/Next navigation buttons are wired but permanently `Visibility="Collapsed"` —
  no code currently shows them when `allImagePaths.Count > 1`. Should add
  `UpdateNavigationButtons()` in a future pass.
- `FitToWindow()` still exists but has no UI entry point now that default is 1:1.
  Could be wired to a future "Fit" button or `F` key shortcut.
