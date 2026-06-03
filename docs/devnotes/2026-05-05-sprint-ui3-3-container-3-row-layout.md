---
Title: Phase UI-3.3 — Container Creation (3-Row Layout)
Date: 2026-05-05
Owner: Jeremy
Versions Affected: 1.6.0+
Links:
 - Planning: docs/PLANNING.md §Sprint IV-1 Phase UI-3.3
 - PR/SHAs: N/A
---

## Context & Goal

Update all thumbnail container creation to use a 3-row layout (image, label,
timestamp) driven by `MediaReference` metadata. This enables per-image
customization (labels, visibility toggles, size variants) once the UI
controls are added in phases UI-3.4 and UI-3.5.

## Decisions & Alternatives

### Decision: Replace container.Tag from string path → MediaReference

Containers previously stored `container.Tag = imagePath` (a string). Now
`container.Tag = refData` (a `MediaReference` object). All consumers that
read `container.Tag` were updated to cast to `MediaReference` and extract
`.FullPath`.

**Why:** The container tag needs to carry all per-image metadata, not just
the path. Storing the full `MediaReference` avoids a second lookup.

**Alternative considered:** Keep path as tag, store `MediaReference` in a
separate dictionary. Rejected — adds another data structure to keep in sync.

### Decision: Container width = MediaReference.ThumbnailSize

Container width is now driven by `refData.ThumbnailSize` (default: 150 = Big).
This replaces the hardcoded `AppConstants.MediaContainerWidth = 150`.

**Why:** Per-image size variants (Big/Medium/Small) are a core feature of
UI-3. Making the container responsive to `ThumbnailSize` now means the size
change handler in UI-3.4 only needs to update the `MediaReference` and
rebuild the container.

### Decision: CalculateMinHeight helper

`CalculateMinHeight(width)` returns `width + 20` (image area + label +
timestamp overhead). Maintains a roughly square aspect ratio.

**Why:** The old code used fixed `MediaContainerMinHeight = 140`. With
variable widths (60/100/150), a proportional height makes more sense.

## Implementation Notes

### Files changed

1. **UI/MediaSection.xaml.cs**
   - `CreateImageContainer`: 3-row layout, accepts optional `MediaReference`,
     stores `MediaReference` on tag, uses `ThumbnailSize` for width
   - `CreatePlaceholderContainer`: 3-row layout, accepts optional `MediaReference`,
     stores `MediaReference` on tag, uses `ThumbnailSize` for width
   - `CreateTimestampText`: new helper, formats date/time based on
     `ShowDate`/`ShowTime` flags, supports override text for "loading…"
   - `LoadThumbnailAsync`: accepts optional `MediaReference`, respects
     `ThumbnailSize` for image dimensions, updates timestamp via row 2
   - `EnsureThumbnailLoaded`: passes `MediaReference` through
   - `RebuildUIFromData`: builds `MediaReference` from `imageFiles`/
     `imageTimestamps`, passes to container creation
   - `LoadImagesFromFiles`: builds `MediaReference`, passes to container creation
   - `ShowFullSizeImage`: reads `FullPath` from `MediaReference` tag
   - `RemoveImageByPath`: matches `FullPath` from `MediaReference` tag

### 3-Row Layout Structure

```
Grid (container, Tag = MediaReference)
├── Row 0: Grid (image area, MinHeight = containerWidth - 10)
│         └── Image or "· · ·" placeholder
├── Row 1: TextBlock (label, conditionally Visible/Collapsed)
└── Row 2: TextBlock (timestamp, formatted per ShowDate/ShowTime)
```

### Backward compatibility

- `MediaReference` parameters are optional (`MediaReference? mediaRef = null`)
- When `null`, a default `MediaReference` is created with sensible defaults
  (`ThumbnailSize = 150`, `ShowLabel = false`, `ShowDate = true`,
  `ShowTime = true`, `IsHidden = false`)
- Existing images without metadata render identically to before (image +
  timestamp, label hidden)

## Testing & Acceptance

- [x] `dotnet build` — 0 errors
- [ ] Manual test: thumbnails render with 3-row layout
- [ ] Manual test: existing images show image + timestamp, label hidden
- [ ] Manual test: image viewer navigation still works
- [ ] Manual test: image deletion still works
- [ ] Manual test: drag-drop reordering still works

## Performance & Limits

- Container creation is synchronous (no change)
- Thumbnail loading is async (no change)
- `CalculateMinHeight` is O(1)

## Follow-ups

- Phase UI-3.4: Context menu with size/visibility/label actions
- Phase UI-3.5: Label editing via dialog

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
