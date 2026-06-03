# Code Quality Sprint E — Deferred Items Tracking Document
# Issues: §18, §19, §22, §24, §25, §26, §28, §30
# This is NOT a Cursor execute prompt.
# These items are tracked here for Sprint R (release prep) and beyond.
# Do not action these until feature work is stable and the earlier sprints are done.

---

## DEFERRED-1 — NoteListWindow: DataTemplate + ItemsControl refactor
**Finding:** §18, §19 WPF Anti-Patterns
**Priority:** Medium — before v1.0
**Effort:** Large (3-4h)

NoteListWindow builds its entire card UI imperatively in CreateNoteWindowCard() —
~80 lines of code-behind constructing Grids, Borders, TextBlocks, Buttons manually.
This approach is not theme-aware, fragile to change, and hard to maintain.

The WPF-native pattern: bind `NoteWindowManager.Instance.NoteWindows` (an
ObservableCollection) to an `ItemsControl` via `ItemsSource`, define the card
appearance in a `DataTemplate` in XAML, and use `ICommand` or event handlers
on the data model.

**When to do it:** After NoteListWindow UX is finalized (no more card layout changes).
Doing this while UI is still being designed means refactoring twice.

**Approach when ready:**
1. Add `INotifyPropertyChanged` to `NoteWindowData` (or use CommunityToolkit if
   re-added) so bindings update automatically
2. Define `DataTemplate` in NoteListWindow.xaml
3. Bind `ItemsControl.ItemsSource` to `NoteWindowManager.Instance.NoteWindows`
4. Delete `CreateNoteWindowCard()` and `RefreshNoteWindowsList()` from code-behind
5. Commands or routed events for Rename/Close buttons

---

## DEFERRED-2 — Service layer extraction for testability
**Finding:** §22 Testability
**Priority:** Medium — Sprint R (pre-release)
**Effort:** Large (1-2 days)

`TabManager` and `MediaSection` are untestable without a running WPF runtime because
they directly instantiate and manipulate WPF visual elements in their logic paths.

To make them testable:
- Extract `ITabService` interface with `CreateTab`, `DeleteTab`, `GetSaveData` etc.
- Extract `IMediaService` interface with `AddImage`, `RemoveImage` etc.
- Move business logic (save/load data, validation, state management) into service
  implementations that don't reference WPF types
- Keep only pure UI code in the WPF controls
- Unit test the service classes without WPF

**When to do it:** Sprint R. This is architecture work, not a bug fix. Doing it now
would be premature — the UI and data model need to be stable first.

---

## DEFERRED-3 — DPI-aware window chrome height + left-monitor position restore
**Finding:** §24 FluentWindow Edge Cases
**Priority:** Medium — before v1.0
**Effort:** Small (1-2h)

Two related issues:

**Issue A — WindowChromeHeight constant:**
`AppConstants.WindowChromeHeight = 55` is used in `AutoSizeWindow()` in
`ImageViewerWindow.xaml.cs` to calculate available image area. At 150% DPI, the
actual chrome is taller — images sized slightly too large, scroll bars appear.

Fix: Replace the constant with a runtime measurement after the window is loaded:
```csharp
// In ApplyOneToOne() or after window Loaded event:
double actualChrome = this.ActualHeight - ImageScrollViewer.ActualHeight;
// Use actualChrome instead of AppConstants.WindowChromeHeight for sizing
```

**Issue B — Negative coordinate rejection:**
`MainWindow` constructor rejects window positions with `Left < 0` or `Top < 0`,
which are valid coordinates on monitors positioned left of or above the primary
monitor. Users lose their window position every session if they use a left-side monitor.

Fix in MainWindow position restore (constructor):
```csharp
// Old:
if (WindowData.WindowLeft >= 0 && WindowData.WindowLeft < SystemParameters.VirtualScreenWidth)
    this.Left = WindowData.WindowLeft;

// New — use VirtualScreen bounds which handle negative coords:
if (WindowData.WindowLeft >= SystemParameters.VirtualScreenLeft &&
    WindowData.WindowLeft < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth)
    this.Left = WindowData.WindowLeft;

if (WindowData.WindowTop >= SystemParameters.VirtualScreenTop &&
    WindowData.WindowTop < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
    this.Top = WindowData.WindowTop;
```

**When to do it:** Issue B (negative coords) is a real user-facing bug — fix before v1.0.
Issue A (DPI chrome) is a minor visual issue — fix before v1.0 but low urgency.

---

## DEFERRED-4 — DropShadowEffect count audit
**Finding:** §25 XAML Rendering
**Priority:** Low
**Effort:** Trivial (verification only)

DropShadowEffect is software-rendered by CPU — each simultaneous visible instance
costs CPU time on every render pass. If more than 2-3 are visible simultaneously
the app may stutter during animations or scrolls.

Current known usages in the codebase:
- TabManager.cs: drag visual DropShadowEffect (visible only during drag — OK)
- MediaSection.xaml.cs: insertion indicator DropShadowEffect (visible during drag — OK)
- MediaSection.xaml.cs: image drag ghost DropShadowEffect (visible during drag — OK)
- Any EditorFocusGlow effects in XAML (check DarkTheme.xaml)

These are all drag-only, so maximum 3 simultaneous during drag, 0 at idle. This is
acceptable. No action needed unless new persistent DropShadowEffects are added.

**Rule going forward:** Never add a persistent DropShadowEffect (always-visible).
Use `Effect` only during transient UI states (drag, focus, hover).

---

## DEFERRED-5 — Accessibility
**Finding:** §26 Accessibility
**Priority:** Low — nice to have for v1.0
**Effort:** Small (2-3h total)

Three accessibility gaps:

**Gap A — Thumbnail AutomationProperties.Name:**
Screen readers cannot describe thumbnail images. Add to each thumbnail Image element:
```csharp
// In MediaSection when creating Image element:
System.Windows.Automation.AutomationProperties.SetName(image, 
    mediaRef.Label ?? Path.GetFileNameWithoutExtension(imagePath));
```

**Gap B — Custom splitter keyboard handler:**
The NoteTab text/media splitter is a Border with mouse drag events but no keyboard
handler. Screen reader users cannot resize it. Add `KeyDown` handling:
```csharp
splitter.Focusable = true;
splitter.KeyDown += (s, e) => {
    if (e.Key == Key.Up) AdjustRowHeights(-20);
    if (e.Key == Key.Down) AdjustRowHeights(20);
};
```

**Gap C — Prev/Next button labels:**
Once Prev/Next are unhidden (pre-sprint fix), ensure they have `ToolTip` and
`AutomationProperties.Name` set to "Previous image" / "Next image".

---

## DEFERRED-6 — Release readiness
**Finding:** §28 Release Readiness
**Priority:** High — must complete before shipping
**Effort:** Medium (2-4h)

Checklist for Sprint R:

- [ ] Audit `SnipShottyBoard.csproj` publish profile settings
- [ ] Verify `Examples/` folder is excluded from published output
- [ ] Verify stray root `.md` files (prompt1.md, summary.md, etc.) are excluded
      from published output via `<None Include="*.md"><CopyToOutputDirectory>Never</CopyToOutputDirectory></None>`
- [ ] Verify Serilog log caps are set (max log file size, rolling interval)
- [ ] Run publish in Release mode and inspect output folder — no .md, no Examples/
- [ ] Verify `debugImageLogging = false` in release (already gated by #if DEBUG ✅)
- [ ] Run `dotnet publish -c Release` and test the output exe from a clean folder
- [ ] Verify app creates %AppData%\SnipShottyBoard on first run (DataManager.Initialize ✅)
- [ ] Verify crash recovery works from the published build

---

## DEFERRED-7 — NoteListWindow card virtualization at scale
**Finding:** §25 XAML Rendering
**Priority:** Low — only matters if users have 20+ note windows
**Effort:** Small (after DEFERRED-1 DataTemplate refactor)

`NoteWindowsList` is a `StackPanel` (or similar non-virtualizing panel). With 20+
note windows, all cards render simultaneously. Replace with `VirtualizingStackPanel`
via `ItemsControl` (requires DEFERRED-1 first).

---

## DEFERRED-8 — ThemeManager stub: document intent
**Finding:** §5 Dead Code, §17 FluentWindow Gaps
**Status:** Intentional by design — dark mode only

`ThemeManager` is three no-op methods. `LightTheme.xaml` exists on disk but is never
loaded. `OnThemeChanged` event is subscribed in MainWindow but never raised.

This is intentional — the app is dark-mode only. The infrastructure is kept in case
light mode is added later without touching call sites.

**No action needed.** Add a comment to ThemeManager explaining this:
```csharp
// Dark mode is the only supported theme.
// ThemeManager is intentionally minimal — stub methods kept for future light mode support.
// LightTheme.xaml exists on disk as a starting point but is not loaded.
// OnThemeChanged is never raised; MainWindow subscribes defensively.
```

---

## PRIORITY ORDER FOR DEFERRED ITEMS

| ID | Item | Do before v1.0? | Effort |
|---|---|---|---|
| DEFERRED-3B | Left-monitor position restore | YES — user-facing bug | Small |
| DEFERRED-6 | Release readiness checklist | YES — required | Medium |
| DEFERRED-3A | DPI chrome height | YES — visual issue | Small |
| DEFERRED-5 | Accessibility (3 gaps) | Nice to have | Small |
| DEFERRED-1 | NoteListWindow DataTemplate | Nice to have | Large |
| DEFERRED-2 | Service layer / testability | Sprint R | Large |
| DEFERRED-4 | DropShadow audit | No action needed | — |
| DEFERRED-7 | NoteListWindow virtualization | After DEFERRED-1 | Small |
| DEFERRED-8 | ThemeManager comment | Trivial | Trivial |
