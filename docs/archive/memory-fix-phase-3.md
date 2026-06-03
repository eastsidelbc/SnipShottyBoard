# Memory Fix — Phase 3: Threading & Cancellation
# Expected result: ~120MB → ~110MB
# Part 3 of 4 — Phase 2 must be complete and building clean before starting this.

You are continuing the memory remediation of SnipShottyBoard (WPF .NET 8).
Phases 1 and 2 are done. This phase fixes threading bugs in the thumbnail loading pipeline.
These are more invasive than previous phases — read the entire async pipeline carefully
before making any change.

## RULES
- Read `UI/MediaSection.xaml.cs` IN FULL before touching anything
- Understand the full flow: EnsureThumbnailLoaded → LoadThumbnailAsync → CreateThumbnailBitmap
- One fix at a time, build after each
- Do NOT change thumbnail visual output or loading behavior
- Do NOT change the semaphore limit (keep at 4)
- Build gate: `dotnet build` must pass 0 errors 0 warnings after every fix
- After ALL fixes pass build, write ONE devnote (format at bottom)

---

## FIX 1 — Remove ImageCacheManager access from background thread in CreateThumbnailBitmap
**File:** `UI/MediaSection.xaml.cs` — `CreateThumbnailBitmap()`
**Finding:** LEAK-4

`CreateThumbnailBitmap()` is called inside `Task.Run()` (background thread).
It currently calls:
1. `ImageCacheManager.Instance.GetFromCache(imagePath)` — line ~204
2. `ImageCacheManager.Instance.AddToCache(imagePath, bitmap)` — line ~234

`ImageCacheManager` has no locking — it's documented as dispatcher-thread-only.
Up to 4 concurrent background workers (semaphore = 4) can simultaneously corrupt
the internal `_index` Dictionary and `_lruList`.

**The fix:** Remove ALL `ImageCacheManager` access from `CreateThumbnailBitmap()`.
The cache check and cache write are already handled correctly in `LoadThumbnailAsync`'s
dispatcher block. `CreateThumbnailBitmap` should only decode — nothing else.

In `CreateThumbnailBitmap()`:
- DELETE the `GetFromCache` check at the top
- DELETE the `AddToCache` call at the bottom
- The method becomes a pure decoder: takes a path, returns a `BitmapImage`, no side effects

Verify that `LoadThumbnailAsync` still calls `AddToCache` on the dispatcher thread
(in the `Application.Current.Dispatcher.Invoke` block). It should already — confirm
and leave that call in place.

After the change, `CreateThumbnailBitmap` signature and return type stay the same.
Only the cache interactions are removed from inside it.

---

## FIX 2 — Pass CancellationToken through to LoadThumbnailAsync
**File:** `UI/MediaSection.xaml.cs` — `EnsureThumbnailLoaded()` and `LoadThumbnailAsync()`
**Finding:** LEAK-7 (Bug B)

Phase 1 fixed Bug A (CTS not disposed). This fixes Bug B: the token is never actually
passed to the task, so cancellation has zero effect and the `catch (OperationCanceledException)`
in `LoadThumbnailAsync` is dead code that can never trigger.

**Step 1** — Update `LoadThumbnailAsync` signature to accept a token:
```csharp
// Old:
private async Task LoadThumbnailAsync(Grid container, string imagePath, DateTime? timestamp, MediaReference? mediaRef = null)

// New:
private async Task LoadThumbnailAsync(Grid container, string imagePath, DateTime? timestamp, MediaReference? mediaRef = null, CancellationToken cancellationToken = default)
```

**Step 2** — Pass the token to `_loadSemaphore.WaitAsync()`:
```csharp
// Old:
await _loadSemaphore.WaitAsync();

// New:
await _loadSemaphore.WaitAsync(cancellationToken);
```

**Step 3** — Pass the token from `EnsureThumbnailLoaded` into the task:
```csharp
// Old (after Phase 1 fix):
_pendingLoadsCts?.Cancel();
_pendingLoadsCts?.Dispose();
_pendingLoadsCts = new CancellationTokenSource();
_ = Task.Run(() => LoadThumbnailAsync(container, imagePath, timestamp, mediaRef));

// New:
_pendingLoadsCts?.Cancel();
_pendingLoadsCts?.Dispose();
_pendingLoadsCts = new CancellationTokenSource();
var token = _pendingLoadsCts.Token;
_ = Task.Run(() => LoadThumbnailAsync(container, imagePath, timestamp, mediaRef, token));
```

**Step 4** — The existing `catch (OperationCanceledException)` in `LoadThumbnailAsync`
is now live. Verify the finally block still calls `_loadSemaphore.Release()` — it must,
even on cancellation. Confirm this is already there (it should be) and leave it intact.

**Step 5** — Also pass `cancellationToken` into the inner `Task.Run` inside
`LoadThumbnailAsync` if there is one (for the background decode step):
```csharp
await Task.Run(() => { /* decode */ }, cancellationToken);
```

---

## AFTER BOTH FIXES PASS BUILD

Manually test: open a tab with 10+ images, switch tabs rapidly, verify:
- No crash
- Thumbnails still load correctly (may load slightly fewer in-flight on fast switch — expected)
- No `ObjectDisposedException` in logs

Write devnote to:
`docs/devnotes/2026-05-19-sprint-memory-phase3-threading.md`

Format:
```
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
```
