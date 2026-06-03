---
Title: Sprint D Phase 1a — Debug Image Load Breadcrumbs
Date: 2026-04-29
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT D
---

## Context & Goal

Add debug logging breadcrumbs across the image load path in ImageViewerWindow to diagnose the GIF thread ownership crash. Without logging, we're guessing at the root cause. With logging, we can see Thread IDs, session numbers, and exact failure points in the log file.

## Decisions & Alternatives

**Decision:** File-only logging via existing LoggingService — no dev console UI.
**Why:** Keeps the codebase simple. Logging is diagnostic, not a feature. Flip `debugImageLogging = false` to disable when done.

**Tag format:** `[IMG-sess#-filename]` — grep-able, includes session number and filename for correlation.
**Thread ID on every line:** Critical for identifying cross-thread bugs.

## Implementation Notes

### Fields Added
```
private static bool debugImageLogging = true;  // Toggle to disable
private int imageLoadSession = 0;              // Increments per LoadImage() call
```

### LogImage Helper Method
Single entry point — all context auto-included. Guards against `debugImageLogging = false`. Uses `LoggingService.LogDebugStatic` for normal, `LoggingService.LogErrorStatic` for exceptions.

### 8 Breadcrumb Points

| # | Location | Message |
|---|----------|---------|
| 1 | `LoadImage()` entry, after incrementing session | `📥 LoadImage START` |
| 2 | `LoadImage()` format detection | `Format detected: {extension}` |
| 3 | `LoadStaticAsync()` cache HIT | `⚡ Cache HIT for static image` |
| 4 | `LoadStaticAsync()` cache MISS | `🔄 Cache MISS, decoding on background thread` |
| 5 | `LoadGifAsync()` entry | `🎞️ Creating GIF BitmapImage (OnDemand cache)` |
| 6 | `LoadGifAsync()` before SetAnimatedSource | `Calling SetAnimatedSource, bitmap={W}x{H}, currentImage is null={bool}` |
| 7 | `LoadGifAsync()` after SetAnimatedSource | `✅ SetAnimatedSource complete` |
| 8 | Any catch block | `💥 Exception: {type}` (with full stack trace) |

### GIF try/catch
Wrapped `ImageBehavior.SetAnimatedSource()` calls in try/catch within `LoadGifAsync`. On failure, logs full exception and re-throws so user sees the error.

### LoadImage() Changes
- Moved `currentImagePath` assignment BEFORE the `File.Exists` check so the filename is available in breadcrumb tags even for failed loads
- Added `imageLoadSession++` at entry
- Added catch block logging

## Testing & Acceptance

- [ ] Open any image (PNG or GIF) → grep `IMG` in latest log file → see at least 2 entries per load
- [ ] Each entry has Thread= and session tag format `[IMG-sess#-filename]`
- [ ] Crash a GIF load → grep for the GIF filename → see full exception details
- [ ] Session numbers increment: sess1, sess2, sess3... when cycling images
- [ ] PNG loads still work identically (no regression)
- [ ] `dotnet build` succeeds with 0 errors, 0 new warnings ✅

## Performance & Limits

- Zero cost when `debugImageLogging = false` — method returns immediately
- No UI overhead — all logging goes to file only
- Thread ID captured via `Thread.CurrentThread.ManagedThreadId`

## Follow-ups

- Phase D.1b: Fix GIF thread bug using log data to verify
- Flip `debugImageLogging = false` after D.1b verification complete

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
