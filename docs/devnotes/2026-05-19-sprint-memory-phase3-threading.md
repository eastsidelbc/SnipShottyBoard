# Memory Fix Phase 3 — Threading & Cancellation
Date: 2026-05-19

## Changes
- LEAK-4: Removed all ImageCacheManager access from CreateThumbnailBitmap (background-thread-safe)
- LEAK-7B: CancellationToken now passed through EnsureThumbnailLoaded → LoadThumbnailAsync
           Token passed to _loadSemaphore.WaitAsync() — cancellation now functional
           Token passed to inner Task.Run decode step

## Expected RAM impact
~120MB → ~110MB idle
Benefit is primarily correctness: no dict corruption risk, actual cancellation on tab switch

## Build status
0 errors, 0 warnings

## Next
Phase 4: memory-fix-phase-4.md
