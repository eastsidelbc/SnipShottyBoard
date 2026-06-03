---
Title: Phase UI-3.2 — Save/Load Bridge for Media schema v3
Date: 2026-05-05
Owner: Jeremy
Versions Affected: 1.6.0+
Links:
 - Planning: docs/PLANNING.md §Sprint IV-1 Phase UI-3.2
 - PR/SHAs: N/A
---

## Context & Goal

Complete the save/load bridge so that `Media` (filename-only references with
per-image customization fields) are persisted correctly through the
TabManager → SavedNote → JSON → SavedNote → TabManager round-trip.

This enables schema v3 (per-image customization: labels, thumbnail sizes,
visibility toggles, etc.) to survive app restarts.

## Decisions & Alternatives

### Decision: Add MediaReferences property on MediaSection/NoteTab

Rather than keeping the round-trip through `SavedNote.ImageFiles` computed
properties (full paths ↔ Media), add a direct `MediaReferences` property on
`MediaSection` that bridges full paths ↔ `List<MediaReference>`.

**Why:** Eliminates the intermediate `SavedNote` conversion layer.
`TabManager.GetSaveData()` now reads `MediaReferences` directly from the UI
layer, and `TabManager.LoadTabs()` writes `MediaReferences` directly.

**Alternative considered:** Keep using `SavedNote.ImageFiles` computed
properties. Rejected because it adds a conversion step through `SavedNote`
that duplicates the bridge logic already on `MediaSection`.

### Decision: Bump CurrentNoteSchemaVersion 2→3

Schema v3 adds per-image customization fields (Label, ThumbnailSize,
IsHidden, ShowLabel, ShowDate, ShowTime) to `MediaReference`. These fields
have sensible defaults, so migration from v2→v3 requires no data
transformation — just bumping the version stamp.

### Decision: Fix MigrateNotes to iterate notes

`MigrateNotes()` was a no-op (copied notes without calling migration).
Fixed to clone each note and call `MigrateNoteToCurrent()`. This ensures
legacy notes.json data gets properly migrated.

## Implementation Notes

### Files changed

1. **Core/Schema/MigrationService.cs**
   - `CurrentNoteSchemaVersion`: 2 → 3
   - Renamed `MigrateNoteToMediaRefs` → `MigrateNoteToCurrent`
   - Fixed `MigrateNotes()` to clone and migrate each note
   - `MigrateNoteWindows()` updated to call `MigrateNoteToCurrent`

2. **UI/MediaSection.xaml.cs**
   - Added `using SnipShottyBoard.Core.Models`
   - Added `MediaReferences` property:
     - Getter: builds `List<MediaReference>` from `imageFiles` + `imageTimestamps`
     - Setter: clears and rebuilds `imageFiles`/`imageTimestamps` from
       `MediaReference.FullPath` and `MediaReference.DateAdded`

3. **UI/Views/NoteTab.xaml.cs**
   - Added `MediaReferences` property delegating to `MediaSectionControl`

4. **UI/TabManager.cs**
   - `GetSaveData()`: changed `ImageFiles`/`ImageTimestamps` → `Media`
   - `LoadTabs()`: changed `ImageFiles`/`ImageTimestamps` → `MediaReferences`

### Save cycle (new)

```
TabManager.GetSaveData()
  → tab.Content.MediaReferences (MediaSection)
    → builds List<MediaReference> from full paths + timestamps
  → SavedNote.Media = MediaReferences
  → JSON serializes Media (filename-only + customization fields)
```

### Load cycle (new)

```
JSON deserializes Media → SavedNote.Media
  → TabManager.LoadTabs()
    → noteTab.MediaReferences = savedNote.Media
      → MediaSection.MediaReferences setter
        → rebuilds imageFiles (full paths) + imageTimestamps
        → calls LoadImagesFromFiles()
        → UI refreshes
```

## Testing & Acceptance

- [x] `dotnet build` — 0 errors, 0 warnings
- [ ] Manual test: add images, save, restart, verify images load
- [ ] Manual test: verify images.json contains filename-only Media entries
  (not full paths)

## Performance & Limits

- `MediaReferences` getter iterates all images O(n) — acceptable for
  typical tab sizes (< 100 images)
- `MediaReferences` setter calls `LoadImagesFromFiles()` which triggers
  async thumbnail generation — same behavior as `ImageFiles` setter

## Follow-ups

- Phase UI-3.3: Per-image customization UI (context menu, properties panel)
- Consider: deprecate `ImageFiles`/`ImageTimestamps` properties on
  `SavedNote` once all code paths use `Media`

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
