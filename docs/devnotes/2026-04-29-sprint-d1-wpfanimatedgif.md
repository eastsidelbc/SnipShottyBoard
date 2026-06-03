---
Title: Sprint D Phase 1 — WpfAnimatedGif Integration
Date: 2026-04-29
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT D
 - PR/SHAs: 43f854c (parent commit)
---

## Context & Goal

GIFs don't animate in ImageViewerWindow — they crash with `System.ArgumentException: "DependencyObject is not a context for this Freezable"`. Root cause: `BitmapImage` with `CacheOption.OnDemand` created on a background thread crosses into the UI thread via `Dispatcher.Invoke`, carrying broken inheritance context metadata. WPF's `Freezable.RemoveContextInformation()` fails because the BitmapImage doesn't recognize the target `Image` control as a valid context.

## Decisions & Alternatives

**Decision:** Use WpfAnimatedGif v2.0.2 instead of fixing the native approach.

**Why:** Even if we moved BitmapImage creation to the UI thread, WPF has documented bugs with GIF frame timing (plays too fast) and repeat behavior (sometimes plays once instead of looping). WpfAnimatedGif provides correct per-frame duration parsing, respects GIF repeat metadata, and manages decoder lifecycle properly.

**Alternative considered:** Native fix (create BitmapImage on UI thread). Rejected because it doesn't solve the frame timing bugs.

## Implementation Notes

### Changes Made

1. **SnipShottyBoard.csproj** — Added `<PackageReference Include="WpfAnimatedGif" Version="2.0.2" />`

2. **UI/ImageViewerWindow.xaml** — Added `xmlns:gif="http://wpfanimatedgif.codeplex.com"` namespace declaration

3. **UI/ImageViewerWindow.xaml.cs**:
   - Added `using WpfAnimatedGif;`
   - Rewrote `LoadGifAsync` to use `ImageBehavior.SetAnimatedSource()` instead of `DisplayImage.Source =`
   - Removed try/catch fallback to `LoadStaticAsync` — no longer needed
   - Renamed `ApplyImage` → `ApplyStaticImage` to isolate GIF and static code paths
   - GIF path now calls `UpdateImageInfo()` and `AutoSizeWindow()` directly, not through `ApplyStaticImage`
   - GIFs update `currentImagePath` but do NOT set `currentImage` (GIFs use AnimatedSource, not Source property)

### Critical Memory Leak Fix

WpfAnimatedGif issues #75/#83: The AnimationCache holds references to decoders. Must call `ImageBehavior.SetAnimatedSource(DisplayImage, null)` BEFORE setting a new GIF to clear the previous decoder. This is implemented in `LoadGifAsync`.

## Testing & Acceptance

- [ ] Open a single GIF from vault → it animates smoothly at correct speed and loops properly
- [ ] Status bar shows: filename, pixel dimensions, file size, modification date
- [ ] Window auto-sizes to fit the GIF at original resolution (within screen limits)
- [ ] No "Freezable" exception in log file
- [ ] Static PNG/WebP still works exactly as before (cached, fast load, status bar info present)
- [ ] `dotnet build` succeeds with 0 errors, 0 new warnings ✅

## Performance & Limits

- GIFs skip the LRU cache (BitmapImages can't be frozen)
- WpfAnimatedGif manages its own internal cache — must be cleared on navigation (Phase D.2)
- Package size: ~20KB, no transitive dependencies

## Follow-ups

- Phase D.2: Add `ClearPreviousImage()` method for navigation memory cleanup
- Phase D.2: Update `ReleaseImageResources()` to clear AnimatedSource on window close
- Manual memory verification: Task Manager check during 5+ navigation cycles

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
