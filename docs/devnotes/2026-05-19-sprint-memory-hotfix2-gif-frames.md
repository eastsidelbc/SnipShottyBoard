---
Title: Memory Hotfix 2 — GIF Frame Buffers Not Released
Date: 2026-05-19
Owner: Jeremy
Versions Affected: current
Links:
  - Planning: docs/PLANNING.md §memory audit
  - Prior fix: docs/devnotes/docs\memory-fix-phase-1.md
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

After hotfix 1 (LOH compaction on viewer close), idle RAM was ~110 MB.
Opening viewer, cycling through images including GIFs, then closing → stuck at ~180 MB.
~70 MB not returning to baseline. This fix addresses that second leak.

## Root Cause

WpfAnimatedGif decodes ALL frames of a GIF upfront into pixel buffers on load.
A 30-frame 800×600 GIF = 800 × 600 × 4 × 30 bytes ≈ 57 MB of decoded frame data.

The `AnimationController` holds a reference to the `BitmapDecoder` which owns all those frames.
Previous code in both cleanup paths called `SetAnimatedSource(null)` directly — this stops
playback but does NOT dispose the controller. The controller, decoder, and all frame buffers
stayed alive in memory.

`AnimationController` IS disposable. Calling `ctrl.Dispose()` releases the decoder and all
frame pixel buffers. This must happen BEFORE `SetAnimatedSource(null)`.

## Fix

Two places in `UI/ImageViewerWindow.xaml.cs`:

**ClearPreviousImage()** — called on every navigation:
```
var ctrl = ImageBehavior.GetAnimationController(DisplayImage);
ctrl?.Dispose();
ImageBehavior.SetAnimatedSource(DisplayImage, null);
```

**ReleaseImageResources()** — called on window close:
Same pattern applied inside the existing try/catch.

## Expected Result

RAM returns to ~110 MB baseline after viewer close, including GIF-heavy sessions.
Combined with hotfix 1 LOH compaction, full recovery within ~5 seconds of ESC.

## Build Status

0 C# errors. 286 pre-existing warnings (unchanged).
Build-time MSB3027 during testing was due to the running app locking the .exe — not a code error.

## Follow-ups

None. This closes the GIF frame leak identified in the memory audit plan.
