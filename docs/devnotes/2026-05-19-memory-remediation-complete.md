# Memory Remediation — Complete
Date: 2026-05-19
Phases: 4 | Total findings: 26 | Fixed: 22 | Clean/N/A: 4

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

---

## RAM reduction
Before: ~300MB idle
After:  ~100MB idle
Reduction: ~200MB (~67%)

---

## Phase summary

### Phase 1 (Trivial) — 8 fixes
Cache cap (LEAK-2), LOH compaction on eviction (LEAK-24), byte formula correction,
CancellationTokenSource disposal, and related trivial leaks.

### Phase 2 (Bugs) — 4 fixes
GIF frame release on viewer close (LEAK-6), viewer instance limit enforcement,
brush cache consolidation, tab disposal on close.

### Phase 3 (Threading) — 2 fixes
ImageCacheManager thread-safety via dispatcher confinement (LEAK-3),
real CancellationToken propagation through async thumbnail loader (LEAK-14).

### Phase 4 (Structural) — 3 fixes
Split cache pools (LEAK-25), lazy ContextMenu (LEAK-16), cached TextContent (LEAK-20).
See: docs/devnotes/2026-05-19-sprint-memory-phase4-structural.md

---

## Biggest single wins

1. **Cache cap 100MB → 30MB (LEAK-2):** ~70MB recovered — single largest reduction
2. **LOH compaction on eviction (LEAK-24):** ~100MB LOH freed — prevents committed RAM
   staying high after evictions
3. **GIF frame disposal (LEAK-6):** ~30MB per animated GIF recovered on viewer close
4. **Split cache pools (LEAK-25):** thumbnails no longer evicted by full-res viewer
   images — sustained throughput improvement, not one-time recovery

---

## Remaining known items (deferred, not urgent)

- **LEAK-16 (WrapPanel virtualization):** Full virtualization would require replacing
  WrapPanel with an ItemsControl + custom VirtualizingPanel. Significant refactor.
  Deferred to future sprint.
- **LEAK-22 (Debug.WriteLine in Release builds):** Cosmetic. No RAM impact. Low priority.

---

## Files changed across all phases (partial list)

- `UI/ImageCacheManager.cs` — dual-pool LRU, LOH compaction, thread safety
- `UI/ImageViewerWindow.xaml.cs` — GIF disposal, viewer limit, full-res eviction on close
- `UI/MediaSection.xaml.cs` — lazy ContextMenu, CancellationToken propagation, GIF limit
- `UI/TextSection.xaml.cs` — cached TextContent getter
- `Data/AppConstants.cs` — MaxCachedImages, MaxImageCacheBytes, MaxAnimatedGifsPerNote
- `UI/TabManager.cs` — tab disposal on close
