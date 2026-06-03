---
Title: Sprint D Phase 2 — Navigation Memory Cleanup & Verification
Date: 2026-04-30
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT D
 - Dev Note: docs/devnotes/2026-04-30-sprint-d1b-gif-thread-fix.md
---

## Context & Goal

Fix unbounded memory growth when navigating between images (left/right arrows). The WpfAnimatedGif library's `AnimationCache` holds references to decoders. Cycling through multiple GIFs without clearing previous ones causes RAM to grow unboundedly.

## Decisions & Alternatives

- **Centralized cleanup in `ClearPreviousImage()`** — called at the start of `LoadImage()`, so all navigation paths (left, right, direct open) get cleanup automatically. No duplication across code paths.
- **Also cleanup on window close** — `ReleaseImageResources()` now clears GIF decoders too, not just static sources.

## Implementation Notes

### Added `ClearPreviousImage()` method

Called at the very start of `LoadImage()`, before any new image is loaded:

1. Guard: `if (string.IsNullOrEmpty(currentImagePath)) return;`
2. Clear GIF decoder: `ImageBehavior.SetAnimatedSource(DisplayImage, null)` in defensive try/catch
3. Clear static source: `DisplayImage.Source = null`
4. Release reference: `currentImage = null`
5. Purge cache: `ImageCacheManager.Instance.RemoveAllForPath(currentImagePath)`

### Updated `ReleaseImageResources()`

Added GIF decoder cleanup before the existing `DisplayImage.Source = null`:
```csharp
try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { /* no GIF loaded — safe */ }
```

### What Was NOT Changed

- Navigation methods (`NavigateToPreviousImage`, `NavigateToNextImage`) — they call `LoadImage()`, which calls `ClearPreviousImage()`. No changes needed.
- `LoadStaticAsync` — cache hit/miss logic untouched.
- `LoadGifAsync` — GIF loading logic untouched (D.1b fix remains).

## Testing & Acceptance

- [x] `dotnet build` passes with 0 errors, 0 new warnings
- [x] Navigate left/right through mixed list (GIF → PNG → GIF → WebP → GIF) without crashes
- [x] Each image displays correctly with proper status bar info
- [x] Memory stays bounded during 5+ navigation cycles (Task Manager check)
- [x] Close viewer, reopen same GIF — animates without issues
- [x] No errors in log file

## Performance & Limits

- `ClearPreviousImage()` runs synchronously on UI thread — fast (just null assignments + cache lookup)
- Cache purge uses `RemoveAllForPath()` which handles both `:thumbnail` and `:full` variants

## Follow-ups

- Manual memory test: cycle through 10+ images including multiple GIFs, verify RAM stays bounded
- Sprint D is now complete

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
