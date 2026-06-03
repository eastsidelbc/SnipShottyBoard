# SNIPSHOTTYBOARD — DEEP CODEBASE AUDIT REPORT
**Generated:** 2026-04-23  
**Auditor:** Cursor AI (Sonnet 4.6)  
**Purpose:** Complete architectural understanding for external planning and feature development  
**Scope:** Every file, folder, and line examined systematically  

---

## ⚠️ CRITICAL — READ THIS FIRST

**THE PROJECT DOES NOT BUILD IN ITS CURRENT STATE.**

The latest commit (`ec5d913 — "phases 0-5"`, Nov 23 2025) introduced `using` references to three namespaces and classes that **do not exist as files anywhere in the codebase**:

| Missing Class | Referenced Namespace | Used In |
|---|---|---|
| `MigrationService` | `SnipShottyBoard.Core.Schema` | `Core/Managers/DataManager.cs` |
| `PathSanitizer` | `SnipShottyBoard.Infrastructure.Helpers` | `Core/Managers/DataManager.cs`, `Core/Managers/AtomicFileManager.cs`, `UI/MediaSection.xaml.cs` |
| `WindowPositionTracker` | `SnipShottyBoard.Core.Utils` | `UI/Views/MainWindow.xaml.cs` |

No corresponding `.cs` files exist. No corresponding folders exist. A `dotnet build` confirms: **Build FAILED** (plus an unrelated NuGet network error in the audit environment — the code errors are separate).

This must be resolved before any other development work.

**Also:** `docs/CR.md` was **deleted** in this same commit. The old `.cursorrules` file references CR.md as the normative source of truth. That reference is now broken.

---

## 1. WHAT IS THIS PROJECT?

### In Plain English

SnipShottyBoard (SSB) is a **desktop sticky notes application** for Windows. You open it and see a small floating window with tabs at the top. Each tab is an independent note. In each note you can:
- Type text (with rich formatting — bold, italic, bullets, numbered lists)
- Paste screenshots or images with Ctrl+V
- View image thumbnails in a scrollable panel
- Double-click a thumbnail to view it full-screen

The window stays on your desktop. Notes save automatically every 5 seconds to JSON files in `%AppData%\SnipShottyBoard\`. When you close and reopen the app, everything is exactly where you left it — same tabs, same content, same window position and size.

### What Problem It Solves

It's a lightweight clipboard manager and quick-note tool. Instead of 20 browser tabs or a heavy note app, you keep SSB open in the corner. Screenshots go directly into notes. Text snippets go in without leaving what you're doing.

### What the User Actually Does

1. Launch `SnipShottyBoard.exe`
2. See a floating window with one tab ("Note 1")
3. Create new tabs with the `+` button or `Ctrl+T`
4. Type notes, paste images with `Ctrl+V`
5. Rename tabs by double-clicking them
6. Drag tabs to reorder them
7. Switch themes (light/dark) with the moon button
8. Adjust settings (font size, auto-save interval, etc.) with the ⚙️ button
9. Open multiple windows for organization (via the 📝 button)
10. The app auto-saves constantly. User rarely thinks about saving.

---

## 2. TECH STACK — COMPLETE

### Runtime and Framework

```
Language:          C# (nullable reference types enabled)
Runtime:           .NET 8 (net8.0-windows)
UI Framework:      WPF (Windows Presentation Foundation)
Target platform:   Windows 10/11 (TargetPlatformVersion 10.0)
Output type:       WinExe (windowed application, no console)
```

### NuGet Dependencies (all 3)

| Package | Version | What It Does |
|---|---|---|
| `WPF-UI` | 4.0.3 | Modern WPF controls library (used for `WindowChrome`, modern styling). Namespace: `http://schemas.lepo.co/wpfui/2022/xaml` |
| `Serilog.Sinks.File` | 6.0.0 | Structured logging to rolling daily log files in `%AppData%\SnipShottyBoard\logs\`. Provides `LoggerConfiguration`, `WriteTo.File`, etc. |
| `CommunityToolkit.Mvvm` | 8.4.0 | MVVM helpers (ObservableCollection, event utilities). Referenced in project but usage in source code is minimal — primarily `ObservableCollection<T>` in `NoteWindowManager.cs`. |

### Build Tooling

```
Build system:      dotnet SDK (Microsoft.NET.Sdk)
Project file:      SnipShottyBoard.csproj
Publish script:    scripts/publish.ps1 (single-file self-contained exe for win-x64)
Release script:    scripts/publish.release.ps1 (full release packaging)
```

The publish script compiles a **self-contained single-file executable** (no .NET runtime required on the target machine). Release artifacts go to `releases/vX.Y.Z/`.

### Data Storage

```
Format:            JSON (System.Text.Json — built into .NET 8, no package needed)
Location:          %AppData%\Roaming\SnipShottyBoard\
Files:
  notewindows.json    ← main note data (tabs per window, content, images)
  notes.json          ← legacy format (pre-multi-window, kept for migration)
  settings.json       ← user preferences
  images/             ← all pasted images stored as files
  logs/               ← daily Serilog log files
  *.bak               ← immediate backup before each save
  *-YYYYMMDD-HHmmss.json ← rolling backups (20 most recent kept)
  *.info              ← verification metadata sidecar files
```

### Theming

Two complete XAML resource dictionaries define all colors, brushes, and control styles:
- `Resources/Themes/DarkTheme.xaml` (~53KB)
- `Resources/Themes/LightTheme.xaml` (~53KB)

Default theme is Dark, loaded in `App.xaml`. Switching theme replaces the merged dictionary at runtime.

---

## 3. CURRENT STATE

### Version Status — THERE IS A DISCREPANCY

| File | Version |
|---|---|
| `SnipShottyBoard.csproj` | **1.6.0** |
| `VERSION` file | **1.5.0** |
| `docs/CHANGELOG.md` last entry | **[1.6.0] - 2025-11-23** |

The `.csproj` and CHANGELOG are in sync at 1.6.0. The `VERSION` file was not updated when the version bumped. This is a known inconsistency to fix.

### What Is Fully Working (as of last working commit — `769b74f`)

All features through v1.5.0 were working:
- ✅ Multi-tab notes (create, delete, rename, reorder via drag-and-drop)
- ✅ Rich text editing (bold, italic, underline, strikethrough, bullets, numbered lists)
- ✅ Image paste (Ctrl+V) with thumbnail previews
- ✅ Image drag-and-drop from desktop
- ✅ Full-screen image viewer with left/right arrow navigation
- ✅ Auto-save every 5 seconds (configurable)
- ✅ Dark / Light theme toggle
- ✅ Per-tab splitter position (text vs media ratio, persisted per tab)
- ✅ Always-on-top pin button (📌)
- ✅ Minimize button in titlebar
- ✅ Multi-row tab wrapping (tabs wrap to next row when window is narrow)
- ✅ Row-aware drag-and-drop for tabs
- ✅ Arrow key navigation between tabs (left/right/up/down/home/end)
- ✅ Settings window (font size, auto-save, theme, max tabs, etc.)
- ✅ Custom dialog system (styled confirmation/info/warning/error dialogs)
- ✅ Status bar (tab count, word count, save status, live clock)
- ✅ Help system (keyboard shortcuts guide)
- ✅ Multiple note windows (via 📝 button)
- ✅ Right-click tab context menu (rename, duplicate, delete)
- ✅ Keyboard shortcuts (Ctrl+T, Ctrl+W, Ctrl+Tab, F2, etc.)
- ✅ Window position/size memory
- ✅ Atomic file saves with rolling backups
- ✅ Orphaned image cleanup on startup
- ✅ Serilog structured logging

### What Is Broken / Incomplete (current HEAD — `ec5d913`)

❌ **PROJECT DOES NOT BUILD** — three missing class files (see Critical section above)

The following features were partially added in the broken commit and are incomplete:
- ❌ `MigrationService` — schema migration for data versioning (design intent: migrate old SavedNote format to new format)
- ❌ `PathSanitizer` — sanitize file paths before logging (design intent: prevent logging full system paths in logs)
- ❌ `WindowPositionTracker` — debounced window position saving (design intent: prevent disk I/O on every pixel of window drag)

These were planned as Phase 5 improvements. The code skeletons calling these classes were written but the implementation files were never created.

---

## 4. FOLDER AND FILE STRUCTURE — COMPLETE TREE

```
SnipShottyBoard/
│
├── .cursor/
│   └── rules/                      ← AI behavior rules (always apply)
│       ├── bug-protocol.mdc        ← Bug spiral prevention protocol
│       ├── building.mdc            ← Feature roadmap and code quality rules
│       ├── core.mdc                ← Jeremy's profile, communication style, role definitions
│       └── memory.mdc              ← Session memory, file system, compress context protocol
│
├── .cursorignore                   ← Tells Cursor what to ignore in indexing
├── .cursorrules                    ← OLD rules file (legacy) — references deleted CR.md
├── .gitignore
│
├── assets/
│   └── app.ico                     ← Application icon (410KB)
│
├── Core/                           ← Business logic layer
│   ├── Managers/
│   │   ├── AtomicFileManager.cs    ← Crash-safe file writes with rolling backups
│   │   ├── DataManager.cs          ← Central data persistence (save/load notes, windows, settings, images)
│   │   └── NoteWindowManager.cs    ← Manages multiple note window instances (singleton)
│   └── Models/
│       ├── AppSettings.cs          ← User preferences model (theme, font, auto-save, window state)
│       └── SavedNote.cs            ← Single note/tab data model (title, text, images, splitter ratio, order)
│
├── Data/
│   ├── AppConstants.cs             ← All magic numbers as named constants (TabMinWidth, etc.)
│   └── AppData.cs                  ← Top-level data container (Notes + Settings, for legacy load)
│
├── docs/
│   ├── BUGS.md                     ← Bug log template (not yet populated with WPF bugs)
│   ├── CHANGELOG.md                ← Full version history from 1.0.0 to 1.6.0 (accurate)
│   ├── DECISIONS.md                ← Decision log template (contains React placeholder content)
│   ├── devnotes/
│   │   ├── 2025-10-01-splitter-persist-and-titlebar-buttons.md
│   │   └── 2025-10-01-tabs-multiline-wrapping.md
│   ├── DEVNOTES.md                 ← Session diary template (not populated)
│   ├── LEARNING.md                 ← Concepts template (contains React placeholder content)
│   ├── LM_STUDIO_SYSTEM_PROMPTS.md ← System prompts for LM Studio planning brain
│   ├── MCP_SETUP.md                ← MCP server setup guide
│   ├── PLANNING.md                 ← Sprint planning template (empty/placeholder)
│   ├── PROJECT_MEMORY.md           ← Project memory template (contains React placeholders — NOT filled in)
│   ├── QUICK_CHECK.md              ← Fast context loading template
│   ├── README.md                   ← Project description (accurate WPF content)
│   ├── SESSION_END.md              ← End-of-session paste template
│   ├── SESSION_START.md            ← Start-of-session paste template
│   └── WORKFLOW_GUIDE.md           ← How the two-brain system works
│
├── Examples/
│   └── MediaSectionRefactored.cs   ← Reference example showing old vs new boilerplate patterns
│                                      (NOT compiled into the app — namespace is SnipShottyBoard.Examples)
│
├── Infrastructure/
│   ├── Diagnostics/
│   │   └── GifDiagnostics.cs       ← DEBUG-only GIF animation diagnostics (Serilog, #if DEBUG)
│   └── Logging/
│       └── LoggingService.cs       ← Serilog wrapper with categories (Info/Debug/Warning/Error/Static)
│
├── integrations/
│   └── mcp-servers/
│       ├── snipshottyboard-mcp.js  ← Node.js MCP server (read/write files, git, grep in project)
│       ├── package.json            ← MCP server package (ES module, @modelcontextprotocol/sdk)
│       ├── run-snipshottyboard-mcp.bat ← Launch script for MCP server
│       ├── SNIPSHOTTYBOARD-SETUP.md ← MCP setup instructions
│       └── claude_desktop_config_snipshottyboard.json ← Claude Desktop MCP config snippet
│
├── Resources/
│   └── Themes/
│       ├── DarkTheme.xaml          ← Complete dark theme (~53KB, all colors/styles)
│       └── LightTheme.xaml         ← Complete light theme (~53KB, all colors/styles)
│
├── scripts/
│   ├── publish.ps1                 ← Single-file win-x64 publish (basic)
│   ├── publish.release.ps1         ← Full release packaging (versioned folder, checksums, zip)
│   └── README.md                   ← Scripts documentation
│
├── UI/
│   ├── Views/
│   │   ├── MainWindow.xaml         ← Main window layout (custom chrome, tab strip, content area, status bar)
│   │   ├── MainWindow.xaml.cs      ← Main window orchestrator (~747 lines)
│   │   ├── NoteTab.xaml            ← Individual tab layout (TextSection + MediaSection in Grid with splitter)
│   │   ├── NoteTab.xaml.cs         ← Tab controller and event router (~303 lines)
│   │   ├── SettingsWindow.xaml     ← Settings UI (all preference controls)
│   │   └── SettingsWindow.xaml.cs  ← Settings logic (~428 lines)
│   ├── CustomDialog.xaml           ← Custom styled dialog window
│   ├── CustomDialog.xaml.cs        ← Dialog logic (~207 lines)
│   ├── CustomInputDialog.xaml      ← Input dialog (for tab rename)
│   ├── CustomInputDialog.xaml.cs   ← Input dialog logic (~162 lines)
│   ├── CustomTab.cs                ← CustomTab model class (~23 lines)
│   ├── DialogHelper.cs             ← Static factory for all dialog types (~276 lines)
│   ├── EventHelper.cs              ← Event attachment helpers (~246 lines)
│   ├── HelpManager.cs              ← Help window content and display (~302 lines)
│   ├── ImageViewerWindow.xaml      ← Full-screen image viewer layout
│   ├── ImageViewerWindow.xaml.cs   ← Image viewer with GIF support and nav (~797 lines)
│   ├── KeyboardHandler.cs          ← Keyboard shortcut routing (~246 lines)
│   ├── MediaSection.xaml           ← Image grid layout (WrapPanel for thumbnails)
│   ├── MediaSection.xaml.cs        ← Image paste, display, drag-drop, click logic (~1239 lines)
│   ├── NoteListWindow.xaml         ← Note window manager list UI
│   ├── NoteListWindow.xaml.cs      ← Opens/manages note windows (~362 lines)
│   ├── ResourceHelper.cs           ← Resource dictionary access helpers (~118 lines)
│   ├── SafeExecutionHelper.cs      ← try/catch wrapper utilities (~98 lines)
│   ├── SettingsManager.cs          ← Settings application to UI (~123 lines)
│   ├── StatusBarManager.cs         ← Status bar updates (tab count, word count, time) (~61 lines)
│   ├── TabManager.cs               ← Tab CRUD, drag-and-drop, keyboard nav, context menu (~1641 lines)
│   ├── TextSection.xaml            ← RichTextBox layout with placeholder
│   ├── TextSection.xaml.cs         ← Rich text editing, formatting, word count (~470 lines)
│   ├── ThemeManager.cs             ← Theme switching (load/apply XAML resource dictionaries) (~81 lines)
│   ├── ThemeResourceHelper.cs      ← Safe theme resource access with fallbacks (~211 lines)
│   └── UIFactory.cs                ← UI element factory (create styled buttons, containers) (~292 lines)
│
├── App.xaml                        ← Application entry point (loads DarkTheme.xaml by default)
├── App.xaml.cs                     ← App startup/shutdown, global exception handling, orphan cleanup
├── AssemblyInfo.cs                 ← Theme resource dictionary location hints
├── modernwpf.md                    ← Empty placeholder file (0 bytes)
├── prompt1.md                      ← The audit prompt that generated this report
├── SnipShottyBoard.csproj          ← Project file (v1.6.0, net8.0-windows, WPF)
├── SnipShottyBoard_Roadmap.txt     ← Comprehensive feature roadmap (Phases 1-10)
├── Snippets.txt                    ← Workflow prompt snippets for AI assistants
└── VERSION                         ← Version file (currently says 1.5.0 — STALE)
```

---

## 5. ARCHITECTURE

### How the Code Is Organized

SSB uses a **layered architecture with manager classes**. There is no MVVM framework (no ViewModels, no data binding to properties). Instead, the pattern is:

```
UI Layer        →  MainWindow, NoteTab, MediaSection, TextSection (XAML + code-behind)
Manager Layer   →  TabManager, ThemeManager, DataManager, etc. (class files, injected into MainWindow)
Data Layer      →  AppConstants, AppData, AppSettings, SavedNote (pure C# models)
Infrastructure  →  LoggingService, AtomicFileManager, GifDiagnostics (cross-cutting concerns)
```

### How the Main Components Connect

```
App.xaml.cs
  ↓ OnStartup
  → Creates LoggingService
  → Schedules orphaned image cleanup (Task.Run, 5s delay)
  → Calls base.OnStartup → creates MainWindow

MainWindow (orchestrator)
  ├── Creates TabManager (handles all tab operations)
  ├── Creates ThemeManager (handles theme switching)
  ├── Creates StatusBarManager (updates status bar)
  ├── Creates KeyboardHandler (routes keyboard shortcuts)
  ├── Creates HelpManager (shows help window)
  ├── Creates SettingsManager (applies settings to UI)
  ├── Has autoSaveTimer (DispatcherTimer, 5s) → calls SaveApplicationData()
  ├── Has statusTimer (DispatcherTimer, 1s) → calls UpdateStatusBar()
  └── Wires TabManager events → MainWindow callbacks

TabManager
  ├── Owns List<CustomTab> (the tab data)
  ├── Controls tabHeaderPanel (WrapPanel) — renders tab buttons
  ├── Controls tabContentArea (ContentPresenter) — shows active tab's NoteTab
  ├── Handles drag-and-drop (mouse events on tab buttons)
  └── Fires events: OnDataChanged, OnStatusUpdateRequested, OnLogDebug, OnLogError

NoteTab (per tab)
  ├── Contains TextSection (RichTextBox for text editing)
  └── Contains MediaSection (WrapPanel for image thumbnails)

DataManager (static)
  ├── Saves/loads via AtomicFileManager
  ├── Manages image files (save from clipboard, copy dropped, delete)
  └── Handles orphaned image cleanup

NoteWindowManager (singleton)
  └── Manages multiple MainWindow instances (each window = one NoteWindowData)
```

### Event-Driven Communication

Components don't call each other directly — they raise events. Example:
- User types in TextSection → `OnDataChanged` fires → NoteTab propagates → TabManager propagates → MainWindow sets `hasUnsavedChanges = true`
- Auto-save timer fires → MainWindow calls `SaveApplicationData()` → `DataManager.SaveNoteWindows()`

### Patterns Used

| Pattern | Where | Notes |
|---|---|---|
| Manager pattern | All `*Manager.cs` files | Each manager owns one concern |
| Singleton | `NoteWindowManager` | `Instance` property, private constructor |
| Event-driven | `TabManager` events | Loose coupling to MainWindow |
| Factory | `UIFactory.cs`, `DialogHelper.cs` | Create styled UI elements |
| Atomic file I/O | `AtomicFileManager` | Write-to-temp → backup → atomic replace |
| Rolling backups | `AtomicFileManager` | 20 most recent saves kept |
| IDisposable | `TabManager` | Clears event handlers on dispose |

---

## 6. CODE PATTERNS AND CONVENTIONS

### Naming Conventions

```
Classes:      PascalCase     → TabManager, SavedNote, AppConstants
Methods:      PascalCase     → SaveNotes(), LoadSettings(), CreateNewTab()
Private fields: camelCase    → tabHeaderPanel, isDragging, selectedTab
Properties:   PascalCase     → SelectedTab, TabCount, ImageFiles
Constants:    PascalCase     → AppConstants.TabMinWidth (NOT UPPER_SNAKE like .cursor/rules says)
Events:       On + PascalCase → OnDataChanged, OnLogDebug, OnMediaChanged
```

Note: The `.cursor/rules/building.mdc` specifies `UPPER_SNAKE` for constants, but the actual codebase uses `PascalCase` in `AppConstants`. New code should match the existing pattern.

### File Organization Pattern

Every UI component has two files:
- `ComponentName.xaml` — layout (XAML markup)
- `ComponentName.xaml.cs` — code-behind (C# logic)

Manager classes are single `.cs` files in the `UI/` directory.

### AppConstants Pattern — Design Tokens

All "magic numbers" are defined in `Data/AppConstants.cs`. This is the single source of truth for configurable values. When the old `.cursorrules` mentioned design token/constant NAMES — these are the AppConstants:

| Constant | Value | Purpose |
|---|---|---|
| `TabMinWidth` | 80px | Minimum tab button width |
| `TabMaxWidth` | 200px | Maximum tab button width |
| `TabStripMaxHeight` | 200px | Max height before tab strip scrolls |
| `TabRowGroupingTolerance` | 5px | Y-position tolerance for detecting same row |
| `TabDragHysteresisBuffer` | 5.0px | Dead zone to prevent drop indicator flicker |
| `DefaultAutoSaveIntervalSeconds` | 5s | Auto-save frequency |
| `SplitterMinRatio` | 0.2 | Minimum text section width ratio |
| `SplitterMaxRatio` | 0.8 | Maximum text section width ratio |
| `SplitterDefaultRatio` | 0.5 | Default 50/50 split |
| `MaxAnimatedGifsPerNote` | 5 | GIF limit per note (memory) |
| `MaxCachedImages` | 100 | LRU cache size for images |
| `MaxImageCacheBytes` | 100MB | Memory limit for image cache |
| `RichTextBoxUndoLimit` | 150 | Caps undo stack (memory) |

### AccentBrush

`AccentBrush` is defined in both theme files (`#4A90E2` — a medium blue). It's used for:
- Active tab underline (2px blue strip)
- Tab drop indicator (blue vertical line, 3px wide)
- Pin button active state background
- Any selected/active UI state

### Logging Pattern

All logging goes through `LoggingService`. Categories used:
- `"UI"` — user interface events
- `"Data"` — file I/O, persistence
- `"Perf"` — performance timing (ms for saves/loads)
- `"Lifecycle"` — app start/stop
- `"System"` — system-level errors
- `"Manager"` — manager-level events

Static methods (`LogInfoStatic`, `LogErrorStatic`) are used in static contexts (DataManager).

---

## 7. EXISTING DOCUMENTATION

### What Exists and Its State

| File | Content | Accuracy |
|---|---|---|
| `docs/CHANGELOG.md` | Full version history 1.0.0–1.6.0 | ✅ Accurate and detailed |
| `docs/README.md` | Project overview, features, keyboard shortcuts | ✅ Accurate |
| `SnipShottyBoard_Roadmap.txt` | Phase 1–10 roadmap, library decisions, tech debt | ✅ Accurate and detailed |
| `docs/devnotes/2025-10-01-tabs-multiline-wrapping.md` | Full technical notes on multi-row tab implementation | ✅ Accurate |
| `docs/devnotes/2025-10-01-splitter-persist-and-titlebar-buttons.md` | Splitter and titlebar button implementation | ✅ Accurate |
| `docs/MCP_SETUP.md` | MCP server setup guide | ✅ Accurate |
| `docs/LM_STUDIO_SYSTEM_PROMPTS.md` | Prompts for LM Studio planning sessions | ✅ Accurate |
| `docs/WORKFLOW_GUIDE.md` | Two-brain system workflow | ✅ Accurate |
| `scripts/README.md` | Script usage documentation | ✅ Accurate |
| `integrations/mcp-servers/SNIPSHOTTYBOARD-SETUP.md` | MCP server setup | ✅ Accurate |

### Template Files — NOT Yet Filled In

These files exist as templates from the new `.cursor/rules/` system but contain **React/TypeScript placeholder content** that does not reflect SnipShottyBoard:

| File | State |
|---|---|
| `docs/PROJECT_MEMORY.md` | React template — tech stack says "React 19 + TypeScript 5 + Vite 6" |
| `docs/PLANNING.md` | Template only — no active sprint |
| `docs/DECISIONS.md` | React template decisions (DEC-001 = Vite+React, etc.) |
| `docs/BUGS.md` | Template only — no bugs logged yet |
| `docs/DEVNOTES.md` | Template only — no sessions logged |
| `docs/LEARNING.md` | React/JavaScript concepts — not WPF concepts |

These ALL need to be updated with actual SnipShottyBoard content.

### What Is Missing

- ❌ `docs/CR.md` — **DELETED** in latest commit. Was the normative source of truth for all patterns. The old `.cursorrules` still references it. Its content covered: tab patterns, drag/drop rules, coordinate transforms, hysteresis, docs governance, ADR process. This content now lives only in the CHANGELOG and devnotes files.
- ❌ No architecture diagram
- ❌ No setup/onboarding guide for new contributors
- ❌ `docs/AUDIT_REPORT.md` — was deleted in the latest commit; this document replaces it

---

## 8. GIT STATUS

### Repository Info

```
Git initialized:  Yes
Total commits:    14
First commit:     "initial commit for snipshottyboard wpf net 8 app"
Last commit:      ec5d913 — "phases 0-5" (Nov 23, 2025)
```

### Full Git Log (newest first)

```
ec5d913  phases 0-5                                          ← LATEST — BROKEN BUILD
532a9f4  chore: clean .gitignore and remove .exe files
769b74f  feat(ui): add visual separation for tab strip       ← LAST KNOWN GOOD BUILD
196d6b3  chore: Release v1.5.0
83bdfbe  feat: Phase 1 - Persist splitter position as ratio
8c35a89  feat: Phases 2 & 3 - Titlebar button updates       (part of v1.5.0)
e3a7294  chore(release): prepare v1.4.0
0afe63c  docs: normalize CR vs Dev Notes, adopt docs/ layout
219476a  Increase drag visual transparency, add v1.3.0 to CHANGELOG
748e6e2  Complete tab drag-and-drop UX enhancement
dc13206  Fix drop indicator coordinate transform errors
17a673c  Fix build issues: Remove empty GifFramePlayer files
adca645  Phase 1: Add drop indicator line for tab drag-and-drop
467422c  feat: Add comprehensive architecture audit and Cursor Rules (CR.md)
5666a23  initial commit for snipshottyboard wpf net 8 app
```

### Uncommitted Changes (as of audit)

The git status shows new untracked files — all from the new cursor rules system:
```
?? .cursor/rules/bug-protocol.mdc
?? .cursor/rules/building.mdc
?? .cursor/rules/core.mdc
?? .cursor/rules/memory.mdc
?? .cursorignore
?? docs/BUGS.md
?? docs/DECISIONS.md
?? docs/DEVNOTES.md
?? docs/LEARNING.md
?? docs/LM_STUDIO_SYSTEM_PROMPTS.md
?? docs/MCP_SETUP.md
?? docs/PLANNING.md
?? docs/PROJECT_MEMORY.md
?? docs/QUICK_CHECK.md
?? docs/SESSION_END.md
?? docs/SESSION_START.md
?? docs/WORKFLOW_GUIDE.md
?? prompt1.md
 D .cursorrules   ← deleted from working tree
```

---

## 9. WHAT NEEDS WORK

### CRITICAL — Fix Before Anything Else

**1. Missing Source Files (Build Failure)**

Create these three files to restore the build:

**`Infrastructure/Helpers/PathSanitizer.cs`** — Used in DataManager, AtomicFileManager, MediaSection. Intent: sanitize full file paths before writing to logs (prevent leaking full `C:\Users\Jeremy\...` paths in log files). Implementation: likely a static class with a `SanitizePath(string)` method that returns just the filename or a shortened path.

**`Core/Schema/MigrationService.cs`** — Used in DataManager for `MigrateNotes()`, `MigrateNoteWindows()`, `MigrateAppSettings()`. Intent: detect and migrate old data schemas to new format (using the `DataVersion`/`SchemaVersion`/`SettingsVersion` fields added in v1.6.0). Implementation: a static class with those three methods.

**`Core/Utils/WindowPositionTracker.cs`** — Used in MainWindow for `SetupPositionTracking()`. Comment says "debounced position tracking (prevents choppy dragging from disk I/O)". Intent: debounce window position saves so moving the window doesn't trigger a disk write on every pixel.

### HIGH — Should Fix Soon

**2. VERSION file is stale**
`VERSION` says `1.5.0`. Should be `1.6.0`. Fix: update `VERSION` file to `1.6.0`.

**3. Template docs contain wrong content**
`docs/PROJECT_MEMORY.md`, `docs/DECISIONS.md`, `docs/LEARNING.md`, `docs/DEVNOTES.md`, `docs/BUGS.md`, `docs/PLANNING.md` all contain React/TypeScript placeholder content. They need to be rewritten with actual SnipShottyBoard content, or cleared and filled in with real data.

**4. `.cursorrules` references deleted `docs/CR.md`**
The `.cursorrules` file tells AI assistants to "Read docs/CR.md from the repository and treat it as the normative source of truth." That file no longer exists. Either:
- Restore CR.md from git history (`git show 0afe63c:docs/CR.md`)
- Or update `.cursorrules` to point to the new `.cursor/rules/*.mdc` system

### MEDIUM — Technical Debt

**5. TabManager.cs is 1641 lines**
Single largest file. The building rules say files should be under 200 lines. TabManager handles: tab CRUD, drag-and-drop, keyboard navigation, context menus, settings, and dispose. Should be split into focused sub-managers.

**6. MediaSection.xaml.cs is 1239 lines**
Handles image paste, drag-drop between thumbnails, click/double-click detection, GIF support, lazy loading. Similar splitting needed.

**7. 262 compiler warnings**
The codebase builds with 262 warnings (reported in CHANGELOG for v1.6.0 entry). Most are nullable reference warnings. While not blocking, they hide real issues.

**8. NoteWindowManager uses Debug.WriteLine instead of LoggingService**
`NoteWindowManager.cs` lines 67–68, 84–85: uses `System.Diagnostics.Debug.WriteLine()` instead of the `LoggingService`. Inconsistent with the rest of the codebase.

**9. DataManager has duplicate `<summary>` XML comments**
Several methods in `DataManager.cs` have two consecutive `/// <summary>` blocks (old one left in, new one added above it). Minor but messy.

**10. `Examples/` folder is in the project root**
`Examples/MediaSectionRefactored.cs` is a reference example file. Its namespace is `SnipShottyBoard.Examples`. It's not excluded from compilation (not `#if false` or anything). It will be compiled into the build even though it serves no functional purpose. The class inherits from `UserControl` but has no XAML file and no matching `.g.cs`. This likely compiles fine (`partial class` without XAML is valid) but is confusing.

### LOW — Nice to Have

**11. `modernwpf.md` is empty**
Zero bytes. Was presumably created as a placeholder for ModernWpf research notes. Either fill it or delete it.

**12. `hell -ExecutionPolicy Bypass...` file in root**
There's a file in the root directory named `hell -ExecutionPolicy Bypass -File .scriptspublish.ps1`. This is clearly an accidentally saved command that looks like a PowerShell command that was typed in the wrong place. Should be deleted.

**13. MCP server hardcodes Jeremy's desktop path**
`integrations/mcp-servers/snipshottyboard-mcp.js` line 11: `'C:\\Users\\Jeremy\\Desktop\\GitHub\\SnipShottyBoard'` as default `PROJECT_ROOT`. This is overridable via `SSB_PROJECT_ROOT` env var, but the fallback is incorrect for Jeremy's current machine (`c:\Users\Soy\Documents\Repos\SnipShottyBoard`).

---

## 10. THINGS FROM OLD .CURSORRULES — EXPLAINED IN CONTEXT

The old `.cursorrules` referenced concepts that are now spread across the codebase. Here is each one located and explained:

### `docs/CR.md` as normative source of truth

CR.md was the **Cursor Rules document** — a normative specification written in MUST/SHOULD language (not tutorials or code). It defined how the app should work architecturally. It was maintained alongside code.

**Status:** DELETED in commit `ec5d913`. Its content can be recovered with `git show 0afe63c:docs/CR.md` (the commit `0afe63c` is where it was in best shape). The `.cursorrules` still references it. The new system replaces it with `.cursor/rules/*.mdc` files.

### Tabs, Grouping, Coordinates, Hysteresis

These are the core algorithms in `UI/TabManager.cs`:

**Tabs:** Custom WPF `Button` elements styled to look like browser tabs. Each tab button is added to a `WrapPanel` (the `tabHeaderPanel`). The active tab has `Tag="Selected"` which triggers XAML style triggers for blue underline and font weight change.

**Grouping (Row Detection):** When tabs wrap to multiple rows, the code needs to know which row each tab is in. It does this by getting the Y-position (`TranslatePoint`) of each tab button relative to the WrapPanel and grouping tabs within `TabRowGroupingTolerance` (5px) of each other into the same "row." Code: `TabManager.cs` — method `GetTabsGroupedByRow()`.

**Coordinates:** All drag/drop coordinate math uses `TransformToAncestor(MainWindow)` to convert between different visual tree coordinate spaces. This was a critical bug fix in v1.3.0 — using the wrong ancestor caused "Visual is not an ancestor" exceptions.

**Hysteresis:** When dragging a tab over another tab's boundary, the drop indicator would flicker back and forth rapidly. Hysteresis adds a 5px dead zone (`TabDragHysteresisBuffer`): the indicator only moves to a new position if the mouse has moved more than 5px past the previous position. Code: `TabManager.cs` — `dropTargetIndex` vs `lastDropTargetIndex` comparison.

### TabMinWidth, TabRowGroupingTolerance, AccentBrush constants

All defined in `Data/AppConstants.cs`:
- `TabMinWidth = 80` — line 181
- `TabMaxWidth = 200` — line 186  
- `TabRowGroupingTolerance = 5` — line 199
- `TabDragHysteresisBuffer = 5.0` — line 204

`AccentBrush` is not in AppConstants (it's a XAML resource, not a C# constant). It's defined in both `Resources/Themes/DarkTheme.xaml` and `Resources/Themes/LightTheme.xaml` as `#4A90E2` (medium blue). Access it in code via `ThemeResourceHelper.GetBrush("AccentBrush")`.

### Dev Notes format

Dev Notes live in `docs/devnotes/YYYY-MM-DD-kebab-title.md`. They have front-matter (Title, Date, Owner, Versions Affected, Links) and contain implementation details, algorithms, exact numeric constants, and testing criteria. They are explicitly NOT the normative spec (CR.md was for that). The division:
- **CR.md (normative):** MUST/SHOULD rules, no code, no numbers — just named constants
- **Dev Note (descriptive):** How it was actually built, why decisions were made, what numbers were used

### ADRs

ADR = Architectural Decision Record. The format was established in `docs/adr/YYYY-MM-DD-kebab-title.md` with fields: Date, Status (Proposed/Accepted/Superseded), Context, Decision, Consequences, Alternatives, Links. No ADR files currently exist in the repository (the `docs/adr/` folder does not exist). The CHANGELOG mentions ADRs were planned but not implemented.

### Publish/release process

Two scripts exist:

**`scripts/publish.ps1`** (basic):
1. `dotnet restore`
2. Clean `bin\Release`
3. `dotnet publish` → single-file, win-x64, no symbols, no PDB
4. Output at `bin\Release\net8.0-windows\win-x64\publish\SnipShottyBoard.exe`

**`scripts/publish.release.ps1`** (full release):
1. Bumps version numbers
2. Builds Release
3. Creates `releases/vX.Y.Z/` folder
4. Copies `SnipShottyBoard.exe`, writes `README.txt`, `release_notes.txt`, `checksums.txt` (SHA256)
5. Optionally creates `SnipShottyBoard_vX.Y.Z.zip`

The `.cursorrules` (old) specified this workflow: update version → build → create releases folder → write artifacts → verify CHANGELOG links Dev Note.

---

## APPENDIX A — COMPLETE SOURCE FILE INVENTORY

| File | Lines (approx) | Purpose |
|---|---|---|
| `App.xaml` | 15 | Application definition, default theme |
| `App.xaml.cs` | 152 | App startup/shutdown, global exception handling |
| `AssemblyInfo.cs` | 11 | Theme resource hints |
| `Core/Managers/AtomicFileManager.cs` | 337 | Crash-safe file I/O with backups |
| `Core/Managers/DataManager.cs` | 724 | Central data persistence |
| `Core/Managers/NoteWindowManager.cs` | 123 | Multi-window management singleton |
| `Core/Models/AppSettings.cs` | 192 | User preferences model |
| `Core/Models/SavedNote.cs` | 125 | Single note data model |
| `Data/AppConstants.cs` | 228 | All named constants |
| `Data/AppData.cs` | 50 | Legacy combined data container |
| `Examples/MediaSectionRefactored.cs` | ~331 | Reference example (not functional) |
| `Infrastructure/Diagnostics/GifDiagnostics.cs` | 46 | DEBUG-only GIF logging |
| `Infrastructure/Logging/LoggingService.cs` | ~316 | Serilog wrapper |
| `UI/CustomDialog.xaml.cs` | ~207 | Custom dialog logic |
| `UI/CustomInputDialog.xaml.cs` | ~162 | Input dialog logic |
| `UI/CustomTab.cs` | ~23 | Tab data model |
| `UI/DialogHelper.cs` | ~276 | Dialog factory |
| `UI/EventHelper.cs` | ~246 | Event attachment helpers |
| `UI/HelpManager.cs` | ~302 | Help system |
| `UI/ImageViewerWindow.xaml.cs` | ~797 | Full-screen image/GIF viewer |
| `UI/KeyboardHandler.cs` | ~246 | Keyboard shortcut routing |
| `UI/MediaSection.xaml.cs` | ~1239 | Image management |
| `UI/NoteListWindow.xaml.cs` | ~362 | Note window list |
| `UI/ResourceHelper.cs` | ~118 | Resource access |
| `UI/SafeExecutionHelper.cs` | ~98 | Safe execution wrappers |
| `UI/SettingsManager.cs` | ~123 | Settings application to UI |
| `UI/StatusBarManager.cs` | ~61 | Status bar updates |
| `UI/TabManager.cs` | ~1641 | All tab operations (largest file) |
| `UI/TextSection.xaml.cs` | ~470 | Rich text editing |
| `UI/ThemeManager.cs` | ~81 | Theme switching |
| `UI/ThemeResourceHelper.cs` | ~211 | Safe theme resource access |
| `UI/UIFactory.cs` | ~292 | UI element factory |
| `UI/Views/MainWindow.xaml.cs` | ~747 | Main window orchestrator |
| `UI/Views/NoteTab.xaml.cs` | ~303 | Tab controller |
| `UI/Views/SettingsWindow.xaml.cs` | ~428 | Settings logic |

---

## APPENDIX B — DATA FILE LOCATIONS

When the app runs on Jeremy's machine:

```
%AppData%\Roaming\SnipShottyBoard\
├── notewindows.json         ← PRIMARY: all windows and their tabs
├── notewindows.json.bak     ← immediate backup from last save
├── notewindows.json.info    ← verification metadata
├── notewindows-YYYYMMDD-HHmmss.json  ← rolling backups (up to 20)
├── notes.json               ← LEGACY: old single-window format
├── settings.json            ← user preferences
├── settings.json.bak
├── settings.json.info
├── notewindows_snapshot_applied.flag ← one-time migration flag
├── notewindows-20251120-172254.json  ← canonical snapshot from Phase 4 incident
├── images/
│   └── img_20240101_120000_a1b2c3d4.png  ← pasted images
└── logs/
    └── snipshottyboard-20240101.log      ← daily Serilog files (7 days kept)
```

---

## APPENDIX C — KNOWN FRAGILE AREAS

| Area | Why Fragile | What To Watch |
|---|---|---|
| Missing files (`MigrationService`, `PathSanitizer`, `WindowPositionTracker`) | Build breakers — don't exist | Must be created before any work |
| Data migration logic | Complex, was the source of the critical v1.6.0 bug | Test thoroughly after creating MigrationService |
| GIF animation | Complex WPF threading, multiple loading strategies | `ImageViewerWindow.xaml.cs` has multiple fallback paths |
| Tab coordinate math | Multi-row requires TransformToAncestor; wrong ancestor = crash | Always use `MainWindow` as common ancestor |
| Hysteresis logic | Easy to regress by changing comparison direction | `TabDragHysteresisBuffer` constant, test drag near boundaries |
| Atomic file writes | Uses `File.Replace()` which behaves differently across drives/filesystems | Test on different drive configurations |
| PowerShell file writing | Default encoding is UTF-16, breaks JSON | Always use `[System.IO.File]::WriteAllText()` with UTF-8 |
| Theme resource access | Resources may not exist during startup before `InitializeComponent()` | `ThemeResourceHelper` wraps access with null checks |
| Window position on multi-monitor | Off-screen windows from disconnected monitors | Position validation in `MainWindow` constructor lines 160-170 |

---

*End of audit report. Total files examined: 65+. Report generated 2026-04-23.*
