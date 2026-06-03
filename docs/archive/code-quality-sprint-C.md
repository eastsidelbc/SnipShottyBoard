# Code Quality Sprint C — Performance, Simplifications, FluentWindow, Warnings, Security
# Issues: §13, §16, §17, §20, §25, §27
# Apply after Sprint A and Sprint B pass build.

Read every file listed before touching anything.

## RULES
- One fix at a time, build after each
- No behavior changes beyond what's specified  
- Build gate: `dotnet build` — 0 errors, 0 warnings after every fix
- Write devnote after all fixes pass build (format at bottom)

---

## FIX 1 — Cache the error brush in StatusBarManager.ShowSaveError()
**File:** `UI/StatusBarManager.cs` — `ShowSaveError()`
**Finding:** §13 Performance

The save/unsaved brushes are already correctly cached as static frozen fields. ✅
BUT `ShowSaveError()` creates `new SolidColorBrush(Color.FromRgb(244, 67, 54))` on
every call. Add a third cached brush:

```csharp
// Add to existing static fields:
private static readonly SolidColorBrush _errorBrush;

// Add to static constructor alongside _savedBrush and _unsavedBrush:
_errorBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
_errorBrush.Freeze();

// In ShowSaveError(), change:
// Old:
var errorBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
errorBrush.Freeze();
saveStatus.Foreground = errorBrush;

// New:
saveStatus.Foreground = _errorBrush;
```

---

## FIX 2 — GIF pause DispatcherTimer: reuse field instead of new on every click
**File:** `UI/ImageViewerWindow.xaml.cs` — `DisplayImage_MouseLeftButtonUp()`
**Finding:** §16 Simplifications

A new `DispatcherTimer` is created on every GIF pause/play click. Add a class-level
field and reuse it:

```csharp
// Add field to class:
private DispatcherTimer? _gifStatusTimer;

// In DisplayImage_MouseLeftButtonUp(), replace timer creation:
// Old:
DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
timer.Tick += (s, args) => {
    timer.Stop();
    StatusZoom.Text = prevZoomText;
    UpdateStatusZoom();
};
timer.Start();

// New:
_gifStatusTimer?.Stop();
if (_gifStatusTimer == null)
{
    _gifStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
    _gifStatusTimer.Tick += (s, args) =>
    {
        _gifStatusTimer.Stop();
        UpdateStatusZoom(); // restores zoom text from current state
    };
}
_gifStatusTimer.Start();
```

The `prevZoomText` capture is no longer needed — `UpdateStatusZoom()` reads from
current state, which is correct since pause/play already toggled `isGifPaused`.

---

## FIX 3 — NoteWindowManager.SaveNoteWindows: eliminate read round-trip
**File:** `Core/Managers/NoteWindowManager.cs` — `SaveNoteWindows()`
**Finding:** §16 Simplifications

Every `SaveNoteWindows()` call reads `master.json` from disk before writing it back.
This happens on every autosave (every 5 seconds). The read is only needed to preserve
the `Settings` section — but Settings can be cached from the first load.

```csharp
// Add field to NoteWindowManager:
private AppSettings? _cachedSettings;

// Update LoadNoteWindows() to cache settings:
private void LoadNoteWindows()
{
    try
    {
        var master = DataManager.LoadMasterData();
        _cachedSettings = master.Settings; // cache for future saves
        // ... rest unchanged
    }
    catch { ... }
}

// Update SaveNoteWindows() to avoid re-reading:
public void SaveNoteWindows()
{
    try
    {
        var master = new MasterData
        {
            Windows = NoteWindows.ToList(),
            Settings = _cachedSettings ?? new AppSettings()
        };
        DataManager.SaveMasterData(master);
    }
    catch (Exception ex)
    {
        LoggingService.LogErrorStatic("Error saving note windows", ex, "Data");
    }
}
```

Also replace the two `Debug.WriteLine` calls in `NoteWindowManager` with
`LoggingService.LogDebugStatic(...)` and `LoggingService.LogErrorStatic(...)`.

---

## FIX 4 — MediaReference.FullPath: cache the images folder path
**File:** `Core/Models/MediaReference.cs` — `FullPath` getter
**Finding:** §16 Simplifications

Every `FullPath` access calls `Environment.GetFolderPath()` (a P/Invoke system call)
and two `Path.Combine()` allocations. The images folder never changes during a session.
Cache it as a static field:

```csharp
// Old:
public string FullPath
{
    get
    {
        var imagesFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SnipShottyBoard", "images");
        return Path.Combine(imagesFolder, Filename);
    }
}

// New:
private static readonly string _imagesFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "SnipShottyBoard", "images");

public string FullPath => Path.Combine(_imagesFolder, Filename);
```

`DataManager.GetImagesFolder()` already uses the same path — confirm they match.

---

## FIX 5 — Add WindowCornerPreference="Round" to windows missing it
**Files:** `UI/ImageViewerWindow.xaml`, `UI/Views/SettingsWindow.xaml`, `UI/NoteListWindow.xaml`
**Finding:** §17 FluentWindow Gaps

Read each XAML file. On the root `<ui:FluentWindow ...>` element, check for
`ui:WindowHelper.WindowCornerPreference`. If missing, add it:

```xaml
<!-- Add to FluentWindow element if not present: -->
ui:WindowHelper.WindowCornerPreference="Round"
```

Check `MainWindow.xaml` first to see the correct attribute syntax already used there,
then mirror it on the three other windows. Do not change any other attributes.

---

## FIX 6 — CS8620 nullable fix in MediaSection
**File:** `UI/MediaSection.xaml.cs` — line ~1783
**Finding:** §20 Compiler Warnings

`List<string?>` passed where `List<string>` expected. Find the line flagged by CS8620.
It will be a LINQ query selecting paths from containers that may contain null values.

Filter nulls before passing:
```csharp
// Old (produces CS8620 — string? in List<string> context):
.Select(container => (container.Tag as MediaReference)?.FullPath)
.Where(path => !string.IsNullOrEmpty(path))
.ToList()

// New (explicit non-null projection):
.Select(container => (container.Tag as MediaReference)?.FullPath)
.Where(path => !string.IsNullOrEmpty(path))
.Select(path => path!)   // safe: Where already filtered nulls/empty
.ToList()
```

After fixing CS8620, check `dotnet build` output and address any additional nullable
warnings that are straightforward (missing `?` annotations, null-forgiving operators
on already-guarded values). Do NOT fix warnings that require logic changes — only
annotation fixes.

---

## FIX 7 — Enforce MaxTabs limit in TabManager.CreateNewTab()
**File:** `UI/TabManager.cs` — `CreateNewTab()`
**Finding:** §27 Security + Stability

`appSettings.MaxTabs` is stored but never checked. A user with `MaxTabs = 20` can
create unlimited tabs — memory grows without bound.

Add a guard at the top of `CreateNewTab()`:

```csharp
public void CreateNewTab()
{
    // Enforce MaxTabs limit from settings
    if (appSettings != null && tabs.Count >= appSettings.MaxTabs)
    {
        CustomDialog.ShowInformation(
            Application.Current.MainWindow,
            $"Maximum of {appSettings.MaxTabs} tabs reached.\n\nClose a tab before creating a new one.",
            "Tab Limit Reached",
            "📋");
        return;
    }

    try
    {
        // ... existing implementation unchanged
    }
}
```

---

## AFTER ALL 7 FIXES PASS BUILD

Write devnote to:
`docs/devnotes/2026-05-19-code-quality-sprint-C.md`

```
# Code Quality Sprint C — Performance, Simplifications, FluentWindow, Warnings, Security
Date: 2026-05-19

## Fixes

FIX-1: StatusBarManager._errorBrush cached as static frozen field (was new per ShowSaveError call)
FIX-2: ImageViewerWindow GIF status timer reused via _gifStatusTimer field (was new per click)
FIX-3: NoteWindowManager.SaveNoteWindows no longer reads master.json before writing
        Settings cached from initial load — saves ~1 disk read per autosave cycle
        Debug.WriteLine replaced with LoggingService
FIX-4: MediaReference.FullPath caches images folder in static field
        Eliminates P/Invoke system call + 2 allocations on every FullPath access
FIX-5: WindowCornerPreference="Round" added to ImageViewerWindow, SettingsWindow, NoteListWindow
FIX-6: CS8620 fixed in MediaSection — .Select(path => path!) added after null filter
        Additional straightforward nullable annotation warnings cleaned up
FIX-7: TabManager.CreateNewTab() now enforces appSettings.MaxTabs
        Shows CustomDialog when limit reached, returns early

## Build status
0 errors, 0 warnings

## Next
Code Quality Sprint D: docs/code-quality-sprint-D.md
```
