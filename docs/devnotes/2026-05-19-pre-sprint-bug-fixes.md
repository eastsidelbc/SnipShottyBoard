# Pre-Sprint Bug Fixes — 5 Audit Findings
Date: 2026-05-19

---
Title: Pre-Sprint Bug Fixes — 5 Audit Findings
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
  - Planning: docs/PLANNING.md
  - Source: docs/pre-sprint-bug-fixes.md
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

---

## Context & Goal

5 confirmed bugs from the deep code audit (docs/AUDIT_REPORT.md), applied before Sprint G.
All fixes are surgical — no refactoring, no behavior changes outside exact scope.

---

## Fixes Applied

### FIX-1a: SaveMasterData now throws IOException when AtomicSave returns false
**File:** `Core/Managers/DataManager.cs` — `SaveMasterData()`
**Root cause:** AtomicSave returned false on disk failure but SaveMasterData swallowed it silently.
Callers received no exception, continued as normal, marked state clean.
**Fix:** Added `throw new IOException(...)` in the else branch after logging.
Failure now propagates up to SaveApplicationData's try/catch.

### FIX-1b: SaveApplicationData keeps hasUnsavedChanges = true on failure + shows status bar error
**Files:** `UI/Views/MainWindow.xaml.cs` — `SaveApplicationData()`, `UI/StatusBarManager.cs`
**Root cause:** The catch block only logged. State was never marked dirty again.
Status bar said "Saved" even when nothing was written.
**Fix:**
- Catch block now sets `hasUnsavedChanges = true` and calls `statusBarManager?.ShowSaveError()`
- Added `ShowSaveError()` to StatusBarManager: shows "⚠️ Save failed" for 5s, reverts to "Unsaved"
- Uses a local DispatcherTimer to auto-revert — matches existing timer pattern in the class

### FIX-2: Recovery merge now matches notes by index first, title as fallback
**File:** `Core/Managers/DataManager.cs` — `MergeRecoveryIntoSaved()`
**Root cause:** `FirstOrDefault(n => n.Title == recNote.Title)` — two tabs named "Note 1"
always resolved to the first match. Second tab was never updated.
**Fix:** Try by index position first (verify title matches as sanity check), fall through to
title-only match when index is out of range (tabs added/removed between save and crash).

### FIX-3: UpdateSettings() resets skipDeleteConfirmation when ConfirmTabDeletion = true
**File:** `UI/TabManager.cs` — `UpdateSettings()`
**Root cause:** `IsDeleteConfirmationDisabled = (settings.ConfirmTabDeletion == false) || skipDeleteConfirmation`.
When user re-enabled via Settings, `appSettings.ConfirmTabDeletion` went true but
`skipDeleteConfirmation` stayed true from the "don't ask again" click. OR condition = still true.
**Fix:** After `this.appSettings = settings`, added: `if (settings.ConfirmTabDeletion) skipDeleteConfirmation = false;`

### FIX-4a: Vault cleanup passes daysGracePeriod: 1 (was 0)
**File:** `UI/Views/MainWindow.xaml.cs` — `ShowVaultAudit()`
**Root cause:** Dialog said files within 24h are safe, but the call used `daysGracePeriod: 0`
which deletes ALL orphaned files with no grace period.
**Fix:**
- `CleanupOrphanedImages(daysGracePeriod: 0)` → `daysGracePeriod: 1`
- Updated dialog text to correctly say files added in the last 24h are protected

### FIX-4b: Prev/Next navigation buttons now show when multiple images are available
**Files:** `UI/ImageViewerWindow.xaml.cs`
**Root cause:** Buttons existed in XAML as `Visibility="Collapsed"` but `UpdateNavigationButtons()`
was never implemented or called. Keyboard navigation (arrow keys) worked fine.
**Fix:**
- Added `UpdateNavigationButtons()`: shows buttons when `allImagePaths.Count > 1`, collapses otherwise
- Called from `LoadImage()` immediately after setting `currentImagePath`
- Covers both the single-image and multi-image constructors (all paths go through LoadImage)

### FIX-5: MigrateNoteToCurrent() explicitly fixes ThumbnailSize <= 0
**File:** `Core/Schema/MigrationService.cs` — `MigrateNoteToCurrent()`
**Root cause:** Comment said "already set in MediaReference ctor" — wrong. JSON deserialization
overwrites ctor defaults with whatever was in the file. Old JSON with `thumbnailSize: 0`
(or missing field defaulting to 0) produced ThumbnailSize=0. Thumbnails rendered zero-width.
**Fix:** Added foreach loop inside `if (note.DataVersion < CurrentNoteSchemaVersion)`:
```csharp
foreach (var media in note.Media)
{
    if (media.ThumbnailSize <= 0)
        media.ThumbnailSize = AppConstants.ThumbnailSizeDefault;
}
```
Uses `ThumbnailSizeDefault` (150px = ThumbnailSizeBig) — matches what the ctor actually sets.

---

## Build Status
0 errors, 276 warnings (all pre-existing, none introduced by these fixes)

---

## Testing & Acceptance

- FIX-1: Simulate disk full or AtomicSave failure → status bar shows "⚠️ Save failed", state stays dirty
- FIX-2: Open two tabs named "Note 1" → crash → recover → verify correct text in each tab
- FIX-3: Click "don't ask again" → open Settings → re-enable → delete a tab → confirm dialog appears
- FIX-4a: Open Developer → Audit Vault → verify dialog says "protected and will NOT be deleted"
- FIX-4b: Open image viewer with multiple images → Prev/Next buttons visible; single image → hidden
- FIX-5: Load legacy pre-v3 data with thumbnailSize=0 → thumbnails render at 150px width

---

## Notes

FIX-8 (Dispatcher.Invoke blocking inside Task.Run) deferred — requires careful
async refactor of LoadThumbnailAsync. Tracked separately for Phase 3 of memory sprint.
