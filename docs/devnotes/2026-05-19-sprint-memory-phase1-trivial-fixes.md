---
Title: Memory Fix Phase 1 — Trivial Fixes
Date: 2026-05-19
Owner: Jeremy
Versions Affected: current
Links:
  - Planning: docs/PLANNING.md
  - Audit Plan: docs/memoryauditplan.md
  - Phase 2: docs/memory-fix-phase-2.md
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

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

0 errors, 0 warnings (278 pre-existing warnings unchanged)

## Next

Phase 2: docs/memory-fix-phase-2.md
