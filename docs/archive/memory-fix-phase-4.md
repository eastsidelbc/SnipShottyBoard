# Memory Fix — Phase 4: Structural Polish
# Expected result: ~110MB → ~100MB
# Part 4 of 4 — Phases 1, 2, and 3 must be complete and building clean before starting this.

You are completing the memory remediation of SnipShottyBoard (WPF .NET 8).
This is the final phase. Changes here are structural — they touch the cache architecture
and UI construction patterns. Read everything carefully. These have the highest risk
of side effects, so test thoroughly after each fix.

## RULES
- Read every file involved IN FULL before touching anything
- One fix at a time, build AND manually test after each
- Do NOT change any visual behavior, thumbnail appearance, or cache hit behavior
- Preserve all existing MediaReference metadata (Label, ThumbnailSize, IsHidden, etc.)
- Build gate: `dotnet build` must pass 0 errors 0 warnings after every fix
- After ALL fixes pass build, write the final devnote AND a session summary (formats at bottom)

---

## FIX 1 — Split LRU cache into two pools: thumbnails vs full-res
**File:** `UI/ImageCacheManager.cs`
**Finding:** LEAK-25

Currently one shared LRU pool holds both thumbnails (small, many, reused constantly)
and full-res viewer images (large, infrequently reused, should evict fast).
A single full-res image (20–50MB) can evict dozens of thumbnails, causing re-decodes
across all tabs. After the viewer closes, the full-res image should not compete with
thumbnails for cache space.

**The split:**
- Thumbnail pool: 30MB, 60 items (current AppConstants values from Phase 1)
- Full-res pool: 5 items max, no byte cap (each is evicted on viewer close via Phase 1 Fix 4)

**Implementation approach:**
Add a second internal cache structure in `ImageCacheManager` for full-res entries.
Full-res keys are identified by the `:full` suffix (already defined as `FullResCacheSuffix`
in `ImageViewerWindow`). Use this suffix to route gets/sets to the correct pool.

Specifically:
1. Add a second `LinkedList<CacheEntry> _fullResLruList` and `Dictionary<string, LinkedListNode<CacheEntry>> _fullResIndex`
2. Add `long _fullResCurrentBytes` and `const int MaxFullResItems = 5`
3. In `AddToCache(string path, BitmapImage bitmap)`: if `path.EndsWith(":full")` → route to full-res pool, else → thumbnail pool
4. In `GetFromCache(string path)`: same routing logic
5. In `RemoveFromCache(string path)`: same routing logic
6. Full-res pool eviction: evict LRU when count > 5 (no byte cap — viewer close handles cleanup)
7. Thumbnail pool eviction: existing logic unchanged (30MB / 60 items from Phase 1)

Do NOT change the public API of `ImageCacheManager` — `AddToCache`, `GetFromCache`,
`RemoveFromCache`, `RemoveAllForPath` signatures stay identical.

---

## FIX 2 — Lazy-create ContextMenu on right-click, not on container creation
**File:** `UI/MediaSection.xaml.cs` — `SetupContainerInteractions()`
**Finding:** LEAK-16

Every image container gets a fully-constructed `ContextMenu` with 8 `MenuItem` children
built eagerly at creation time. With 30 images per tab × multiple tabs = hundreds of
`MenuItem` objects always in memory, never shown unless the user right-clicks.

**The fix:** Assign `ContextMenu` lazily on first right-click using `ContextMenuOpening`.

Replace the eager ContextMenu construction in `SetupContainerInteractions()`:
```csharp
// Old: build full ContextMenu at container creation time
var contextMenu = new ContextMenu();
// ... 8 MenuItems added ...
container.ContextMenu = contextMenu;

// New: build ContextMenu lazily on first open
container.ContextMenu = new ContextMenu(); // empty placeholder — required for event to fire
container.ContextMenuOpening += (s, e) =>
{
    var menu = (ContextMenu)container.ContextMenu;
    if (menu.Items.Count > 0) return; // already built — don't rebuild

    // Move all existing MenuItem construction here, unchanged:
    var copyItem = new MenuItem { ... };
    // ... rest of existing construction unchanged ...
    // Items added to `menu` not `contextMenu`
};
```

This defers the allocation of all MenuItems until the first right-click on that specific
container. Containers never right-clicked = zero MenuItem objects allocated.

**Important:** The per-container `ContextMenuOpening` handler captures `container` and
`mediaRef` — same as the existing lambdas. No new capture scope introduced.

---

## FIX 3 — Stop TextRange serialization every second for word count
**File:** `UI/TextSection.xaml.cs` (or wherever `TextContent` getter is defined)
**Finding:** LEAK-20

`MainWindow.UpdateStatusBar()` → `tabManager.SelectedTab?.Content?.TextContent` is called
every 1 second by the status timer. If `TextContent` reads from the `RichTextBox` by
creating a `TextRange` and calling `TextRange.Text`, that's RTF/text serialization
happening 1×/second regardless of whether the content changed.

**Read `TextSection.xaml.cs` first.** Check the `TextContent` getter.

If it does `new TextRange(RichTextBox.Document.ContentStart, RichTextBox.Document.ContentEnd).Text`:

Add a cached plain-text field that updates only on actual text change:
```csharp
// Add field:
private string _cachedTextContent = string.Empty;

// In the existing OnTextChanged handler (already fires on user edits):
_cachedTextContent = new TextRange(
    RichTextBox.Document.ContentStart,
    RichTextBox.Document.ContentEnd).Text;

// Change TextContent getter:
// Old:
public string TextContent =>
    new TextRange(RichTextBox.Document.ContentStart,
                  RichTextBox.Document.ContentEnd).Text;

// New:
public string TextContent => _cachedTextContent;
```

This means serialization happens only when text actually changes, not 1×/second.
The cached value is always fresh because it updates on every edit via the existing handler.

---

## AFTER ALL 3 FIXES PASS BUILD

### Manual test checklist:
- [ ] Open app, load 3 tabs with 10 images each → idle RAM ~100MB
- [ ] Open image viewer, cycle through 10 images, close → RAM returns to baseline
- [ ] Open same image twice (click same thumbnail twice) → second click brings existing window to front, no new window
- [ ] Right-click thumbnail → ContextMenu appears correctly with all options
- [ ] Drag and drop image reorder → works, thumbnails reload
- [ ] Word count in status bar updates correctly while typing
- [ ] Switch tabs rapidly → no crash, thumbnails load correctly
- [ ] Open GIF in viewer, navigate away → RAM drops back (GIF frames released)

---

### Devnote — write to:
`docs/devnotes/2026-05-19-sprint-memory-phase4-structural.md`

Format:
```
# Memory Fix Phase 4 — Structural Polish
Date: 2026-05-19

## Changes
- LEAK-25: Split ImageCacheManager into thumbnail pool (30MB/60 items) +
           full-res pool (5 items max) — thumbnails no longer evicted by viewer images
- LEAK-16: ContextMenu construction deferred to first right-click (lazy)
           Previously: 8 MenuItems × all containers at load time
           Now: 0 MenuItems allocated until user right-clicks
- LEAK-20: TextContent getter now returns cached string updated only on edit
           Previously: TextRange serialization 1×/second from status timer
           Now: zero allocations per status tick

## Expected RAM impact
~110MB → ~100MB idle

## Build status
0 errors, 0 warnings
```

---

### Final session summary — write to:
`docs/devnotes/2026-05-19-memory-remediation-complete.md`

Format:
```
# Memory Remediation — Complete
Date: 2026-05-19
Phases: 4 | Total findings: 26 | Fixed: 22 | Clean/N/A: 4

## RAM reduction
Before: ~300MB idle
After:  ~100MB idle
Reduction: ~200MB (~67%)

## Phase summary
Phase 1 (Trivial):   8 fixes — cache cap, LOH compaction, byte formula, CTS disposal
Phase 2 (Bugs):      4 fixes — GIF frame release, viewer limit, brush cache, tab disposal
Phase 3 (Threading): 2 fixes — cache thread safety, real cancellation support
Phase 4 (Structure): 3 fixes — split cache pools, lazy ContextMenu, cached TextContent

## Biggest single wins
1. Cache cap 100MB → 30MB (LEAK-2): ~70MB recovered
2. LOH compaction on eviction (LEAK-24): ~100MB LOH freed
3. GIF frame disposal (LEAK-6): ~30MB per animated GIF recovered
4. Split cache pools (LEAK-25): thumbnails no longer evicted by full-res images

## Remaining known items (deferred, not urgent)
- LEAK-16 (WrapPanel virtualization): would require ItemsControl refactor — future sprint
- LEAK-22 (Debug.WriteLine in Release): cosmetic, no RAM impact
```
