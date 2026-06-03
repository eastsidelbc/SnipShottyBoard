# LEARNING.md
# ============================================================
# Concepts Explained — SnipShottyBoard
# Location: docs/LEARNING.md
# Updated: end of each session with new concepts covered
# ============================================================

---

## WPF FUNDAMENTALS

### XAML vs Code-Behind
```
Learned: 2024 (initial build)

Analogy:
Like HTML and JavaScript. XAML defines what things look like
and where they are. The code-behind (.xaml.cs) makes them
actually do things.

Technical:
XAML (eXtensible Application Markup Language) is XML that
defines WPF UI structure and appearance. Every XAML file has
a paired .xaml.cs "code-behind" file that contains the C# logic.

In SSB:
MainWindow.xaml = the layout (tab strip, content area, status bar)
MainWindow.xaml.cs = the logic (save, load, orchestrate managers)
```

### DynamicResource vs StaticResource
```
Learned: 2024

Analogy:
StaticResource = reading a book once and memorizing it.
DynamicResource = reading the book every time you need it.
If the book changes — StaticResource still has the old text.
DynamicResource always has the current text.

Technical:
StaticResource resolves once at load time — doesn't update.
DynamicResource resolves every time it's accessed — updates
when the resource changes. Used for theme switching because
when we swap the theme dictionary, all DynamicResource
references automatically pick up the new values.

In SSB:
{DynamicResource AccentBrush} in XAML updates immediately
when the theme changes from Dark to Light.
Never use StaticResource for theme colors.
```

### Visual Tree and Coordinate Transforms
```
Learned: From bug fix (commit dc13206)

Analogy:
Like nested boxes. Each box's coordinates are relative
to its own top-left corner, not the screen. To get the
screen position of something deep in nested boxes, you
have to add up all the offsets up the chain.

Technical:
WPF has a visual tree — every control is nested inside
another. element.TranslatePoint(point, ancestor) converts
a point from the element's coordinate space to the ancestor's
coordinate space. TransformToAncestor(ancestor) does the same.

Critical rule in SSB:
Always use MainWindow as the common ancestor for all
coordinate transforms. Using a different ancestor that
isn't actually above the element = crash.
Bug history: This was the exact bug fixed in dc13206.
```

### DispatcherTimer
```
Learned: Early build

Analogy:
Like a kitchen timer that calls a function when it rings.
Unlike System.Timers.Timer, it rings on the UI thread
so you can safely update the UI from its callback.

Technical:
WPF is single-threaded for UI. DispatcherTimer fires its
Tick event on the UI (dispatcher) thread, making it safe
to update UI elements directly. System.Timers.Timer fires
on a background thread — updating UI from there = crash.

In SSB:
autoSaveTimer — fires every 5 seconds → saves all notes
statusTimer — fires every 1 second → updates the clock
WindowPositionTracker uses DispatcherTimer for debouncing.
```

---

## C# CONCEPTS

### Nullable Reference Types
```
Learned: v1.6.0 work

Analogy:
Like being required to check if a package was delivered
before opening it. Nullable reference types force you to
check if something is null before using it.

Technical:
<Nullable>enable</Nullable> in the csproj turns on nullable
reference type warnings. The compiler warns when you might
use a null value. The ? suffix means "this can be null".
The ! suffix means "I'm sure this isn't null, trust me".

In SSB:
string? means the string can be null.
string means the compiler expects it to not be null.
262 current warnings are mostly nullable warnings.
```

### Static vs Instance Classes
```
Learned: From reading DataManager.cs

Analogy:
Static class = a whiteboard in the hallway everyone shares.
Instance class = a personal notepad only you have.
Everyone reads/writes the same whiteboard.

Technical:
Static classes can't be instantiated — they have one shared
state for the whole app. Instance classes create separate
objects with their own state.

In SSB:
DataManager is static — one shared data persistence layer.
LoggingService is static — one shared logger.
NoteWindowManager is a singleton — technically an instance
but enforced to only ever create one.
TabManager is an instance — each window has its own.
```

### Events in C#
```
Learned: From TabManager architecture

Analogy:
Like a doorbell. You install the doorbell (subscribe to event).
When someone presses it, everyone subscribed hears it.
The person pressing the button doesn't know or care who hears.

Technical:
Events decouple the component that fires them from the
component that handles them. TabManager fires OnDataChanged.
MainWindow subscribes to it. TabManager doesn't know
MainWindow exists — it just fires the event.
This is called loose coupling.

In SSB:
TabManager.OnDataChanged → MainWindow marks hasUnsavedChanges
TabManager.OnStatusUpdateRequested → MainWindow updates status bar
TextSection.OnDataChanged → NoteTab → TabManager → MainWindow
```

---

## WPF PATTERNS USED IN SSB

### Manager Pattern
```
Learned: From architecture audit

Analogy:
Like different departments in a company.
The HR department handles HR. IT handles IT.
Nobody crosses into another department's business.

Technical:
Each Manager class owns exactly one concern:
TabManager = all tab operations
ThemeManager = theme switching
DataManager = data persistence
StatusBarManager = status bar display
None of them reach into each other's responsibilities.
They communicate via events (see Events concept above).

In SSB:
MainWindow creates and wires all the managers.
MainWindow is the orchestrator that connects them.
Managers are injected into MainWindow, not created globally.
```

### Atomic File Writes
```
Learned: From AtomicFileManager.cs

Analogy:
Like saving a backup of a document before overwriting it.
If the power goes out mid-save, you still have the backup.

Technical:
Write to a temporary file first.
Copy the current file to a backup.
Then use File.Replace() to swap the temp file in.
This is atomic on most filesystems — either the old version
exists or the new version exists, never a corrupt half-written file.

In SSB:
AtomicFileManager wraps every JSON save.
20 rolling backups kept in AppData.
.bak file = immediate pre-save backup.
.info file = verification metadata.
```

---

## .NET 8 CONCEPTS

### Single-File Publish
```
Learned: From scripts/publish.ps1

Analogy:
Like zipping your entire application into one portable file.
No installer needed. Copy the exe to any Windows machine
and it just runs — .NET runtime included.

Technical:
dotnet publish -r win-x64 /p:PublishSingleFile=true
/p:SelfContained=true bundles the .NET runtime + all
dependencies into a single executable. The exe is larger
(~60-80MB) but requires nothing pre-installed.

In SSB:
scripts/publish.ps1 builds this way.
The resulting exe is in bin/Release/net8.0-windows/win-x64/publish/
```

---

## CONCEPTS TO COVER

Things Jeremy should learn as they come up:

- [ ] MVVM pattern — what ViewModels are and why SSB doesn't use them
- [ ] WPF data binding — how {Binding} works vs code-behind updates
- [ ] IDisposable pattern — why TabManager implements it
- [ ] async/await in WPF — why some things use Task.Run
- [ ] ObservableCollection — why NoteWindowManager uses it
- [ ] WPF WrapPanel — how tab multi-row wrapping works
- [ ] DI containers — why the roadmap mentions Prism
- [ ] Rolling file logs — how Serilog manages log rotation
