---
Title: Fix Image Cycling Ghost Images
Date: 2026-04-30
Owner: Jeremy
Versions Affected: 1.7.0+
Links:
 - Planning: docs/PLANNING.md §BUG FIX — Image Cycling Ghost Images
 - PR/SHAs: N/A
---

## Context & Goal

When cycling through images in ImageViewerWindow using left/right arrow keys, reaching the end and wrapping would hit "ghost" paths — files deleted or moved externally that no longer exist on disk. The viewer would show a blank screen with "❌ Image file not found" and trap the user until closing the window.

Root cause: `MediaSection.imageFiles` contained all paths including ghosts. Thumbnails correctly skip ghosts via `DataManager.ValidateImageFile()`, but the navigation list passed to ImageViewerWindow was the raw `imageFiles` list, not the validated subset.

## Round 2 — LinkedListNode Race Condition

After the ghost fix, rapid key presses still caused: "The LinkedListNode does not belong to current LinkedList."

**Root cause:** `LoadStaticAsync` is `async void`. When pressing RIGHT rapidly:
1. Image B starts decoding on background thread
2. Press RIGHT again → `ClearPreviousImage()` removes B from LRU cache
3. Background decode for B completes → tries to access removed cache node → 💥

The stale background decode had no way to be cancelled. Multiple concurrent loads fought over the shared cache.

**Fix (ImageViewerWindow):** Added `CancellationTokenSource _currentLoadCts`. Every call to `LoadImage()` cancels the previous in-flight decode. The background task checks `IsCancellationRequested` and returns null if cancelled. The `Dispatcher.Invoke` callback double-checks `currentImagePath == imagePath` before applying.

**Fix (ImageCacheManager):** Defensive try/catch on all `_lruList.Remove(node)` calls. If a node was already removed by concurrent access, the operation is safely skipped rather than crashing.

## Decisions & Alternatives

**Decision 1:** Derive the navigation list from what's actually rendered in the UI. Every thumbnail container stores its path as `.Tag`. Extract those paths and pass them to the viewer.

**Why:** What you see = what you cycle through. No filtering logic needed — the UI already did the filtering. Same behavior as Windows Photos and File Explorer.

**Decision 2:** Cancel stale background decodes instead of queuing them.

**Why:** Only the latest image matters. A cancelled decode wastes nothing — the bitmap is discarded, never cached, never applied. Alternative of queuing would create a backlog of unnecessary work.

## Implementation Notes

**Files changed:**
- `UI/MediaSection.xaml.cs` — derive navigation list from rendered thumbnails
- `UI/ImageViewerWindow.xaml.cs` — add CancellationTokenSource, cancel stale loads, defensive path check in Dispatcher.Invoke
- `UI/ImageCacheManager.cs` — defensive try/catch on LinkedList operations

**ImageViewerWindow changes:**
- Added `CancellationTokenSource? _currentLoadCts` field
- `LoadImage()` cancels previous CTS before starting new load
- `LoadStaticAsync()` creates fresh CTS, passes cancellation to background task
- `LoadStaticAsync()` checks `currentImagePath == imagePath` in Dispatcher.Invoke before applying
- `OnClosed()` cancels + disposes CTS

**ImageCacheManager changes:**
- `GetFromCache()`: try/catch on `_lruList.Remove(node)`, check `node.List == null` for orphaned nodes
- `RemoveFromCache()`: try/catch on `_lruList.Remove(node)`

## Testing & Acceptance

- `dotnet build` passes with 0 errors
- Requires manual testing: cycle right to end → wraps to first, cycle left from start → wraps to last
- Rapid key presses → no LinkedListNode errors, no "Failed to load image" in status bar

## Performance & Limits

- LINQ query over ImagePanel.Children runs in O(n) where n is visible thumbnails (< 50)
- Cancelled decodes return null immediately, no cache pollution

## Follow-ups

None.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
