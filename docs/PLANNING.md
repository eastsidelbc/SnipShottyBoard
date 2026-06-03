## WORKFLOW PROTOCOL 🔒 LOCKED

**Planning:** Claude or LM Studio → design phases → files written → go to Cursor.
**Building:** Drag COMPRESSED_CONTEXT.md into Cursor → sprint loop runs → review when done.
Last updated: 2026-05-05

| Tool | Role | Sync trigger |
|------|------|--------------|
| Claude (claude.ai) | Planning, architecture, phases | Writes files via MCP |
| LM Studio | Same as Claude, local | Say "sync" |
| Cursor | Execute, code, build | Sprint loop auto-updates docs |

**File roles:**
- `PLANNING.md` — full blueprint, always wins on conflict
- `COMPRESSED_CONTEXT.md` — session starter, drag into Cursor
- `BUGS.md` — bug history (open + fixed)

---

## COMPLETED SPRINTS ✅

Full phase details archived in `docs/devnotes/`. Shown here at a glance:

| Sprint | What Was Built | Dev Notes |
|--------|---------------|-----------|
| A | Data layer (master.json, MediaReference model) | Various |
| B | LRU cache 100img/100MB, lazy loading, GIF cleanup | sprint-d2/d3 |
| 6A-6C | Deep dark chrome, indigo/purple, borderless editor | Codebase audit |
| H | Hygiene (CHANGELOG, VERSION 1.7.0, doc cleanup) | — |
| C | Crash recovery (silent journaling + startup restore) | crash-recovery* |
| P | AtomicFileManager camelCase fix (data persists!) | sprint-p1 |
| D | GIF viewer (WpfAnimatedGif, thread fix, memory cleanup) | sprint-d1*, d1b, d1a |
| F | FluentWindow native chrome (all 4 windows converted) | fluentwindow-mainwindow |
| I | Image dedup + vault hygiene | — |
| IV-1 | Image viewer zoom/pan/GIF/keyboard (2026-05-05) | sprint-iv1-image-viewer-zoom-pan |
| UI-1 | Layer cleanup + glow polish (transparent sections, purple hover) | sprint-ui1-layer-cleanup |
| UI-2 | Title bar icon swap + alignment (PackIcons, fixed sizing) | sprint-ui2-title-bar-icons |
| UI-3 | Thumbnail context menu + labels (6 phases, schema v3) | sprint-ui3-* (8 files) |
| UI-4 | Context menu dark styling + toggle checkmarks (4 phases) | sprint-ui4-context-menu-styling |
| UI-5 | Tab rename edit visuals + MediaSection global menu (3 steps) | sprint-ui5-tab-rename-media-menu |
| BUG FIX | Ghost images on cycling | sprint-ghost-images-fix |
| BUG FIX | Text editor natural behavior | — |
| BUG FIX | B-THEME: Theme toggle data loss | bug-theme-toggle-data-loss |
| BUG FIX | B-SAVE: ContentCardStyle forward reference → save corruption | bug-save-corruption-contentcardstyle |
| HYGIENE-1 | Production cleanup (debug gate, double tracker, version sync, dead migration) | sprint-hygiene-1-* (4 files) |
| HYGIENE-2 | Serilog logging level gate (DEBUG vs Release) | sprint-hygiene-2-logging-level |
| HYGIENE-3 | Memory leak fixes (ImageViewer forced GC + MainWindow event unsubscribe) ✅ | sprint-hygiene-3-memory-leaks |
| HYGIENE-4 | NCHITTEST signed coordinate fix (multi-monitor resize, closes B-WF) ✅ | 2026-05-19-sprint-hygiene-4-nchittest-signed-coords |

---

## OPEN ITEMS & ROADMAP TO v1.0

**Next:** Sprint G (native Windows features)

```
✅ DONE: HYGIENE-1 — 4 production cleanup fixes
✅ DONE: HYGIENE-2 — Serilog logging level gate
✅ DONE: HYGIENE-4 — NCHITTEST signed coordinate fix (multi-monitor resize, closes B-WF)
✅ DONE: B-LABELSIZE — Per-image label/size/visibility persisted via [JsonIgnore] on
         SavedNote.ImageFiles + ImageTimestamps; TabManager.GetSaveData now carries
         DataVersion + TabOrder (2026-06-03 off-sprint)
✅ DONE: B-MULTIWIN — Sticky-Notes-style multi-window restore. New IsOpen flag on
         NoteWindowData, App.OnStartup now restores every window flagged open at
         last shutdown, MainWindow_Closing tracks per-window vs last-window close
         (2026-06-03 off-sprint)
⬅ NEXT: Sprint G — System tray, minimize-to-tray, jump lists, start-with-Windows
⬅ Sprint G  — System tray, minimize-to-tray, jump lists, start-with-Windows
⬅ Sprint E  — Memory & performance audit
⬅ Sprint R  — Code health & technical debt (split TabManager/MediaSection, fix 262 warnings)
⬅ Sprint V  — v1.0 release prep

🔧 MINOR: Prev/Next nav buttons permanently Visibility="Collapsed" — need
       UpdateNavigationButtons() wired when allImagePaths.Count > 1.
```

**Build order:** G → E → R → V
**Target:** v1.0 — stable, polished, memory-safe

---

## APP VISION & ARCHITECTURE

**Identity:** Professional floating reference station for desktop power users.
Isolated multi-window workspaces, deep dark chrome, borderless text editing, media vault.

**Stack:** WPF .NET 8 • MaterialDesignInXAML 5.3.1 • WPF-UI 4.0.3 • WpfAnimatedGif 2.0.2

**Data:** Single master.json • Atomic writes • Media as physical files in %AppData%\SnipShottyBoard\images\
Schema v3: MediaReference has Label, ThumbnailSize, IsHidden, ShowLabel, ShowDate, ShowTime

**Protected files — never touch without exhaustive planning:**
- `UI/TabManager.cs` — 1641 lines, complex event wiring, drag-drop math
- `UI/MediaSection.xaml.cs` — 1239 lines, complex threading

---

## SPRINT HYGIENE-1 — PRODUCTION CLEANUP 🧹

**4 issues found, all low-risk, ready to implement.**

### Issue 1: `debugImageLogging = true` in production ✅ DONE
DECISION: Gate behind `#if DEBUG`
APPROACH: Wrap the static field with preprocessor so Release builds get `false`
FILE: `UI/ImageViewerWindow.xaml.cs` (~line 46)
RISK: Low — only affects log file volume. No behavior change.
Dev Note: `docs/devnotes/2026-05-19-sprint-hygiene-1-issue1-debug-gate.md`

### Issue 2: Double `WindowPositionTracker` per secondary window ✅ DONE
DECISION: Remove the tracker created in `NoteListWindow.OpenNoteWindow()`
APPROACH: Delete the `WindowPositionTracker` instantiation block and its closing cleanup handler from `OpenNoteWindow()`. MainWindow already self-manages position tracking via `SetupPositionTracking()` called in its own constructor.
FILE: `UI/NoteListWindow.xaml.cs` — `OpenNoteWindow()` method
RISK: Low — each window still gets exactly one tracker from its own code path.
Dev Note: `docs/devnotes/2026-05-19-sprint-hygiene-1-issue2-double-tracker.md`

### Issue 3: AssemblyVersion stuck at 1.6.0.0 ✅ DONE
DECISION: Sync `AssemblyVersion` and `FileVersion` to match `<Version>` 1.7.0
APPROACH: Update both XML properties in .csproj to `1.7.0.0`
FILE: `SnipShottyBoard.csproj` (~line 13–14)
RISK: None — cosmetic/version alignment only.
Dev Note: `docs/devnotes/2026-05-19-sprint-hygiene-1-issue3-version-sync.md`

### Issue 4: Nov 2025 dev-era snapshot path artifact running every startup ✅ DONE
DECISION: Delete the entire `ApplyCanonicalSnapshotIfNeeded()` migration code
APPROACH: Remove the two path constants (`CanonicalSnapshotPath`, `MigrationFlagPath`), the method body (~30 lines), and its call from the static constructor. The one-time migration already ran for all existing users — this is dead code.
FILE: `Core/Managers/DataManager.cs` (~lines 20–26 + static ctor + method)
RISK: Low — flag file (`notewindows_snapshot_applied.flag`) prevents re-run. Any user who never got the snapshot has nothing to migrate from that specific dev dump anyway.
Dev Note: `docs/devnotes/2026-05-19-sprint-hygiene-1-issue4-dead-migration.md`

**Sprint HYGIENE-1 COMPLETE — all 4 issues resolved.**

---

## SPRINT HYGIENE-2 — PRODUCTION LOGGING LEVEL 🧹 ✅ COMPLETE

**1 issue found, fixed.**

### Issue 1: Serilog `MinimumLevel.Debug()` enabled in Release ✅ DONE
**DECISION:** Gate Serilog MinimumLevel behind `#if DEBUG` preprocessor directives.
**APPROACH:** Variable `minLevel` set to `Debug` in DEBUG builds, `Information` in Release. Both main and fallback logger configs use `.MinimumLevel.Is(minLevel)`.
**FILE:** `Infrastructure/Logging/LoggingService.cs`
**Dev Note:** `docs/devnotes/2026-05-19-sprint-hygiene-2-logging-level.md`

**Sprint HYGIENE-2 COMPLETE.**

---

## SPRINT HYGIENE-3 — MEMORY LEAK FIXES 🧹 ✅ COMPLETE

**2 issues found, both low-risk, ready to implement.**

### Issue 1: ImageViewerWindow forced GC blocks UI thread on close 🔴 CRASH/FREEZE RISK
**FILE:** `UI/ImageViewerWindow.xaml.cs` — method `ReleaseImageResources()` (~line 429)
**PROBLEM:** Calls `GC.Collect();` and `GC.WaitForPendingFinalizers();` from the UI thread (`OnClosed`). These block the entire UI until all finalizers complete, freezing every open window during close.
**FIX:** Delete both GC lines. Also delete unused `long memBefore = GC.GetTotalMemory(false);`. The cleanup already done above (nulling `DisplayImage.Source`, disposing `StreamSource`, dropping `currentImage`) is sufficient — the GC will collect naturally without blocking.

**BEFORE:**
```csharp
private void ReleaseImageResources()
{
    try
    {
        long memBefore = GC.GetTotalMemory(false);  // ← DELETE THIS
        
        try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { }
        if (currentImage != null && !currentImage.IsFrozen)
            try { currentImage.StreamSource?.Dispose(); } catch { }

        if (DisplayImage != null) DisplayImage.Source = null;
        currentImage = null;
        currentImagePath = null;
        
        GC.Collect();                     // ← DELETE THIS
        GC.WaitForPendingFinalizers();    // ← DELETE THIS
    }
    catch (Exception ex) { /* log */ }
}
```

**AFTER:**
```csharp
private void ReleaseImageResources()
{
    try
    {
        try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { }
        if (currentImage != null && !currentImage.IsFrozen)
            try { currentImage.StreamSource?.Dispose(); } catch { }

        if (DisplayImage != null) DisplayImage.Source = null;
        currentImage = null;
        currentImagePath = null;
    }
    catch (Exception ex)
    {
        LoggingService.LogErrorStatic($"[CLS] Error during resource cleanup: {ex.Message}", ex, "ImageLoad");
    }
}
```

### Issue 2: MainWindow never unsubscribes from ThemeManager event 🟡 MEMORY LEAK
**FILE:** `UI/Views/MainWindow.xaml.cs` — methods `SetupEventHandlers()` and `MainWindow_Closing()`
**PROBLEM:** A lambda is subscribed to `themeManager.OnThemeChanged`. The lambda captures `this`, so as long as ThemeManager holds that event subscription, the MainWindow instance can never be garbage collected.

**FIX (3 steps):**
1. Replace inline lambda in `SetupEventHandlers()` with named method reference.
2. Add the named handler method to the class.
3. Unsubscribe in `MainWindow_Closing()`.

**STEP A — Find and replace in `SetupEventHandlers()`:**
```csharp
// BEFORE:
themeManager.OnThemeChanged += () => {
    hasUnsavedChanges = true;
    UpdateStatusBar();
    this.InvalidateVisual();
    tabManager.RefreshTabVisuals();
};

// AFTER:
themeManager.OnThemeChanged += OnThemeChangedHandler;
```

**STEP B — Add named handler (anywhere in the class):**
```csharp
private void OnThemeChangedHandler()
{
    hasUnsavedChanges = true;
    UpdateStatusBar();
    this.InvalidateVisual();
    tabManager.RefreshTabVisuals();
}
```

**STEP C — Add unsubscribe at top of `MainWindow_Closing()`:**
```csharp
private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
{
    try
    {
        // 🔌 Unsubscribe event handlers to prevent memory leaks
        if (themeManager != null)
            themeManager.OnThemeChanged -= OnThemeChangedHandler;

        // ... rest of existing cleanup ...
```

**Sprint HYGIENE-3 DECISION:** Both fixes are safe, zero-risk memory hygiene changes. Fix 1 removes blocking GC anti-pattern. Fix 2 properly extracts + unsubscribes event handler.
**NEXT STEP:** Apply Fix 1 (ImageViewerWindow), then Fix 2 (MainWindow). Build and verify. Open/close image viewers to confirm no freeze.

---

## SPRINT HYGIENE-4 — NCHITTEST SIGNED COORDINATE FIX 🧹 ✅ COMPLETE

**1 issue found, fixed.**

### Issue 1: Resize hit detection breaks on monitors left of primary ✅ DONE

**FILE:** `Core/Utils/WindowChromeFix.cs` — method `HandleNCHitTest()` (~line 125–126)

**PROBLEM:** Coordinate extraction from `WM_NCHITTEST` lParam uses unsigned masking. When a window is on a secondary monitor to the left of primary (negative screen X), `(int)lParam & 0xFFFF` produces a large positive number instead of the negative coordinate. All edge/resize hit detection breaks — resize cursors never appear, dragging edges causes white flash and ghost button artifacts because Windows can't determine which edge is being grabbed.

**Root cause:** `WM_NCHITTEST` packs **signed 16-bit** coordinates into lParam. The code treated them as unsigned.

**FIX:** Cast through `(short)` to sign-extend both X and Y properly.

**BEFORE:**
```csharp
int x = lParam.ToInt32() & 0xFFFF;
int y = lParam.ToInt32() >> 16;
```

**AFTER:**
```csharp
int x = (short)(lParam.ToInt32() & 0xFFFF);
int y = (short)(lParam.ToInt32() >> 16);
```

**RISK:** Zero. `(short)` cast is the documented Windows pattern for unpacking lParam coordinates. No behavioral change on single-monitor or right-side multi-monitor setups — only fixes broken behavior on left-side monitors.

**DECISION:** Two-line hygiene fix. Applies to all windows using `WindowChromeFix.Apply()`.
**NEXT STEP:** Build, verify with a window dragged to a left-side monitor edge.
**Closes:** B-WF (white flash + ghost buttons on resize)

**Sprint HYGIENE-4 COMPLETE.**

---

## SPRINT G — NATIVE WINDOWS FEATURES

**⚠️ STUB — plan with Claude before Cursor executes.**

Goal: System tray icon, minimize to tray, taskbar jump lists, Start With Windows.
- System tray: right-click → Open, New Window, Exit. Double-click restores.
- Jump list: right-click taskbar → New Note Window, Open Logs.
- Start With Windows: registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

---

## SPRINT E — MEMORY & PERFORMANCE AUDIT

**⚠️ STUB — plan with Claude before Cursor executes.**

Goal: Profile RAM over realistic session. Find event handler leaks on tab/window close.
Verify GIF frame disposal, LRU eviction, DispatcherTimer cleanup. Set RAM baseline target.

---

## SPRINT R — CODE HEALTH & TECHNICAL DEBT

**⚠️ STUB — plan with Claude before Cursor executes.**
TabManager.cs and MediaSection.xaml.cs are PROTECTED — plan exhaustively before touching.

Goal: Split TabManager.cs (1641 lines) and MediaSection.xaml.cs (1239 lines).
Fix 262 compiler warnings. Wire WindowPositionTracker, PathSanitizer, test MigrationService.

---

## SPRINT V — v1.0 RELEASE PREP

**⚠️ STUB — plan with Claude before Cursor executes.**

Goal: Version bump to 1.0.0, publish.ps1 polish, release package (exe + README + checksums),
final README, git tag v1.0.0, verify single-file publish.

---

## BACKLOG (approved, not started)

- Note Windows panel → toolbar button or Ctrl+Shift+N
- Settings access → Ctrl+, shortcut (VS Code style)
- Theme toggle → move inside Settings → Appearance tab
- Help/shortcuts → F1 shortcut
- Scrollbar → restore sleek thin character from before
- Media thumbnails → larger decode/display size
- B-WF white flash → plan new approach with Claude (DwmSetWindowAttribute?)

---

## IDEAS (not yet approved)

- Avalonia port (cross-platform, full rewrite)
- DI container (Prism or Microsoft.Extensions.DI)
- Unit test project (needs Sprint R first)
