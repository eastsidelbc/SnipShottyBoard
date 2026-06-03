# Memory Fix Phase 4 — Structural Polish
Date: 2026-05-19

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

---

## Changes

### LEAK-25: Split ImageCacheManager into two pools
**File:** `UI/ImageCacheManager.cs`

Added a second independent LRU pool for full-res viewer images.

- **Thumbnail pool:** 30MB / 60 items (AppConstants.MaxImageCacheBytes / MaxCachedImages)
- **Full-res pool:** 5 items max, no byte cap

Routing is automatic: keys ending with `":full"` (the `FullResCacheSuffix` defined in
`ImageViewerWindow`) go to the full-res pool. All other keys go to the thumbnail pool.

Public API unchanged — `GetFromCache`, `AddToCache`, `RemoveFromCache`,
`RemoveAllForPath` signatures identical.

`RemoveAllForPath` now searches both pools for variants.
`Clear` clears both pools.
`EvictLeastRecentlyUsedFullRes` added — includes LOH compaction like the thumbnail eviction.

**Why it matters:** A single full-res image (20–50MB) could previously evict dozens of
thumbnails, forcing re-decodes on every tab. With separate pools, opening the image
viewer doesn't touch the thumbnail cache at all.

---

### LEAK-16: Lazy ContextMenu construction
**File:** `UI/MediaSection.xaml.cs` — `SetupContainerInteractions()`

Replaced eager 8-item ContextMenu construction (at container creation time) with a lazy
`ContextMenuOpening` handler that builds items only on the first right-click.

- Empty `ContextMenu` placeholder assigned at creation — required for `ContextMenuOpening` event to fire
- Guard `if (menu.Items.Count > 0) return;` prevents rebuild on subsequent right-clicks
- All MenuItem captures (container, mediaRef) are identical to before — no new scope introduced

**Before:** 8+ MenuItem objects × all visible containers, allocated at load time, even if
never right-clicked.
**After:** 0 MenuItem objects allocated until user right-clicks a specific container.

Containers never right-clicked = zero MenuItem objects allocated for their lifetime.

---

### LEAK-20: Cached TextContent getter
**File:** `UI/TextSection.xaml.cs`

Added `_cachedTextContent` field (initialized to `string.Empty`).

`NoteRichTextBox_TextChanged` now updates `_cachedTextContent` via `GetPlainText()` on
every actual edit.

`TextContent` getter changed from `GetPlainText()` (creates `TextRange` each call) to
`_cachedTextContent` (field read, zero allocations).

`UpdatePlaceholderVisibility()` still calls `GetPlainText()` directly — this is correct
since it only fires on actual change events, not the 1-second timer.

**Before:** `TextRange` serialization 1×/second from status bar timer, regardless of
whether text changed.
**After:** Zero allocations per status tick. Serialization only happens on actual edits.

---

## Expected RAM impact
~110MB → ~100MB idle

## Build status
0 errors, 276 warnings (all pre-existing, none introduced by Phase 4)

## Testing checklist
- [ ] Open app, load 3 tabs with 10 images each → idle RAM ~100MB
- [ ] Open image viewer, cycle through 10 images, close → RAM returns to baseline
- [ ] Open same image twice → second click brings existing window to front (no duplicate)
- [ ] Right-click thumbnail → ContextMenu appears with all options working
- [ ] Drag and drop image reorder → works, thumbnails reload
- [ ] Word count in status bar updates while typing
- [ ] Switch tabs rapidly → no crash, thumbnails load correctly
- [ ] Open GIF in viewer, navigate away → RAM drops back
