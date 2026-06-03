# Code Quality Sprint B — DataManager Init Order + Window ID
# Issues: §22, §23 from audit
# Part 2 of 2 — apply after Sprint A passes build

These two fixes are structural. Read every file listed IN FULL before touching anything.
Sprint A must be complete and building clean before starting this.

## RULES
- Read every file before editing
- Build after EACH fix — these are independent, order doesn't matter
- Do NOT change any save/load behavior or data format
- Do NOT change NoteWindowManager.Instance or the Singleton pattern
- Build gate: `dotnet build` — 0 errors, 0 warnings after every fix
- Write devnote after both fixes pass build (format at bottom)

---

## FIX 1 — DataManager static constructor does file I/O — errors masked as TypeInitializationException
**Files:** `Core/Managers/DataManager.cs`, `App.xaml.cs`
**Finding:** §22 Initialization Order

### Root cause
`DataManager`'s static constructor calls `EnsureDirectoryExists()` and
`MigrateToMasterIfNeeded()`. Both do file I/O. If either throws (permissions error,
disk full, antivirus lock), .NET wraps it as `TypeInitializationException` with the
real exception buried inside. The global exception handler in App.xaml.cs catches a
confusing wrapper instead of a meaningful error. Every `DataManager.X` call after a
failed static init also throws the same opaque exception forever.

### Fix — move I/O out of static constructor into explicit Initialize()

**Step 1 — DataManager.cs:** Remove the two method calls from the static constructor.
Add a public static `Initialize()` method:

```csharp
// Old static constructor:
static DataManager()
{
    EnsureDirectoryExists();
    MigrateToMasterIfNeeded();
}

// New static constructor — path setup only, no I/O:
static DataManager()
{
    // Path constants only — no file I/O here.
    // File I/O happens in Initialize(), called explicitly from App.OnStartup.
}

// New method — called once from App.OnStartup:
public static void Initialize()
{
    EnsureDirectoryExists();
    MigrateToMasterIfNeeded();
}
```

Keep `EnsureDirectoryExists()` and `MigrateToMasterIfNeeded()` exactly as they are.
Only move the call site.

**Step 2 — App.xaml.cs:** Call `DataManager.Initialize()` as the FIRST thing in
`OnStartup`, before `LoggingService` initialization and before `TryRestoreFromRecovery`:

```csharp
// Old OnStartup:
protected override void OnStartup(StartupEventArgs e)
{
    loggingService = new LoggingService();
    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    ...
    if (DataManager.TryRestoreFromRecovery()) { ... }
    ...
    base.OnStartup(e);
}

// New OnStartup:
protected override void OnStartup(StartupEventArgs e)
{
    // Initialize DataManager first — creates app directories and runs one-time migration.
    // Must be explicit (not static ctor) so errors surface as real exceptions, not
    // TypeInitializationException. Logging not yet available — errors go to MessageBox.
    try
    {
        DataManager.Initialize();
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"Failed to initialize application data folder:\n{ex.Message}\n\n" +
            $"Check that the app has write access to %AppData%\\SnipShottyBoard.",
            "Startup Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        // Shutdown gracefully — can't run without data folder
        this.Shutdown(1);
        return;
    }

    loggingService = new LoggingService();
    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    this.DispatcherUnhandledException += OnDispatcherUnhandledException;
    loggingService.LogInfo("🚀 Application starting with global exception handling", "Lifecycle");

    if (DataManager.TryRestoreFromRecovery())
    {
        loggingService.LogInfo("💾 Unsaved text recovered silently on startup", "Lifecycle");
    }

    // ... rest of OnStartup unchanged
    base.OnStartup(e);
}
```

**Verify:** No other code references `DataManager.EnsureDirectoryExists()` or
`DataManager.MigrateToMasterIfNeeded()` directly (they should be private). The static
path field initializers (`MasterFilePath`, `ImagesFolder`, etc.) are fine in the static
initializer — they only compute strings, no I/O.

---

## FIX 2 — Window identification uses WPF Tag property; primary window never tagged
**Files:** `UI/NoteListWindow.xaml.cs`, `UI/Views/MainWindow.xaml.cs`
**Finding:** §23 Multi-Window Correctness

### Root cause
Window identity is tracked via `mainWindow.Tag = windowId.ToString()`.
`WPF.Tag` is an untyped `object` property designed for arbitrary user data — it's not
intended as a window identifier. More critically: the PRIMARY MainWindow created at
startup is never assigned a `Tag`. NoteListWindow's duplicate detection searches:

```csharp
if (window is MainWindow mainWindow && mainWindow.Tag?.ToString() == windowId.ToString())
```

The primary window has `Tag = null`, so this condition is never true for it. Clicking
the primary window in NoteListWindow opens a SECOND MainWindow for the same data
instead of focusing the existing one.

### Fix — expose WindowId as a typed property on MainWindow

**Step 1 — MainWindow.xaml.cs:** Add a public property that exposes the window's
data ID directly. `WindowData` is already set in the constructor:

```csharp
// Add property to MainWindow (alongside existing WindowData property):
public Guid WindowId => WindowData?.Id ?? Guid.Empty;
```

No other changes to MainWindow.

**Step 2 — NoteListWindow.xaml.cs:** Replace all `Tag`-based lookups with `WindowId`:

```csharp
// In OpenNoteWindow() — duplicate detection:
// Old:
foreach (Window window in Application.Current.Windows)
{
    if (window is MainWindow mainWindow && mainWindow.Tag?.ToString() == windowId.ToString())
    {
        window.Activate();
        window.Focus();
        return;
    }
}

// New:
foreach (Window window in Application.Current.Windows)
{
    if (window is MainWindow mainWindow && mainWindow.WindowId == windowId)
    {
        window.Activate();
        window.Focus();
        return;
    }
}

// In OpenNoteWindow() — window creation (remove Tag assignment entirely):
// Old:
var noteWindow = new MainWindow(windowData);
noteWindow.Tag = windowId.ToString(); // ← DELETE THIS LINE
noteWindow.Title = windowData.Title;

// New:
var noteWindow = new MainWindow(windowData);
noteWindow.Title = windowData.Title;
// WindowId comes from WindowData.Id automatically via the new property
```

```csharp
// In RenameNoteWindow() — finding open window to update title:
// Old:
if (window is MainWindow mainWindow && mainWindow.Tag?.ToString() == windowId.ToString())

// New:
if (window is MainWindow mainWindow && mainWindow.WindowId == windowId)
```

```csharp
// In CloseNoteWindow() — finding window to close:
// Old:
if (window is MainWindow mainWindow && mainWindow.Tag?.ToString() == windowId.ToString())

// New:
if (window is MainWindow mainWindow && mainWindow.WindowId == windowId)
```

**Verify:** Search the entire codebase for `.Tag` assignments and reads on `MainWindow`.
Remove any remaining `mainWindow.Tag = ...` assignments. The `Tag` property on other
controls (tab buttons, media containers, etc.) is unrelated — leave those alone.

---

## AFTER BOTH FIXES PASS BUILD

Write devnote to:
`docs/devnotes/2026-05-19-code-quality-sprint-B.md`

```
# Code Quality Sprint B — Init Order + Window Identity
Date: 2026-05-19

## Fixes

FIX-1: DataManager static constructor stripped of all file I/O
       New DataManager.Initialize() called explicitly from App.OnStartup
       Startup failure now shows meaningful MessageBox with actionable message
       instead of TypeInitializationException wrapping the real cause
       Files: DataManager.cs, App.xaml.cs

FIX-2: Window identification migrated from WPF Tag property to typed WindowId property
       MainWindow.WindowId = WindowData.Id (Guid, not string)
       Primary window (no Tag previously) now correctly identified in all lookups
       NoteListWindow duplicate detection, rename, and close all use WindowId
       Files: MainWindow.xaml.cs, NoteListWindow.xaml.cs

## Build status
0 errors, 0 warnings

## Impact
- Startup errors now diagnosable (real exception visible, not TypeInitializationException)
- Opening the primary window from NoteListWindow now focuses it instead of duplicating
- Rename and close from NoteListWindow now correctly targets primary window
```
