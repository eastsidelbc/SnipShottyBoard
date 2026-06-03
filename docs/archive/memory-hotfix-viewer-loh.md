# Memory Hotfix — Viewer Close RAM Not Returning to Baseline
# Symptom: Idle ~100MB. Open viewer, cycle images, close → stuck at ~217MB.
# Root cause: LOH never compacted after viewer closes. GC not triggered.
# This hotfix sits between Phase 1 and Phase 2. Apply it now, then continue with Phase 2.

Read `UI/ImageViewerWindow.xaml.cs` IN FULL before touching anything.

## CONTEXT

Phase 1 fixed cache cleanup correctly:
- ClearPreviousImage() evicts the previous :full key on navigation ✅
- ReleaseImageResources() evicts the last :full key on close ✅
- BitmapImages have zero references after close ✅

The problem: full-res BitmapImages (5–50MB each) live on the Large Object Heap (LOH).
When freed, .NET does NOT compact LOH or return the memory to the OS automatically.
Phase 1 set GCSettings.LargeObjectHeapCompactionMode = CompactOnce inside the cache
eviction loop — but that only queues compaction for the NEXT GC collection. If no GC
is triggered after close, committed RAM stays high indefinitely. Task Manager shows
217MB even though the objects are technically freed.

Fix: trigger a gen2 GC with LOH compaction on a background thread when the viewer closes.
This is the correct place — window is gone, user won't feel any pause.
This is NOT the same as the GC.Collect() removed in Hygiene-3, which was on the UI
thread during navigation. This runs async after close.

---

## FIX 1 — Force GC + LOH compaction on viewer close (background thread)
**File:** `UI/ImageViewerWindow.xaml.cs` — `OnClosed()`

```csharp
// Old:
protected override void OnClosed(EventArgs e)
{
    _currentLoadCts?.Cancel();
    _currentLoadCts?.Dispose();
    _currentLoadCts = null;

    ReleaseImageResources();
    base.OnClosed(e);
}

// New:
protected override void OnClosed(EventArgs e)
{
    _currentLoadCts?.Cancel();
    _currentLoadCts?.Dispose();
    _currentLoadCts = null;

    ReleaseImageResources();
    base.OnClosed(e);

    // Reclaim LOH memory from full-res BitmapImages on a background thread.
    // Large objects (>85KB) live on the LOH which is not compacted by default.
    // Without this, Task Manager shows committed RAM ~117MB above baseline after close.
    Task.Run(() =>
    {
        System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
            System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    });
}
```

---

## FIX 2 — Dispose the GIF pause DispatcherTimer after it fires
**File:** `UI/ImageViewerWindow.xaml.cs` — `DisplayImage_MouseLeftButtonUp()`

A new DispatcherTimer is created on every GIF pause/play click and never disposed.
Minor but worth fixing while here.

```csharp
// Old:
DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
timer.Tick += (s, args) => {
    timer.Stop();
    StatusZoom.Text = prevZoomText;
    UpdateStatusZoom();
};
timer.Start();

// New:
DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
timer.Tick += (s, args) => {
    timer.Stop();
    timer.Tick -= null; // clear handler
    StatusZoom.Text = prevZoomText;
    UpdateStatusZoom();
};
timer.Start();
```

---

## VERIFY

Build: `dotnet build` — 0 errors, 0 warnings.

Manual test:
1. Launch app — note idle RAM (~100MB)
2. Open image viewer, cycle through 5–10 images with arrow keys
3. Close viewer (ESC or X)
4. Wait 3–5 seconds
5. Check Task Manager — RAM should return to ~100–110MB baseline

If RAM drops back: hotfix working. Continue to Phase 2.
If RAM still high after 10 seconds: paste Task Manager reading here before continuing.

---

## DEVNOTE

Write to: `docs/devnotes/2026-05-19-sprint-memory-hotfix-viewer-loh.md`

```
# Memory Hotfix — Viewer Close LOH Compaction
Date: 2026-05-19

## Symptom
Idle: ~100MB. After viewer open/cycle/close: stuck at ~217MB (~117MB not released).

## Root cause
Full-res BitmapImages on Large Object Heap. LOH not compacted automatically.
Phase 1 queued CompactOnce on cache eviction but never triggered GC.
Cache cleanup was correct — zero references after close — but LOH memory not returned.

## Fix
OnClosed() now triggers gen2 GC + LOH compaction on background thread after close.
Two-pass GC (Collect → WaitForFinalizers → Collect) ensures all finalizeable objects
are collected and LOH is fully compacted before committed memory is measured.

## Changes
- ImageViewerWindow.xaml.cs — OnClosed(): background Task.Run GC + LOH compaction
- ImageViewerWindow.xaml.cs — GIF pause timer handler self-clears after firing

## Expected result
RAM returns to ~100MB baseline within 3–5 seconds of closing viewer.

## Build status
0 errors, 0 warnings
```
