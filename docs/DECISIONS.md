# DECISIONS.md

# ============================================================

# Architectural Decision Log — SnipShottyBoard

# Never delete entries — mark old ones SUPERSEDED

# Location: docs/DECISIONS.md

# ============================================================

---

## ACTIVE DECISIONS

---

### DEC-001

```
Status:    ACTIVE
Date:      2024 (initial build)
Category:  Platform

Decision:
Build as a WPF .NET 8 Windows-only desktop application.

Why:
WPF gives full native Windows integration — system tray,
always-on-top, clipboard access, drag-drop from desktop.
.NET 8 is the current LTS release with long support window.
Windows-only is acceptable — Jeremy's personal tool.

Alternatives considered:
→ Avalonia: Cross-platform but less mature Windows integration.
            Being considered for future port (see Roadmap).
→ Electron: Too heavy for a lightweight sticky note app.
→ WinUI 3: Newer but smaller ecosystem and more complex setup.

Professional standard: WPF + .NET 8 is standard for
Windows-only desktop tools in 2024-2026.
Revisit when: Cross-platform support is needed.
```

---

### DEC-002

```
Status:    ACTIVE
Date:      2024
Category:  Data Persistence

Decision:
Store all data as JSON files in %AppData%\Roaming\SnipShottyBoard\.
Use System.Text.Json (built-in, no extra package).
Use atomic file writes with rolling backups.

Why:
No installation of external database needed.
AppData is the correct Windows location for app data.
Atomic writes (write-to-temp → backup → replace) prevent
data loss on crash or power failure.
Rolling 20-backup system recovers from any bad save.

Alternatives considered:
→ SQLite: More structured but overkill for this data model.
→ LiteDB: Embedded document DB — adds dependency complexity.
→ Registry: Wrong tool — not for large amounts of data.

Professional standard: JSON in AppData is standard for
small Windows apps with simple data needs.
Revisit when: Data becomes relational or multi-user.
```

---

### DEC-003

```
Status:    ACTIVE
Date:      2024
Category:  Architecture

Decision:
Use layered Manager Pattern, not MVVM.
Managers own one concern each and communicate via events.

Why:
For a single-developer personal tool, MVVM adds ceremony
without meaningful benefit. The Manager Pattern:
- Is easier to understand and debug
- Keeps each file focused on one thing
- Uses events for loose coupling between components
- Still testable — managers are just classes

Actual pattern:
UI Layer → Manager Layer → Data Layer → Infrastructure

Alternatives considered:
→ MVVM with ViewModels: More overhead, harder to get right
                         for a beginner. CommunityToolkit.Mvvm
                         is installed but minimally used.
→ Pure code-behind: No separation at all — rejected.

Professional standard: Manager Pattern is valid for
single-developer desktop apps of this scale.
Revisit when: App grows to need ViewModels (probably never
for this codebase).
```

---

### DEC-004

```
Status:    ACTIVE
Date:      2024
Category:  UI Theming

Decision:
Two complete XAML resource dictionaries for Dark and Light themes.
Switch themes by replacing the merged dictionary at runtime.
All colors accessed via {DynamicResource BrushName}.
NEVER inline hex values in XAML.

Why:
XAML resource dictionaries are the WPF-idiomatic theming approach.
DynamicResource means the UI updates immediately on theme switch.
Named brushes (AccentBrush, BackgroundBrush etc) decouple
XAML from specific colors — change the theme file, not every XAML file.

Key theme tokens:
AccentBrush = #4A90E2 (medium blue — active states, indicators)
Access in code: ThemeResourceHelper.GetBrush("AccentBrush")

Professional standard: Yes — standard WPF theming approach.
```

---

### DEC-005

```
Status:    ACTIVE
Date:      2024
Category:  Constants

Decision:
All named constants live in Data/AppConstants.cs.
Naming convention: PascalCase (TabMinWidth, not TAB_MIN_WIDTH).
NEVER inline magic numbers in C# or XAML.

Why:
Single source of truth for all configurable values.
PascalCase matches C# conventions for public static members.
Named constants make code self-documenting.

Key constants:
TabMinWidth = 80
TabMaxWidth = 200
TabRowGroupingTolerance = 5
TabDragHysteresisBuffer = 5.0
DefaultAutoSaveIntervalSeconds = 5
SplitterDefaultRatio = 0.5

Professional standard: Yes. Note: PascalCase for constants
is the correct convention for this codebase — NOT UPPER_SNAKE.
```

---

### DEC-006

```
Status:    ACTIVE
Date:      2024
Category:  Tab Drag-Drop

Decision:
Use coordinate transforms via TransformToAncestor(MainWindow)
for all drag-drop coordinate math.
Hysteresis buffer of 5px to prevent drop indicator flicker.

Why:
WPF visual tree coordinate spaces are relative to each control.
Wrong ancestor in TransformToAncestor = crash ("Visual is not
an ancestor"). Using MainWindow as the common ancestor is
consistent and safe. See commit dc13206 for the fix history.

Hysteresis prevents rapid indicator flickering when mouse
hovers near the boundary between two tabs. The 5px dead zone
means the indicator only moves when intent is clear.

Professional standard: Yes — hysteresis is standard UX
for any drag-drop UI with visual indicators.
Revisit when: Tab strip layout changes significantly.
```

---

### DEC-007

```
Status:    ACTIVE
Date:      2024
Category:  Logging

Decision:
All logging goes through LoggingService (Serilog wrapper).
Never use Debug.WriteLine or Console.WriteLine.
Sanitize all file paths before logging via PathSanitizer.
Log categories: UI, Data, Perf, Lifecycle, System, Manager.

Why:
Consistent structured logging enables debugging without
attaching a debugger. Rolling daily files in AppData mean
logs are always available when bugs are reported.
Path sanitization prevents leaking C:\Users\Soy\... paths.

Exception: NoteWindowManager.cs lines 67-68, 84-85 still
uses Debug.WriteLine — this is known tech debt to fix.

Professional standard: Yes — Serilog is industry standard.
```

---

### DEC-008

```
Status:    ACTIVE
Date:      2026-04-23
Category:  AI Development Setup

Decision:
Use Qwen3.6-27B Q6_K via LM Studio → ngrok → Cursor
as the primary AI development assistant.

Two-brain system:
LM Studio = planning and architecture brain
Cursor = building and execution brain
docs/PLANNING.md = bridge between them

Why:
Best performing local coding model for RTX 5090 32GB.
77.2% SWE-bench score. Full thinking mode.
Completely private — nothing leaves the machine.

Note: ngrok URL changes on restart. Update Cursor settings
when it changes. Consider ngrok static domain.
```

---

### DEC-009

```
Status:    ACTIVE
Date:      2026-04-24
Category:  UI Theming

Decision:
Use plain Border with DropShadowEffect for content card
containers, not materialDesign:Card from MDXAML.

Why:
materialDesign:Card has its own style system that can
conflict with custom DarkTheme.xaml brushes. A plain
Border with a named style (ContentCardStyle) gives full
control over the visual appearance without MDXAML
interference. DropShadowEffect provides the elevation
look without needing MDXAML's shadow system.

Parameters: CornerRadius=6, Padding=8, Margin=0,4,
DropShadowEffect (Color=#000000, Opacity=0.25, Blur=6).

Professional standard: Border + DropShadowEffect is the
standard WPF approach for card-like containers.
```

---

### DEC-010

```
Status:    ACTIVE
Date:      2026-04-24
Category:  Visual Overhaul

Decision:
Replace Phase 1-5 "blue dark" theme entirely with
"Sleek Dark / Notion-Edge Depth" aesthetic.

New design language:
- Base chrome: deep solid dark #111113 (no transparency/glass)
- Content cards: solid #18181B (zinc-800) with subtle shadows
- Accent: indigo-to-purple gradient #6366F1 → #8B5CF6
- Glow color: indigo #6366F1 via DropShadowEffect
- Tabs: rectangular (CornerRadius=4), gradient underline on active
- Editor: borderless until focused, then indigo glow ring
- Typography: native Segoe UI, tighter line height

Why:
Delivers the "powerful but minimal" aesthetic Jeremy wants.
Old blue theme (#4A90E2, #1E2A3A) was functional but not the
target aesthetic. Clean slate approach avoids incremental
compromise — rewrite tokens, replace containers.

Rollback: git reset --hard 8cbae4f restores Phase 1-5 state.

Professional standard: Solid dark chrome with gradient accents
is standard for premium desktop apps (Notion, Linear, VS Code).
```

---

### DEC-010

```
Status:    ACTIVE
Date:      2026-04-25
Category:  Data Architecture

Decision:
Consolidate all app data into a Single Source of Truth (`master.json`).
Separate binary media from text/state by storing physical files in `%AppData%\SnipShottyBoard\images\`.
JSON only holds filename references (`"filename": "a8f3c2.gif", "dateAdded": "..."`).

Why:
Eliminates scattered legacy files (`notewindows.json`, `notes.json`, etc.).
Makes backups, portability, and manual recovery trivial.
Prevents database bloat from embedding binary BLOBs.
Enables orphan cleanup: app scans JSON references vs actual disk files on startup.

Professional standard: Standard pattern for desktop tools (Obsidian, VS Code).
Revisit when: Binary storage becomes critical (unlikely for this use case).
```

---

### DEC-011

```
Status:    ACTIVE
Date:      2026-04-25
Category:  Multi-Window Strategy

Decision:
`NoteWindowManager` orchestrates completely isolated workspaces.
Each window gets a unique ID, independent tab list, and private state.
No tabs or notes are shared between windows.

Why:
Matches user workflow (e.g., Window 1 = Coding refs, Window 2 = Design assets).
Prevents accidental cross-window data leaks or UI confusion.
Simplifies memory management: each window's cache is independent and unloadable.

Professional standard: Standard for multi-instance desktop apps.
Revisit when: Cross-window tab dragging/sharing is explicitly requested.
```

---

### DEC-012

```
Status:    ACTIVE
Date:      2026-04-25
Category:  Memory & GIF Management

Decision:
Implement Lazy Loading for the Media Vault.
GIFs render as static first-frame thumbnails by default.
Full `AnimationClock` only loads in `ImageViewerWindow` on double-click.
LRU (Least Recently Used) cache caps total memory at 100 images / 100MB.

Why:
WPF GIF decoding is RAM-heavy. 20 animated GIFs = potential gigabytes of usage.
Lazy loading keeps UI buttery smooth while preserving animation capability.
LRU prevents memory leaks on long-running sessions.

Professional standard: Industry standard for image-heavy desktop apps (Photoshop, Figma).
Revisit when: Hardware acceleration APIs mature in WPF .NET 9+.
```

---

### DEC-013

```
Status:    ACTIVE
Date:      2026-04-25
Category:  Data Architecture

Decision:
Replace flat `ImageFiles` (full paths) + `ImageTimestamps` with
structured `MediaReference` objects storing only filenames.

New model:
- MediaReference: { "filename": "img_abc.png", "dateAdded": "..." }
- Full path resolved at runtime via MediaReference.FullPath property
- SavedNote.Media is a List<MediaReference>

Why:
Full paths in JSON are fragile — they break if AppData location changes,
user profile is renamed, or data is ported between machines. Storing
just filenames makes the data portable and self-contained.

Backward compat:
- [Obsolete] ImageFiles/ImageTimestamps property accessors convert
  transparently for existing callers (TabManager.cs, NoteTab.xaml.cs)
- MigrationService bumps note schema version to 2
- On load: legacy full paths → filename-only refs
- On save: new format written, legacy properties ignored

Professional standard: Standard pattern for portable data formats.
Revisit when: Media needs additional metadata (dimensions, type, etc.)
```

---

### DEC-014

```
Status:    ACTIVE
Date:      2026-04-25
Category:  Memory & GIF Management

Decision:
Use async lazy-loading with placeholder containers for Media Vault
thumbnails. Limit concurrent decodes to 4 via SemaphoreSlim.
GIFs always render as static first-frame thumbnails in the vault.

Implementation:
- Placeholder containers render instantly (no decode, no file I/O)
- LoadThumbnailAsync() decodes on background thread via Task.Run
- SemaphoreSlim(4,4) limits concurrent decodes
- CreateStaticGifThumbnail() forces GIFs to static frame (OnLoad + Freeze)
- EnsureThumbnailLoaded() fire-and-forget with CancellationTokenSource
- Dispose() cancels pending loads and disposes semaphore
- ImageCacheManager: dual-eviction (100 items OR 100MB)

Why:
Synchronous thumbnail loading on the UI thread freezes the UI with
many images (50+). Lazy-loading makes tab switches instant. GIFs in
the vault don't need animation — full animation only in ImageViewerWindow.

Professional standard: Standard pattern for image-heavy desktop apps
(Photoshop, Figma, VS Code). SemaphoreSlim concurrency limiting is
the .NET standard for throttling async operations.
Revisit when: WPF .NET 9+ hardware acceleration matures.
```

---

### DEC-015

```
Status:    ACTIVE
Date:      2026-04-25
Category:  Memory & GIF Management

Decision:
ImageViewerWindow uses LRU cache for static images (key: path:full),
skips cache for GIFs. All image decode runs on background thread.

Implementation:
- Static images: cache hit = instant display. Cache miss = decode on
  background thread via Task.Run, then cache and display on UI thread.
- GIFs: never cached. Decode on background thread, apply OnDemand
  settings on UI thread. Falls back to static loading on exception.
- Cache key strategy: thumbnails use bare path, full-res uses path:full.
  Both share the same LRU eviction pool (100 items / 100MB).
- RemoveAllForPath() evicts all variants (thumbnail + full-res) on delete.
- ImageViewerWindow reduced from 647 → 478 lines (debug spam removed).

Why:
GIF BitmapImages can't be frozen, making cache lifetime management
complex and error-prone. GIFs are cheap enough to re-decode on each
open. Static images benefit significantly from caching — navigating
left/right through a gallery of PNGs becomes instant on cache hits.
Background decode prevents UI freeze on large images.

Professional standard: Standard pattern for image viewers — cache
static content, re-decode animated content, offload I/O to background.
Revisit when: GIF cache becomes a performance concern (unlikely).
```

---

## SUPERSEDED DECISIONS

### DEC-S01 [SUPERSEDED by .cursor/rules/*.mdc]

```
Status:    SUPERSEDED
Original:  Use docs/CR.md as normative source of truth
Superseded: 2026-04-23
By:        .cursor/rules/ system (4 .mdc files)

Original decision:
CR.md (Cursor Rules) was a normative MUST/SHOULD document
defining architectural standards. Deleted in ec5d913.

Why superseded:
The new .cursor/rules/*.mdc system serves the same purpose
with better integration into Cursor AI. The 4 focused .mdc
files (core, bug-protocol, building, memory) replace CR.md
completely and are automatically loaded by Cursor.
```

---

## REJECTED OPTIONS


| Option               | Rejected Because                          | Revisit If                  |
| -------------------- | ----------------------------------------- | --------------------------- |
| Avalonia port        | Major rewrite — not ready                 | Cross-platform needed       |
| Multi-project split  | Abandoned mid-attempt in modernwpf branch | Codebase stable + DI needed |
| SQLite               | Overkill for simple data model            | Data becomes relational     |
| MVVM with ViewModels | Too much ceremony for single dev          | App grows significantly     |
| WinUI 3              | Smaller ecosystem, more complex           | WinUI matures               |


---

## JEREMY'S CALLS LOG

None yet.

---

## NOTES FOR AI

This is a WPF .NET 8 app. All decisions reflect that.
When adding new decisions — always check if it contradicts
DEC-005 (PascalCase constants) or DEC-004 (no inline hex values).
These two are enforced throughout the codebase and matter.