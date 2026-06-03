# SnipShottyBoard

> A sticky notes app, but **better** and **professional** — a floating desktop board for text snippets and pasted screenshots that lives next to your workflow.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows) ![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet) ![WPF](https://img.shields.io/badge/UI-WPF-1A78AC) ![Version](https://img.shields.io/badge/version-1.7.3-6366F1) ![Status](https://img.shields.io/badge/status-active%20development-orange)

---

## What is it?

SnipShottyBoard (SSB) is a desktop note app that fills the gap between **Notepad** and a full knowledge base. It's optimized for the common power-user pattern of **"I'll just paste this into a sticky note for now"** — except now those sticky notes have tabs, rich text, image vaults, GIF animation, and survive restarts cleanly.

Multiple independent floating windows. Each window is its own workspace with its own tabs. Whatever windows you had open when you closed the app come back exactly where you left them — Windows Sticky Notes app style.

> A WPF desktop app built for Windows 10/11. **Not** an Electron wrapper. **Not** a web app. Native .NET 8.

---

## Highlights

- 📝 **Multi-tab notes per window** — browser-style tabs, drag to reorder, double-click to rename, right-click for actions
- 🖼️ **Image-first capture** — `Ctrl+V` pastes a screenshot, drag-and-drop from the desktop, scrollable Media Vault per tab
- 🎬 **Full-screen image/GIF viewer** — zoom, pan, animated GIF playback, keyboard navigation between images
- 🪟 **Multi-window workspaces** — open as many isolated windows as you need (Coding / Design / TODOs)
- 💾 **Aggressive persistence** — autosave every 5 s, crash-recovery snapshot every 2 s, atomic writes with 20 rolling backups
- 🎨 **Per-image customization** — labels, three thumbnail sizes (60/100/150 px), hide/show, toggle date/time stamps
- ⌨️ **Rich-text editor** — bold, italic, underline, strikethrough, bullet lists, numbered lists, 150-step undo
- 🎯 **Always on top** — pin button, sleek dark FluentWindow chrome, glow effects on focus
- 🧹 **Self-cleaning vault** — orphaned image files auto-purged with a 24 h grace period
- 🪞 **Multi-monitor aware** — signed coordinate math fixes resize on monitors left of primary

---

## Tech stack

| Layer | Choice |
|---|---|
| Language | C# (nullable reference types on) |
| Runtime | .NET 8 (`net8.0-windows`) |
| UI | WPF + WPF-UI 4.0.3 FluentWindow chrome |
| Icons & accents | MaterialDesignThemes 5.3.1 |
| GIF playback | WpfAnimatedGif 2.0.2 |
| Logging | Serilog (file sink, rolling daily, 7-day retention) |
| Persistence | `System.Text.Json` → `master.json` (atomic write, 20 backups) |
| Build output | Single-file self-contained `win-x64` exe |

No Electron. No npm. No React. No Python. C# / XAML only.

---

## Screenshots

> _Drop a screenshot of the main window into `assets/screenshots/` and update this section._
>
> Suggested shots: main multi-tab window, image viewer with a GIF, multi-window layout, right-click context menu showing the per-image options.

---

## Quick start (run from source)

**Requirements**
- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (any 8.0.x)
- A clone of this repo

**Run**

```powershell
git clone https://github.com/eastsidelbc/SnipShottyBoard.git
cd SnipShottyBoard
dotnet run --project SnipShottyBoard.csproj
```

**Build a Release binary**

```powershell
dotnet build SnipShottyBoard.csproj -c Release
```

**Publish a single-file self-contained `.exe`**

```powershell
dotnet publish SnipShottyBoard.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish
```

The output `publish\SnipShottyBoard.exe` runs without requiring the .NET runtime to be installed.

---

## Keyboard shortcuts

### Tabs
| Shortcut | Action |
|---|---|
| `Ctrl+T` | New tab |
| `Ctrl+W` | Close current tab (with confirmation, can be silenced per session) |
| `Ctrl+R` | Rename current tab |
| `Ctrl+Tab` | Next tab |
| `Ctrl+Shift+Tab` | Previous tab |
| `Tab` / `Shift+Tab` | Switch tabs (when focus is outside the editor) |

### Editing
| Shortcut | Action |
|---|---|
| `Ctrl+V` | Paste image from clipboard or paste text |
| `Ctrl+B` | Bold |
| `Ctrl+I` | Italic |
| `Ctrl+U` | Underline |
| `Ctrl+S` | Strikethrough |
| `Ctrl+.` | Bullet list |
| `Ctrl+L` | Numbered list |

### Image viewer
| Shortcut | Action |
|---|---|
| `Left` / `Right` | Previous / next image in current note |
| `Mouse wheel` | Zoom 25 %–500 % |
| `Double-click` | Reset to 100 % (1:1) |
| `Click + drag` | Pan when zoomed in |
| Single click (GIF) | Play / pause |
| `Esc` | Close viewer |

### Window
| Shortcut | Action |
|---|---|
| Pin button (📌) | Toggle always-on-top |
| Note Windows button | Open the window manager (`Ctrl+Shift+N` planned) |
| `Esc` (in Note Windows) | Close the manager |

---

## Data & where things live

```
%AppData%\Roaming\SnipShottyBoard\
├── master.json           ← single source of truth: windows + tabs + text + media refs
├── master.json.bak       ← immediate backup before every atomic write
├── master.json.info      ← integrity sidecar (size, mtime)
├── master-YYYYMMDD-HHMMSS.json   ← rolling backups (up to 20)
├── master.json.recovery  ← crash-recovery snapshot (written every 2 s while dirty)
├── settings.json         ← user preferences (theme, pin state, auto-save interval)
├── images/               ← physical PNG / GIF / JPG files (referenced by master.json)
└── logs/                 ← daily Serilog files (auto-cleaned after 7 days)
```

Images are stored as **real files** with stable filenames. `master.json` only holds `{ "filename": "...", "dateAdded": "..." }` references plus per-image v3 metadata (label, thumbnail size, visibility flags). Resizing a thumbnail does not re-encode the file.

---

## Architecture at a glance

```
UI Layer        MainWindow, NoteTab, MediaSection, TextSection, ImageViewerWindow, NoteListWindow
Manager Layer   TabManager, ThemeManager, DataManager, NoteWindowManager, ImageCacheManager,
                StatusBarManager, KeyboardHandler, HelpManager, SettingsManager
Data Layer      AppConstants, MasterData, SavedNote, MediaReference, AppSettings, NoteWindowData
Infrastructure  AtomicFileManager, MigrationService, LoggingService, PathSanitizer, WindowChromeFix,
                WindowPositionTracker
```

**Pattern:** Layered Manager Pattern (event-driven, not MVVM). Components fire events, orchestrators respond.

**Key invariants:**
- All constants live in `Data/AppConstants.cs` (no inline magic numbers).
- All colors live in `Resources/Themes/DarkTheme.xaml` / `LightTheme.xaml` (no inline hex in XAML).
- File writes go through `AtomicFileManager` (temp-file + verify + atomic replace + rolling backup).
- Logging goes through `LoggingService`; paths are sanitized via `PathSanitizer` before they hit the log.
- WPF coordinate transforms always use `MainWindow` as the common ancestor (lesson from `BUG-H001`).

For deeper context see [`docs/PROJECT_MEMORY.md`](docs/PROJECT_MEMORY.md).

---

## Project layout

```
SnipShottyBoard/
├── App.xaml / App.xaml.cs              ← entry point + startup window restore
├── SnipShottyBoard.csproj              ← .NET 8 WPF project file
├── VERSION                              ← plaintext semver mirror of csproj <Version>
├── assets/app.ico                       ← application icon
├── Core/
│   ├── Managers/                        ← DataManager, NoteWindowManager, AtomicFileManager
│   ├── Models/                          ← SavedNote, MediaReference, AppSettings
│   ├── Schema/                          ← MigrationService (schema v1 → v3)
│   └── Utils/                           ← WindowPositionTracker, WindowChromeFix
├── Data/                                ← AppConstants, MasterData, AppData
├── Infrastructure/
│   ├── Diagnostics/                     ← GifDiagnostics
│   ├── Helpers/                         ← PathSanitizer
│   └── Logging/                         ← LoggingService (Serilog wrapper)
├── Resources/Themes/                    ← DarkTheme.xaml (complete), LightTheme.xaml (partial)
├── UI/
│   ├── Views/                           ← MainWindow, NoteTab, SettingsWindow
│   ├── TabManager.cs                    ← tab logic (drag/drop, rename, context menus)
│   ├── MediaSection.xaml.cs             ← image vault + per-image v3 customization
│   ├── TextSection.xaml.cs              ← rich-text editing
│   ├── ImageViewerWindow.xaml.cs        ← full-screen zoom/pan/GIF viewer
│   ├── NoteListWindow.xaml.cs           ← multi-window manager
│   └── (managers, helpers, dialogs)
└── docs/                                ← project memory, bugs, planning, dev notes
```

---

## What's working / what's not (honesty corner)

**Stable and battle-tested**
- Multi-tab text + image notes, autosave, crash recovery
- Atomic file writes with rolling backups
- GIF playback in the image viewer (with zoom/pan)
- Multi-window restore (Sticky Notes app behavior, as of `v1.7.1`)
- Per-image label / size / visibility persistence (as of `v1.7.1`)
- FluentWindow chrome with pin, drop shadows, dark theme
- Path-jail guards on all image file operations — tampered data cannot escape the app vault (as of `v1.7.2`)
- Pin button (📌) accent color shows immediately on click without needing to move the mouse (as of `v1.7.3`)

**Known limitations**
- Light theme is incomplete (intentionally not exposed in the UI — see `B-THEME` in `docs/BUGS.md`).
- A faint white flash can appear when resizing edges or opening the image viewer (`B-WF`, cosmetic only).
- `ImageViewerWindow` Prev / Next buttons are hidden by default (keyboard arrows work; visible buttons are planned).
- `TabManager.cs` (≈ 1641 lines) and `MediaSection.xaml.cs` (≈ 1239 lines) are scheduled for splitting in Sprint R.

See [`docs/BUGS.md`](docs/BUGS.md) for the complete bug history and `docs/PLANNING.md` for the active roadmap.

---

## Roadmap (next sprints)

| Sprint | Focus |
|---|---|
| **G** | Native Windows features: system tray, minimize-to-tray, jump lists, start-with-Windows |
| **E** | Memory & performance audit — profile RAM over a realistic session, set baseline |
| **R** | Code health — split `TabManager` and `MediaSection`, fix 262 warnings, wire `PathSanitizer` everywhere |
| **V** | v1.0 release prep — version bump, publish polish, README finalization, git tag |

Build order is locked: **G → E → R → V**.

---

## Contributing

This is primarily a personal project, but issues and PRs are welcome. A few notes for would-be contributors:

1. **Read the docs first.** [`docs/PROJECT_MEMORY.md`](docs/PROJECT_MEMORY.md) is the architectural source of truth. [`docs/BUGS.md`](docs/BUGS.md) explains why certain choices look the way they do.
2. **Follow the existing patterns.** PascalCase constants, manager pattern, `LoggingService` over `Debug.WriteLine`, `PathSanitizer` over raw paths in logs, `AppConstants` over inline magic numbers, `{DynamicResource}` over inline hex.
3. **Never widen a bug fix into a refactor.** See [`.cursor/rules/bug-protocol.mdc`](.cursor/rules/bug-protocol.mdc) — one fix, one bug, nothing else.
4. **Every change needs a Dev Note.** See `docs/devnotes/` for the format.

---

## License

[MIT](LICENSE) — free to use, modify, and distribute. Attribution appreciated.

---

## Acknowledgements

- [WPF-UI](https://github.com/lepoco/wpfui) — FluentWindow chrome and modern controls
- [Material Design In XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) — icons and accents
- [WpfAnimatedGif](https://github.com/XamlAnimatedGif/WpfAnimatedGif) — clean GIF playback in a `BindableImage`
- [Serilog](https://serilog.net/) — structured logging that doesn't get in the way

---

_Made by **eastsidelbc** — a love letter to "I'll just paste it somewhere for now."_
