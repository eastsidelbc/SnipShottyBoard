# Bug Fix — Thumbnail Size, Labels, and Per-Image State Not Persisting
# Symptom: Right-click → Small, add a label. Close app. Reopen → back to default size, label gone.

Read these files IN FULL before touching anything:
- UI/MediaSection.xaml.cs
- Core/Models/MediaReference.cs
- Core/Models/SavedNote.cs
- UI/TabManager.cs (GetSaveData method only)

---

## ROOT CAUSE ANALYSIS

Per-image state lives in `MediaReference` (ThumbnailSize, Label, ShowLabel, ShowDate, ShowTime, IsHidden).
The save/load chain is:

  SAVE: container.Tag (MediaReference) → MediaSection.MediaReferences getter → GetSaveData() → SavedNote.Media → JSON
  LOAD: JSON → SavedNote.Media → MediaReferences setter → LoadImagesFromFiles → CreatePlaceholderContainer(mediaRef) → container.Tag

The JSON serialization (AtomicFileManager) uses camelCase consistently — that is NOT the bug.

There are THREE breaks in this chain. Find and confirm each before fixing.

---

## BUG 1 — ChangeThumbnailSize does not trigger OnMediaChanged
**File:** `UI/MediaSection.xaml.cs` — `ChangeThumbnailSize()`

`RebuildSingleContainer` calls `OnMediaChanged` at the end. But verify whether
`ChangeThumbnailSize` always reaches `RebuildSingleContainer`. If the lazy ContextMenu
(Sprint C) was implemented incorrectly and captures a stale `container` reference,
`container.Parent` will be null in `RebuildSingleContainer` → early return → no rebuild,
no `OnMediaChanged`, no save trigger.

**Verify:** Add a temporary log line at the top of `RebuildSingleContainer`:
```csharp
LoggingService.LogDebugStatic($"RebuildSingleContainer: panel={container?.Parent?.GetType().Name ?? "NULL"}", "Media");
```

If panel is null, the lazy menu is capturing a stale container. The fix: in the
`ContextMenuOpening` lazy builder inside `SetupContainerInteractions`, do NOT capture
`container` at registration time. Instead, read the current container from the sender:

```csharp
container.ContextMenuOpening += (s, e) =>
{
    var currentContainer = s as Grid;  // always the live container
    var currentRef = currentContainer?.Tag as MediaReference;
    if (currentRef == null) return;

    var menu = (ContextMenu)currentContainer.ContextMenu;
    if (menu.Items.Count > 0) return;

    // Build menu using currentContainer and currentRef, NOT the captured container/mediaRef
    var deleteItem = new MenuItem { ... };
    deleteItem.Click += (_, __) => DeleteImageFromMenu(currentContainer, currentRef);
    
    var renameItem = new MenuItem { ... };
    renameItem.Click += (_, __) => EditLabel(currentContainer, currentRef);
    
    // Size items:
    sizeItem.Click += (_, __) => ChangeThumbnailSize(currentContainer, currentRef, capturedSize);
    
    // etc — replace ALL captured `container` and `mediaRef` references with `currentContainer` and `currentRef`
};
```

This ensures the menu always operates on whatever container is currently in the visual tree.

---

## BUG 2 — EditLabel updates Tag but not OnSplitterRatioChanged → save not triggered
**File:** `UI/MediaSection.xaml.cs` — `EditLabel()`

`EditLabel` calls `OnMediaChanged?.Invoke()` — verify this chain reaches `hasUnsavedChanges = true`
in MainWindow. If NoteTab's `_onMediaChangedHandler` field was not correctly stored and
unsubscribed (Phase 2 fix), `OnMediaChanged` fires into the void.

**Verify:** Add temporary log at the top of `EditLabel`:
```csharp
LoggingService.LogDebugStatic($"EditLabel: OnMediaChanged subscriber count check", "Media");
```

And in NoteTab.Dispose(), confirm `MediaSectionControl.OnMediaChanged -= _onMediaChangedHandler`
is present. If `_onMediaChangedHandler` is null (field never populated), the Phase 2 fix
was not applied. Apply it:

```csharp
// In NoteTab constructor:
_onTextChangedHandler = () => OnDataChanged?.Invoke();
_onMediaChangedHandler = () => OnDataChanged?.Invoke();
TextSectionControl.OnTextChanged += _onTextChangedHandler;
MediaSectionControl.OnMediaChanged += _onMediaChangedHandler;
```

---

## BUG 3 — MediaReferences getter may miss containers or read stale Tags
**File:** `UI/MediaSection.xaml.cs` — `MediaReferences` getter

The getter reads `ImagePanel.Children.OfType<Grid>()`. Confirm:
1. ALL image containers in the panel ARE Grid elements with non-null Tags
2. No container has `Tag = null` after a rebuild

After Bug 1 fix, verify that after `RebuildSingleContainer`, the NEW container's Tag
is correctly set. Read `CreatePlaceholderContainer` — confirm:
```csharp
container.Tag = refData;  // refData must be the passed mediaRef, not a new instance
```

If `mediaRef` was null when passed to `CreatePlaceholderContainer`, `refData` becomes
a fresh `MediaReference` with default ThumbnailSize=150, Label="". This would explain
the "resets to default" symptom.

**Add assertion in CreatePlaceholderContainer:**
```csharp
if (mediaRef == null)
    LoggingService.LogWarningStatic($"CreatePlaceholderContainer called with null mediaRef for {Path.GetFileName(imagePath)} — will use defaults", "Media");
```

If this fires, trace back EVERY call site and ensure mediaRef is always passed.

---

## AFTER CONFIRMING ROOT CAUSES

Once confirmed, the permanent fix is:

**Fix A:** Lazy ContextMenu always reads `currentContainer = s as Grid` and `currentRef = currentContainer.Tag as MediaReference` at open time, never captures at registration time.

**Fix B:** NoteTab Phase 2 handler fields confirmed populated and unsubscribed in Dispose.

**Fix C:** Every `EnsureThumbnailLoaded` and `CreatePlaceholderContainer` call has a non-null `mediaRef`. Any null path is a bug — trace and fix.

**Fix D — SavedNote.Media is the single source of truth:**
`MediaReference` already has all per-image fields (ThumbnailSize, Label, ShowLabel, ShowDate, ShowTime, IsHidden). These are serialized correctly by AtomicFileManager. The only way they don't persist is if either the save doesn't capture the updated values, or the load doesn't restore them. Bugs 1-3 above are the causes — fix those and persistence works.

---

## BUILD + TEST

Build: `dotnet build` — 0 errors.

Test:
1. Add an image → right-click it → Rename → "My Label" → close app → reopen → label present ✓
2. Right-click image → Small → close app → reopen → thumbnail still small ✓
3. Right-click empty space → Size → Small (all) → close → reopen → all thumbnails small ✓
4. Verify master.json in %AppData%\SnipShottyBoard\ after step 1: `"label": "My Label"` present

---

## DEVNOTE

Write to: `docs/devnotes/2026-05-19-bug-thumbnail-state-persistence.md`

```
# Bug Fix — Thumbnail Size and Label State Not Persisting
Date: 2026-05-19

## Symptoms
- Right-click → Small → reopen → back to 150px default
- Label set via Rename → reopen → label gone

## Root Causes Found
[Fill in which bugs were confirmed after running the diagnostic logs]

## Fixes Applied
[Fill in which fixes were needed]

## Architecture Note
MediaReference is the correct single source of truth for per-image state.
The save/load chain (container.Tag → MediaReferences getter → SavedNote.Media → JSON) is
architecturally sound. The bugs were in event wiring and closure capture, not in the model.
```
