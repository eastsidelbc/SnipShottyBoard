# 📝 SnipShottyBoard Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### 🏗️ Architecture Refactoring
- **Phase C - Sticky Notes Architecture Refactoring** (2024-12-02): Major architecture overhaul to treat all workspace windows as equal peers
  - **DATE**: 2024-12-02
  - **COMPLEXITY**: High (Multi-file refactoring + bug fixes)
  - **PHILOSOPHY**: "Sticky notes" paradigm - every workspace window is an equal peer, no "main" window concept
  
  #### Renamed Components (Phase C.5)
  - `MainWindow` → `WorkspaceWindow`
  - `MainWindowViewModel` → `WorkspaceWindowViewModel`
  - `NoteListWindow` → `WorkspaceManagerWindow`
  - `NoteListWindowViewModel` → `WorkspaceManagerWindowViewModel`
  - `NoteListWindowManager` → `WorkspaceManagerService`
  
  #### Critical Bug Fixes
  - **NotesEngine Overwrite Bug** (🔴 CRITICAL): Fixed data loss where only one workspace was saved, overwriting entire file
    - **ROOT CAUSE**: `NotesEngine` was saving only the current workspace instead of read-modify-write pattern
    - **FIX**: Implemented proper read-modify-write: load all workspaces → update one → save all
  - **First Workspace Undeletable**: Fixed bug where first workspace (Workspace A) couldn't be deleted
    - **ROOT CAUSE**: App shutdown mode was `OnMainWindowClose`, causing app exit when first window closed
    - **FIX**: Changed to `ShutdownMode.OnLastWindowClose` + mark deleted BEFORE closing window
  - **Duplicate Workspaces**: Fixed duplicates appearing after creating new note window
    - **FIX**: Added defensive deduplication logic and proper ID validation
  - **Resurrection Bug**: Fixed deleted workspaces reappearing when creating new workspace
    - **ROOT CAUSE**: `CreateNewWorkspace` used `LoadWorkspacesAsync()` which includes deleted workspaces
    - **FIX**: Changed to `LoadActiveWorkspacesAsync()` to only load non-deleted workspaces
  - **Startup Deadlock**: Fixed app freezing on startup
    - **ROOT CAUSE**: Sync-over-async deadlock from `.GetAwaiter().GetResult()` on UI thread
    - **FIX**: Created synchronous `CreateStartupWindows()` with `Task.Run()` for background I/O
  - **StickyTheme Duration Type**: Fixed `InvalidCastException` from `TransformTransition` error
    - **ROOT CAUSE**: `Duration="{StaticResource Duration.Fast}"` used Double instead of TimeSpan
    - **FIX**: Changed to `Duration="0:0:0.15"` (proper TimeSpan format)
  
  #### New Features
  - **Save Locking**: Added `SemaphoreSlim` for concurrency safety during workspace saves
  - **Backup Organization**: Backup files now stored in `/backups` subfolder (cleaner AppData)
  - **Unique Note IDs**: Added `Guid Id` to `SavedNote` model for unique identification
  - **Trash UI Enhancements**:
    - "Delete All" button (red) to permanently delete all trashed workspaces
    - Individual "Delete" buttons next to each "Restore" button
  - **WorkspaceManagerWindow Taskbar Icon**: Added `Icon="/Assets/avalonia-logo.ico"`
  - **Deep Copy on Load**: `LoadWorkspacesAsync` returns deep copies to prevent object sharing between ViewModels
  - **Note ID Migration**: Automatic migration assigns unique IDs to notes without them
  
  #### Removed
  - `RecoverWorkspacesFromAllFilesAsync` method (dangerous, could resurrect deleted workspaces)
  - `Design.DataContext` from XAML files (caused issues with ViewModels requiring constructor parameters)
  
  #### Files Created
  - `docs/devnotes/12-02/2025-12-02-PHASE_C_ARCHITECTURE_FIXES.md` (~400 lines)
  - `docs/devnotes/12-02/2025-12-02-PHASE_C5_WINDOW_RENAME.md` (~116 lines)
  
  #### Files Renamed
  - `MainWindow.axaml` → `WorkspaceWindow.axaml`
  - `MainWindow.axaml.cs` → `WorkspaceWindow.axaml.cs`
  - `MainWindowViewModel.cs` → `WorkspaceWindowViewModel.cs`
  - `NoteListWindow.axaml` → `WorkspaceManagerWindow.axaml`
  - `NoteListWindow.axaml.cs` → `WorkspaceManagerWindow.axaml.cs`
  - `NoteListWindowViewModel.cs` → `WorkspaceManagerWindowViewModel.cs`
  - `NoteListWindowManager.cs` → `WorkspaceManagerService.cs`
  
  #### Files Modified
  - `App.axaml.cs` (shutdown mode, startup logic, static WorkspaceWindowManager)
  - `WorkspaceEngine.cs` (save locking, deep copies, migration, removed recovery method)
  - `IWorkspaceEngine.cs` (removed recovery method)
  - `NotesEngine.cs` (read-modify-write pattern fix)
  - `AtomicFileManager.cs` (backup subfolder, reduced max backups to 10)
  - `WindowStateService.cs` (synchronous loading for startup)
  - `WorkspaceWindowManager.cs` (synchronous startup, proper ViewModel creation)
  - `SavedNote.cs` (added `Guid Id` property)
  - `NoteWorkspace.cs` (updated `Clone()` for note IDs)
  - `StickyTheme.axaml` (fixed Duration type)
  
  #### Testing Verified
  - ✅ Delete all workspaces → Empty state shows correctly
  - ✅ Create new workspace after deleting all → Only new workspace appears (no resurrection)
  - ✅ Deleted workspaces stay in Trash permanently
  - ✅ Restore from Trash works correctly
  - ✅ Multiple workspace windows can be open simultaneously
  - ✅ App starts without freezing
  - ✅ Taskbar icons show for all windows
  
  #### Deferred to Future Phases
  - Phase C.3: Separate window state from workspace data (medium priority)
  - Phase C.4: Consolidate save logic (medium priority)

### 🔍 Diagnostics
- **Phase 9.7.2 - Workspace ID Resolution Diagnostics** (2024-11-25): Added comprehensive logging to trace workspace ID flow
  - **DATE**: 2024-11-25
  - **COMPLEXITY**: Low (Logging enhancement)
  - **PURPOSE**: Investigate reported bug where different workspaces show same content
  - **ADDED**: Enhanced logging with `═══` markers for workspace selection flow
  - **ADDED**: Workspace note count and titles in logs
  - **ADDED**: Workspace ID tracking at each pipeline stage
  - **RESULT**: Confirmed workspace IDs are distinct and correctly propagated
  - **RESULT**: Confirmed workspace data is distinct in WorkspaceEngine
  - **FINDING**: Window tracking may have issues (windows not retained in _openWindows)
  - **FILES MODIFIED**: 3 (NoteListWindowViewModel.cs, WorkspaceWindowManager.cs, MainWindowViewModel.cs, ~30 lines)
  - **NEXT STEPS**: Run acceptance tests to reproduce user-reported issue

### ✨ Features
- **Phase 9.7.1 - Multi-Window Workspace Initialization Fix** (2024-11-25): Fixed tabs and buttons in workspace windows
  - **DATE**: 2024-11-25
  - **COMPLEXITY**: Medium (Architecture fix)
  - **BUG**: Workspace windows opened from NoteListWindow had no tabs and non-functional buttons
  - **ROOT CAUSE**: `WorkspaceWindowManager` was setting window DataContext to `WorkspaceViewModel` instead of `MainWindowViewModel`
  - **IMPACT**: New Tab button, Folder button, and tab strip were not working in multi-window mode
  - **FIX**: Create proper `MainWindowViewModel` for each workspace window
  - **ADDED**: `InitializeWithWorkspaceAsync()` method to initialize with specific workspace (bypasses last-open logic)
  - **ADDED**: `App.WindowManager` static property for multi-window access to singleton
  - **TESTING**: ✅ Opened workspace from NoteListWindow - tabs visible, buttons functional
  - **FILES MODIFIED**: 3 (WorkspaceWindowManager.cs, MainWindowViewModel.cs, App.axaml.cs, ~120 lines)
  - **MICROLEARNING**: MVVM DataContext requirements, static service access patterns, multi-window initialization

- **Phase 9.7 - NoteListWindow Refinements** (2024-11-25): Professional, predictable workspace management
  - **DATE**: 2024-11-25
  - **COMPLEXITY**: Medium (UI/UX refinements + validation)
  - **GHOST CARD FIX**: Added workspace validation to filter invalid entries (empty GUID, null title, null Notes)
  - **SINGLETON BEHAVIOR**: NoteListWindow now singleton - clicking folder button shows/focuses existing window
  - **DELETE WORKSPACE**: Added 🗑 button per workspace card with proper cleanup (closes window, persists changes)
  - **ALWAYS SHOW TABS**: Verified tab strip always visible (already implemented in Phase 9.2)
  - **WORKSPACE VALIDATION**: Filters out invalid workspaces with detailed logging
  - **DE-DUPLICATION**: Removes duplicate workspaces by ID (takes first occurrence)
  - **TESTING**: ✅ Tested with 3 workspaces, delete functionality, singleton behavior
  - **FILES CREATED**: 1 (NoteListWindowManager.cs, ~80 lines)
  - **FILES MODIFIED**: 5 (NoteListWindowViewModel.cs, MainWindowViewModel.cs, WorkspaceEngine.cs, WorkspaceWindowManager.cs, NoteListWindow.axaml, ~300 lines)
  - **MICROLEARNING**: Singleton pattern for windows, workspace validation strategies, graceful degradation
  - See [Phase 9.7 Audit](audits/11-25/2024-11-25-phase-9.7-notelistwindow-refinements.md)

- **Phase 9.6d - Workspace Recovery & Multi-Window Manager** (2024-11-25): Implemented automatic workspace recovery and multi-window support
  - **DATE**: 2024-11-25
  - **COMPLEXITY**: High (Multi-window architecture + Data recovery)
  - **SINGLE SOURCE OF TRUTH**: Refactored `WorkspaceEngine` to maintain in-memory workspace list
  - **WORKSPACE RECOVERY**: Automatic recovery from all JSON files in AppData (notewindows*.json, workspace-*.json)
  - **MULTI-WINDOW SUPPORT**: `WorkspaceWindowManager` enables multiple workspace windows open simultaneously
  - **NOTELISTWINDOW AS MANAGER**: Window stays open and acts as launcher/manager, not "switch and close" dialog
  - **DUPLICATE PREVENTION**: Checks if workspace window already open, brings to front instead of creating duplicate
  - **AUTO-CLEANUP**: Removes window from tracking when closed, saves workspace data on close
  - **TESTING**: ✅ Recovered 1 workspace from backup files, opened 3 windows simultaneously
  - **ARCHITECTURE**: Dictionary<Guid, Window> for O(1) window lookups
  - **BACKWARDS COMPATIBLE**: Supports both legacy single-window and new multi-window modes
  - **FILES CREATED**: 1 (WorkspaceWindowManager.cs, ~180 lines)
  - **FILES MODIFIED**: 5 (WorkspaceEngine.cs, IWorkspaceEngine.cs, MainWindowViewModel.cs, NoteListWindowViewModel.cs, ~400 lines)
  - **MICROLEARNING**: Single Source of Truth pattern, multi-window tracking, data recovery strategies
  - See [Comprehensive Audit](audits/11-25/2024-11-25-phase-9.6d-workspace-recovery-and-manager-behavior.md)

### 🐛 Bug Fixes
- **Phase 9.6c - Workspace Save Critical Bug Fix** (2024-11-25): Fixed critical data loss bug in WorkspaceViewModel.SaveAsync()
  - **DATE**: 2024-11-25
  - **SEVERITY**: 🔴 CRITICAL (Data Loss)
  - **BUG**: Every save operation deleted all workspaces except the currently active one
  - **ROOT CAUSE**: `SaveAsync()` created list with only current workspace, overwriting entire file
  - **DISCOVERY**: Found via Phase 9.6b enhanced logging - file size dropped from 2468 to 661 bytes
  - **FIX**: Implemented Read-Modify-Write pattern: load all workspaces, update current, save all
  - **IMPACT**: Users lost all workspaces except current on every auto-save (5 sec), window close, or workspace switch
  - **TESTING**: Verified with multi-workspace scenarios - all workspaces now persist correctly
  - **LOGGING**: Added `[CRITICAL FIX]` tags to trace load-update-save flow
  - **FILES MODIFIED**: 1 file (WorkspaceViewModel.cs, ~20 lines)
  - **MICROLEARNING**: Read-Modify-Write pattern for shared mutable collections
  - See [Critical Bug Fix Document](audits/2024-11-25-phase-9.6c-workspace-save-critical-fix.md)

### 🔍 Diagnostics
- **Phase 9.6b - AppData File Path Audit** (2024-11-25): Added comprehensive logging to diagnose workspace persistence issues
  - **DATE**: 2024-11-25
  - **ISSUE**: User reports old notewindows.json in AppData but app shows only new workspace
  - **ACTION**: Added detailed file path logging to JsonStorageBackend
  - **LOGGING**: Shows exact file paths, file existence, sizes, and all backup files
  - **LOGGING**: Shows deserialization success/failure
  - **OUTCOME**: ✅ Logging revealed Phase 9.6c critical bug (file size dropping after save)
  - **FILES MODIFIED**: 1 file (JsonStorageBackend.cs, ~30 lines)
  - See [File Path Audit](audits/2024-11-25-phase-9.6b-appdata-file-path-audit.md)

### 🐛 Bug Fixes (Previous)
- **Phase 9.6 - Workspace Disappearing Bug Fix** (2024-11-25): Fixed critical bug where original workspace disappeared after creating new one
  - **DATE**: 2024-11-25
  - **SEVERITY**: 🔴 CRITICAL (Data Loss)
  - **BUG**: After creating new workspace, original workspace disappeared from NoteListWindow
  - **ROOT CAUSE**: `CreateNewWorkspace()` loaded from disk BEFORE saving current workspace
  - **FIX**: Save current workspace FIRST, then load, then add new, then save all
  - **IMPACT**: Users could lose access to their original workspace and unsaved changes
  - **TESTING**: Verified with full repro scenario - all workspaces now persist correctly
  - **LOGGING**: Added comprehensive logging to track workspace state at each step
  - **FILES MODIFIED**: 3 files (WorkspaceEngine.cs, NoteListWindowViewModel.cs, MainWindowViewModel.cs, ~37 lines)
  - See [Bug Fix Document](audits/2024-11-25-phase-9.6-workspace-disappearing-bug-fix.md)

### ✨ Features
- **Phase 9.6 - Multi-Workspace Wiring Verification** (2024-11-25): Verified workspace wiring is correct and complete
  - **DATE**: 2024-11-25
  - **STATUS**: ✅ VERIFIED CORRECT - No changes needed
  - **FINDING**: Architecture already implements all Phase 9.6 requirements
  - **SINGLE SOURCE OF TRUTH**: `IWorkspaceEngine` + `notewindows.json` (no in-memory duplication)
  - **WORKSPACE SELECTION**: Works via `MainWindowViewModel.SwitchWorkspaceAsync()`
  - **NEW WORKSPACE CREATION**: Uses shared `WorkspaceEngine.CreateWorkspaceAsync()` pipeline
  - **PERSISTENCE**: All workspaces saved to `notewindows.json`, session state restored
  - **NO DUPLICATES/GHOSTS**: GUIDs prevent duplicates, fresh disk reads prevent ghosts
  - **ARCHITECTURE**: Engine pattern with no shared in-memory collections (simpler, more correct)
  - **BENEFIT**: Current implementation is clean, testable, and maintainable
  - See [Analysis Document](audits/2024-11-25-phase-9.6-multi-workspace-wiring-analysis.md)

- **Phase 9.5.1 - NoteListWindow Enhancements** (2024-11-25): Made workspace picker draggable, removed red visuals, added new workspace button
  - **DATE**: 2024-11-25
  - **IMPLEMENTED**: Window dragging via header `PointerPressed` + `BeginMoveDrag`
  - **IMPLEMENTED**: Visual cleanup - removed red default Avalonia focus/selection styles
  - **IMPLEMENTED**: "+ New Note Window" button in bottom-left to create workspaces
  - **IMPLEMENTED**: `CreateNewWorkspaceCommand` in `NoteListWindowViewModel`
  - **ARCHITECTURE**: Reuses `WorkspaceEngine.CreateWorkspaceAsync()` and `SaveWorkspacesAsync()`
  - **PERSISTENCE**: New workspaces saved to `notewindows.json` immediately
  - **UX**: New workspace appears in list instantly and is auto-selected
  - **BENEFIT**: Users can now create workspaces without leaving the picker
  - **FILES MODIFIED**: 3 files (NoteListWindow.axaml, NoteListWindow.axaml.cs, NoteListWindowViewModel.cs, ~140 lines)
  - See [Audit Document](audits/2024-11-25-notelistwindow-drag-newbutton-cleanup.md)

### ✨ Features
- **Phase 9.5 - NoteListWindow (Workspace Picker)** (2024-11-25): Added workspace picker dialog for managing and switching between workspaces
  - **DATE**: 2024-11-25
  - **CREATED**: WorkspaceListItemViewModel - presentation model for workspace list items
  - **CREATED**: NoteListWindowViewModel - ViewModel for workspace picker with load/select commands
  - **CREATED**: NoteListWindow.axaml + code-behind - dialog window for workspace selection
  - **IMPLEMENTED**: OpenNoteListWindowCommand in MainWindowViewModel (triggered by Folder button 📁)
  - **IMPLEMENTED**: SwitchWorkspaceAsync method - centralized workspace switching logic
  - **WIRED**: Folder button in main window header to open workspace picker
  - **FEATURES**: Double-click to activate, keyboard navigation (Enter/Esc), current workspace highlighting
  - **ARCHITECTURE**: Mediator pattern - NoteListWindowViewModel coordinates between View and MainWindowViewModel
  - **STUBBED**: "Open in New Window" for Phase 9.6+ multi-window support
  - **PHILOSOPHY**: Single-window workspace switching (multi-window deferred to Phase 9.6+)
  - **IMPACT**: Users can now manage multiple workspaces and switch between them easily
  - **BENEFIT**: Centralized workspace management, keyboard-friendly, consistent with app design
  - **FILES CREATED**: 3 files (WorkspaceListItemViewModel.cs, NoteListWindowViewModel.cs, NoteListWindow.axaml + .cs, ~800 lines)
  - **FILES MODIFIED**: 2 files (MainWindowViewModel.cs, MainWindow.axaml, ~200 lines)
  - See [Phase 9.5 Dev Note](devnotes/2024-11-25-phase-9.5-notelist-window.md)

### 🐛 Critical Fixes
- **Phase 9.5F - InputBindings Fix** (2024-11-25): Fixed Avalonia XAML build errors in NoteListWindow
  - **DATE**: 2024-11-25
  - **SEVERITY**: 🔴 CRITICAL - Build failed, app couldn't compile
  - **ROOT CAUSE**: Used WPF's `InputBindings` and `MouseBinding` which don't exist in Avalonia
  - **FIXED**: Replaced with Avalonia's native `DoubleTapped` event in XAML + code-behind handler
  - **APPROACH**: Event-driven instead of command binding for double-click workspace activation
  - **LESSON LEARNED**: Avalonia uses routed events (`DoubleTapped`, `Tapped`) instead of WPF's input bindings
  - **IMPACT**: Build succeeds, double-click functionality works as intended
  - **FILES MODIFIED**: 2 files (NoteListWindow.axaml, NoteListWindow.axaml.cs, ~50 lines changed)
  - See [Phase 9.5F Dev Note](devnotes/2024-11-25-phase-9.5f-inputbindings-fix.md)

- **Phase 9.5F - Settings Deadlock Fix** (2024-11-25): Fixed critical sync-over-async deadlock preventing app startup
  - **DATE**: 2024-11-25
  - **SEVERITY**: 🔴 CRITICAL - App hung at startup, no window appeared, completely unusable
  - **ROOT CAUSE**: Sync-over-async deadlock - blocking UI thread with `.GetAwaiter().GetResult()` on settings I/O
  - **FIXED**: Replaced blocking synchronous load with non-blocking background `Task.Run`
  - **STRATEGY**: Lazy load pattern - app uses default settings immediately, loads from disk in background
  - **PERFORMANCE**: Startup time improved from infinite hang to ~730ms
  - **ROBUSTNESS**: App now works even if settings file is missing/corrupted (graceful degradation)
  - **LESSON LEARNED**: Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on async methods in UI code
  - **KEY INSIGHT**: `ConfigureAwait(false)` does NOT prevent blocking - it only affects continuation context
  - **IMPACT**: App now starts reliably, window appears immediately, settings load in background
  - **FILES MODIFIED**: 1 file (App.axaml.cs, ~20 lines changed)
  - See [Phase 9.5F Dev Note](devnotes/2024-11-25-phase-9.5f-settings-deadlock-fix.md)

### 🐛 Critical Fixes
- **Phase 9.5 - Window Visibility Fix** (2024-11-25): Fixed critical bug where app built but no window appeared
  - **DATE**: 2024-11-25
  - **SEVERITY**: 🔴 CRITICAL - App was completely unusable (no visible UI)
  - **ROOT CAUSE**: Missing `mainWindow.Show()` call in `App.axaml.cs` (accidentally deleted during Phase 9.x refactoring)
  - **FIXED**: Added `mainWindow.Show()` after `desktop.MainWindow = mainWindow` (1 line change, critical impact)
  - **ENHANCED**: Geometry validation to prevent off-screen windows (multi-monitor safety)
  - **ADDED**: Size validation (min 400x300, max 10000x10000, default 1000x700)
  - **ADDED**: Position validation (clamp to ±10000, default 100,100)
  - **ADDED**: Enhanced diagnostic logging for initialization flow
  - **LESSON LEARNED**: Build success ≠ functional success (always test after refactoring)
  - **IMPACT**: App now shows window correctly, handles invalid geometry gracefully
  - **FILES MODIFIED**: 2 files (App.axaml.cs, MainWindowViewModel.cs, ~100 lines)
  - See [Phase 9.5 Dev Note](devnotes/2024-11-25-phase-9.5-window-visibility-fix.md)

### 🏗️ Architecture
- **Phase 9.4 - Settings Engine & Settings Persistence** (2024-11-25): Introduced centralized settings management with atomic persistence
  - **DATE**: 2024-11-25
  - **CREATED**: `AppSettings` model with strongly-typed settings and sensible defaults
  - **CREATED**: `ISettingsEngine` and `SettingsEngine` for centralized settings management
  - **IMPLEMENTED**: Atomic JSON storage via existing `IStorageBackend` infrastructure
  - **INTEGRATED**: Settings loaded on app startup, applied to auto-save timer interval
  - **IMPLEMENTED**: Functional update pattern (`UpdateAsync(Action<T>)`) for atomic modifications
  - **IMPLEMENTED**: Settings validation (clamp values to safe ranges)
  - **PREPARED**: APIs for future settings UI (Clone, Validate, Reset)
  - **PHILOSOPHY**: Centralize configuration, validate early, persist safely
  - **IMPACT**: All settings in one place, no more scattered constants, ready for settings UI in Phase 9.5+
  - **BENEFIT**: Better maintainability, consistent validation, atomic persistence, future-ready
  - **FILES CREATED**: 3 files (AppSettings.cs, ISettingsEngine.cs, SettingsEngine.cs, ~800 lines)
  - **FILES MODIFIED**: 4 files (App.axaml.cs, MainWindowViewModel.cs, WorkspaceViewModel.cs, MigrationService.cs, ~50 lines)
  - See [Phase 9.4 Dev Note](devnotes/2024-11-25-phase-9.4-settings-engine.md)

- **Phase 9.3 - Window Manager & Multi-Window Prep** (2024-11-25): Introduced centralized window management and session restore
  - **DATE**: 2024-11-25
  - **CREATED**: `IWindowManager` and `WindowManager` for centralized window tracking
  - **IMPLEMENTED**: Window registration/unregistration lifecycle
  - **IMPLEMENTED**: Bidirectional workspace ↔ window binding (O(1) lookups)
  - **CREATED**: `WindowStateService` for lightweight session persistence
  - **IMPLEMENTED**: Last open workspace restore on app startup
  - **INTEGRATED**: WindowManager into App.axaml.cs and MainWindowViewModel
  - **STUBBED**: Future multi-window APIs (OpenWorkspaceWindowAsync, CloseWindowAsync, etc.)
  - **PHILOSOPHY**: Build abstractions that support future features without overengineering the present
  - **IMPACT**: App now remembers last open workspace, ready for multi-window support in Phase 9.4+
  - **BENEFIT**: Better UX (session restore), cleaner architecture, prepared for multi-window features
  - **FILES CREATED**: 3 files (IWindowManager.cs, WindowManager.cs, WindowStateService.cs, ~650 lines)
  - **FILES MODIFIED**: 3 files (App.axaml.cs, MainWindowViewModel.cs, MainWindow.axaml.cs, ~110 lines)
  - See [Phase 9.3 Dev Note](devnotes/2024-11-25-phase-9.3-window-manager-multi-window-prep.md)

- **Phase 9.2 - Window + Tab Lifecycle Hardening** (2024-11-25): Hardened window and tab lifecycle with safe closing, position restore, and cleanup
  - **DATE**: 2024-11-25
  - **IMPLEMENTED**: Safe window closing (SaveAndStopAsync, geometry capture, timer stop)
  - **IMPLEMENTED**: Window position & size restore on startup
  - **IMPLEMENTED**: Tab deletion UI with close button (✕) and intelligent selection update
  - **IMPLEMENTED**: Orphaned media cleanup on startup (7-day grace period)
  - **VERIFIED**: Text editor styling (natural blue selection, no red outline) - already working from Phase 8.6.F
  - **VERIFIED**: Media paste per-tab - already working correctly
  - **IMPACT**: App now handles window lifecycle robustly, no data loss, no memory leaks
  - **BENEFIT**: Users can close tabs, window remembers position, orphaned files cleaned up automatically
  - **FILES MODIFIED**: 5 files (~370 lines added/modified)
  - See [Phase 9.2 Dev Note](devnotes/2024-11-25-phase-9.2-window-tab-hardening.md)

### 🎨 UI/UX
- **Phase 8.5 - Media Gallery Redesign** (2024-11-25): Redesigned media section as modern React/Tailwind/shadcn-style gallery
  - **DATE**: 2024-11-25
  - **REDESIGNED**: Media section with premium card-based layout (160x180px cards, 12px gaps)
  - **IMPLEMENTED**: Intentional empty state (🖼️ icon, "No media yet" title, helpful instructions)
  - **REFINED**: Minimal inline toolbar (➕ Add, 📋 Paste, 🔍 View, 🗑 buttons with subtle styling)
  - **ENHANCED**: Media cards with large image preview, clean metadata (filename + type/date)
  - **APPLIED**: Subtle hover effects (SurfaceAlt background, 150ms transitions, accent selection border)
  - **INTEGRATED**: Full design token usage (Background, Surface, SurfaceAlt, Border, Accent, Text*, Space.*, Radius.*, Duration.Fast)
  - **PHILOSOPHY**: Inspired by shadcn/ui cards, Tailwind galleries, Notion/Linear attachment UIs
  - **IMPACT**: Media section now feels like modern webapp gallery (not generic desktop file manager)
  - **BENEFIT**: Breathable spacing, clear visual hierarchy, intentional design, smooth interactions
  - **FILES MODIFIED**: 1 file (MediaSectionView.axaml)
  - See [Phase 8.5 Dev Note](devnotes/2024-11-25-phase-8.5-media-gallery-redesign.md)

- **Phase 8.0-8.2 Refinement** (2024-11-25): Enhanced design tokens and integrated header to align with premium dark-mode philosophy
  - **DATE**: 2024-11-25
  - **ENHANCED**: Design token system with semantic tokens (Header, HeaderBorder, Space.0, Space.5, Duration.*, Depth.*)
  - **REFINED**: Integrated header with subtle depth (44px height, dedicated Header color #0D0D15, balanced 12,10 padding)
  - **IMPROVED**: Header visual hierarchy (accent icon, refined title font-size 13px, letter-spacing 0.3)
  - **PRESERVED**: Clean professional tab design from Phase 8.4 (bottom border, no rounded corners)
  - **STANDARDIZED**: Transition durations (150ms/200ms/300ms) and spacing scale (0/4/8/12/16/20/24)
  - **PHILOSOPHY**: Aligned with shadcn/ui, Tailwind, Raycast, Linear, Notion, Vercel, Discord aesthetics
  - **IMPACT**: Complete semantic token system enables consistent premium black-glass aesthetic across entire UI
  - **BENEFIT**: Easier to refine specific UI areas, global consistency, prepared for Phase 8.5+ (media section, text editor)
  - **FILES CREATED**: 2 new docs (1,800+ lines total)
  - **FILES MODIFIED**: 2 files (StickyTheme.axaml, MainWindow.axaml)
  - See [Phase 8.0-8.2 Audit](audits/2024-11-25-phase-8.0-8.2-design-philosophy-audit.md)
  - See [Phase 8.0-8.2 Dev Note](devnotes/2024-11-25-phase-8.0-8.2-refinement.md)

- **Phase 8.4 - Modern Tab Redesign** (2024-11-24): Redesigned tabs with clean professional aesthetic
  - **DATE**: 2024-11-24
  - **REDESIGNED**: Tab UI with clean bottom-border design (GitHub/VS Code/Linear style)
  - **IMPLEMENTED**: Subtle hover feedback (150ms transitions, SurfaceAlt background)
  - **IMPLEMENTED**: Active state with accent bottom border (2px cyan line)
  - **FIXED**: Text cutoff issue with proper padding (16,10,16,12)
  - **REMOVED**: Rounded corners (professional, not playful)
  - **STANDARDIZED**: Typography (13px, Medium weight, 0.2 letter-spacing)
  - **IMPACT**: Tabs feel modern, professional, and polished (matches shadcn/ui aesthetic)
  - **BENEFIT**: Clean visual hierarchy, no text cutoff, smooth transitions
  - **FILES MODIFIED**: 1 file (TabStripView.axaml)
  - See [Phase 8.4 Dev Note](devnotes/2024-11-24-phase-8.4-modern-tab-redesign.md)

- **Phase 8.3 - Integrated Dark Header** (2024-11-24): Implemented VS Code/Discord-style integrated header with native caption buttons
  - **DATE**: 2024-11-24
  - **IMPLEMENTED**: Extended client area with `PreferSystemChrome` (native min/max/close buttons)
  - **IMPLEMENTED**: Custom dark header (app icon, title, 135px spacer for caption buttons)
  - **IMPLEMENTED**: Window dragging from header (`BeginMoveDrag` on `PointerPressed`)
  - **APPLIED**: Design tokens (Brush.App.Surface, Brush.App.Border, Space.2)
  - **IMPACT**: Modern webapp aesthetic, no "stock Windows application" vibe
  - **BENEFIT**: Clean, minimal, professional header that matches premium dark theme
  - **FILES MODIFIED**: 2 files (MainWindow.axaml, MainWindow.axaml.cs)
  - See [Phase 8.3 Dev Note](devnotes/2024-11-24-phase-8.3-integrated-dark-header.md)
  - See [Phase 8.3 Audit](audits/2024-11-24-phase-8.3-integrated-header-audit.md)

- **Phase 8.2 - Design Tokens** (2024-11-24): Created comprehensive design token system (StickyTheme.axaml)
  - **DATE**: 2024-11-24
  - **CREATED**: `StickyTheme.axaml` with complete token system (colors, brushes, radii, spacing, typography)
  - **DEFINED**: Black-glass color palette (Background #080810, Surface #11111A, Accent #4DE2F1)
  - **DEFINED**: Spacing scale (4/8/12/16/24), Radii (SM/MD/LG/Full), Typography (Body/Title/Mono)
  - **APPLIED**: Tokens to MainWindow background and title bar
  - **IMPACT**: All visual decisions flow from centralized token system
  - **BENEFIT**: Consistent aesthetic, easy to refine globally, prepared for full UI redesign
  - **FILES CREATED**: 1 new file (StickyTheme.axaml)
  - **FILES MODIFIED**: 2 files (App.axaml, MainWindow.axaml)
  - See [Phase 8.2 Dev Note](devnotes/2024-11-24-phase-8.2-design-tokens.md)

- **Phase 8.1 - ShadUI Integration** (2024-11-24): Integrated ShadUI as base theme for modern component library
  - **DATE**: 2024-11-24
  - **INTEGRATED**: ShadUI package as base theme (loaded before custom styles)
  - **LOCKED**: Dark mode (`RequestedThemeVariant="Dark"`)
  - **CONFIGURED**: Style cascade (ShadUI → FluentTheme → StickyTheme overrides)
  - **IMPACT**: Modern component base with clean defaults
  - **BENEFIT**: Leverages shadcn/ui-inspired components, reduces custom styling needed
  - **FILES MODIFIED**: 2 files (SnipShottyBoard.Avalonia.csproj, App.axaml)
  - See [Phase 8.1 Dev Note](devnotes/2024-11-24-phase-8.1-shadui-integration.md)

### 🏗️ Architecture
- **Phase 7.15F - MediaEngine Focus Fix** (2025-11-24): Fixed focus issues preventing paste/drag-drop on tabs 2+
  - **DATE**: 2025-11-24
  - **FIXED**: Paste (Ctrl+V) and drag-drop only worked on first tab - now works on all tabs
  - **REVERTED**: Per-tab `MediaSectionView` instances (Phase 7.15F attempt) - caused all media to disappear
  - **IMPLEMENTED**: Workspace-level Ctrl+V handling in `WorkspaceView.KeyDown` - delegates to `MediaSectionViewModel`
  - **ENHANCED**: Comprehensive logging for tab changes and media operations (WorkspaceViewModel, MediaSectionViewModel, WorkspaceView)
  - **RESTORED**: Shared `MediaSectionView` with 5-row layout (header, tabs, text, splitter, media)
  - **BENEFIT**: Paste works reliably on all tabs without complex focus management
  - **FILES CREATED**: 2 new docs (1,400+ lines total)
  - **FILES MODIFIED**: 9 files (~300 lines changed)
  - See [Phase 7.15 Fix Audit](audits/2025-11-24-phase-7.15-media-engine-fix.md)
  - See [Notes & Media Pipeline Architecture](notes/PHASE_7.15_NOTES_MEDIA_PIPELINE.md)

- **Phase 7.15 - MediaEngine** (2025-11-24): Implemented third domain engine for media operations
  - **DATE**: 2025-11-24
  - **NEW**: `IMediaEngine` interface for media operations (AddMediaFromFileAsync, AddMediaFromFilesAsync, AddMediaFromBitmapStreamAsync, RemoveMediaFromNoteAsync, GetMediaForNoteAsync)
  - **NEW**: `MediaEngine` concrete implementation (uses `INotesEngine` for active note resolution, `IWorkspaceEngine` for persistence, `IMediaStorageService` for file operations)
  - **UPDATED**: `MediaSectionViewModel` uses `IMediaEngine` for file picker, drag-drop, and clipboard file paste
  - **UPDATED**: `WorkspaceViewModel` exposes `MediaEngine`, `WorkspaceId`, and `Logger` properties for child ViewModels
  - **UPDATED**: `MainWindowViewModel` passes `IMediaEngine` to `WorkspaceViewModel`
  - **UPDATED**: `App.axaml.cs` registers `MediaEngine` in DI container
  - **FIXED**: "Only first tab gets paste" bug - media now correctly attaches to active note via NotesEngine
  - **IMPACT**: All major media entry points (file picker, drag-drop, clipboard file paste) now route through MediaEngine
  - **BENEFIT**: Testable (can mock `IMediaEngine`), reusable (UI-agnostic), fixes multi-tab paste isolation
  - **FILES CREATED**: 2 new files (465 lines)
  - **FILES MODIFIED**: 4 files (+120 lines)
  - See [Phase 7.15 Audit](audits/2025-11-24-phase-7.15-media-engine.md) (superseded by Phase 7.15F)
  - See [Engine Blueprint](architecture/ENGINE_BLUEPRINT.md)

- **Phase 7.14 - NotesEngine** (2025-11-24): Implemented second domain engine for note-level operations
  - **DATE**: 2025-11-24
  - **NEW**: `INotesEngine` interface for note operations (GetNotesForWorkspaceAsync, CreateNoteAsync, DeleteNoteAsync, RenameNoteAsync, UpdateNoteContentAsync, GetActiveNoteIndexAsync, SetActiveNoteIndexAsync)
  - **NEW**: `NotesEngine` concrete implementation (uses `IWorkspaceEngine` for persistence)
  - **UPDATED**: `WorkspaceViewModel` uses `INotesEngine` for note operations instead of direct manipulation
  - **UPDATED**: `WorkspaceHeaderViewModel` calls async `CreateNewNoteAsync()` method
  - **UPDATED**: `MainWindowViewModel` passes `INotesEngine` to `WorkspaceViewModel`
  - **UPDATED**: `App.axaml.cs` registers `NotesEngine` in DI container
  - **IMPACT**: ViewModels are now thinner, all note business logic in engine layer
  - **BENEFIT**: Testable (can mock `INotesEngine`), reusable (UI-agnostic), prepared for MediaEngine (Phase 7.15)
  - **FILES CREATED**: 2 new files (449 lines)
  - **FILES MODIFIED**: 4 files (+27 lines)
  - See [Phase 7.14 Audit](audits/2025-11-24-phase-7.14-notes-engine.md)
  - See [Engine Blueprint](architecture/ENGINE_BLUEPRINT.md)

- **Phase 7.13 - WorkspaceEngine** (2025-11-24): Implemented first domain engine for workspace operations
  - **DATE**: 2025-11-24
  - **NEW**: `IWorkspaceEngine` interface for workspace operations (LoadWorkspacesAsync, SaveWorkspacesAsync, CreateWorkspaceAsync, DeleteWorkspaceAsync, EnsureWorkspaceExistsAsync)
  - **NEW**: `WorkspaceEngine` concrete implementation (uses `IStorageEngine` for persistence)
  - **UPDATED**: `MainWindowViewModel` uses `IWorkspaceEngine` instead of `IWorkspaceStorageService`
  - **UPDATED**: `WorkspaceViewModel` uses `IWorkspaceEngine` instead of `IWorkspaceStorageService`
  - **UPDATED**: `App.axaml.cs` registers `WorkspaceEngine` in DI container
  - **IMPACT**: ViewModels are now thin and delegate all business logic to engine layer
  - **BENEFIT**: Testable (can mock `IWorkspaceEngine`), reusable (UI-agnostic), follows ENGINE_BLUEPRINT architecture
  - **FILES CREATED**: 2 new files (370 lines)
  - **FILES MODIFIED**: 3 files (+11 lines, significant refactoring)
  - See [Phase 7.13 Audit](audits/2025-11-24-phase-7.13-workspace-engine.md)
  - See [Engine Blueprint](architecture/ENGINE_BLUEPRINT.md)

- **Phase 7.12 - StorageEngine & Backends** (2025-11-24): Implemented storage abstraction layer with pluggable backends
  - **DATE**: 2025-11-24
  - **NEW**: `IStorageEngine` high-level storage API (LoadAsync, SaveAsync, ExistsAsync, DeleteAsync, ListKeysAsync)
  - **NEW**: `IStorageBackend` interface for pluggable storage implementations (JSON now, SQLite future)
  - **NEW**: `JsonStorageBackend` wraps `AtomicFileManager` for file-based JSON storage with rolling backups
  - **NEW**: `StorageEngine` coordinator orchestrates backend operations with comprehensive logging
  - **UPDATED**: `WorkspaceStorageService` uses `StorageEngine` internally (bridge pattern, zero breaking changes)
  - **UPDATED**: `App.axaml.cs` registers `StorageEngine` and `JsonStorageBackend` in DI container
  - **IMPACT**: Enables future SQLite migration without changing engine code (Phases 7.19/7.20)
  - **BENEFIT**: Testable (can mock `IStorageEngine`), swappable backends, clear separation of concerns
  - **FILES CREATED**: 4 new files (678 lines)
  - **FILES MODIFIED**: 5 files (+32 lines)
  - See [Phase 7.12 Audit](audits/2025-11-24-phase-7.12-storage-engine.md)
  - See [Engine Blueprint](architecture/ENGINE_BLUEPRINT.md)

### 🚨 Fixed (CRITICAL)
- **Clipboard Paste Not Saving** (2025-11-24): Fixed 3 critical bugs preventing clipboard-pasted images from persisting
  - **DATE**: 2025-11-24 (discovered during Phase 7.12 testing)
  - **BUG 1 - Focus** (Phase 7.10 regression): `MediaSectionView` wasn't receiving keyboard focus, preventing Ctrl+V from working
    - **FIX**: Added `Focusable="True"` to XAML and auto-focus on load/click in code-behind
    - **FILE**: `SnipShottyBoard.Avalonia/Views/Sections/MediaSectionView.axaml.cs` (+14 lines)
  - **BUG 2 - Auto-Save** (Phase 7.10 regression): Auto-save timer wasn't triggered after clipboard paste
    - **FIX**: Added `_workspaceViewModel?.MarkChanged()` call after successful paste in `MediaSectionViewModel`
    - **FILE**: `SnipShottyBoard.Avalonia/ViewModels/Workspace/MediaSectionViewModel.cs` (+2 lines)
  - **BUG 3 - Persistence** (Phase 7.10 regression): Pasted media only added to ViewModel, not underlying model
    - **FIX**: Added items to both `MediaItems` (ViewModel) AND `note.GetModel().MediaItems` (model) in `MediaClipboardHandler`
    - **FILE**: `SnipShottyBoard.Avalonia/Services/Clipboard/MediaClipboardHandler.cs` (+4 lines)
  - **RESULT**: Screenshot paste (Win+Shift+S) and Explorer file paste (Ctrl+C → Ctrl+V) now work end-to-end
  - **IMPACT**: All clipboard paste operations now persist correctly across app restarts
  - **TESTING**: All 7 test scenarios passing (load, save, auto-save, persistence, drag-drop, clipboard paste, backups)
  - See [Phase 7.12 Audit](audits/2025-11-24-phase-7.12-storage-engine.md) §Bonus Fixes

- **JSON Deserialization Case Mismatch**: Fixed critical data loss bug where notes/windows/settings reset to empty on every restart
  - **ROOT CAUSE**: Save path used `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, load path used default PascalCase
  - **IMPACT**: All data appeared "empty" on load despite valid JSON files (silent data corruption)
  - **FIX**: Added consistent `JsonSerializerOptions` with camelCase to all 4 deserialize locations in `AtomicFileManager.cs`
  - **RESULT**: Notes, windows, and settings now persist correctly across restarts
  - **SIDE EFFECT FIX**: Eliminates endless timestamped backup spam (was caused by empty data ≠ saved data)
  - See [Dev Note: JSON camelCase Deserialization Fix](devnotes/2025-11-23-json-camelcase-deserialization-fix.md)
  - See [Persistence Failure Audit](audits/2025-11-23-persistence-failure-camelcase-mismatch.md)

### 🔧 Changed
- **Meaningful Change Detection for Backups**: Implemented intelligent backup system to prevent backup spam
  - **NEW**: `AtomicFileManager.AtomicSaveWithChangeDetection()` compares old vs new JSON content
  - **CHANGE**: Backups now only created when data actually changes (not on every autosave)
  - **REDUCTION**: Rolling backups reduced from 20 → 5 per file type
  - **IMPACT**: 75% reduction in backup spam, cleaner AppData folder
  - **UNCHANGED**: Atomic write safety, recovery logic, and `.bak` files preserved
  - **TRIGGERS**: Backups created on content edits, image add/remove, tab create/delete, window open/close
  - **SKIPPED**: Backups skipped on window position changes, autosave with no edits, focus changes
  - See [Dev Note: Meaningful Change Detection](devnotes/2025-11-23-meaningful-change-detection.md)
  - See [Backup Spam Diagnosis](audits/2025-11-23-backup-spam-diagnosis.md)

### 📚 Docs
- **Phase 7.14 NotesEngine Audit**: Complete implementation details, architecture diagrams, testing results (900+ lines)
- **Phase 7.13 WorkspaceEngine Audit**: Complete implementation details, architecture diagrams, testing results (1,200+ lines)
- **Phase 7.13 Session Summary**: Quick reference guide for WorkspaceEngine architecture
- **Phase 7.12 StorageEngine Audit**: Complete implementation details, testing results, bug fixes (1,100+ lines)
- **Phase 7.12 Session Summary**: Quick reference guide for StorageEngine architecture
- **Engine Blueprint**: Comprehensive architecture document for all future engine phases (2,270 lines)
- **MASTER_SOT.md §5.1**: Enhanced with meaningful change detection policy and backup limits
- **ARCHITECTURE_GOVERNANCE.md §4.2**: Added "Meaningful Change Detection" section with patterns and trigger table
- **Backup Spam Diagnosis**: READ-ONLY audit explaining why backup spam occurred and options for resolution

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
  - **MASTER_SOT.md**: Rewritten § Tabs Pattern as normative spec (removed code/values)
  - **ARCHITECTURE_GOVERNANCE.md**: Added § Docs Governance appendix (defines governance vs Dev Notes split)
  - **Promotion Rule**: Added rule for Dev Notes → MASTER_SOT graduation
  - Cross-links established: MASTER_SOT/ARCHITECTURE_GOVERNANCE ↔ Dev Note (bidirectional)
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
- **ARCHITECTURE_GOVERNANCE.md § Tab Drag-and-Drop**: Added comprehensive documentation
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
- **Enhanced Governance Docs**: Updated compliance status and architecture validation in MASTER_SOT.md and ARCHITECTURE_GOVERNANCE.md

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