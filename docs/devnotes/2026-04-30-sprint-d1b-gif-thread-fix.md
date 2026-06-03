---
Title: Sprint D Phase 1b — Fix GIF Thread Ownership Bug + currentImage
Date: 2026-04-30
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT D
---

## Context & Goal

Fix three bugs in `ImageViewerWindow.xaml.cs` that prevent GIFs from animating:
1. Thread ownership crash — BitmapImage created on background thread
2. Missing `currentImage` assignment — status bar shows "No image loaded"
3. Freezable context crash — null-clear on first load

## Root Cause

`LoadGifAsync` created the `BitmapImage` inside `Task.Run()` on a background thread. Even though `Dispatcher.Invoke()` brought execution back to the UI thread, the `BitmapImage` object retained permanent ownership by the background thread. When `WpfAnimatedGif`'s `SetAnimatedSource` internally reads `bitmap.UriSource`, WPF throws `InvalidOperationException: The calling thread cannot access this object because a different thread owns it`.

Additionally, `currentImage` was never set in the GIF path, so `UpdateImageInfo()` hit the "No image loaded" else branch.

## Implementation Notes

### Fix Applied — Rewrote LoadGifAsync

**Before:** `async void LoadGifAsync(string imagePath)` — `Task.Run` → `Dispatcher.Invoke`
**After:** `void LoadGifAsync(string imagePath)` — everything runs directly on UI thread

Key changes:
1. **Removed `Task.Run` + `Dispatcher.Invoke`** — BitmapImage is created directly on UI thread
2. **Method signature changed** from `async void` to `void` — no longer async
3. **Added `currentImage = bitmap;`** — after `SetAnimatedSource` succeeds
4. **Defensive null-clear** — `try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { }` — safe on first load
5. **Kept RenderOptions settings** — help with GIF rendering quality

### Why OnDemand doesn't need background thread

GIFs use `BitmapCacheOption.OnDemand` — frames decode lazily as they animate, so there's no blocking decode step. Creating on the UI thread is safe and eliminates the thread ownership issue entirely.

### What Was NOT Changed

- `LoadStaticAsync` — static images still use `Task.Run` + `Freeze()` (correct pattern for PNG/JPG/WebP)
- `ApplyStaticImage` — untouched
- LRU cache — untouched
- Keyboard shortcuts — untouched
- Navigation — untouched

## Testing & Acceptance

- [x] `dotnet build` passes with 0 errors, 0 new warnings
- [ ] Open a GIF → it animates smoothly at correct speed, loops properly
- [ ] Status bar shows: filename, pixel dimensions, file size, modification date (not "No image loaded")
- [ ] Window auto-sizes to fit the GIF at original resolution
- [ ] Ctrl+C copies GIF to clipboard successfully
- [ ] Left/right navigation: GIF → PNG → GIF cycles without crash
- [ ] Log file shows Thread=1 (UI thread) for all GIF load steps
- [ ] No exceptions in log file
- [ ] Static PNG/WebP still works identically (no regression)

## Performance & Limits

- GIF creation now runs on UI thread — decode is lazy (OnDemand), so no blocking
- No change to static image path — still uses background thread + Freeze()

## Follow-ups

- Test GIF animation manually
- Continue to D.2 (Navigation Memory Cleanup)

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
