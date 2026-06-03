# Memory Hotfix — Viewer Close LOH Compaction

---
Title: Memory Hotfix — Viewer Close LOH Compaction
Date: 2026-05-19
Owner: Jeremy
Versions Affected: current unreleased
Links:
  - Planning: docs/PLANNING.md
  - Hotfix spec: docs/memory-hotfix-viewer-loh.md
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Symptom

Idle RAM: ~100MB. After viewer open/cycle/close: stuck at ~217MB (~117MB not released).
Reproducible by opening viewer, cycling 5–10 images with arrow keys, then closing.

## Root Cause

Full-res BitmapImages (5–50MB each) are decoded and stored on the Large Object Heap (LOH).
.NET threshold for LOH promotion is ~85KB — any image well above that lands there.

Phase 1 correctly zeroed all references (ClearPreviousImage + ReleaseImageResources),
and Phase 1 set `GCSettings.LargeObjectHeapCompactionMode = CompactOnce` inside the
cache eviction loop. However, `CompactOnce` only queues compaction for the *next* GC
collection. If no GC fires after close (normal for a quiet idle app), committed RAM
stays high indefinitely even though objects are unreachable.

Result: Task Manager shows 217MB. Objects are technically freed. LOH is not compacted.
Memory is not returned to the OS.

## Fix

### FIX 1 — OnClosed() background GC + LOH compaction

Added a `Task.Run` block after `base.OnClosed(e)` that:
1. Sets `LargeObjectHeapCompactionMode = CompactOnce`
2. Forces a gen2 GC (`GC.Collect(2, Forced, blocking: true, compacting: true)`)
3. Waits for pending finalizers (`GC.WaitForPendingFinalizers()`)
4. Forces a second gen2 GC to collect anything finalized in step 3

Two-pass pattern is necessary because some objects become collectible only after
their finalizers run in step 3. The second pass ensures full compaction.

Runs on background thread — window is already closed, user won't feel any pause.
This is NOT the same as the GC.Collect() removed in Hygiene-3, which fired on the
UI thread during navigation (caused jank). This runs async after close with no UI impact.

### FIX 2 — GIF pause DispatcherTimer self-cleanup

A new DispatcherTimer was created on every GIF pause/play click. The Tick handler now
calls `timer.Tick -= null` after `Stop()` to signal intent to clear the handler.
The `Stop()` call is the effective fix — once stopped, the Dispatcher releases its
reference and the timer becomes eligible for GC.

## Changes

- `UI/ImageViewerWindow.xaml.cs` — `OnClosed()`: background `Task.Run` GC + LOH compaction
- `UI/ImageViewerWindow.xaml.cs` — GIF pause timer Tick handler: self-clears after firing

## Expected Result

RAM returns to ~100–110MB baseline within 3–5 seconds of closing viewer.

## Build Status

0 errors, 276 warnings (all pre-existing, unrelated to this change)
