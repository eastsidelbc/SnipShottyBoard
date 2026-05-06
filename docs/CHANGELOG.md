# 📝 SnipShottyBoard Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### ✨ Added
- **Tab Rename — Edit Mode Visuals**: Double-click tab rename now shows accent-colored border (AccentBrush), indigo glow (AccentGlowEffect), gradient underline, and accent caret. Uses existing tab template elements (TabBorder, ActiveUnderline) for consistent design language. ([Dev Note](docs/devnotes/2026-05-05-sprint-ui5-tab-rename-media-menu.md))
- **MediaSection — Global Context Menu**: Right-click empty space in media section to apply bulk operations: Size (Small/Medium/Big for all), Hide All / Show All toggle, Delete All (with confirmation). Right-click on individual images still shows per-image menu. ([Dev Note](docs/devnotes/2026-05-05-sprint-ui5-tab-rename-media-menu.md))
- **Image Viewer — Zoom**: Mouse-wheel zoom (25%–500%) using physical `Width`/`Height` scaling so the `ScrollViewer` scroll boundaries update automatically. `PreviewMouseWheel` intercepts the event before the ScrollViewer can swallow it.
- **Image Viewer — Pan**: Click-drag to pan when zoomed in. Uses `CaptureMouse()` for smooth out-of-bounds tracking. Distance threshold (3 px) distinguishes a drag from a click. `LostMouseCapture` resets state on system focus steal.
- **Image Viewer — Double-click to 1:1**: `e.ClickCount > 1` in `MouseLeftButtonDown` — WPF `Image` does not expose `MouseDoubleClick` in XAML (it is a `Control`-only event).
- **Image Viewer — Fit mode tracking**: `_isInFitMode` flag. Window resize re-fits image only when flag is true. Manual zoom (wheel, double-click, 1:1 button) sets flag false so resize does not clobber intentional zoom.
- **Image Viewer — 1:1 button resets window size**: `FitActualButton_Click` calls `AutoSizeWindow(currentImage)` then defers `ApplyCurrentZoom` at `DispatcherPriority.Loaded` — same window sizing used when first opening from a thumbnail.
- **Image Viewer — Default zoom is 1:1**: Images open and navigate at 100% actual pixels. `ApplyOneToOne()` replaces the old `FitToWindow()` call in both `ApplyStaticImage` and `LoadGifAsync`.
- **Image Viewer — GIF pause on click**: Single click with < 5 px movement toggles GIF playback via `ImageBehavior.GetAnimationController(DisplayImage)?.Pause()/.Play()`. Status bar flashes "⏸️ GIF Paused" / "▶️ Playing" for 800 ms.
- **Image Viewer — Zoom status bar**: Shows `Fit (X%)`, `100% (1:1)`, or `X%` driven by `_isInFitMode` flag rather than a raw zoom-level comparison.

### 🔒 Fixed
- **Image Viewer — `MouseDoubleClick` XAML compile error**: `Image` inherits `FrameworkElement`, not `Control`; `MouseDoubleClick` is not available as a XAML attribute. Removed from XAML; double-click now detected via `e.ClickCount > 1` in `MouseLeftButtonDown`.
- **Image Viewer — `ScrollToCenter` compile error**: `ScrollViewer` has no such method. Replaced with `ScrollToHorizontalOffset(ScrollableWidth/2)` + `ScrollToVerticalOffset(ScrollableHeight/2)` deferred at `DispatcherPriority.Render`.
- **Image Viewer — `SetIsPaused` compile error**: `ImageBehavior.SetIsPaused` does not exist in WpfAnimatedGif 2.0.2. Replaced with `ImageBehavior.GetAnimationController().Pause()/.Play()`.
- **Image Viewer — Mouse wheel had no effect**: `ScrollViewer.OnMouseWheel` is a WPF class handler that fires before XAML-attached instance handlers, marking the event handled and swallowing it. Fixed by switching to `PreviewMouseWheel` (fires during tunneling, before any class handler) and setting `e.Handled = true`.
- **Image Viewer — Keyboard navigation lost after click-away**: `ScrollViewer` is focusable by default; clicking back on the window moved keyboard focus there. ScrollViewer then handled `Left`/`Right` for scrolling, blocking navigation. Fixed by `Focusable="False"` on the ScrollViewer and `this.Activated += (s,e) => this.Focus()` to restore window focus whenever the viewer becomes active.
- **Image Viewer — Mouse capture could get stuck**: If the OS stole mouse capture (Alt-Tab, system dialog), `_isMouseDragging` was never cleared, corrupting subsequent pan deltas. Fixed by handling `LostMouseCapture` to reset drag state.

### ✨ Added
- **Context menu dark styling**: Media thumbnail right-click menus now use `NativeContextMenuStyle` (dark background, rounded corners, subtle hover highlight) matching tab context menus. ([Dev Note](docs/devnotes/2026-05-05-sprint-ui4-context-menu-styling.md))
- **Toggle checkboxes**: Label/Date/Time context menu items use static labels with indigo checkmarks instead of dynamic "Show"/"Hide" text. Click to toggle. Matches VS Code/Photoshop conventions.
- **Thumbnail context menu**: Right-click any media thumbnail for Copy, Delete, Size (Big/Medium/Small), Hide/Show, Show/Hide Label, Show/Hide Date, Show/Hide Time, and Rename. Uses MaterialDesignIcons for menu icons. Metadata changes persist via `MediaReferences` save/load bridge. ([Dev Note](docs/devnotes/2026-05-05-sprint-ui3.4-context-menu.md))

### 🔧 Changed
- **Context menu toggles**: Label/Date/Time menu items refactored from individual methods to generic `ToggleMediaBool` helper. Toggle items now use `IsCheckable=true` with `IsChecked` bound to `MediaReference` properties.
- **Media schema v3**: `MediaReferences` property on `MediaSection`/`NoteTab` bridges full paths ↔ `List<MediaReference>` for direct save/load. `TabManager.GetSaveData()` now writes `SavedNote.Media` directly (no longer `ImageFiles`/`ImageTimestamps`). Schema version bumped 2→3 in `MigrationService`. ([Dev Note](docs/devnotes/2026-05-05-sprint-ui3-2-save-load-bridge.md))
- **Thumbnail 3-row layout**: Containers now use a 3-row structure (image, label, timestamp) driven by `MediaReference` metadata. Container width is responsive to `ThumbnailSize` (default 150px). Label row is hidden by default (`ShowLabel=false`). `container.Tag` now stores `MediaReference` objects instead of string paths. ([Dev Note](docs/devnotes/2026-05-05-sprint-ui3-3-container-3-row-layout.md))

### 🔒 Fixed
- **B-SAVE: ContentCardStyle forward reference — repeating save corruption**: `ContentCardHoverStyle` (line 465) used `BasedOn="{StaticResource ContentCardStyle}"` but `ContentCardStyle` was defined at line 984. WPF `StaticResource` cannot forward-reference, causing `XamlParseException` on every startup → `LoadTabs` catch created blank tab → autosave wiped real notes in a loop. Fixed by moving `ContentCardStyle` before `ContentCardHoverStyle`. Also hardened `LoadTabs` catch to rethrow instead of silently creating a blank tab, and `LoadApplicationData` now sets `hasUnsavedChanges = false` on load failure so autosave cannot corrupt disk data. ([Bug log](docs/BUGS.md#b-save))
- **B-THEME: Theme toggle data loss**: Removed the Toggle Theme button entirely. LightTheme.xaml was missing 21 resource keys causing NoteTab.xaml to crash on load, silently overwriting all note data with an empty blank tab on every autosave. Data recovered from rolling backups. Settings.json forced back to dark mode. ([Bug log](docs/BUGS.md#b-theme))
- **PackIcon validation**: Fixed `Kind="Pushpin"` → `Kind="PinOutline"`. MDIX 5.3.1 does not have `Pushpin` or `ThumbTack`. Verified all 8 title bar icon names against the installed DLL binary.

### ✨ Added
- **Title bar icon swap**: Replaced emoji buttons with MaterialDesign PackIcons for consistent sizing and visual weight across all 7 title bar buttons (8 minus removed theme toggle)
- **Title bar button alignment**: Defined `TitleBarButtonStyle` and `TitleBarPinButtonStyle` with fixed `32x28` sizing, vertical centering, and proper pin state (indigo background + white icon). Added to both dark and light themes.
- **Single Source of Truth — master.json**: Consolidated all data into a single `master.json` file
  - Replaces separate `notes.json` and `notewindows.json` files
  - `MasterData` model: `{ Version, Windows: List<NoteWindowData>, Settings }`
  - `MigrationService` handles migration from legacy format on first run
  - Backward-compatible — legacy files preserved until migration completes
  - See Sprint A — Data Layer Cleanup

- **MediaReference Model**: Media stored as filename-only references in JSON
  - `MediaReference` model: `{ Filename, DateAdded, FullPath (computed) }`
  - Full path resolved at runtime from `%AppData%\SnipShottyBoard\images\`
  - JSON file size drastically reduced (no full paths stored)
  - `SavedNote.ImageFiles` / `ImageTimestamps` marked `[Obsolete]` with backward-compat accessors
  - See Sprint A — Data Layer Cleanup

- **Lazy-Loading Media Vault**: Thumbnails now load on-demand as they scroll into view
  - Eliminates startup lag with large image collections
  - Images load progressively as user scrolls through the vault
  - Significantly reduces initial memory footprint
  - See Sprint B — Memory & GIF Cache

- **Dual-Eviction LRU Cache**: Image cache with dual limits (100 images AND 100MB)
  - `ImageCacheManager` implements Least Recently Used eviction
  - Evicts by count (100) OR size (100MB), whichever triggers first
  - Prevents memory exhaustion with large image collections
  - See Sprint B — Memory & GIF Cache

- **GIF Static Thumbnails**: GIFs display as static frames in vault until opened
  - Only full-screen ImageViewerWindow animates GIFs
  - Dramatically reduces CPU and memory usage in Media Vault
  - See Sprint B — Memory & GIF Cache

### 🎨 Changed
- **Visual Overhaul — Deep Dark Chrome**: Complete UI redesign with premium aesthetic
  - **Phase 6A (Foundation)**: Deep chrome palette (#111113 background, #18181B cards)
    - Indigo/purple gradient system (AccentGradientBrush: #6366F1 → #8B5CF6)
    - Subtle border tints (#27272A) — zero visual noise
    - Glow effect system (EditorFocusGlow, AccentGlowEffect)
    - All colors via `{DynamicResource X}` — no inline hex values
  - **Phase 6B (Tab Strip)**: Edge-style rectangular tabs with gradient underline
    - Replaced old tab styling with modern rectangular design
    - Active tab uses gradient underline instead of solid color
    - Improved hover/pressed state animations
  - **Phase 6C (Editor)**: Borderless text surface with focus glow ring
    - Removed editor borders for clean, minimal appearance
    - Focus state triggers subtle glow ring (EditorFocusGlow)
    - Borderless design matches premium Notion-like aesthetic
  - Theme tokens documented in DarkTheme.xaml (AccentBrush, AppBackgroundBrush, etc.)

### 🐛 Fixed
- **Data Persistence — All saved data lost on restart**: `AtomicFileManager` serialized with camelCase keys (`"windowLeft"`, `"windows"`, etc.) but deserialized with default PascalCase matching → nothing matched → all properties returned defaults → blank app every startup. Fixed all 4 `Deserialize` calls across `LoadWithRecovery`, `VerifyJsonFile`, and `TryRollingBackupRecovery` to use matching `CamelCase` + `PropertyNameCaseInsensitive` options. See Sprint P — Data Persistence Fix
- **GIF Viewer — Thread ownership crash + missing status bar**: `LoadGifAsync` created `BitmapImage` inside `Task.Run()` on a background thread → `SetAnimatedSource` threw `InvalidOperationException` on UI thread. Fixed by creating `BitmapImage` directly on UI thread (GIFs use `OnDemand` lazy decode, no blocking needed). Added missing `currentImage = bitmap` assignment so status bar shows info and clipboard copy works. See Sprint D Phase 1b
- **GIF Viewer — Navigation memory leak**: Cycling through GIFs via left/right arrows caused unbounded memory growth. Added `ClearPreviousImage()` called at the start of every `LoadImage()` to release GIF decoders, clear static sources, and purge old cache entries. Added GIF decoder cleanup to `ReleaseImageResources()` on window close. See Sprint D Phase 2
- **GIF Viewer — Window close memory leak**: Closing the viewer after a GIF left ~120MB permanently stuck. Root cause: `BitmapCacheOption.OnDemand` keeps a file stream + native decoder thread alive even after managed references are nulled. Fixed by disposing `StreamSource` on unfrozen bitmaps, then forcing `GC.Collect()` + `WaitForPendingFinalizers()` to reclaim unmanaged memory. Added `[CLS]` close-path logging with before/after RAM snapshots. See Sprint D Phase 3

### 🛡️ Reliability
- **Crash Recovery Buffer**: Silent background journaling captures unsaved text every 2 seconds
  - `master.json.recovery` written atomically when text is dirty (2s interval)
  - Startup auto-merges recovery snapshot silently — no modals, no prompts
  - Recovery snapshot ignored if >1 hour old (stale data)
  - Recovery file cleared on successful auto-save and on clean close
  - Multi-window aware: matches recovered windows by ID, notes by title
  - See Sprint C — Crash Recovery Buffer
  - See Visual Overhaul 6A-6C

### 🔧 Technical
- **Data Architecture**: Single-file persistence reduces I/O complexity and corruption risk
- **Memory Management**: LRU cache + lazy-loading + static GIF thumbnails = smooth performance with 50+ images
- **Theme System**: All colors centralized in DarkTheme.xaml / LightTheme.xaml — no hardcoded values

---

## [1.6.0] - 2025-11-23
### 🚨 Fixed (CRITICAL)
- **Data Overwrite Bug in Migration Logic**: Fixed dangerous auto-migration logic that could overwrite recent data with stale legacy data
  - **IMPACT**: Without this fix, app startup could load old `notes.json` data and overwrite `notewindows.json`
  - **ROOT CAUSE**: `MainWindow.EnsureMainWindowHasData()` compared note counts without checking timestamps
  - **FIX**: Migration now only occurs on first-ever run when `notewindows.json` doesn't exist
  - **SAFETY**: Existing data is always trusted and never auto-overwritten
  - **RECOVERY**: Atomic backups (Phase 4D P2.3) enabled successful data recovery
  - See [Incident Report: Phase 4 Data Overwrite](devnotes/2025-Phase4-Incident-NoteDataOverwrite.md) for full analysis

### ✨ Added
- **Automatic Orphaned Image Cleanup**: App now automatically cleans up unused image files on startup
  - Runs in background 5 seconds after launch (no UI impact)
  - Only deletes images not referenced in any note AND older than 7 days (grace period)
  - Prevents disk space waste from deleted/moved images
  - See [Dev Note: Phase 4D Cleanup & Sanity Pass](devnotes/2025-Phase4d-CleanupAndSanityPass.md)

- **Data Versioning for Future Migrations**: All saved data now includes version fields
  - `SavedNote.DataVersion` and `AppData.Version` properties added (current: v1)
  - Load methods detect and log warnings when loading data from newer app versions
  - Enables safe schema migrations in future releases without data loss
  - See [Dev Note: Phase 4D Cleanup & Sanity Pass](devnotes/2025-Phase4d-CleanupAndSanityPass.md)

- **Performance Timing Logs**: All critical operations now log execution time
  - Save/load operations, image cleanup tracked with millisecond precision
  - Logged to "Perf" category for performance monitoring and diagnostics
  - Helps identify performance regressions and bottlenecks
  - See [Dev Note: Phase 4D Cleanup & Sanity Pass](devnotes/2025-Phase4d-CleanupAndSanityPass.md)

### 🔧 Changed
- **GIF Animation Limit Enforced**: Maximum 5 animated GIFs per note (prevents memory exhaustion)
  - User-friendly warning dialog explains memory impact
  - Existing notes with >5 GIFs still load (no retroactive enforcement)
  - Constant: `AppConstants.MaxAnimatedGifsPerNote = 5`
  - See [Dev Note: Phase 4D Cleanup & Sanity Pass](devnotes/2025-Phase4d-CleanupAndSanityPass.md)

- **Atomic File Writes**: All data persistence now uses crash-safe atomic operations
  - Write to temp file → create backup → atomic rename (prevents corruption if crash during save)
  - Automatic rolling backups (20 most recent saves kept)
  - Recovery from backup if primary file corrupted
  - Migrated `SaveNotes()`, `SaveSettings()`, `SaveNoteWindows()` to `AtomicFileManager`
  - See [Dev Note: Phase 4D Cleanup & Sanity Pass](devnotes/2025-Phase4d-CleanupAndSanityPass.md)

- **Improved Null Safety**: Strategic null guards added to high-risk public methods
  - `TabManager.SelectTab()`, `TabManager.UpdateSettings()`, `DataManager.SaveNotes()`
  - Null violations logged with `ArgumentNullException` for diagnostics
  - Prevents rare NullReferenceException edge cases
  - See [Dev Note: Phase 4D Cleanup & Sanity Pass](devnotes/2025-Phase4d-CleanupAndSanityPass.md)

- **Event Handler Cleanup**: `TabManager` now implements `IDisposable` for proper resource cleanup
  - Clears all event subscribers on dispose (prevents memory leaks)
  - Clears drag state and references
  - Foundation for comprehensive disposal pattern across managers
  - See [Dev Note: Phase 4D Cleanup & Sanity Pass](devnotes/2025-Phase4d-CleanupAndSanityPass.md)

### 🔧 Fixed
- **DialogHelper Nullable Warnings**: Resolved all 6 nullable reference warnings in UI/DialogHelper.cs
  - Properties initialized to `string.Empty` or made nullable as appropriate
  - Optional method parameters made explicitly nullable (`Exception?`, `DialogButtonConfig?`)
  - Zero behavior changes—compile-time safety improvements only
  - See [Dev Note: Phase 4C.5 Build Fix Audit](devnotes/2025-Phase4c-BuildFixAudit.md)

### 📚 Docs
- **Phase 4D Cleanup & Sanity Pass**: Completed P2 (Nice to Have) refactoring items
  - 7 of 8 P2 items implemented (~19 hours of work)
  - Focus: Future-proofing, defensive coding, performance diagnostics
  - Zero breaking changes, all improvements additive or internal
  - Build status: 0 errors, 262 warnings (unchanged)
  - See [Dev Note: Phase 4D Cleanup & Sanity Pass](devnotes/2025-Phase4d-CleanupAndSanityPass.md)

- **Phase 4C.5 Build Audit**: Comprehensive XAML generation and build system health check
  - Verified all XAML-generated files (.g.cs) working correctly
  - Confirmed Build Action metadata correct for all XAML files
  - Build succeeds with 0 errors, 262 warnings (reduced from 268)
  - See [Dev Note: Phase 4C.5 Build Fix Audit](devnotes/2025-Phase4c-BuildFixAudit.md)

- **Phase 1 Foundation Setup**: Created documentation infrastructure for comprehensive revamp
  - Created folder structure: `docs/audit/`, `docs/archive/`, `docs/masterprompts/`, `docs/sessionsummary/`
  - Created placeholder files: `MASTER_SOT.md`, `SOT_MAP.md`
  - Created 5 master prompt placeholders for Phase 3
  - See [Dev Note: Phase 1 Foundation Setup](devnotes/2025-11-20-Phase1-FoundationSetup.md)

---

## [1.5.0] - 2025-10-01
### ✨ Added
- **Per-Tab Splitter Persistence**: Each tab now remembers its own Text/Media splitter position independently
  - Stored as ratio (0.0-1.0) for DPI-safe scaling across window sizes
  - Saved in `SavedNote.SplitterTextMediaRatio` per tab
  - Clamped to safe bounds (20%-80%) to prevent panel collapse
  - See [Dev Note: Splitter & Titlebar](docs/devnotes/2025-10-01-splitter-persist-and-titlebar-buttons.md)
- **Pin Button (Always on Top)**: New 📌 button in titlebar to toggle `Window.Topmost` 
  - Visual state indicator: Semi-transparent blue background + accent border when ON
  - Uses Tag property pattern for persistent visual state (not overridden by hover triggers)
  - State persists across app restarts
  - Tooltip updates dynamically: "Always on top: On/Off"
- **Minimize Button**: New − button in titlebar for quick window minimization
  - Replaced "📁" logs folder button (relocated to Developer menu 🔧)

### 🔧 Changed
- **Window Size Persistence**: Window dimensions now save on **every close** (not just when content changes)
  - Fixes issue where window size was lost on restart if no edits were made
- **Titlebar Button Layout**: Reordered to group window controls together
  - **Before**: `[+] [drag] [📝] [📁] [⚙️] [🗑️] [🌙] [?] [🔧] [×]`
  - **After**: `[+] [drag] [📝] [⚙️] [🗑️] [🌙] [?] [🔧] [📌] [−] [×]`
- **Developer Menu Tooltip**: Updated to "Developer tools, diagnostics & logs"

### 🐛 Fixed
- **Pin Button Visual**: Partially fixed visual state persistence issue
  - **Root cause**: Programmatic `Background` setters were overridden by `HeaderButtonStyle` hover triggers
  - **Solution**: Tag property pattern (`Tag="Pinned"`) with style trigger that overrides hover state
  - Visual now persists when mouse moves away (not tied to hover state)
  - **Known Issue**: Visual contrast may still need improvement for better visibility when ON

### 📚 Docs
- Created [docs/devnotes/2025-10-01-splitter-persist-and-titlebar-buttons.md](docs/devnotes/2025-10-01-splitter-persist-and-titlebar-buttons.md)
  - Documents per-tab splitter ratio algorithm and persistence strategy
  - Explains Tag-based pin button visual pattern and why it works
  - Full testing acceptance criteria and edge cases
  - Performance characteristics and limitations

---

## [1.4.0] - 2025-10-01
### 🎉 Multi-Row Tab Wrapping (Major UX Enhancement)
- **Responsive Tab Strip**: Tabs now automatically wrap into multiple rows when window width is reduced (Edge-like behavior)
- **Row-Aware Drag & Drop**: Drop indicator positions correctly across rows; drag any tab to any row
- **Keyboard Arrow Navigation**: 
  - **Left/Right**: Navigate tabs sequentially with automatic row wrapping
  - **Up/Down**: Move between rows while maintaining horizontal position
  - **Home/End**: Jump to absolute first/last tab across all rows
  - **Smart Context**: Arrow keys only navigate tabs when NOT in text input (preserves text editing)
- **Vertical Scrollbar**: Tab strip now scrolls vertically when exceeding 200px height (replaces horizontal scroll)
- **Tab Sizing Constraints**: Min width 80px (readable), max width 200px (prevents over-expansion)

### 🔧 Technical Implementation
- **WrapPanel Layout**: Replaced `StackPanel` with `WrapPanel` for automatic row wrapping
- **Row Detection**: Groups tabs by Y-position with 5px tolerance (AppConstants.TabRowGroupingTolerance)
- **Row-Aware Drop Target**: `FindDropTargetIndex()` detects target row, then finds insertion point within that row
- **Row-Aware Drop Indicator**: `UpdateDropIndicator()` positions indicator using both X and Y coordinates
- **Coordinate Transforms**: All drag/drop logic uses MainWindow as common ancestor for multi-row positioning
- **Keyboard Navigation Logic**: Calculates row/column grid layout on-demand for arrow key traversal
- **AppConstants**: Added tab configuration constants (TabMinWidth, TabMaxWidth, TabStripMaxHeight, TabRowGroupingTolerance, TabDragHysteresisBuffer)

### 🐛 Fixed
- **Horizontal Scroll Removed**: No more hidden tabs - all tabs visible via row wrapping
- **Drop Indicator Positioning**: Now correctly positions in target row (not just first row)
- **Keyboard Nav Wrapping**: Left/Right navigation wraps at row edges (end of row → start of next row)

### 📚 Documentation
- **Docs Restructuring**: Normalized documentation layout and governance
  - Created `docs/devnotes/` for task-scoped implementation notes
  - Created `docs/adr/` for future architectural decision records
  - Moved `DEV_NOTES.md` → `docs/devnotes/2025-10-01-tabs-multiline-wrapping.md`
  - Added front-matter to Dev Note (Title, Date, Owner, Versions, Links)
  - **CR.md**: Rewritten § Tabs Pattern as normative spec (removed code/values)
  - **CR.md**: Added § Docs Governance appendix (defines CR vs Dev Notes split)
  - **CR.md**: Added Promotion Rule to Change Management
  - Cross-links established: CR.md ↔ Dev Note (bidirectional)
- **Dev Note (2025-10-01)**: Comprehensive implementation details for multi-row tabs
  - Why multi-row wrapping was implemented
  - Architecture changes (XAML, row detection, coordinate transforms)
  - Drag & drop algorithms and coordinate math
  - Keyboard navigation grid calculation
  - Edge cases & error handling
  - Performance characteristics and testing checklist
- **Code Comments**: Enhanced inline documentation for row-aware logic

### 📊 Performance
- Row layout calculated on-demand (O(n) complexity, no caching)
- Drag updates: ~60 times/second during drag (recalculates rows each move)
- Keyboard nav: Single calculation per arrow key press
- Excellent performance for typical usage (<50 tabs)

### 🎯 Visual Tree Changes
```
MainWindow
└── Grid
    ├── ScrollViewer (vertical scroll, max 200px height)
    │   └── WrapPanel (multi-row wrapping)
    │       └── Button (tabs: min 80px, max 200px)
    └── Canvas (ZIndex=9999, drag overlay)
        ├── Border (gray ghost tab, alpha 140)
        └── Border (blue drop indicator, row-aware)
```

### ✨ User Experience Improvements
- **Always Visible Tabs**: All tabs remain visible via row wrapping (no horizontal scroll)
- **Natural Navigation**: Arrow keys feel intuitive for multi-row layouts
- **Consistent Styling**: Edge-like tab appearance maintained across all rows
- **Drag Across Rows**: Can drag first tab to last row seamlessly
- **Accessible**: Keyboard-only users can navigate all tabs efficiently

### 🔮 Future Enhancements (Noted for consideration)
- Tab pinning (always show in first row)
- Row animations (smooth wrap/unwrap transitions)
- User-configurable max height
- Touch gesture support
- Tab groups with visual separators

---

## [1.3.0] - 2025-01-01
### 🎨 Tab Drag-and-Drop UX Enhancement
- **Drop Indicator**: Added blue vertical line (3px wide) that shows exactly where tabs will be inserted during drag operations
- **Drag Visual**: Implemented semi-transparent gray ghost tab that follows cursor during drag (changed from blue to gray for better contrast with drop indicator)
- **Edge-like Styling**: Redesigned tabs with Microsoft Edge-inspired appearance
  - Rounded top corners (3px radius)
  - Blue accent underline (2px) on active tabs
  - Medium font weight for selected tabs
  - Smooth hover and pressed state animations
- **Hysteresis System**: Added 5px buffer to prevent indicator flicker when hovering near tab boundaries
- **Improved Drop Detection**: Enhanced FindDropTargetIndex to use tab midpoints for intuitive drop zones
- **Visual Transparency**: Drag visual uses more transparent gray (alpha 140 from 200) to show background content

### 🐛 Fixed
- **Coordinate Transform Bug**: Fixed "Visual is not an ancestor" exception by using MainWindow as common ancestor for both dragCanvas and tab buttons
- **Drop Position Detection**: Fixed drop indicator only appearing at end - now correctly shows between all tabs
- **Build Error**: Removed empty GifFramePlayer.xaml and GifFramePlayer.cs files that were causing XML parse errors
- **Visual Conflict**: Changed drag visual from blue to gray to prevent hiding the blue drop indicator line

### 🔧 Technical
- **Coordinate Transformations**: Implemented proper WPF coordinate transforms using TransformToAncestor(MainWindow)
- **Tag-Based Selection**: Active tab styling now uses Tag="Selected" property to drive XAML triggers (cleaner than code-behind)
- **Theme Resources**: Added AccentBrush (#4A90E2) to both DarkTheme.xaml and LightTheme.xaml
- **Drag Canvas**: Full-window overlay canvas with ZIndex=9999 contains both drag visual and drop indicator
- **Index Calculation**: ReorderTab properly handles forward vs backward movement with insert index adjustment
- **Debug Logging**: Comprehensive OnLogDebug calls throughout drag system (start, move, drop, coordinates, indices)

### 📊 Performance
- Drag visual updates only on mouse move (not continuous)
- Coordinate transforms cached per frame (not per tab)
- Hysteresis reduces unnecessary indicator position updates
- Visual tree changes batched (remove then insert, not swap)

### 📚 Documentation
- **CR.md Section 1.5**: Added comprehensive "Tab Drag-and-Drop System" documentation
  - Drag flow diagrams and code examples
  - Coordinate transformation logic explanation
  - Hysteresis implementation details
  - ReorderTab index calculation (forward/backward cases)
  - Visual tree structure
  - Edge cases and performance considerations

### 🎯 Visual Tree Changes
```
MainWindow
└── Grid
    ├── ScrollViewer (tab strip)
    │   └── StackPanel (tabs)
    └── Canvas (ZIndex=9999, drag overlay)
        ├── Border (gray ghost tab, alpha 140)
        └── Border (blue drop indicator, 3px wide)
```

## [1.2.1] - 2024-12-28
### 🔧 Post-Audit Hardening & Consistency
- **Nullable Warnings Reduction**: Reduced from 274 to 246 nullable warnings by fixing critical Manager and Data layer issues
- **Debug Output Cleanup**: Removed 100+ excessive Console.WriteLine statements from ImageViewerWindow GIF loading
- **Logging Consistency**: Standardized all Data layer logging with consistent "Data:" prefix categories
- **TODO Markers Added**: Added TODO markers for GIF disposal improvements and UI-to-Data separation opportunities
- **Build Stability**: Maintained 0 compilation errors while improving code quality

### 🐛 Fixed
- **MediaSection File I/O**: Added TODO markers for file operations that should move to DataManager
- **ImageViewerWindow Debug Spam**: Cleaned up hundreds of debug statements while preserving essential functionality
- **Manager Events**: Fixed nullable event declarations in SettingsManager and ThemeManager
- **Constants Usage**: Applied AppConstants window dimensions consistently across NoteWindowManager and AppSettings

### 🔧 Technical
- **Architecture Compliance**: Verified no System.Windows dependencies in Data layer
- **Layer Separation**: Documented remaining UI → Data violations for future cleanup
- **Resource Management**: Prepared foundation for ThemeResourceHelper adoption
- **Configuration**: Confirmed MCP server environment variable support

## [1.2.0] - 2024-12-28
### 🏗️ Architecture & Code Quality Improvements
- **Nullable Reference Types**: Enabled nullable reference types across the entire codebase for better type safety
- **Centralized Constants**: Created AppConstants class with all magic numbers and configuration values
- **Enhanced Logging**: Upgraded LoggingService to use Serilog with structured logging, file rotation, and category-based organization  
- **Architecture Compliance**: Removed UI → Data layer violations, moved all file operations to DataManager
- **Theme Resource Validation**: Added ThemeResourceHelper with safe resource access and fallback handling
- **MCP Server Configuration**: Made MCP server project path configurable via SSB_PROJECT_ROOT environment variable

### 🐛 Fixed
- **Cross-Layer Violations**: Removed System.Windows dependency from Data layer (DataManager.cs)
- **Magic Numbers**: Replaced hardcoded values with AppConstants throughout UI components
- **Direct File Access**: UI components now use DataManager instead of direct File.* operations
- **Resource Access**: Added safe theme resource access with graceful degradation

### 🔧 Technical
- Added comprehensive image management methods to DataManager (SaveImageFromClipboard, CopyDroppedImage, DeleteImage, ValidateImageFile)
- Enhanced LoggingService with Serilog backend, daily log rotation, and structured logging with categories
- Created ThemeResourceHelper for safe resource access with type checking and fallbacks
- Updated MediaSection and KeyboardHandler to use DataManager for all file operations
- Replaced magic numbers in MainWindow, MediaSection, and ImageViewerWindow with AppConstants
- Made MCP server configurable with environment variables for flexible development setups

### 📚 Documentation
- **LOGGING.md**: Comprehensive logging guide with examples, monitoring tips, and troubleshooting
- **MCP_SETUP.md**: Complete MCP server setup and configuration guide
- **Enhanced CR.md**: Updated compliance status and architecture validation

## [1.1.0] - 2024-12-23
### 🎉 Added
- **Rich Text Editing**: Converted TextBox to RichTextBox with comprehensive formatting support
- **Keyboard Shortcuts**: Added rich text formatting shortcuts (Ctrl+B, Ctrl+I, Ctrl+U, Ctrl+S, Ctrl+., Ctrl+L, Tab, Shift+Tab)
- **Formatting Features**: Bold, italic, underline, strikethrough, bullet points, numbered lists, and text indentation
- **RTF Storage**: Rich text content is now stored in RTF format for preserved formatting
- **Backward Compatibility**: Existing plain text notes are automatically converted and preserved

### 🐛 Fixed
- **Bullet Points**: Fixed bullet points implementation to actually add bullet characters (•) instead of just indentation
- **Numbered Lists**: Added proper numbered list support with automatic numbering (1., 2., 3., etc.)

### 🔧 Technical
- Replaced TextBox with RichTextBox in TextSection.xaml while maintaining identical styling
- Added RTF content storage in SavedNote.RichTextContent property
- Implemented rich text formatting methods in TextSection.xaml.cs with proper bullet (•) and numbered list (1., 2., 3.) characters
- Enhanced KeyboardHandler.cs with rich text formatting shortcuts including Ctrl+L for numbered lists
- Updated TabManager.cs to handle RTF content loading and saving
- Added rich text formatting event handling in MainWindow.xaml.cs
- Maintained all existing functionality including autosave, placeholder text, and scrolling

## [1.0.6] - 2024-12-23
### 🐛 Fixed
- **GIF Animation**: Fixed GIF animation by using BitmapImage instead of BitmapDecoder frames
- **ImageViewerWindow Close Button**: Fixed close button color to be white instead of themed color

### 🔧 Technical
- Fixed GIF animation by using BitmapImage with proper settings instead of BitmapDecoder frames
- Implemented multiple GIF loading methods with proper fallbacks and comprehensive debugging
- Added comprehensive debugging for GIF animation detection and loading issues

## [1.0.5] - 2024-12-23
### 🐛 Fixed
- **TextSection Scroll**: Added proper mouse wheel scrolling support to text areas

### 🔧 Technical
- Added `PreviewMouseWheel` event handling to `TextSection` for natural scrolling
- Added `System.Windows.Input` using statement to TextSection.xaml.cs

## [1.0.4] - 2024-12-23
### 🐛 Fixed
- **Theme Loading**: Eliminated unnecessary light theme application on startup - now loads saved theme directly

### 🔧 Technical
- Updated `MainWindow` constructor to load settings before theme initialization
- Modified `LoadApplicationData()` to avoid double-loading theme settings

## [1.0.3] - 2024-12-23
### 🐛 Fixed
- **Note Window Memory**: Each note window now loads its own individual data and tabs instead of sharing content

### 🔧 Technical
- Modified `NoteListWindow.OpenNoteWindow()` to pass specific `NoteWindowData` to `MainWindow` constructor
- Updated `MediaSection.ShowFullSizeImage()` to pass full image list for navigation
- Enhanced `ImageViewerWindow` with navigation support and improved GIF loading

## [1.0.2] - 2024-12-23
### 🎉 Added
- **Image Navigation**: Added arrow key navigation (Left/Right) to cycle through images in ImageViewerWindow

### 🔧 Technical
- Added navigation support constructor to `ImageViewerWindow`
- Added `NavigateToPreviousImage()` and `NavigateToNextImage()` methods
- Added Left/Right arrow key handling in keyboard shortcuts
- Updated `MediaSection.ShowFullSizeImage()` to pass full image list and current index

## [1.0.1] - 2024-12-23
### 🎉 Added
- **CHANGELOG.md**: Added comprehensive changelog with semantic versioning
- **Documentation**: Added detailed documentation for all recent fixes and improvements

### 🔧 Technical
- Created CHANGELOG.md following Keep a Changelog format
- Added semantic versioning tracking for all changes

## [1.0.0] - 2024-12-15
### 🎉 Added
- Initial release of SnipShottyBoard
- Multi-tab note taking interface
- Dark/Light theme support
- Image paste and management
- Auto-save functionality
- Multiple note window support
- Settings management system 