# PROJECT_MEMORY.md

# ============================================================

# Master Project Brain — SnipShottyBoard

# Location: docs/PROJECT_MEMORY.md

# Updated: 2026-04-24 | Session: Phase 4 Typography

# ============================================================

# NOTE FOR AI: Read this file FIRST every session.

# This is the master context. Everything else references this.

# ============================================================

---

## PROJECT IDENTITY

```
Name:         SnipShottyBoard (SSB)
Type:         Windows desktop application (WPF)
Purpose:      A sticky notes app, but better and professional.
              A floating desktop board that keeps text snippets
              and pasted screenshots accessible without leaving your workflow.
Target user:  Jeremy — personal productivity tool
Status:       Sprint G — Native Windows Features (planning with Claude)
Started:      2024 (initial commit)
Last session: 2026-04-30
Version:      1.6.0 (csproj) — VERSION file at 1.7.0
```

---

## WHAT THE APP DOES

User opens SnipShottyBoard and gets a small floating window with browser-style tabs. Each tab is an independent note.

In each note the user can:

- Type rich text (bold, italic, underline, bullets, lists) in a borderless surface that glows on focus
- Paste screenshots directly with Ctrl+V or drag-drop from desktop
- View image thumbnails in a scrollable Media Vault (GIFs show static first-frame by default to save RAM)
- Double-click any thumbnail to open the Full-Screen Viewer (images/GIFs cycle with left/right arrows)
- Drag tabs to reorder them, double-click to rename, right-click for context menu
- Open multiple isolated windows (e.g., "Coding", "Design") that do not share tabs or state

The window stays on the desktop. Notes save automatically every 5 seconds to a single `master.json` in AppData. 
Media is stored as physical files in `%AppData%\SnipShottyBoard\images\`. JSON only holds filename references.
On reopen — everything is exactly where it was left, with orphaned images auto-cleaned and crash recovery available.

---

## TECH STACK

```
Language:      C# (nullable reference types enabled)
Runtime:       .NET 8 (net8.0-windows)
UI Framework:  WPF (Windows Presentation Foundation)
Target OS:     Windows 10/11 only
Output:        Single-file self-contained win-x64 exe

NuGet packages:
  WPF-UI 4.0.3              ← Modern WPF controls, borderless chrome
  Serilog.Sinks.File 6.0.0  ← Structured rolling daily log files
  CommunityToolkit.Mvvm 8.4.0 ← ObservableCollection, MVVM helpers

Serialization: System.Text.Json (built into .NET 8, no package)
Build output:  dotnet publish → single exe, no runtime needed
```

---

## DATA STORAGE

```
Location: %AppData%\Roaming\SnipShottyBoard\

Structure (v1.0 Standard):
  master.json           ← SINGLE SOURCE OF TRUTH: all windows, tabs, text content, window positions
  master.json.bak       ← immediate backup before each atomic save
  master.json.info      ← verification metadata sidecar
  settings.json         ← user preferences ONLY (theme, pin state, auto-save interval)
  images/               ← physical image files (PNG/GIF). JSON only stores filename references.
  logs/                 ← daily Serilog log files. Auto-deleted after 7 days.

Reference Pattern (v2 — Sprint A Phase A.2):
  master.json does NOT contain binary image data.
  It stores MediaReference objects: { "filename": "a8f3c2.gif", "dateAdded": "2026-04-25" }
  MediaReference.FullPath resolves the full path at runtime by combining
  %AppData%\SnipShottyBoard\images\ + filename.
  Legacy ImageFiles/ImageTimestamps accessors provide backward compat.
```

---

## PROJECT STRUCTURE

```
SnipShottyBoard/
├── .cursor/rules/           ← AI behavior rules (4 .mdc files)
├── .cursorignore
├── assets/app.ico
├── Core/
│   ├── Managers/
│   │   ├── AtomicFileManager.cs   ← crash-safe file I/O + backups
│   │   ├── DataManager.cs         ← central data persistence (master.json orchestrator)
│   │   └── NoteWindowManager.cs   ← MULTI-WINDOW ORCHESTRATOR: isolates window states
│   ├── Models/
│   │   ├── AppSettings.cs         ← user preferences model
│   │   ├── MediaReference.cs      ← filename-only media ref (filename + dateAdded)
│   │   └── SavedNote.cs           ← single note/tab data model (text + Media list)
│   ├── Schema/
│   │   └── MigrationService.cs    ← data version migration
│   └── Utils/
│       └── WindowPositionTracker.cs ← debounced position saves
├── Data/
│   ├── AppConstants.cs            ← ALL named constants (single source)
│   ├── AppData.cs                 ← legacy data container
│   └── MasterData.cs              ← SINGLE SOURCE OF TRUTH model (windows + settings)
├── docs/                          ← all project memory files
├── Examples/
│   └── MediaSectionRefactored.cs  ← reference only, not functional
├── Infrastructure/
│   ├── Diagnostics/GifDiagnostics.cs
│   ├── Helpers/
│   │   └── PathSanitizer.cs       ← path sanitization for logs
│   └── Logging/LoggingService.cs  ← Serilog wrapper
├── integrations/mcp-servers/      ← Node.js MCP server for LM Studio
├── Resources/Themes/
│   ├── DarkTheme.xaml             ← complete dark theme (~53KB)
│   └── LightTheme.xaml            ← complete light theme (~53KB)
├── scripts/
│   ├── publish.ps1                ← basic publish
│   └── publish.release.ps1        ← full release with artifacts
└── UI/
    ├── Views/
    │   ├── MainWindow.xaml(.cs)   ← main orchestrator (747 lines)
    │   ├── NoteTab.xaml(.cs)      ← individual tab controller (303 lines)
    │   └── SettingsWindow.xaml(.cs) ← settings UI (428 lines)
    ├── TabManager.cs              ← ALL tab logic (1641 lines) ← fragile
    ├── MediaSection.xaml(.cs)     ← image management + lazy load targets (1239 lines) ← fragile
    ├── TextSection.xaml(.cs)      ← rich text editing (470 lines)
    ├── ImageViewerWindow.xaml(.cs) ← full-screen viewer with GIF animation engine (478 lines after B.2)
    └── [other manager files]
```

---

## ARCHITECTURE

Pattern: Layered Manager Pattern (NOT MVVM despite CommunityToolkit)

```
UI Layer:        MainWindow, NoteTab, MediaSection, TextSection, ImageViewerWindow
Manager Layer:   TabManager, ThemeManager, DataManager, NoteWindowManager, ImageCacheManager
Data Layer:      AppConstants, SavedNote, AppSettings, master.json references
Infrastructure:  LoggingService, AtomicFileManager, PathSanitizer, MigrationService
```

Communication: Event-driven (not data binding)
Components fire events → orchestrators respond

**Key System Roles:**

- `MainWindow`: Owns the auto-save timer (DispatcherTimer, 5s), wires TabManager events
- `NoteWindowManager`: Singleton that isolates window instances. Each window has unique ID, state, and private tab list. No shared tabs.
- `DataManager`: Reads/writes `master.json`. Resolves media references from `images/` folder. Handles orphan cleanup.
- `ImageCacheManager` (Implemented Sprint B B.1, extended B.2): Dual-eviction LRU system caps memory at 100 images OR 100MB (whichever triggers first). Supports separate cache keys for thumbnails (bare path) and full-res viewer images (`path:full`). `RemoveAllForPath()` evicts all variants on file deletion.

---

## CRITICAL DESIGN TOKENS

All magic numbers live in Data/AppConstants.cs — NEVER inline:

```
TabMinWidth                = 80px   ← minimum tab button width
TabMaxWidth                = 200px  ← maximum tab button width
TabStripMaxHeight          = 200px  ← max tab strip height
TabRowGroupingTolerance    = 5px    ← Y-tolerance for row detection
TabDragHysteresisBuffer    = 5.0px  ← dead zone for drop indicator
DefaultAutoSaveIntervalSec = 5s     ← auto-save frequency
SplitterDefaultRatio       = 0.5    ← default 50/50 text/media split
SplitterMinRatio           = 0.2    ← minimum text section ratio
SplitterMaxRatio           = 0.8    ← maximum text section ratio
MaxAnimatedGifsPerNote     = 5      ← GIF limit (memory)
MaxCachedImages            = 100    ← LRU image cache size
MaxImageCacheBytes         = 100MB  ← memory limit for image cache
RichTextBoxUndoLimit       = 150    ← caps undo stack
```

XAML color tokens — defined in both theme files:

```
AccentBrush     = #6366F1  ← indigo accent (replaced old blue #4A90E2)
AccentGradientBrush = #6366F1 → #8B5CF6  ← indigo-to-purple gradient
ContentCardBrush = #18181B  ← zinc-800 solid card surface
AppBackgroundBrush = #111113  ← deep solid dark chrome
```

Access in code: ThemeResourceHelper.GetBrush("AccentBrush")
NEVER use hex colors inline in XAML — always {DynamicResource Name}

---

## NAMING CONVENTIONS

```
Classes:         PascalCase   → TabManager, SavedNote
Methods:         PascalCase   → SaveNotes(), CreateNewTab()
Private fields:  camelCase    → tabHeaderPanel, isDragging
Properties:      PascalCase   → SelectedTab, TabCount
Constants:       PascalCase   → AppConstants.TabMinWidth
                              NOTE: PascalCase NOT UPPER_SNAKE
Events:          On+PascalCase → OnDataChanged, OnLogDebug
XAML files:      PascalCase   → CustomTab.cs, TabManager.cs
```

---

## KNOWN FRAGILE AREAS


| Area                              | Why Fragile                                      | What To Watch                                 |
| --------------------------------- | ------------------------------------------------ | --------------------------------------------- |
| TabManager.cs (1641 lines)        | Too large — hard to reason about                 | Coordinate math, hysteresis logic             |
| MediaSection.xaml.cs (1239 lines) | Too large                                        | GIF threading, lazy loading                   |
| Tab coordinate math               | Multi-row needs TransformToAncestor              | Always use MainWindow as ancestor             |
| Hysteresis logic                  | Easy to regress                                  | TabDragHysteresisBuffer constant              |
| Atomic file writes                | File.Replace() behaves differently across drives | Test on different drive configs               |
| PowerShell encoding               | Defaults to UTF-16 — breaks JSON                 | Always use UTF-8 WriteAllText                 |
| Theme resources                   | May not exist during startup                     | ThemeResourceHelper wraps with null checks    |
| Window position on multi-monitor  | Off-screen on disconnected monitors              | Position validation in MainWindow constructor |
| GIF animation                     | Complex WPF threading                            | Multiple fallback paths in ImageViewerWindow  |
| MigrationService                  | Complex — source of v1.6.0 bug                   | Test thoroughly after data format changes     |


---

## GIT HISTORY SUMMARY

```
5666a23  initial commit
467422c  Add CR.md and architecture audit
adca645  Phase 1: Add drop indicator for tab drag
dc13206  Fix coordinate transform errors
748e6e2  Complete tab drag-and-drop UX
219476a  v1.3.0 — increase drag transparency
0afe63c  Normalize CR vs Dev Notes
e3a7294  Release v1.4.0
8c35a89  Phases 2 & 3 — titlebar button updates
83bdfbe  Phase 1 — persist splitter position
196d6b3  Release v1.5.0
769b74f  feat: add visual separation for tab strip ← LAST GOOD BUILD
532a9f4  chore: clean .gitignore
ec5d913  phases 0-5 ← BROKEN — 3 missing files
```

Last known good build: 769b74f

---

## ENVIRONMENT SETUP

```
OS:           Windows 11
Editor:       Cursor AI IDE
Shell:        PowerShell

AI — LM Studio (Planning brain):
Model:        qwen/qwen3.6-27b (Q6_K, 22.5GB)
Port:         1234
Purpose:      Planning, consulting, architecture

AI — Cursor (Building brain):
Model:        qwen/qwen3.6-27b via LM Studio → ngrok
Purpose:      Building, coding, file writing

MCP Server:   integrations/mcp-servers/snipshottyboard-mcp.js
              Resolves dynamically: env var SSB_PROJECT_ROOT > script location

IMPORTANT — PowerShell UTF-8:
[System.IO.File]::WriteAllText(path, content,
[System.Text.UTF8Encoding]::new($false))
Never use Out-File or >> for code files.
```

---

## CURRENT PRIORITIES

```
Build Order (Locked): H → C → P → D → F → G → E → R → V

Sprint G — Native Windows Features (next, plan with Claude first):
1. System tray icon + minimize-to-tray
2. Taskbar jump lists
3. Start with Windows
4. Optional: toast notifications

Sprint F — FluentWindow Native Chrome Conversion ✅ COMPLETE:
1. Phase F.0 — DarkTheme resources (TitleBarButtonStyle, TitleBarPinButtonStyle) ✅
2. Phase F.1 — MainWindow FluentWindow conversion ✅
3. Phase F.2 — SettingsWindow FluentWindow conversion ✅
4. Phase F.3 — ImageViewerWindow FluentWindow + color fixes ✅
5. Phase F.4 — NoteListWindow FluentWindow conversion ✅
6. Phase F.5 — Native tab context menus ✅
7. Phase F.6 — Cleanup + backlog notes ✅

Completed (all verified):
✅ Visual Overhaul 6A-6C — Deep dark chrome, gradient system, glow effects
✅ Sprint A — Data Layer Cleanup (master.json, MediaReference, orphan cleanup)
✅ Sprint B — Memory & GIF Cache (LRU cache, lazy-loading, static GIF thumbnails)
✅ Sprint H — Hygiene Triage
✅ Sprint C — Crash Recovery Buffer
✅ Sprint P — Data Persistence Fix
✅ Sprint D — Professional GIF Viewer
✅ Sprint F — FluentWindow Native Chrome Conversion (all 4 windows + context menus)
```

---

## NOTES FOR AI READING THIS

This is a WPF .NET 8 Windows desktop app — NOT a web app.
No React. No TypeScript. No npm. No Tailwind.
C#, XAML, WPF, .NET 8. Windows only.

Constants use PascalCase (AppConstants.TabMinWidth)
NOT UPPER_SNAKE — the codebase uses PascalCase throughout.

The build is currently GREEN. Before anything else:
Read docs/PLANNING.md to see the current phase status.

Bug protocol applies especially here — the codebase has
complex coordinate math and threading that can spiral badly.
Always understand root cause before touching code.