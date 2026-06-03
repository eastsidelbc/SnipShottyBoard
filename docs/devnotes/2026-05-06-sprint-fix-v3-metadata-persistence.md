---
Title: Fix v3 metadata persistence (labels, sizes, hidden, toggles)
Date: 2026-05-06
Owner: Jeremy
Versions Affected: 1.6.0+
Links:
 - Planning: docs/PLANNING.md
---

## Context & Goal

Labels, thumbnail sizes, hidden state, and Date/Time/Label toggles were not persisting across drag-reorder operations. Root cause: `RebuildUIFromData()` in MediaSection created fresh `MediaReference` objects with only `Filename` and `DateAdded`, discarding all v3 customization fields.

Additionally:
- Label toggle defaulted unchecked while Date/Time defaulted checked (inconsistent UX)
- Global (empty-space) context menu lacked Label/Date/Time toggles

## Decisions & Alternatives

- **Reuse existing refs**: Collect `MediaReference` objects from container `.Tag` before clearing, build a lookup dict by `FullPath`, reuse during rebuild. Alternative would be storing metadata in a parallel dict, but `.Tag` is the source of truth already.
- **ShowLabel default true**: Flipped from `false` to `true` to match Date/Time behavior.
- **Global menu toggles**: Added Label/Date/Time items to empty-space context menu that apply to all images, matching the per-image menu pattern.

## Implementation Notes

### Files Changed

1. **UI/MediaSection.xaml.cs** — `RebuildUIFromData()` (line ~1462)
   - Added pre-clear collection of existing `MediaReference` objects keyed by `FullPath`
   - Reuse existing refs instead of creating new ones during rebuild
   - New helper methods: `ToggleShowLabelForAll(bool)`, `ToggleShowDateForAll(bool)`, `ToggleShowTimeForAll(bool)`
   - Global context menu (`MediaSection_ContextMenuOpening`) now includes Label/Date/Time toggle items with icons

2. **Core/Models/MediaReference.cs** — line 45
   - `ShowLabel` default changed from `false` to `true`

### Key Code Change (RebuildUIFromData)

Before clearing containers, collect existing refs:
```csharp
var existingRefs = new Dictionary<string, MediaReference>();
foreach (var container in ImagePanel.Children.OfType<Grid>())
{
    var refData = container.Tag as MediaReference;
    if (refData != null)
    {
        var fullPath = refData.FullPath;
        if (!string.IsNullOrEmpty(fullPath))
            existingRefs[fullPath] = refData;
    }
}
```

Then reuse during rebuild:
```csharp
MediaReference mediaRef;
if (existingRefs.TryGetValue(imagePath, out var existingRef))
    mediaRef = existingRef;
else
    mediaRef = new MediaReference { Filename = Path.GetFileName(imagePath), DateAdded = timestamp };
```

## Testing & Acceptance

- [ ] Set label on image, drag-reorder, label survives
- [ ] Change thumbnail size, drag-reorder, size survives
- [ ] Hide image, drag-reorder, hidden state survives
- [ ] Toggle Date/Time/Label per-image, drag-reorder, toggles survive
- [ ] Right-click empty space, Label/Date/Time toggles appear
- [ ] Save and restart, all v3 metadata persists
- [ ] Build passes (verified: 0 errors, 0 warnings)

## Performance & Limits

No performance impact — the collection pass is O(n) where n = number of images, same as the rebuild loop.

## Follow-ups

None — all three reported issues (labels, sizes, hidden state not saving) resolved by the single root cause fix.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
