# Memory Fix — Phase 1: Trivial Fixes, Biggest RAM Drop
# Expected result: ~300MB → ~150MB
# Part 1 of 4 — do NOT start Phase 2 until this builds clean.

You are fixing confirmed memory leaks in SnipShottyBoard (WPF .NET 8).
This is Phase 1 of a 4-phase memory remediation plan based on a completed audit.
All findings below are pre-confirmed — no investigation needed. Apply exactly as specified.

## RULES
- Read each file before editing it
- One fix at a time, verify build passes between each
- Do NOT refactor anything outside the exact lines specified
- Do NOT change any UI behavior, layout, or visual output
- Build gate: `dotnet build` must pass 0 errors 0 warnings after every fix
- After ALL fixes in this phase pass build, write ONE devnote (format at bottom)

---

## FIX 1 — Cache byte cap: 100MB → 30MB
**File:** `Data/AppConstants.cs`
**Finding:** LEAK-2

Change:
```csharp
// Old:
public const int MaxCachedImages = 100;
public const long MaxImageCacheBytes = 100 * 1024 * 1024;

// New:
public const int MaxCachedImages = 60;
public const long MaxImageCacheBytes = 30 * 1024 * 1024; // 30MB — right-sized for sticky notes app
```

---

## FIX 2 — Fix byte estimate formula (×2 → ×3)
**File:** `UI/ImageCacheManager.cs` — `EstimateBitmapBytes()`
**Finding:** LEAK-1

WPF allocates decoded pixel buffer + milcore compositor copy + DPI scaling copy.
Actual memory is ~×3 not ×2. Underestimate causes eviction to fire 33% late.

Change:
```csharp
// Old:
return (long)bitmap.PixelWidth * bitmap.PixelHeight * 4 * 2;

// New:
return (long)bitmap.PixelWidth * bitmap.PixelHeight * 4 * 3;
```

---

## FIX 3 — LOH compaction on cache eviction
**File:** `UI/ImageCacheManager.cs` — inside the eviction loop (where entries are removed from `_lruList`)
**Finding:** LEAK-24 / QW-8

Full-res BitmapImages (5–50MB each) live on the Large Object Heap. LOH is not compacted
by default — freed objects leave holes, RAM in Task Manager never drops even after eviction.
Setting CompactOnce tells the next GC to compact LOH once.

Find the eviction method (where `_lruList` entries are removed and `_currentBytes` is decremented).
At the end of that method, after eviction completes, add:

```csharp
// Compact LOH after evicting large bitmaps — prevents committed RAM staying high
System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
    System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
```

---

## FIX 4 — Only evict :full key on viewer navigation, not thumbnail
**File:** `UI/ImageViewerWindow.xaml.cs` — `ClearPreviousImage()`
**Finding:** LEAK-3

`RemoveAllForPath` nukes both the `:full` cache entry AND the thumbnail entry that
MediaSection uses. Thumbnail gets re-decoded from disk unnecessarily on every navigation.
Only the full-res viewer entry should be evicted here.

Change:
```csharp
// Old:
ImageCacheManager.Instance.RemoveAllForPath(currentImagePath);

// New:
ImageCacheManager.Instance.RemoveFromCache(currentImagePath + FullResCacheSuffix);
```

---

## FIX 5 — Fix clickTimer: remove Tick unsubscribe that breaks click detection
**File:** `UI/MediaSection.xaml.cs` — `CancelClickDetection()`
**Finding:** LEAK-9 (timer)

`CancelClickDetection()` unsubscribes `OnClickTimerTick` from the timer. But `AddDragHandlers`
only subscribes if `clickTimer == null`. After first use: timer exists, handler gone, subscription
never re-added → every subsequent image click silently does nothing. This is both a UX bug
and a timer running with no subscriber.

In `CancelClickDetection()`, remove this line:
```csharp
clickTimer.Tick -= OnClickTimerTick; // DELETE THIS LINE
```

The `Tick` subscription is permanent for the lifetime of `MediaSection`. Timer is reused
across clicks. Only `clickTimer.Stop()` is needed in `CancelClickDetection()`.

---

## FIX 6 — Call CancelClickDetection at top of RebuildUIFromData
**File:** `UI/MediaSection.xaml.cs` — `RebuildUIFromData()`
**Finding:** LEAK-11 / QW-6

When the UI rebuilds (tab switch, reorder, etc.), `pendingClickContainer` may still hold
a reference to an old container that's about to be removed from the visual tree.
That stale ref keeps the old container alive until the next click clears it.

At the very top of `RebuildUIFromData()`, before any other logic, add:
```csharp
CancelClickDetection(); // Clear stale container ref before rebuild
```

---

## FIX 7 — Dispose old CTS before replacing in EnsureThumbnailLoaded
**File:** `UI/MediaSection.xaml.cs` — `EnsureThumbnailLoaded()`
**Finding:** LEAK-7 / QW-7

Every call creates a new `CancellationTokenSource` but only cancels (never disposes) the old one.
With 20 images × repeated tab rebuilds, hundreds of undisposed CTSes accumulate per session.

Change:
```csharp
// Old:
_pendingLoadsCts?.Cancel();
_pendingLoadsCts = new CancellationTokenSource();

// New:
_pendingLoadsCts?.Cancel();
_pendingLoadsCts?.Dispose();
_pendingLoadsCts = new CancellationTokenSource();
```

---

## FIX 8 — LoggingService.Shutdown() on app close
**File:** `UI/Views/MainWindow.xaml.cs` — `MainWindow_Closing()`
**Finding:** LEAK-23 / QW-3

Serilog buffers final log entries. Without an explicit flush+close, the last entries are
lost and the file handle stays open until GC finalizes the logger. No RAM impact but
data integrity issue and a leaked handle.

In `MainWindow_Closing()`, after `settingsManager?.Dispose()`, add:
```csharp
loggingService?.Shutdown(); // Flush and close Serilog file handle
```

If `LoggingService` does not have a `Shutdown()` method, add one that calls
`Log.CloseAndFlush()` internally.

---

## AFTER ALL 8 FIXES PASS BUILD

Write devnote to:
`docs/devnotes/2026-05-19-sprint-memory-phase1-trivial-fixes.md`

Format:
```
# Memory Fix Phase 1 — Trivial Fixes
Date: 2026-05-19

## Changes
- LEAK-2: Cache cap reduced to 30MB / 60 items (AppConstants.cs)
- LEAK-1: Byte estimate formula corrected ×2 → ×3 (ImageCacheManager.cs)
- LEAK-24: LOH compaction enabled on cache eviction (ImageCacheManager.cs)
- LEAK-3: Viewer navigation only evicts :full key, not thumbnail (ImageViewerWindow.xaml.cs)
- LEAK-9: clickTimer Tick unsubscribe removed — subscription now permanent (MediaSection.xaml.cs)
- LEAK-11: CancelClickDetection called at top of RebuildUIFromData (MediaSection.xaml.cs)
- LEAK-7: Old CTS disposed before replacement in EnsureThumbnailLoaded (MediaSection.xaml.cs)
- LEAK-23: LoggingService.Shutdown() added to MainWindow_Closing (MainWindow.xaml.cs)

## Expected RAM impact
~300MB → ~150MB idle

## Build status
0 errors, 0 warnings

## Next
Phase 2: memory-fix-phase-2.md
```
