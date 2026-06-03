# BUGS.md
# ============================================================
# Bug History — SnipShottyBoard
# Location: docs/BUGS.md
# Updated: immediately when a bug is fixed
# ============================================================

---

## OPEN BUGS

### B-CLOSEALL — Taskbar "Close all windows" only restored one window on next launch (FIXED 2026-06-03)
```
Status:    FIXED
Severity:  HIGH — broke the multi-window restore promise of v1.7.0
Found:     2026-06-03 (regression in own fix — caught same day)
Fixed:     2026-06-03

SYMPTOMS:
  • Open 2 (or more) MainWindow instances, each with their own tabs/data.
  • Right-click SnipShottyBoard in Windows taskbar → "Close all windows".
  • Reopen the app.
  • Only ONE window restores instead of all of them.
  • Single-window-close behavior (Scenario B in v1.7.0 fix) still worked correctly.

EVIDENCE FROM DISK:
  master.json after the close-all showed:
    "My Notes" → isActive=true, isOpen=false  ← incorrectly marked
    "Main"     → isActive=true, isOpen=true
  Both windows were visibly open when the user triggered taskbar close-all.

ROOT CAUSE — synchronous sibling count can't distinguish intent:
  v1.7.0's MainWindow_Closing handler decided IsOpen by counting other
  MainWindow instances still in Application.Current.Windows:

    var otherOpenWindows = Application.Current.Windows
        .OfType<MainWindow>()
        .Where(w => !ReferenceEquals(w, this))
        .Count();

    if (otherOpenWindows > 0) WindowData.IsOpen = false;
    else                       /* preserve IsOpen=true */;

  When the OS closes every window via taskbar "Close all windows", each
  MainWindow's Closing fires sequentially on the UI thread:
    1. "My Notes".Closing: "Main" still in Windows collection → counts 1
       → sets IsOpen=false → saves.   ← WRONG, but indistinguishable
       from a real single-window close.
    2. "Main".Closing: "My Notes" already destroyed and removed →
       counts 0 → leaves IsOpen=true → saves.

  Synchronously inside one Closing handler there is no signal that
  identifies "the next window is also about to close." Both single-close
  and close-all look identical at that instant.

WHAT WE TRIED FIRST (and rejected):
  • Hooking SessionEnding — only fires for Windows logoff/shutdown, not
    taskbar "close all windows."
  • Tracking a static "shutdown in progress" flag in OnExit — fires too
    late, after all Closing handlers have already written IsOpen=false.
  • Detecting close-all via SC_CLOSE source — Win32 makes title-bar X and
    taskbar close indistinguishable at the message level.

FIX APPLIED — defer the IsOpen=false decision to ApplicationIdle:
  In MainWindow_Closing:
    1. Save normally with IsOpen=true (current state).
    2. Schedule a callback at DispatcherPriority.ApplicationIdle via
       Application.Current.Dispatcher.BeginInvoke(...).
    3. The dispatcher processes the entire queued WM_CLOSE burst before
       running the ApplicationIdle work item.
    4. When the callback runs:
         • If other MainWindow instances are still alive in
           Application.Current.Windows → single-window close → flip
           captured WindowData.IsOpen=false and resave via
           NoteWindowManager.Instance.SaveNoteWindows().
         • If no MainWindow instances remain → close-all happened → app
           is exiting → leave everything alone → IsOpen=true persists
           for every window → all restore next launch.

VERIFICATION:
  • Scenario A — Open 2+ windows, taskbar right-click → Close all.
    Reopen app. All windows restored. ✅
  • Scenario B — Open 3 windows. Close one individually, then another.
    Close the last one. Reopen app. Only the last-closed window
    restored (matches Windows Sticky Notes app). ✅

FILES CHANGED:
  • UI/Views/MainWindow.xaml.cs — lines ~870-925
    Replaced inline sibling-count IsOpen block with deferred
    Dispatcher.BeginInvoke at ApplicationIdle priority.

LESSON LEARNED:
  When deciding "what is the user trying to do?" from a stream of OS
  messages that arrive synchronously, NEVER decide at the moment the
  first message arrives. Defer the decision until the message burst
  has fully drained — that is the only moment the system can tell
  "one message" from "many messages in rapid succession."
```

### B-LABELSIZE — Per-image v3 fields (Label, ThumbnailSize, visibility flags) reset to defaults on every load (FIXED 2026-06-03)
```
Status:    FIXED
Severity:  HIGH — silently destroyed every per-image customization on every restart
Found:     2026-06-03
Fixed:     2026-06-03

SYMPTOMS:
  • User sets a thumbnail size (Small/Medium/Big) — change visible in-session.
  • User edits a label via right-click → Rename — label visible in-session.
  • User toggles per-image visibility flags (label/date/time/hide) — works in-session.
  • Close app → reopen → ALL of those are reset to defaults
    (Label="", ThumbnailSize=150, ShowLabel=true, ShowDate=true, ShowTime=true, IsHidden=false).

EVIDENCE FROM DISK:
  master-20260603-161455.json had "thumbnailSize": 60 (user-set Small) saved correctly.
  master-20260603-161734.json (after restart) had "thumbnailSize": 150 (default).
  Both files contained redundant "media" + "imageFiles" + "imageTimestamps" blocks
  for the same data — the smoking gun.

ROOT CAUSE — [Obsolete] is not [JsonIgnore]:
  SavedNote has three properties writing to the same backing list:
    • Media                (v3 list — source of truth)
    • ImageFiles      [Obsolete]  → getter computes from Media, setter mutates Media
    • ImageTimestamps [Obsolete]  → getter computes from Media, setter mutates Media

  System.Text.Json does NOT skip [Obsolete] properties. It serialized all three,
  producing redundant JSON. On deserialize, System.Text.Json sets properties in
  document order:
    1. "media"      → Media list populated with full v3 MediaReference objects ✅
    2. "imageFiles" → ImageFiles setter calls Media.Clear() and rebuilds list
                      with NEW MediaReference objects using DEFAULT v3 values 💥
    3. "imageTimestamps" → updates DateAdded on each, leaves all other v3 fields default

  Net effect: every load wiped Label, ThumbnailSize, ShowLabel, ShowDate, ShowTime,
  and IsHidden to defaults regardless of what the user saved.

SECONDARY ISSUE FOUND DURING TRACE:
  TabManager.GetSaveData() created fresh SavedNote without copying DataVersion or
  TabOrder, so every saved note had "dataVersion": 1 forever. Migration ran on
  every single load even though the in-memory note got promoted to v3.

FIX APPLIED:
  1. Core/Models/SavedNote.cs — added [JsonIgnore] to ImageFiles + ImageTimestamps.
     Added `using System.Text.Json.Serialization;`.
  2. UI/TabManager.cs — GetSaveData() now copies DataVersion (set to
     MigrationService.CurrentNoteSchemaVersion) and TabOrder (from list index).

LESSON:
  • [Obsolete] is a compiler-warning attribute only. It does NOT affect runtime
    serialization. Use [JsonIgnore] (or [JsonIgnore(Condition = ...)]) when a
    property must not round-trip through JSON.
  • Any property with a side-effect setter that mutates shared state is a
    serialization landmine. If both a "primary" list and a computed "view" list
    serialize, the view list's setter will overwrite the primary one when loaded.
  • When the migrator never sees its own promoted version on disk, suspect the
    save path is throwing the new fields away.

Dev Note: docs/devnotes/2026-06-03-bug-label-size-multiwindow-restore.md
```

---

### B-MULTIWIN — Only first window reopens at startup (FIXED 2026-06-03)
```
Status:    FIXED
Severity:  MEDIUM — multi-window users lost their workspace layout on every restart
Found:     2026-06-03
Fixed:     2026-06-03

SYMPTOM:
  User opens 2+ note windows (Sticky-Notes-style multi-window workspace).
  Closes the app. Reopens. Only ONE window comes back — the "first" one.
  User has to manually open NoteListWindow and click each other window to restore.

ROOT CAUSE — two missing pieces:
  1. App.xaml had StartupUri="UI/Views/MainWindow.xaml" → WPF instantiated exactly
     ONE MainWindow at startup. That window's constructor calls
     EnsureMainWindowHasData() which returns existingWindows.First() — only the
     first saved window ever got bound to a real MainWindow instance. The rest
     sat in NoteWindowManager.NoteWindows with no UI.
  2. MainWindow_Closing never updated WindowData state. There was no way to
     distinguish "user explicitly closed THIS window" from "user shut down the
     entire app" — both left IsActive=true on every window. So even if we had
     restore logic, we couldn't tell what was supposed to come back.

FIX APPLIED (Windows Sticky Notes app behavior):
  1. Core/Managers/NoteWindowManager.cs — added `IsOpen` bool to NoteWindowData,
     defaulting to true so existing data restores cleanly on first upgrade.
  2. UI/Views/MainWindow.xaml.cs constructor — sets WindowData.IsOpen = true
     whenever a window is constructed (covers fresh launch + manual reopen).
  3. UI/Views/MainWindow.xaml.cs MainWindow_Closing —
       if (other MainWindow instances still open) → WindowData.IsOpen = false
                                                    (this is a per-window close)
       else (this is the last window)               → leave IsOpen = true
                                                    (preserve for next launch)
  4. App.xaml — removed StartupUri, set ShutdownMode="OnLastWindowClose" explicitly.
  5. App.xaml.cs OnStartup — added RestoreOpenWindows():
       • Iterate NoteWindowManager.Instance.GetActiveWindows()
       • Open a MainWindow for each with IsOpen == true
       • If none flagged open, fall back to first active window
       • If no active windows at all, open a fresh default MainWindow()

DESIGN CHOICE:
  Followed Windows Sticky Notes app: reopen only the windows that were visible
  at last shutdown. Closing one window mid-session permanently dismisses it from
  future startups until the user manually reopens it from NoteListWindow. The
  user picked this over "always restore every active window."

LESSON:
  • StartupUri is the wrong primitive for multi-window apps. Explicit window
    creation in OnStartup gives full control over count, order, and per-window
    data.
  • "IsActive" was overloaded as both "exists in data" AND "is open." Splitting
    those into IsActive (lifecycle) and IsOpen (session-state) clarifies intent
    and unlocks the restore behavior.
  • Counting other MainWindow instances inside Closing distinguishes per-window
    close from app exit without needing extra state.

Dev Note: docs/devnotes/2026-06-03-bug-label-size-multiwindow-restore.md
```

---

### B-IV1 — ImageViewerWindow Build Errors + Zoom/Pan/Focus Bugs (FIXED 2026-05-05)
```
Status:    FIXED
Severity:  HIGH — window would not compile; zoom/pan/navigation non-functional
Found:     2026-05-05
Fixed:     2026-05-05

SYMPTOMS (compile errors):
  1. XAML: MouseDoubleClick="..." on <Image> → MC3072 (event not on FrameworkElement)
  2. C#: ImageScrollViewer.ScrollToCenter(DisplayImage) → CS compile error (no such method)
  3. C#: ImageBehavior.SetIsPaused(...) → CS0117 (no such method in WpfAnimatedGif 2.0.2)

SYMPTOMS (runtime bugs after compile fixed):
  4. Mouse wheel had no effect — images could not be zoomed
  5. Window resize created dark borders — image did not scale with window
  6. Mouse capture could get stuck — panning/clicking behaviour became erratic
  7. Arrow-key navigation stopped working after clicking away and back

ROOT CAUSES:
  1. MouseDoubleClick is defined on Control, not FrameworkElement. Image inherits
     FrameworkElement, so the XAML attribute is illegal.
  2. ScrollViewer.ScrollToCenter() does not exist. API is ScrollToHorizontalOffset /
     ScrollToVerticalOffset.
  3. WpfAnimatedGif 2.0.2 API is GetAnimationController().Pause()/.Play().
     SetIsPaused() was never part of this library.
  4. ScrollViewer.OnMouseWheel is a WPF class handler — fires before instance handlers,
     marks event Handled, swallows it. Our MouseWheel XAML handler never ran.
     Fix: PreviewMouseWheel (tunneling, fires first) + e.Handled = true.
  5. SizeChanged only called UpdateImageInfo(). FitToWindow() never re-ran on resize.
     Fix: _isInFitMode flag + re-fit on SizeChanged when flag is true.
  6. LostMouseCapture never handled. If OS stole capture (Alt-Tab etc),
     _isMouseDragging stayed true permanently.
     Fix: LostMouseCapture handler resets _isMouseDragging.
  7. ScrollViewer is focusable by default. Clicking image area moved keyboard focus
     there. ScrollViewer handled Left/Right for scrolling (Handled = true) before
     they could bubble to Window.KeyDown.
     Fix: Focusable="False" on ScrollViewer + this.Activated -> this.Focus().

FIXES APPLIED:
  - UI/ImageViewerWindow.xaml: removed MouseDoubleClick; MouseWheel → PreviewMouseWheel;
    added MouseMove, LostMouseCapture; Focusable="False" on ScrollViewer.
  - UI/ImageViewerWindow.xaml.cs: ScrollToCenter → proper offsets; SetIsPaused →
    GetAnimationController API; _isInFitMode flag; ApplyOneToOne() default;
    LostMouseCapture handler; Activated focus reset.

Dev Note: docs/devnotes/2026-05-05-sprint-iv1-image-viewer-zoom-pan.md
```

---

### B-SAVE — ContentCardStyle Forward Reference Caused Repeating Save Corruption (FIXED 2026-05-01)
```
Status:    FIXED
Severity:  CRITICAL — notes and images disappeared on every restart
Found:     2026-05-01 (post B-THEME session)
Fixed:     2026-05-01

SYMPTOMS:
  Notes and images gone after close + reopen. Blank tab every startup.
  Rolling backup (master-20260501-225631.json, 11,335 bytes) confirmed data
  was intact on disk but being overwritten every session.

ROOT CAUSE — Same 4-step chain as B-THEME, new trigger:
  1. During Sprint UI-1, ContentCardHoverStyle was rewritten with:
         BasedOn="{StaticResource ContentCardStyle}"
     ContentCardStyle was defined at line 984 in DarkTheme.xaml.
     ContentCardHoverStyle was placed at line 465 — BEFORE ContentCardStyle.
  2. StaticResource cannot forward-reference (resource must be already parsed).
     When NoteTab.xaml loaded, WPF threw:
         XamlParseException: Cannot find resource named 'ContentCardStyle'
  3. TabManager.LoadTabs() catch block called CreateNewTab() → blank tab.
  4. autosave fired 5s later → blank tab saved over real notes → repeating loop.

FIX APPLIED:
  1. Moved ContentCardStyle from line 984 to line 465 in DarkTheme.xaml,
     placing it immediately BEFORE ContentCardHoverStyle. Forward reference gone.
  2. TabManager.LoadTabs() catch: removed CreateNewTab() call, replaced with
     throw so the caller decides safely.
  3. MainWindow.LoadApplicationData(): added targeted try/catch around
     LoadTabs(). On failure: creates blank tab for the session but immediately
     sets hasUnsavedChanges = false — prevents autosave from overwriting disk.

LESSON:
  - StaticResource in a ResourceDictionary requires the referenced key to appear
    EARLIER in the same file. Forward references silently fail at runtime.
  - Use DynamicResource for most XAML resource references to avoid order dependency.
    (Exception: BasedOn only accepts StaticResource — so ordering IS mandatory.)
  - The LoadTabs catch path must NEVER call CreateNewTab when existing notes were
    present. It must bail without writing any state. Now enforced.
  - The safety rule: if LoadTabs throws, set hasUnsavedChanges = false immediately
    so no autosave can run until the user actively makes a new change.
```

---

### B-THEME — Theme Toggle Caused Data Loss + Save Corruption (FIXED 2026-05-01)
```
Status:    FIXED
Severity:  CRITICAL — caused total data loss and broke save/load cycle
Found:     2026-05-01 (Sprint UI-2 session)
Fixed:     2026-05-01

SYMPTOMS:
  a. Clicking Toggle Theme turned app white
  b. After close + reopen: all notes and images gone
  c. Even fresh notes typed after the incident did not persist across restarts

ROOT CAUSE — 4-step chain:
  1. LightTheme.xaml is missing 21 resource keys present in DarkTheme.xaml,
     including ContentCardStyle, SubtleDividerBrush, EditorSurfaceStyle, etc.
  2. When LightTheme was applied, NoteTab.xaml threw XamlParseException
     ("Cannot find resource named 'ContentCardStyle'") during LoadTabs().
  3. The catch block called CreateNewTab() as fallback → 1 blank empty tab.
  4. The autosave timer (5s) then fired, saving the blank tab state over
     all real note data in master.json. Every subsequent restart repeated
     the cycle: LoadTabs fails → blank tab → save overwrites data → repeat.
  5. Separately, settings.json was saved with isDarkMode=false (Light),
     so every startup loaded the broken LightTheme, compounding the loop.

ADDITIONAL FACTOR:
  ThemeManager.ToggleTheme() called OnThemeChanged twice per toggle
  (once from ApplyTheme() and once from ToggleTheme() itself), which
  fired hasUnsavedChanges=true, triggering autosave faster.

DATA RECOVERY:
  AtomicFileManager rolling backups saved us. master-20260501-210553.json
  (41,805 bytes) was the last good save before the damage. Restored via
  PowerShell while app was fully stopped.

FIX APPLIED:
  1. Removed Toggle Theme button entirely from MainWindow.xaml + handler
     from MainWindow.xaml.cs. LightTheme is incomplete — button was premature.
  2. Fixed settings.json: isDarkMode=true, theme="Dark" so dark theme
     always loads at startup going forward.
  3. PackIcon Kind="Pushpin" → "ThumbTack" → "PinOutline" (MDIX 5.3.1
     does not have ThumbTack; PinOutline is the correct enum name).

LESSON:
  - Never ship a toggle to a half-built theme. LightTheme must have ALL
    resource keys that DarkTheme has before a toggle is safe to expose.
  - The LoadTabs error-catch path (CreateNewTab fallback) is dangerous:
    it can silently overwrite good on-disk data with an empty blank tab.
    Future fix: catch should bail out completely without writing any tabs
    if existing data was present.
  - Always verify PackIconKind names against the installed DLL, not docs.
  - MDIX 5.3.1 icon verification: query DLL with PowerShell
    $text.Contains("IconName") before using any new PackIcon Kind.
```

---

### B-WF — White Flash + Ghost Buttons on Resize (Sprint F regression)
```
Status:    OPEN — multiple fix attempts, none successful
Severity:  Visual polish — ugly but not functional
Found:     2026-05-01
Symptoms:
  a. White line/sliver visible when dragging resize edges
  b. Ghost/mirrored caption buttons visible on left-edge drag
  c. White flash when opening ImageViewerWindow
  d. White flash between image cycles in ImageViewerWindow

ROOT CAUSE (confirmed):
  FluentWindow.SetWindowChrome() sets GlassFrameThickness=0.00001
  when WindowBackdropType=None. Near-zero frame creates DWM
  timing gap showing white before WPF repaints.

ATTEMPTS MADE (all failed or introduced new bugs):
  1. OnSourceInitialized + WindowChrome.SetWindowChrome(GlassFrameThickness=-1)
     Result: Fixed flash briefly but caused ghost caption buttons
     because it conflicts with FluentWindow's HWND hit-test hook.
  2. HwndSource.CompositionTarget.BackgroundColor = dark color
     + Background on FluentWindow element and root Grid
     Result: Flash still visible. Ghost buttons unknown.

CURRENT CODE STATE:
  OnSourceInitialized uses CompositionTarget.BackgroundColor approach.
  Background set on both FluentWindow element and root Grid.
  Files: MainWindow.xaml/cs, ImageViewerWindow.xaml/cs

NEXT STEP: Come back to Claude to plan a new approach.
  Do NOT attempt another fix without planning session first.
  Consider: DwmSetWindowAttribute, WM_ERASEBKGND hook,
  SetClassLong GCLP_HBRBACKGROUND, or accepting the visual.
```

---

## FIXED BUGS

### BUG-001 — Build Failure: 3 Missing Class Files
```
Status:     FIXED
Severity:   Critical
Found:      2026-04-23 (commit ec5d913 introduced this)
Fixed:      2026-04-24
Feature:    Core infrastructure

SYMPTOM:
dotnet build fails with "type or namespace not found" errors
for MigrationService, PathSanitizer, WindowPositionTracker

ROOT CAUSE:
Commit ec5d913 "phases 0-5" added using statements and
method calls to 3 classes that were never implemented.

FIX:
Created all 3 missing files:
  Infrastructure/Helpers/PathSanitizer.cs
  Core/Schema/MigrationService.cs
  Core/Utils/WindowPositionTracker.cs

ADDITIONAL FIX:
App.xaml had hex color codes (#5E7CEC, #00BFA5) for
MaterialDesign BundledTheme which expects named colors.
Changed to PrimaryColor="Indigo", SecondaryColor="Teal".

VERIFIED BY:
dotnet build passes (warnings only, no errors)
dotnet run launches successfully, app window opens
```

---

### BUG-002 — Focus Border Animation Crash (Frozen Brush)
```
Status:     FIXED
Severity:   High
Found:      2026-04-24 (Phase 4 introduced this)
Fixed:      2026-04-24
Feature:    TextSection focus border animation

SYMPTOM:
Every time the text area in TextSection got or lost keyboard focus,
the app threw:
  System.InvalidOperationException: Cannot animate the 'Color'
  property on 'System.Windows.Media.SolidColorBrush' because the
  object is sealed or frozen.
This flooded the logs with errors on every focus change.

ROOT CAUSE:
The `SubtleDividerBrush` theme resource is a frozen SolidColorBrush
(WPF freezes shared ResourceDictionary brushes by default). The
Phase 4 code called BeginAnimation(SolidColorBrush.ColorProperty)
directly on the frozen brush from the theme, which is not allowed.

FIX:
In UI/TextSection.xaml.cs:
- Added `_borderAnimBrush` field for an unfrozen brush copy
- On first focus event, create a new SolidColorBrush from the current
  border color and assign it to TextBorder.BorderBrush
- Animate this unfrozen copy instead of the frozen theme resource

LESSON:
WPF freezes shared brushes from ResourceDictionaries by default.
Never call BeginAnimation on a brush from a theme resource —
always create a new unfrozen copy first.
```

---

### BUG-H001 — Coordinate Transform Crash During Tab Drag
```
Status:     FIXED
Severity:   Critical
Found:      2024 (pre v1.3.0)
Fixed:      Commit dc13206
Feature:    Tab drag-and-drop

SYMPTOM:
App crashes with "Visual is not an ancestor" exception
when dragging tabs. Sometimes crashes, sometimes works —
dependent on which window the drag started from.

ROOT CAUSE:
TransformToAncestor() was called with the wrong ancestor.
In WPF, coordinate transforms require the ancestor to be
in the actual visual tree above the element. Using the
wrong element as the ancestor throws an exception.

The wrong code was using a control as the ancestor when
it wasn't actually an ancestor in the visual tree at
drag time.

FIX:
Changed all coordinate transform calls to use MainWindow
as the common ancestor — it is always in the visual tree
above all tab controls.
TabManager.cs — all TransformToAncestor() calls

VERIFIED BY:
Drag tabs to different positions, different window sizes.
No more crashes. Consistent drop indicator positioning.

WATCH FOR:
Any future changes to the visual tree hierarchy could
reintroduce this. Always use MainWindow as common ancestor.
If the app ever supports detachable tab windows, this
will need revisiting.

LESSON:
In WPF multi-row tab layouts, coordinate math is tricky.
Always establish a single common ancestor for transforms.
Never assume which element is the ancestor — verify it.
```

---

### BUG-H002 — Drop Indicator Flicker Near Tab Boundaries
```
Status:     FIXED
Severity:   High
Fixed:      Tab drag hysteresis implementation
Feature:    Tab drag-and-drop visual feedback

SYMPTOM:
When dragging a tab and hovering near the boundary between
two tabs, the drop indicator line flickers rapidly back
and forth between two positions.

ROOT CAUSE:
Mouse movement near the midpoint of a tab boundary causes
the drop target index to toggle on every mouse move event.
Mouse → crosses midpoint → dropTargetIndex = N
Mouse → moves 1px back → dropTargetIndex = N-1
Mouse → moves 1px forward → N again → flicker

FIX:
Added TabDragHysteresisBuffer = 5.0px dead zone in
TabManager.cs. The drop target only changes if the mouse
has moved more than 5px past the previous boundary.
Comparison: dropTargetIndex vs lastDropTargetIndex.

VERIFIED BY:
Drag tab slowly near boundary — indicator stays stable
until mouse clearly crosses the boundary.

WATCH FOR:
Any change to the drag-over event handler in TabManager.cs
could accidentally remove the hysteresis check. The constant
TabDragHysteresisBuffer in AppConstants.cs is the value.

LESSON:
Any UI with visual indicators for hover positions needs
hysteresis. The exact value (5px) came from testing —
small enough to feel responsive, large enough to stop flicker.
```

---

---

## BUGS FOUND — DEEP CODE AUDIT 2026-05-18

### B-AUDIT-01 — debugImageLogging = true in Production (FIXED 2026-05-19)

```
Status:    FIXED
Severity:  HIGH — excessive disk writes, fills log files
Found:     2026-05-18 (Code Audit)
Fixed:     2026-05-19 (HYGIENE-1 Issue 1 + HYGIENE-2)

SYMPTOM:
debugImageLogging static field is set to true in ImageViewerWindow.xaml.cs line 46.
Every image load, navigation, zoom, and cache hit/miss generates multiple log entries.
With Serilog MinimumLevel.Debug active, these lines write to the rolling log file.

ROOT CAUSE:
Debug flag left enabled from Sprint D development work. Static field is shared
across all ImageViewerWindow instances. Serilog hardcoded to MinimumLevel.Debug().

FIX (two-part):
1. HYGIENE-1 Issue 1: Gated debugImageLogging behind #if DEBUG (ImageViewerWindow.xaml.cs)
2. HYGIENE-2: Gated Serilog MinimumLevel behind #if DEBUG (LoggingService.cs)
   Release builds now only log Info/Warning/Error — Debug traces suppressed entirely.

FILE:        UI/ImageViewerWindow.xaml.cs + Infrastructure/Logging/LoggingService.cs
EFFORT:      Trivial
RISK:        None
```

---

### B-AUDIT-02 — WM_NCHITTEST Coordinate Extraction Wrong for Multi-Monitor

```
Status:    OPEN
Severity:  MEDIUM — causes erratic resize behavior on multi-monitor left-of-primary
Found:     2026-05-18 (Code Audit)

SYMPTOM:
When main window or ImageViewerWindow is positioned on a monitor to the LEFT of
the primary monitor (negative screen coordinates), resize hit-testing misbehaves.
Resize handles may produce wrong HT codes, edge resize may not work.

ROOT CAUSE:
WindowChromeFix.cs line 126-127:
    int x = lParam.ToInt32() & 0xFFFF;    // WRONG — treats as unsigned
    int y = lParam.ToInt32() >> 16;        // WRONG — no sign extension for y
Both should sign-extend via (short) cast. Screen coords in lParam are signed 16-bit.

FILE:        Core/Utils/WindowChromeFix.cs lines 126-127
FIX:
    int x = (short)(lParam.ToInt32() & 0xFFFF);
    int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
EFFORT:      Trivial (2 lines)
RISK:        Low — isolated change, testable by moving window to left monitor
```

---

### B-AUDIT-03 — GC.Collect + WaitForPendingFinalizers on UI Thread

```
Status:    OPEN
Severity:  MEDIUM — blocks UI thread (all open windows) on ImageViewerWindow close
Found:     2026-05-18 (Code Audit)

SYMPTOM:
When ImageViewerWindow is closed, all other open windows briefly freeze.
Duration depends on GIF frame count and image count. Can be 100-500ms.

ROOT CAUSE:
ReleaseImageResources() calls GC.Collect() + GC.WaitForPendingFinalizers()
synchronously on the UI thread from the OnClosed handler. This is an anti-pattern.
The .NET GC does not need to be forced — setting references to null is sufficient.

FILE:        UI/ImageViewerWindow.xaml.cs lines 649-662
FIX:         Remove GC.Collect() and GC.WaitForPendingFinalizers() calls entirely.
             Leave the null assignments — those are correct.
EFFORT:      Trivial (2 lines removed)
RISK:        None
```

---

### B-AUDIT-04 — ImageViewerWindow Prev/Next Buttons Permanently Collapsed

```
Status:    OPEN
Severity:  MEDIUM — navigation buttons non-functional
Found:     2026-05-18 (Code Audit)

SYMPTOM:
PrevButton and NextButton in ImageViewerWindow.xaml have Visibility="Collapsed"
hardcoded. UpdateNavigationButtons() is referenced in PLANNING.md but the method
does not exist in the codebase. Buttons are permanently invisible.

ROOT CAUSE:
Navigation button UI was not implemented during Sprint IV1. Keyboard shortcuts
(Left/Right arrow) work but the visual buttons are always collapsed.

FILE:        UI/ImageViewerWindow.xaml lines 44-62
             UI/ImageViewerWindow.xaml.cs (UpdateNavigationButtons missing)
FIX:         Implement UpdateNavigationButtons() to set Visibility based on index.
             Change default Visibility to Visible in XAML.
EFFORT:      Small (1-2 hours)
RISK:        Low
```

---

### B-AUDIT-05 — IsDeleteConfirmationDisabled Two Sources of Truth

```
Status:    OPEN
Severity:  MEDIUM — re-enabling confirmation in settings may have no effect
Found:     2026-05-18 (Code Audit)

SYMPTOM:
User enables "Don't ask again" during a delete operation (sets skipDeleteConfirmation=true).
User then goes to Settings and re-enables delete confirmation.
appSettings.ConfirmTabDeletion becomes true, BUT skipDeleteConfirmation remains true.
Because the property returns (appSettings...false || skipDeleteConfirmation), the
OR condition still evaluates true → confirmation still disabled.

ROOT CAUSE:
TabManager.IsDeleteConfirmationDisabled line 55 has dual sources:
    return (!appSettings.ConfirmTabDeletion) || skipDeleteConfirmation;
When settings change, skipDeleteConfirmation is not reset.

FILE:        UI/TabManager.cs line 55
FIX:         Listen to settings change event and reset skipDeleteConfirmation when
             appSettings.ConfirmTabDeletion is re-enabled.
             OR: Remove skipDeleteConfirmation entirely and use only appSettings.
EFFORT:      Small
RISK:        High — TabManager is a protected file, test thoroughly
```

---

### B-AUDIT-06 — Vault Cleanup "Yes" Uses daysGracePeriod: 0

```
Status:    OPEN
Severity:  MEDIUM — orphan cleanup can delete recently-added media files
Found:     2026-05-18 (Code Audit)

SYMPTOM:
When user confirms the vault audit dialog, CleanupOrphanedImages(daysGracePeriod: 0)
is called. The dialog dialog states files within 24h will be deleted, which is true,
but the intent of a grace period is to protect files added very recently (race condition
between file paste and note save). Passing 0 eliminates all protection.

ROOT CAUSE:
MainWindow.xaml.cs line 521: hardcoded 0 for grace period.

FILE:        UI/Views/MainWindow.xaml.cs line 521
FIX:         Change to CleanupOrphanedImages(daysGracePeriod: 1) and update
             dialog text to match.
EFFORT:      Trivial
RISK:        Low
```

---

### B-AUDIT-07 — Saved Window Position Validation Rejects Negative Coordinates

```
Status:    OPEN
Severity:  MEDIUM — on multi-monitor left-of-primary, saved position is reset to default
Found:     2026-05-18 (Code Audit)

SYMPTOM:
After saving MainWindow position on a monitor to the left of the primary display
(negative Left coordinate), the position is rejected on next launch and the window
resets to center-of-primary-monitor.

ROOT CAUSE:
MainWindow.xaml.cs lines 166-176 validates:
    WindowData.WindowLeft >= 0 && WindowData.WindowLeft < SystemParameters.VirtualScreenWidth
The check requires Left >= 0 which excludes valid negative positions for left monitors.

FILE:        UI/Views/MainWindow.xaml.cs lines 166-176
FIX:
    Replace validation with:
    WindowData.WindowLeft >= SystemParameters.VirtualScreenLeft &&
    WindowData.WindowLeft < (SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth) &&
    WindowData.WindowTop >= SystemParameters.VirtualScreenTop &&
    WindowData.WindowTop < (SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
EFFORT:      Trivial
RISK:        Low
```

---

### B-AUDIT-08 — MigrationService: Old MediaReference ThumbnailSize Stays 0

```
Status:    OPEN
Severity:  MEDIUM — thumbnails in old notes render as zero-width
Found:     2026-05-18 (Code Audit)

SYMPTOM:
Notes created before ThumbnailSize was added to MediaReference schema load with
ThumbnailSize=0 (JSON missing field defaults to int default = 0). Migration bumps
DataVersion but does NOT fix ThumbnailSize to 150 (ThumbnailSizeBig default).
Thumbnails appear as zero-width or cause layout issues.

ROOT CAUSE:
MigrateNoteToCurrent() in MigrationService.cs bumps DataVersion to 3 but has
no fix for ThumbnailSize == 0 when it is a migrated pre-schema note.

FILE:        Core/Schema/MigrationService.cs (MigrateNoteToCurrent method)
FIX:
    foreach (var media in note.Media ?? Enumerable.Empty<MediaReference>())
    {
        if (media.ThumbnailSize == 0)
            media.ThumbnailSize = AppConstants.ThumbnailSizeBig;
    }
EFFORT:      Small
RISK:        Low — pure migration code, does not change active save logic
```

---

### B-AUDIT-09 — MediaBorder.MouseLeave Handler Can Accumulate

```
Status:    OPEN
Severity:  LOW — handler called multiple times on MouseLeave in rapid click sequences
Found:     2026-05-18 (Code Audit)

SYMPTOM:
In rapid click sequences on the MediaBorder in NoteTab, the MediaBorder.MouseLeave
handler is added multiple times (on each PreviewMouseDown) without being removed first.
Each MouseLeave then calls the handler N times.

ROOT CAUSE:
NoteTab.xaml.cs line 293: `MediaBorder.MouseLeave += MediaBorder_MouseLeave;`
This is in a PreviewMouseDown handler. If user clicks 3 times rapidly without
moving the mouse out, 3 subscriptions accumulate.

FILE:        UI/Views/NoteTab.xaml.cs line 293
FIX:
    MediaBorder.MouseLeave -= MediaBorder_MouseLeave;  // remove first
    MediaBorder.MouseLeave += MediaBorder_MouseLeave;
EFFORT:      Trivial
RISK:        Low
```

---

## BUG PATTERNS — WATCH LIST

| Pattern | Description | Files to Watch | Prevention |
|---------|-------------|----------------|------------|
| WPF coordinate transforms | Wrong ancestor = crash | TabManager.cs, any drag-drop code | Always use MainWindow as ancestor |
| Hysteresis regression | Removing dead zone = flicker | TabManager.cs drag-over handler | Keep TabDragHysteresisBuffer check |
| PowerShell UTF-16 | Default encoding breaks JSON | Any new file via terminal | UTF-8 WriteAllText always |
| Inline magic numbers | Breaks single source of truth | Any new code | Always use AppConstants.X |
| Inline hex colors in XAML | Breaks theming | Any XAML file | Always use {DynamicResource X} |
| GIF threading | Multiple fallback paths can conflict | ImageViewerWindow.xaml.cs | Test GIF paste + viewer carefully |
| Atomic file race | File.Replace() on different drives | AtomicFileManager.cs | Test on D:\ as well as C:\ |
| Frozen brush animation | Can't animate frozen theme brushes | Any animation code | Create unfrozen copy first |

---

## THE BUG PROTOCOL

When a bug appears — NEVER touch code immediately.

1. STOP — read the error completely
2. IDENTIFY — which file, line, function
3. TIME TRAVEL — when did this last work? what changed?
4. EXPLAIN — root cause in plain English first
5. PROPOSE — the fix before touching anything
6. FIX — surgical minimum change only
7. VERIFY — specific bug is gone, nothing new broke
8. LOG — write here immediately

Max 2 fix attempts. If second attempt fails → ROLLBACK.
Never layer fix on fix. See .cursor/rules/bug-protocol.mdc.

---

## BUG TRACKING FLOW (off-sprint)

When a bug is found outside of normal sprint work:

1. **Log here** in `docs/BUGS.md` — add to OPEN BUGS section with symptom + root cause
2. **Add to PLANNING.md** — 🔧 ACTIVE BUG FIXES table at top + full detail block
3. **Update COMPRESSED_CONTEXT.md** — CURRENT STATE → off-sprint bug fix note
4. **Fix it** → update all three files: status → FIXED, add fix details
5. **Move to FIXED BUGS** section in BUGS.md with lessons learned

This way every tool (Claude, LM Studio, Cursor) sees the same bug state from disk.
