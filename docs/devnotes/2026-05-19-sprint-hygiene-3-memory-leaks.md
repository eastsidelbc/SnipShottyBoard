---
Title: HYGIENE-3 — Memory leak fixes (forced GC + event unsubscribe)
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT HYGIENE-3
---

## Context & Goal

Fix two memory issues identified during production hygiene audit:
1. ImageViewerWindow blocks the UI thread on close by forcing GC.Collect()
2. MainWindow never unsubscribes from ThemeManager.OnThemeChanged event

## Issue 1 — ImageViewerWindow forced GC

**Root cause:** `ReleaseImageResources()` called `GC.Collect()` and `GC.WaitForPendingFinalizers()` from the UI thread (invoked by `OnClosed`). These block the entire UI until all finalizers complete, freezing every open window during close.

**Fix:** Deleted `GC.Collect()`, `GC.WaitForPendingFinalizers()`, and unused `long memBefore = GC.GetTotalMemory(false)`. The cleanup above (nulling source, disposing stream, dropping references) is sufficient — GC collects naturally.

**File:** `UI/ImageViewerWindow.xaml.cs` — `ReleaseImageResources()`

## Issue 2 — MainWindow ThemeManager event leak

**Root cause:** Lambda subscribed to `themeManager.OnThemeChanged` captures `this`, preventing MainWindow from being garbage collected as long as ThemeManager holds the subscription.

**Fix (3 steps):**
1. Replaced inline lambda with named method reference: `OnThemeChangedHandler`
2. Added `OnThemeChangedHandler()` method
3. Added unsubscribe `themeManager.OnThemeChanged -= OnThemeChangedHandler` in `MainWindow_Closing()`

**File:** `UI/Views/MainWindow.xaml.cs`

## Testing & Acceptance

- Build: 0 errors, 0 warnings
- Open/close image viewers — no UI freeze on close
- Close MainWindow — no exceptions

## Performance & Limits

- GC.Collect removal eliminates a blocking call that could hold the UI thread for 100s of ms
- Event unsubscribe ensures MainWindow can be fully GC'd when closed

## Follow-ups

None — both fixes complete.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
