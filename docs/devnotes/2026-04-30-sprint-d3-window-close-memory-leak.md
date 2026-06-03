---
Title: Sprint D Phase 3 — Window Close Memory Leak Fix + Close-Path Logging
Date: 2026-04-30
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT D
 - Dev Note: docs/devnotes/2026-04-30-sprint-d2-navigation-memory-cleanup.md
---

## Context & Goal

Fix permanent memory leak when closing ImageViewerWindow after viewing a GIF. After closing, RAM stays at ~204MB and continues climbing instead of returning to baseline (~90-100MB). Each GIF viewed leaks ~120MB permanently.

## Root Cause

`BitmapCacheOption.OnDemand` keeps a live `FileStream` and a native decoder thread running even after managed references are set to null. `ImageBehavior.SetAnimatedSource(null)` only kills WpfAnimatedGif's managed timer/reference — it does NOT close the OnDemand stream or stop the unmanaged decoder. The unmanaged heap frames accumulate outside GC control.

## Implementation Notes

### Rewrote `ReleaseImageResources()` — 5-step teardown

| Step | What | Why |
|------|------|-----|
| A | `SetAnimatedSource(null)` | Kills WpfAnimatedGif's managed DispatcherTimer + animation loop |
| B | `currentImage.StreamSource?.Dispose()` | **THE FIX.** Closes the OnDemand file stream + stops native decoder. Only on unfrozen bitmaps (GIFs). |
| C | `DisplayImage.Source = null` | Clears static image bindings |
| D | `currentImage = null` | Drops last managed reference so GC can collect |
| E | `GC.Collect()` + `GC.WaitForPendingFinalizers()` | Forces unmanaged memory reclamation. Standard WPF media teardown pattern (PhotoViewer, Paint.NET). Window close is NOT a hot path — costs ~5-10ms. |

### Close-path logging

Every close event now logs `[CLS]` tagged entries:
- `[CLS] Window closing — RAM before=XXXMB`
- `[CLS] SetAnimatedSource(null) done`
- `[CLS] StreamSource disposed on unfrozen bitmap` (if GIF)
- `[CLS] Managed refs nulled — RAM after-refs=XXXMB`
- `[CLS] GC done — RAM after-GC=XXXMB, reclaimed=XXXMB`

Grep for `CLS` in log files to verify both ESC and X button paths fire.

### What Was NOT Changed

- `OnClosed` — still calls `ReleaseImageResources()` then `base.OnClosed(e)`
- `ClearPreviousImage()` — untouched (D.2)
- Navigation methods — untouched
- GIF/static loading — untouched

## Testing & Acceptance

- [x] `dotnet build` passes with 0 errors, 0 new warnings
- [ ] Open GIF → close via ESC → wait 5s → RAM returns to ~90-120MB
- [ ] Open GIF → close via X button → wait 5s → RAM returns to same level
- [ ] Repeat 4x with different GIFs → RAM oscillates, does NOT climb cumulatively
- [ ] Close-path logs appear in Serilog file with `[CLS]` tags
- [ ] Static image open/close unchanged (no regression)
- [ ] Navigation (left/right) unchanged (no regression)

## Performance & Limits

- `GC.Collect()` on window close costs ~5-10ms — user never notices
- Only runs once per window close, not on navigation
- `StreamSource?.Dispose()` is safe on null (null-conditional operator)

## Follow-ups

- Manual RAM test required
- Sprint D will be complete after D.3 passes

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
