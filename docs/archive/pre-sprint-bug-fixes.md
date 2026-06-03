# Pre-Sprint Bug Fixes — 5 Confirmed Issues
# All pre-confirmed from audit. Apply before next feature sprint.
# Read each file listed before touching anything.

## RULES
- Read every file IN FULL before editing it
- One fix at a time, build after each
- No refactoring outside the exact scope described
- No behavior changes beyond what's specified
- Build gate: `dotnet build` — 0 errors, 0 warnings after every fix
- Write ONE devnote after all 5 fixes pass build (format at bottom)

---

## FIX 1 — Save failure silently swallowed; user sees "Saved" when data never written
**Files:** `Core/Managers/DataManager.cs` — `SaveMasterData()`
**File:** `UI/Views/MainWindow.xaml.cs` — `SaveApplicationData()`
**Finding:** §14 Error Handling

### Root cause
`SaveMasterData()` calls `AtomicFileManager.AtomicSave()` which returns `false` on failure.
`SaveMasterData` logs the error but does NOT throw. `SaveApplicationData()` calls
`noteManager.SaveNoteWindows()` → `SaveMasterData()`, gets no exception, continues,
sets `hasUnsavedChanges = false`, updates status bar to "Saved". Data was never written.
User has no idea the save failed.

### Fix A — SaveMasterData must propagate failure
In `DataManager.SaveMasterData()`, change the `else` branch when `AtomicSave` returns false:

```csharp
// Old:
else
{
    LoggingService.LogErrorStatic("AtomicSave returned false for master data", null, "Data", ...);
}

// New:
else
{
    LoggingService.LogErrorStatic("AtomicSave returned false for master data", null, "Data", ...);
    throw new IOException($"Failed to save master data to {MasterFilePath} — AtomicSave returned false");
}
```

### Fix B — SaveApplicationData must not mark clean on failure
`SaveApplicationData()` already has a try/catch. The catch currently only logs.
Change it to keep `hasUnsavedChanges = true` and show a status bar warning:

```csharp
// Old catch:
catch (Exception ex)
{
    loggingService.LogError("Error saving app data", ex, "Data");
}

// New catch:
catch (Exception ex)
{
    loggingService.LogError("Error saving app data", ex, "Data");
    hasUnsavedChanges = true; // Do NOT mark clean — save failed
    // Show failure in status bar (don't use MessageBox — too disruptive for background saves)
    statusBarManager?.ShowSaveError(); // See Fix B note below
}
```

If `StatusBarManager` doesn't have `ShowSaveError()`, add a method that temporarily
sets the save status label to "⚠️ Save failed" for 5 seconds, then reverts.
Match existing patterns in StatusBarManager for how save status is displayed.

---

## FIX 2 — Recovery merges by tab title; breaks when two tabs share a name
**File:** `Core/Managers/DataManager.cs` — `MergeRecoveryIntoSaved()`
**Finding:** §14 Error Handling

### Root cause
```csharp
var savedNote = savedWindow.Notes.FirstOrDefault(
    n => !string.IsNullOrEmpty(n.Title) && n.Title == recNote.Title);
```
Two tabs named "Note 1" → FirstOrDefault returns the first match. Wrong tab gets
recovered text. The second "Note 1" is never updated.

### Fix — merge by index with title as fallback
Replace the note-matching logic inside the window merge loop:

```csharp
// Old:
var savedNote = savedWindow.Notes.FirstOrDefault(
    n => !string.IsNullOrEmpty(n.Title) && n.Title == recNote.Title);

// New:
// Try by index first (most reliable for same-named tabs)
var recIndex = recWindow.Notes.IndexOf(recNote);
SavedNote savedNote = null;

if (recIndex >= 0 && recIndex < savedWindow.Notes.Count)
{
    // Index match — verify title roughly matches as a sanity check
    var candidate = savedWindow.Notes[recIndex];
    if (candidate.Title == recNote.Title)
        savedNote = candidate;
}

// Fallback: title match (handles cases where tabs were added/removed between save and crash)
if (savedNote == null)
    savedNote = savedWindow.Notes.FirstOrDefault(
        n => !string.IsNullOrEmpty(n.Title) && n.Title == recNote.Title);
```

No other changes to the merge logic needed.

---

## FIX 3 — Delete confirmation re-enabling in Settings has no effect
**File:** `UI/TabManager.cs` — `IsDeleteConfirmationDisabled`, `UpdateSettings()`
**Finding:** §21 State Management

### Root cause
```csharp
public bool IsDeleteConfirmationDisabled =>
    (appSettings != null && !appSettings.ConfirmTabDeletion) || skipDeleteConfirmation;
```

User clicks "don't ask again" → both `skipDeleteConfirmation = true` AND
`appSettings.ConfirmTabDeletion = false`. User opens Settings, re-enables confirmation →
`appSettings.ConfirmTabDeletion = true`. BUT `skipDeleteConfirmation` is still `true`.
`IsDeleteConfirmationDisabled` = `(false) || (true)` = `true`. Re-enabling broken.

### Fix — sync skipDeleteConfirmation when settings are updated
In `UpdateSettings()`, after updating `this.appSettings`, add:

```csharp
public void UpdateSettings(AppSettings? settings)
{
    if (settings == null) { ... return; }

    this.appSettings = settings;

    // If settings explicitly re-enable confirmation, clear the in-session skip flag too.
    // Without this, the OR condition in IsDeleteConfirmationDisabled means re-enabling
    // in Settings has no effect for the rest of the session.
    if (settings.ConfirmTabDeletion)
        skipDeleteConfirmation = false;

    OnLogDebug?.Invoke($"⚙️ TabManager settings updated - ConfirmTabDeletion: {settings.ConfirmTabDeletion}", string.Empty);
}
```

No other changes needed. `ResetDeleteConfirmationPreference()` already handles the
explicit reset path correctly.

---

## FIX 4 — Two UX bugs: Prev/Next buttons hidden + Vault grace period mismatch
**Finding:** §15 UX Behavior

### Fix 4A — Vault cleanup passes daysGracePeriod: 0 despite dialog saying 24h
**File:** `UI/Views/MainWindow.xaml.cs` — `ShowVaultAudit()`

The dialog message says:
  `"(Files within 24h grace period will also be deleted)"`
But the call is:
  `DataManager.CleanupOrphanedImages(daysGracePeriod: 0)`

`daysGracePeriod: 0` deletes ALL orphaned files immediately, ignoring the grace period
that `DataManager.CleanupOrphanedImages` supports. User is told files in the 24h window
are safe, but they're deleted anyway.

Either: respect the grace period OR update the dialog text to be honest.
The right fix is to respect it:

```csharp
// Old:
var deleted = SnipShottyBoard.Core.Managers.DataManager.CleanupOrphanedImages(daysGracePeriod: 0);

// New:
var deleted = SnipShottyBoard.Core.Managers.DataManager.CleanupOrphanedImages(daysGracePeriod: 1);
```

Also update the dialog message to be accurate — remove the "(Files within 24h grace
period will also be deleted)" line since grace IS now respected:

```csharp
// Old confirmation message last line:
$"(Files within 24h grace period will also be deleted)"

// New:
$"(Files added in the last 24h are protected and will NOT be deleted)"
```

### Fix 4B — Prev/Next navigation buttons permanently Collapsed
**File:** `UI/ImageViewerWindow.xaml` and `UI/ImageViewerWindow.xaml.cs`

Read `ImageViewerWindow.xaml` to find the Prev/Next button XAML elements.
They are set to `Visibility="Collapsed"` and `UpdateNavigationButtons()` was never
implemented. Navigation works via keyboard (arrow keys) but the UI buttons are invisible.

Steps:
1. In `ImageViewerWindow.xaml`: Set Prev/Next buttons to `Visibility="Visible"`.
   If the buttons don't exist in XAML yet, do NOT add them — confirm they exist first.

2. In `ImageViewerWindow.xaml.cs`: Find or add `UpdateNavigationButtons()`:
```csharp
private void UpdateNavigationButtons()
{
    bool hasMultiple = allImagePaths != null && allImagePaths.Count > 1;
    PrevButton.Visibility = hasMultiple ? Visibility.Visible : Visibility.Collapsed;
    NextButton.Visibility = hasMultiple ? Visibility.Visible : Visibility.Collapsed;
}
```

3. Call `UpdateNavigationButtons()` in:
   - `LoadImage()` — after setting `currentImagePath`
   - Constructor — after setting `allImagePaths`

Button names (`PrevButton`, `NextButton`) — verify exact x:Name from the XAML before
wiring. Do not assume names.

---

## FIX 5 — Migration doesn't fix ThumbnailSize = 0 on legacy MediaReference objects
**File:** `Core/Schema/MigrationService.cs` — `MigrateNoteToCurrent()`
**Finding:** §25 Data Migration

### Root cause
When v2 → v3 migration runs, the comment says:
  `// ThumbnailSizeDefault, IsHidden=false, etc. — already set in MediaReference ctor`

This is WRONG. JSON deserialization sets fields from the JSON value, overwriting
constructor defaults. If old JSON has `"thumbnailSize": 0` (or the field is missing
and defaults to 0 in the JSON), deserialization produces `ThumbnailSize = 0`.
The migration never fixes it. Thumbnails render zero-width on any pre-v3 note.

### Fix — explicitly correct ThumbnailSize in migration
In `MigrateNoteToCurrent()`, inside the `if (note.DataVersion < CurrentNoteSchemaVersion)` block,
before bumping `note.DataVersion`, add:

```csharp
// Fix ThumbnailSize = 0 from pre-v3 data (JSON deserialization overwrites ctor defaults)
foreach (var media in note.Media)
{
    if (media.ThumbnailSize <= 0)
        media.ThumbnailSize = AppConstants.ThumbnailSizeMedium; // default: 100px
}
```

Verify that `AppConstants.ThumbnailSizeMedium` exists and is the correct default (100).
If the constant name differs, use whatever constant the rest of the codebase uses for
the default thumbnail size.

---

## AFTER ALL 5 FIXES PASS BUILD

Write devnote to:
`docs/devnotes/2026-05-19-pre-sprint-bug-fixes.md`

```
# Pre-Sprint Bug Fixes — 5 Audit Findings
Date: 2026-05-19

## Fixes Applied

FIX-1a: SaveMasterData now throws IOException when AtomicSave returns false
         — save failure propagates up instead of being swallowed silently
FIX-1b: SaveApplicationData catch block keeps hasUnsavedChanges = true on failure
         — status bar shows save error instead of "Saved"
         Files: DataManager.cs, MainWindow.xaml.cs, StatusBarManager.cs

FIX-2:  Recovery merge now matches notes by index first, title as fallback
         — two tabs with same name no longer corrupt each other during recovery
         File: DataManager.cs — MergeRecoveryIntoSaved()

FIX-3:  UpdateSettings() now resets skipDeleteConfirmation when ConfirmTabDeletion = true
         — re-enabling delete confirmation in Settings now actually works
         File: TabManager.cs — UpdateSettings()

FIX-4a: Vault cleanup now passes daysGracePeriod: 1 (was 0)
         — dialog text and actual behavior now match (24h grace period respected)
         File: MainWindow.xaml.cs — ShowVaultAudit()

FIX-4b: Prev/Next navigation buttons made visible + UpdateNavigationButtons() implemented
         — buttons now show/hide based on whether multiple images are in the list
         Files: ImageViewerWindow.xaml, ImageViewerWindow.xaml.cs

FIX-5:  MigrateNoteToCurrent() now explicitly sets ThumbnailSize = ThumbnailSizeMedium
         when ThumbnailSize <= 0 during v2→v3 migration
         — legacy notes no longer render zero-width thumbnails
         File: MigrationService.cs — MigrateNoteToCurrent()

## Build status
0 errors, 0 warnings

## Notes
FIX-8 (Dispatcher.Invoke blocking inside Task.Run) deferred — requires careful
async refactor of LoadThumbnailAsync. Tracked separately for Phase 3 of memory sprint.
```
