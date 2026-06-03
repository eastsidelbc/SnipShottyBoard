# Audit Priority List
**Source:** Deep Code Audit — 2026-05-18
**Auditor:** Cursor AI (Sonnet 4.6)
**Ordered by:** Priority (fix order)

---

## 🔴 Fix Now — Blockers

### 1. §2 Critical Bugs
`debugImageLogging = true` in production, double WindowPositionTracker per secondary window, AssemblyVersion stuck at 1.6.0.0, and a Nov 2025 dev-era snapshot path artifact running on every startup.

### 2. §26 Logging Quality
Serilog set to `MinimumLevel.Debug` in production — every debug message hits disk. Combined with the debug image flag, log files balloon fast. Wrap in `#if DEBUG`.

### 3. §4 Memory Leaks
`GC.Collect()` + `WaitForPendingFinalizers()` called on the UI thread freezes all open windows on image viewer close. ThemeManager event holds MainWindow references permanently — windows never GC'd in multi-window sessions.

### 4. §1 White Flash / NCHITTEST Fix
Coordinate extraction uses unsigned mask — resize breaks on any monitor to the left of primary. Two-line fix with `(short)` cast on both x and y.

---

## 🟠 Fix Before Next Sprint

### 5. §14 Error Handling
Save failures silently swallowed — user sees "Saved" while data was never written. Recovery merges by tab title, breaking when two tabs share the same name (wrong note gets restored).

### 6. §21 State Management
Delete confirmation has two sources of truth — re-enabling it in settings has no effect if the in-session `skipDeleteConfirmation` flag is still true due to OR condition.

### 7. §15 UX Behavior
Prev/Next navigation buttons permanently `Collapsed` and `UpdateNavigationButtons()` never implemented. Vault cleanup calls `daysGracePeriod: 0` despite the dialog telling the user otherwise.

### 8. §3 Race Conditions
`Dispatcher.Invoke` (blocking) used inside `Task.Run` — should be `BeginInvoke` to avoid UI thread stalls during image loading. Rest of threading model is safe.

### 9. §25 Data Migration
Migration bumps schema version but never fixes `ThumbnailSize = 0` on old `MediaReference` objects — thumbnails render zero-width on any pre-schema legacy note data.

---

## 🟡 Clean Up — Architectural

### 10. §6 Architectural Violations
Inline hex/RGB colors in XAML and code-behind bypass the theme system entirely. `Debug.WriteLine` in 4 files silently drops log output in production builds.

### 11. §8 XAML Issues
`MouseLeave` handler added on every `PreviewMouseDown` without unsubscribing first — accumulates on rapid clicks. `SettingsWindow` uses `StaticResource` for button styles that can throw at parse time.

### 12. §22 Initialization Order
`DataManager` static constructor does file I/O — any failure wraps as `TypeInitializationException`, masking the real error completely. Move to explicit `Initialize()` called from `App.OnStartup`.

### 13. §23 Multi-Window Correctness
Window identification uses the WPF `Tag` property — not intended for this purpose. Primary window has no `Tag` set so duplicate detection never finds it.

### 14. §20 Async/Await
`LoadGifAsync` is synchronous despite its name — blocks the UI thread on large GIF decoding. Rename to `LoadGif()` and move decode into `Task.Run`.

---

## 🟢 Polish — Low Risk

### 15. §13 Performance
`WrapPanel` has no virtualization — 30+ thumbnails all live in the visual tree simultaneously. `StatusBarManager` allocates new `SolidColorBrush` instances every second — cache them.

### 16. §12 Simplifications
GIF pause `DispatcherTimer`, `SaveNoteWindows` read round-trip, and `MediaReference.FullPath` system call all have simpler equivalents that reduce allocations and disk reads.

### 17. §7 FluentWindow Gaps
Three windows missing `WindowCornerPreference="Round"`. `LightTheme.xaml` exists on disk but is never loaded — `ThemeManager` is a complete stub.

### 18. §19 WPF Anti-Patterns
`NoteListWindow` builds its entire visual tree imperatively in code-behind — not theme-aware and hard to maintain. Should be `DataTemplate` + `ItemsControl` + bindings.

### 19. §5 Dead Code
11 items: entire `ThemeManager` class is a no-op stub, `Examples/` folder compiles into the assembly, 6 stray `.md` files at repo root, multiple unused methods and fields.

### 20. §9 Compiler Warnings
~70 warnings, mostly nullable reference annotations. `MediaSection` CS8620 is functional — `List<string?>` passed where `List<string>` expected on line 1783. Filter nulls before passing.

### 21. §10 Codebase Hygiene
Version mismatch in `.csproj` (AssemblyVersion/FileVersion say 1.6.0.0, app is 1.7.0). `COMPRESSED_CONTEXT.md` missing. Stray root files. Duplicate XML doc comments in `DataManager`.

### 22. §17 Testability
`TabManager` and `MediaSection` untestable without WPF runtime. Sprint R needs a service layer extracted from both before unit tests are possible.

### 23. §18 Dependencies
`CommunityToolkit.Mvvm` only uses `ObservableCollection` which is in BCL — package removable, reducing build time and binary size. No vulnerabilities found.

### 24. §24 FluentWindow Edge Cases
`WindowChromeHeight = 55` constant may be wrong at non-100% DPI — `AutoSizeWindow` miscalculates. Window position validation rejects negative coordinates, breaking left-monitor save/restore.

### 25. §27 XAML Rendering
Verify `DropShadowEffect` count stays ≤2 visible simultaneously — each is software-rendered by CPU. `NoteListWindow` cards have no virtualization at scale.

### 26. §28 Accessibility
Thumbnail images have no `AutomationProperties.Name` — screen readers can't describe them. Custom splitter has no keyboard handler. Prev/Next buttons tracked above (§15).

### 27. §16 Security + Stability
Verify `MaxTabs` is actually enforced in `TabManager.CreateNewTab()`. Everything else — `Process.Start`, path handling, Serilog caps — is clean and safe.

### 28. §29 Release Readiness
Publish scripts not audited. Stray root files shouldn't ship. `Examples/` folder in compiled assembly should be excluded via csproj. Most hard release blockers covered above.

### 29. §30 Code Style
Private field naming inconsistent between old/new files (`camelCase` vs `_camelCase`). Resource key strings hardcoded as raw string literals — should be constants in a `ResourceKeys` class.

### 30. §11 Priority Action List
The audit's own cross-reference priority table. Use as a checklist once fixes above are underway — maps each finding to exact file + line number.

---

*Generated from audit_report.md — 2026-05-18*
