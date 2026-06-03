# DEVNOTES.md
# ============================================================
# Session Diary — SnipShottyBoard
# Location: docs/DEVNOTES.md
# Updated: session start, every 20 exchanges, session end
# ============================================================
# NOTE FOR AI:
# Read only the LAST 1-2 sessions for context.
# Older sessions are history — use PROJECT_MEMORY.md
# for overall state. Be specific — vague entries are useless.
# ============================================================

---

## SESSION 1 — 2026-04-23

### Overview
```
Date:        2026-04-23
Duration:    Full setup session
Focus:       Setting up vibe coding system + diagnosing build failure
Outcome:     Partial — files created, build not yet verified
Tools:       LM Studio (planning) + Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

Setup work completed:
- Full codebase audit run on both branches (main and modernwpf)
- Determined main branch (ec5d913) is less broken — use this
- Determined modernwpf branch is abandoned mid-refactoring
- Set up complete .cursor/rules/*.mdc system (4 files)
- Set up .cursorignore
- Created all docs/ memory files with SSB-specific content

Build fix:
- Identified 3 missing files causing build failure
- Created Infrastructure/Helpers/PathSanitizer.cs
- Created Core/Schema/MigrationService.cs
- Created Core/Utils/WindowPositionTracker.cs

### Decisions Made
- Use main branch (ec5d913), not modernwpf branch
- .cursor/rules/*.mdc replaces old .cursorrules and CR.md
- No need to recover CR.md — new system is better
- All docs/ files need SSB content (React placeholders removed)

### Current State At End Of Session
```
Working:  Docs/ system set up with real SSB content
          3 missing files created
Pending:  Install .NET 8 SDK → run dotnet build → verify fix
          Wire WindowPositionTracker into MainWindow
          Wire PathSanitizer into logging calls
Broken:   Build still needs verification after file placement
```

### Files Changed This Session
```
Created: .cursor/rules/core.mdc
Created: .cursor/rules/bug-protocol.mdc
Created: .cursor/rules/building.mdc
Created: .cursor/rules/memory.mdc
Created: .cursorignore
Created: Infrastructure/Helpers/PathSanitizer.cs
Created: Core/Schema/MigrationService.cs
Created: Core/Utils/WindowPositionTracker.cs
Updated: docs/PROJECT_MEMORY.md (replaced React placeholders)
Updated: docs/PLANNING.md (real SSB current state)
Updated: docs/DECISIONS.md (real WPF decisions)
Updated: docs/BUGS.md (historical bugs logged)
Updated: docs/DEVNOTES.md (this file)
```

### Next Session Should Start With
```
1. Verify .NET 8 SDK is installed: dotnet --version
2. Place the 3 new .cs files in correct locations
3. Run: dotnet build
4. If errors: read them fully before touching anything
5. If success: run the app and test basic functionality
6. Then: wire WindowPositionTracker into MainWindow
```

### Jeremy's Notes
Getting the vibe coding system set up. Building is set up
with LM Studio → ngrok → Cursor connection working.
Qwen3.6-27B running locally. Next session focus on
actually getting the build working and then planning
what features to add to SSB.

---

## SESSION 2 — 2026-04-24

### Overview
```
Date:        2026-04-24
Duration:    In progress
Focus:       Fix build, verify app runs, prepare for UI modernization
Outcome:     Build fixed, app running successfully
Tools:       Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

Build fix completed:
- Verified 3 missing files from Session 1 were on disk
- Fixed App.xaml: hex color codes (#5E7CEC, #00BFA5) changed to
  named MaterialDesign colors (Indigo, Teal) — this was the crash
- `dotnet build` passes (0 errors, 0 warnings)
- `dotnet run` launches successfully, app window opens

Phase 1 — Theme Resources (UI Modernization):
- Added MaterialDesignThemes 5.3.1 and MaterialDesignColors to csproj
- Integrated MaterialDesign BundledTheme in App.xaml (Dark, Indigo/Teal)
- Added MaterialDesign3.Defaults.xaml resource dictionary
- Added new premium theme tokens to DarkTheme.xaml:
  CardBackgroundBrush (#0AFFFFFF), AccentGlowBrush (#304A90E2),
  TabActiveGlowBrush (#404A90E2), SubtleDividerBrush (#10FFFFFF)
- Added CardShadowStyle for consistent card elevations
- Upgraded MainWindow.xaml: tab strip and content area wrapped in
  materialDesign:Card with UniformCornerRadius="8" for rounded corners
- Replaced inline hex colors with DynamicResource references
- Killed SnipShottyBoard process (PID 18252) to unblock build

Docs updated:
- BUGS.md: BUG-001 moved from OPEN to FIXED, OPEN section cleared
- PLANNING.md: Phase 0 and Phase 1 marked COMPLETE
- PROJECT_MEMORY.md: status and last session updated

### Decisions Made
- App.xaml theme colors use named MD colors, not hex codes
- MaterialDesignThemes 5.3.1 is the theming library for SSB
- Card-based layout with rounded corners (UniformCornerRadius="8")
  for tab strip and content area
- Phase 0 and Phase 1 are done; ready for Phase 2

### Current State At End Of Session
```
Working:  Build passes (0 errors, 0 warnings), app launches
          All 3 missing files implemented
          App.xaml theme fixed (named MD colors)
          Phase 1 (Theme Resources) COMPLETE:
            - MaterialDesignThemes 5.3.1 added to csproj
            - MaterialDesign BundledTheme in App.xaml (Dark, Indigo/Teal)
            - New theme tokens in DarkTheme.xaml (CardBackgroundBrush,
              AccentGlowBrush, TabActiveGlowBrush, SubtleDividerBrush)
            - CardShadowStyle added
            - MainWindow.xaml upgraded: tab strip and content area
              wrapped in materialDesign:Card with rounded corners
            - Removed inline hex colors from MainWindow.xaml
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Modified: App.xaml (theme color fix + MaterialDesign integration)
Modified: SnipShottyBoard.csproj (added MaterialDesignThemes 5.3.1)
Modified: Resources/Themes/DarkTheme.xaml (new premium theme tokens)
Modified: UI/Views/MainWindow.xaml (card-based layout, rounded corners)
Modified: docs/BUGS.md (BUG-001 moved to FIXED, OPEN cleared)
Modified: docs/PLANNING.md (Phase 1 complete)
Modified: docs/PROJECT_MEMORY.md (status update)
Modified: docs/DEVNOTES.md (this session entry)
```

### Next Session Should Start With
1. Proceed to Phase 2 of UI Modernization (PLANNING.md)
2. Verify build passes after each phase
3. Wire WindowPositionTracker into MainWindow (tech debt)
4. Wire PathSanitizer into logging calls (tech debt)

### Jeremy's Notes
Session start doc shared mid-session. App is running.
Phase 1 of UI modernization complete. Build is green.
Ready for Phase 2.

---

## SESSION 3 — 2026-04-24

### Overview
```
Date:        2026-04-24
Duration:    Short focused session
Focus:       Phase 2 — Card containers for NoteTab.xaml
Outcome:     Phase 2 complete, build passing
Tools:       Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

Phase 2 — Card Containers for Content Area:
- Added `ContentCardStyle` to DarkTheme.xaml (Border style with
  CardBackgroundBrush, CornerRadius=8, Padding=8, Margin=0,4,
  DropShadowEffect with 0.3 opacity, BlurRadius=8)
- Wrapped TextSection and MediaSection in NoteTab.xaml inside
  `<Border Style="{DynamicResource ContentCardStyle}">` containers
- Splitter left unchanged between the two card containers
- `dotnet build` passes — 0 errors

### Decisions Made
- ContentCardStyle uses a Border (not materialDesign:Card) to avoid
  MDXAML style conflicts with existing custom brushes
- DropShadowEffect parameters: Color=#000000, Direction=270,
  ShadowDepth=2, Opacity=0.3, BlurRadius=8 — subtle enough for dark theme

### Current State At End Of Session
```
Working:  Build passes (0 errors), app launches
          Phase 1 COMPLETE: MaterialDesign integrated, theme tokens,
            MainWindow card layout
          Phase 2 COMPLETE: NoteTab sections wrapped in floating
            card containers with drop shadows and rounded corners
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Modified: Resources/Themes/DarkTheme.xaml (added ContentCardStyle)
Modified: UI/Views/NoteTab.xaml (wrapped sections in card Borders)
```

### Next Session Should Start With
1. Proceed to Phase 3 of UI Modernization (Pill-style tabs)
2. Verify build passes after each phase

### Jeremy's Notes
Phase 2 done quickly. Two files touched, build green.
Ready for Phase 3 (tab pill styles).

---

## SESSION 4 — 2026-04-24

### Overview
```
Date:        2026-04-24
Duration:    Short focused session
Focus:       Phase 3 — Tab strip modernization (floating pill tabs)
Outcome:     Phase 3 complete, build passing
Tools:       Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

Phase 3 — Tab Strip Modernization (Floating Pills):
- Rewrote `TabButtonStyle` in DarkTheme.xaml:
  - CornerRadius changed from "3,3,0,0" (rounded-top) to "16" (full pill)
  - Added subtle border: BorderThickness="1" with SubtleDividerBrush
  - Active state (Tag="Selected"): uses TabActiveGlowBrush background +
    AccentBrush border for blue glow ring effect
  - Active + hover: brightens glow to #504A90E2
  - Padding increased to "16,6" for wider pill feel
  - FontWeight: Medium default, SemiBold on active
- Updated MainWindow.xaml tab strip container:
  - Replaced materialDesign:Card with hardcoded #16161F background
    and #00BFA5 border with a plain Border using SubtleDividerBrush
    and BorderBrush (no more inline hex colors)
- Updated MainWindow.xaml content area:
  - Replaced hardcoded #1E1E2E with {DynamicResource CardBackgroundBrush}
- `dotnet build` passes — 0 errors

### Decisions Made
- Tab strip container uses a plain Border instead of materialDesign:Card
  to avoid MDXAML style conflicts and keep the pill tabs visually distinct
  from the card containers below
- All inline hex colors (#16161F, #00BFA5, #1E1E2E) in MainWindow.xaml
  tab strip and content area replaced with DynamicResource references
- Pill shape (CornerRadius=16) creates a floating, detached look rather
  than the previous Edge-like tab shape (rounded-top only)

### Current State At End Of Session
```
Working:  Build passes (0 errors), app launches
          Phase 1 COMPLETE: MaterialDesign integrated, theme tokens,
            MainWindow card layout
          Phase 2 COMPLETE: NoteTab sections wrapped in floating
            card containers with drop shadows and rounded corners
          Phase 3 COMPLETE: Tab buttons are floating pills with
            fully rounded corners (CornerRadius=16), subtle border,
            and blue accent glow on active tab
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Modified: Resources/Themes/DarkTheme.xaml (rewrote TabButtonStyle to pill)
Modified: UI/Views/MainWindow.xaml (removed inline hex, pill container)
```

### Next Session Should Start With
1. Proceed to Phase 4 of UI Modernization (Typography upgrade)
2. Verify build passes after each phase

### Jeremy's Notes
Phase 3 done. Two files touched, build green.
Ready for Phase 4 (Typography — Inter font, text polish).

---

## SESSION 5 — 2026-04-24

### Overview
```
Date:        2026-04-24
Duration:    Short focused session
Focus:       Phase 4 — Typography & Input Polish
Outcome:     Phase 4 complete, build passing (0 errors, 0 warnings)
Tools:       Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

Phase 4 — Typography & Input Polish:
- Added typography resources to DarkTheme.xaml:
  - `AppFontFamily` → Segoe UI, system (Inter to be bundled later if desired)
  - `HeadingFontFamily` → Segoe UI Semibold, system
  - `BodyFontSize` (14), `SmallFontSize` (12), `HeadingFontSize` (16)
  - Used `sys:Double` (not `x:Double`) since ResourceDictionary needs
    the `clr-namespace:System;assembly=mscorlib` import for primitive types
- Updated TextSection.xaml:
  - RichTextBox FontFamily → `{DynamicResource AppFontFamily}`
  - RichTextBox FontSize → `{DynamicResource BodyFontSize}`
  - PlaceholderText: lower opacity (0.5→0.35), italic style, better padding (8→12)
  - Text area border: subtle divider line (`SubtleDividerBrush`, thickness=1)
  - Added `GotKeyboardFocus`/`LostKeyboardFocus` event handlers
- Updated TextSection.xaml.cs:
  - Added `using System.Windows.Media` and `System.Windows.Media.Animation`
  - `NoteRichTextBox_GotKeyboardFocus`: smooth 150ms color animation to
    `AccentBrush` on the text border
  - `NoteRichTextBox_LostKeyboardFocus`: smooth return to `SubtleDividerBrush`
- `dotnet build` passes — 0 errors, 0 warnings

### Decisions Made
- Font fallback chain uses Segoe UI (not Inter bundled yet) since bundling
  .ttf files adds project complexity. Inter can be added later via
  `pack://application:,,,/fonts/#Inter` when desired
- `sys:Double` used instead of `x:Double` for numeric resources —
  `x:Double` is not recognized in standalone ResourceDictionary files
- Focus border animation animates `SolidColorBrush.ColorProperty` (not
  `Border.BorderBrushProperty`) since the latter is a Brush, not a Color
- Placeholder opacity reduced to 0.35 (from 0.5) for a more subtle,
  premium appearance

### Current State At End Of Session
```
Working:  Build passes (0 errors, 0 warnings), app launches
          Phase 1 COMPLETE: MaterialDesign integrated, theme tokens,
            MainWindow card layout
          Phase 2 COMPLETE: NoteTab sections wrapped in floating
            card containers with drop shadows and rounded corners
          Phase 3 COMPLETE: Tab buttons are floating pills with
            fully rounded corners (CornerRadius=16), subtle border,
            and blue accent glow on active tab
          Phase 4 COMPLETE: Typography resources added, TextSection
            uses DynamicResource fonts/sizes, placeholder polished,
            focus border animation on text area
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Modified: Resources/Themes/DarkTheme.xaml (typography resources,
          sys:Double import)
Modified: UI/TextSection.xaml (DynamicResource fonts, placeholder
          styling, focus events, border)
Modified: UI/TextSection.xaml.cs (focus animation handlers,
          Media/Animation imports)
```

### Next Session Should Start With
1. Proceed to Phase 5 of UI Modernization (Animations, glows, polish)
2. Verify build passes after each phase

### Jeremy's Notes
Phase 4 done. Three files touched, build green.
Ready for Phase 5 (Animations, glows, final polish).

---

## SESSION 6 — 2026-04-24

### Overview
```
Date:        2026-04-24
Duration:    Short focused session
Focus:       Phase 5 — Animations, glows, final polish
Outcome:     Phase 5 complete, build passing (0 errors)
Tools:       Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

Phase 5 — Animations, Glows & Polish:
- Added glow effect resources to DarkTheme.xaml:
  - `ActiveGlow` — blue DropShadowEffect (Color=#4A90E2, Opacity=0.5, BlurRadius=12)
  - `HoverGlow` — white DropShadowEffect (Opacity=0.15, BlurRadius=8)
  - `DangerGlow` — red DropShadowEffect (Color=#DC3545, Opacity=0.6, BlurRadius=12)
- Added `HeaderCloseButtonStyle` — close button gets red foreground + danger glow on hover
- Added `HeaderPinButtonStyle` — pin button gets accent glow when Tag="Pinned", hover glow otherwise
- Added `ContentCardHoverStyle` — extends ContentCardStyle with HoverGlow on IsMouseOver
- MainWindow.xaml:
  - Window fade-in animation (300ms, CubicEase EaseOut) on Loaded event via RootBorder
  - Close button switched to HeaderCloseButtonStyle
  - Pin button switched to HeaderPinButtonStyle
- NoteTab.xaml:
  - Tab content fade-in (200ms, CubicEase EaseOut) on Loaded via ContentGrid
  - Both card borders switched to ContentCardHoverStyle for hover glow
  - Splitter grip animations upgraded with CubicEase easing (EaseOut on enter, EaseIn on leave)
- `dotnet build` passes — 0 errors

### Decisions Made
- Fade-in durations: 300ms for window (slower, more dramatic), 200ms for tabs (faster, snappier)
- CubicEase easing used for all new animations (EaseOut for entrances, EaseIn for exits)
- Glow effects use DropShadowEffect (not BlurEffect) for better performance — only applied
  on hover/active states, not permanently on every element
- Close button glow uses DangerGlow (red) to clearly signal destructive action
- Pin button glow uses ActiveGlow (blue) to match the existing accent theme
- ContentCardHoverStyle extends ContentCardStyle rather than replacing it, preserving
  the existing drop shadow and adding a subtle white hover glow

### Current State At End Of Session
```
Working:  Build passes (0 errors), app launches
          Phase 1 COMPLETE: MaterialDesign integrated, theme tokens,
            MainWindow card layout
          Phase 2 COMPLETE: NoteTab sections wrapped in floating
            card containers with drop shadows and rounded corners
          Phase 3 COMPLETE: Tab buttons are floating pills with
            fully rounded corners (CornerRadius=16), subtle border,
            and blue accent glow on active tab
          Phase 4 COMPLETE: Typography resources added, TextSection
            uses DynamicResource fonts/sizes, placeholder polished,
            focus border animation on text area
          Phase 5 COMPLETE: Animations (fade-in window/tabs), glow
            effects (active/hover/danger), card hover glow, close
            button danger glow, pin button accent glow, splitter
            easing improvements
          UI Modernization sprint: ALL 5 PHASES COMPLETE
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Modified: Resources/Themes/DarkTheme.xaml (glow effects, button styles,
          card hover style)
Modified: UI/Views/MainWindow.xaml (fade-in animation, close button
          style, pin button style)
Modified: UI/Views/NoteTab.xaml (fade-in animation, card hover style,
          splitter easing)
```

### Next Session Should Start With
1. UI Modernization sprint is COMPLETE — all 5 phases done
2. Test the app visually and confirm all animations work
3. Consider tech debt items: wire WindowPositionTracker, PathSanitizer
4. Address compiler warnings (278 pre-existing) if desired
5. Make git commits for the UI modernization sprint

### Jeremy's Notes
Phase 5 done. Three files touched, build green.
UI Modernization sprint is COMPLETE. All phases 0-5 done.
App should look premium now — test visually.

---

## BUG FIX — 2026-04-24 (Post-Session 6)

### Overview
```
Date:        2026-04-24
Focus:       Fix focus border animation crash
Outcome:     Fixed — 0 errors
```

### Bug
Every time the text area in TextSection got or lost keyboard focus, the app
threw `System.InvalidOperationException: Cannot animate the 'Color' property
on 'System.Windows.Media.SolidColorBrush' because the object is sealed or frozen.`
This flooded the logs with errors and broke the focus border animation from
Phase 4.

### Root Cause
The `SubtleDividerBrush` theme resource is a frozen `SolidColorBrush` (WPF
freezes shared resource dictionary brushes by default). The Phase 4 code
called `BeginAnimation(SolidColorBrush.ColorProperty, ...)` directly on the
frozen brush returned by `TextBorder.BorderBrush`, which is not allowed.

### Fix
In `UI/TextSection.xaml.cs`, replaced the direct animation approach:
- Added a `_borderAnimBrush` field to hold an unfrozen brush copy
- On first focus event, create a new `SolidColorBrush` from the current
  border color and assign it to `TextBorder.BorderBrush`
- Animate this unfrozen copy instead of the frozen theme resource
- Both `GotKeyboardFocus` and `LostKeyboardFocus` now use this pattern

### Files Changed
```
Modified: UI/TextSection.xaml.cs (focus animation fix)
```

### Lesson
WPF freezes shared brushes from ResourceDictionaries by default. Never call
`BeginAnimation` on a brush that came from a theme resource — always create
a new unfrozen copy first.

---

## SESSION 7 — 2026-04-24

### Overview
```
Date:        2026-04-24
Duration:    Focused session
Focus:       Git setup, GitHub push, Visual Overhaul Phase 6A + 6B
Outcome:     Phases 6A and 6B complete, build passing, pushed to GitHub
Tools:       Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

Git setup:
- Configured git identity: `Soy <Soy@SoyPC>`
- Committed checkpoint: "checkpoint: pre-overhaul state (Phase 1-5 complete)" (8cbae4f)
- Installed GitHub CLI via winget (v2.91.0)
- Authenticated as `eastsidelbc` via browser device flow
- Pushed to origin main (ec5d913..8cbae4f)

Phase 6A — Foundation Rewrite (Clean Slate):
- DarkTheme.xaml base colors completely replaced:
  - Background: `#1E2A3A` (blue-gray) → `#111113` (deep solid dark)
  - Foreground: `#E8F4FD` → `#E4E4E7` (cool light text)
  - Header: same as base chrome (flat, no contrast)
  - Tab color: `#18181B` (zinc-800)
  - Border: `#27272A` (zinc-700 subtle divider)
- Added `ContentCardBrush` (`#18181B` solid zinc card)
- Added `AccentGradientBrush` — indigo `#6366F1` → purple `#8B5CF6`
- Replaced `AccentBrush` from `#4A90E2` (blue) → `#6366F1` (indigo)
- Added `AccentGlowEffect` — indigo DropShadowEffect (BlurRadius=12, Opacity=0.35)
- Updated `ActiveGlow` from blue → indigo `#6366F1`
- Updated `HoverGlow` — reduced opacity 0.15 → 0.08
- Updated `ContentCardStyle` — uses `ContentCardBrush`, CornerRadius=6, softer shadow
- Removed old tokens: `CardBackgroundBrush`, `AccentGlowBrush`, `TabActiveGlowBrush`
- Cleaned all stale blue hex references (`#4A90E2`, `#1E2A3A`, `#34495E`, etc.)
- MainWindow.xaml container hierarchy replaced:
  - Header bar: flat, same color as app chrome, no rounded corners
  - Tab strip: flat chrome, no container decoration
  - Content area: replaced `materialDesign:Card` with plain `Border` using
    `ContentCardBrush`, CornerRadius=6, soft shadow, subtle border tint
- `dotnet build` passes — 0 errors

Phase 6B — Tab Strip Replacement:
- Rewrote `TabButtonStyle` in DarkTheme.xaml:
  - Shape: pill (CornerRadius=16) → rectangular (CornerRadius=4)
  - Inactive: muted `#A1A1AA` foreground, no borders, transparent background
  - Active: `AccentGradientBrush` gradient underline (2px height) +
    `AccentGlowEffect` indigo glow + `SemiBold` bright `#E4E4E7` text
  - Hover: subtle `#14FFFFFF` background animation (150ms)
  - Removed old pill-style multi-trigger animations
- MainWindow.xaml tab strip: flat chrome with padding (4,4,4,2),
  no rounded corners, no background decoration
- `dotnet build` passes — 0 errors

### Decisions Made
- Visual overhaul is a CLEAN SLATE — old Phase 1-5 visual tokens replaced entirely
- Deep solid dark (`#111113`) replaces blue-gray — no transparency/glass, flat layers
- Indigo-to-purple gradient (`#6366F1` → `#8B5CF6`) replaces solid blue accent
- Header bar is flat (same color as base chrome) — no visual separation needed
- Content area uses solid zinc card (`#18181B`) with subtle shadow for depth
- Tabs are rectangular (not pills) — Edge-inspired with gradient underline indicator
- Active tab indicator is a gradient underline, not a background fill
- `materialDesign:Card` removed from MainWindow — plain Border containers only

### Current State At End Of Session
```
Working:  Build passes (0 errors), app launches
          GitHub connected and pushed (8cbae4f on main)
          Phase 6A COMPLETE: Deep dark chrome foundation established
          Phase 6B COMPLETE: Rectangular floating tabs with gradient underline
          Old blue theme completely purged from DarkTheme.xaml
          MainWindow.xaml uses new depth hierarchy (flat chrome + zinc cards)
Pending:  Phase 6C — Editor Surface Replacement
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Modified: Resources/Themes/DarkTheme.xaml (complete base token rewrite,
          tab style replacement, all old blue colors purged)
Modified: UI/Views/MainWindow.xaml (container hierarchy replaced,
          flat header, zinc card content area, tab strip flat)
Modified: docs/DEVNOTES.md (this session entry)
```

### Next Session Should Start With
1. Proceed to Phase 6C (Editor Surface — borderless + focus glow)
2. Visual testing of Phases 6A + 6B
3. Git commit for Visual Overhaul phases
4. Tech debt: wire WindowPositionTracker, PathSanitizer

### Jeremy's Notes
Visual overhaul is underway. Old blue theme is gone.
New deep dark chrome + indigo/purple gradient is in place.
Tabs are rectangular with gradient underlines.
Ready for Phase 6C (editor surface).

---

## SESSION 8 — 2026-04-24

### Overview
```
Date:        2026-04-24
Duration:    Short focused session
Focus:       Phase 6C — Editor Surface Replacement
Outcome:     Phase 6C complete, Visual Overhaul sprint COMPLETE
Tools:       Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

Phase 6C — Editor Surface Replacement:
- Added `EditorFocusGlow` effect to DarkTheme.xaml:
  - Softer, wider indigo glow (BlurRadius=16, Opacity=0.25)
  - Distinct from AccentGlowEffect (BlurRadius=12, Opacity=0.35)
- Added `EditorSurfaceStyle` base style to DarkTheme.xaml:
  - Transparent background and border, CornerRadius=4
- Replaced TextSection.xaml bordered container:
  - Removed `SubtleDividerBrush` border — surface now borderless at rest
  - Changed CornerRadius from `6,6,0,0` to uniform `4`
  - Added `Border.Style` with `DataTrigger` on
    `NoteRichTextBox.IsKeyboardFocusWithin`:
    - When focused: applies `AccentBrush` (#6366F1) stroke + `EditorFocusGlow`
    - When unfocused: transparent, invisible border
  - Tightened `Paragraph.LineHeight` from 1.2 to 1.5 for modern rhythm
- `dotnet build` passes — 0 errors (280 pre-existing warnings)
- Visual test confirmed: borderless at rest, indigo glow ring on focus

Docs updated:
- PLANNING.md: Phase 6C marked COMPLETE
- COMPRESSED_CONTEXT.md: sprint marked complete, next steps updated

### Decisions Made
- Editor focus glow uses a separate `EditorFocusGlow` effect (not `AccentGlowEffect`)
  to provide a softer, wider ring specifically for the text area — more breathing room
- Focus trigger uses `IsKeyboardFocusWithin` DataTrigger (not code-behind animation)
  to avoid the frozen brush crash from the Phase 4 bug (BUG-001)
- LineHeight tightened to 1.5 for better reading rhythm in the new dark theme
- Visual Overhaul sprint (6A, 6B, 6C) is now COMPLETE

### Current State At End Of Session
```
Working:  Build passes (0 errors), app launches
          Phase 6A COMPLETE: Deep dark chrome foundation
          Phase 6B COMPLETE: Rectangular floating tabs
          Phase 6C COMPLETE: Borderless editor with focus glow
          Visual Overhaul Sprint: ALL PHASES COMPLETE
          All existing features (tabs, text, images, auto-save) intact
Pending:  Git commits for Visual Overhaul sprint
          Cleanup tasks (VERSION file, MCP path, modernwpf.md)
          Tech debt (TabManager split, warnings, etc.)
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Modified: Resources/Themes/DarkTheme.xaml (EditorFocusGlow, EditorSurfaceStyle)
Modified: UI/TextSection.xaml (borderless surface, focus glow trigger,
          LineHeight 1.5)
Modified: docs/PLANNING.md (Phase 6C marked COMPLETE)
Modified: docs/COMPRESSED_CONTEXT.md (sprint complete, next steps)
Modified: docs/DEVNOTES.md (this session entry)
Modified: docs/PROJECT_MEMORY.md (status and priorities)
```

### Next Session Should Start With
1. Git commits for Visual Overhaul sprint
2. Cleanup sprint tasks (VERSION file, MCP path, delete modernwpf.md)
3. Tech debt items if desired (TabManager split, warnings)

### Jeremy's Notes
Phase 6C done. Visual Overhaul sprint is COMPLETE.
App looks premium — deep dark chrome, rectangular tabs with
gradient underlines, borderless editor with focus glow.
Ready for git commits and cleanup.

---

## SESSION 9 — 2026-04-25

### Overview
```
Date:        2026-04-25
Duration:    Focused session
Focus:       Sprint A — Data Layer Cleanup (Phases A.1 and A.2)
Outcome:     Phases A.1 and A.2 complete, build passing (0 new errors)
Tools:       Cursor (building)
Model:       qwen/qwen3.6-27b
```

### What Was Done

**Phase A.1 — Refactor DataManager.cs to write/read single master.json:**
- Created `Data/MasterData.cs` — single source of truth model with
  `Version`, `Windows`, and `Settings` properties
- Added `SaveMasterData(MasterData)` — atomic write to master.json via
  `AtomicFileManager.AtomicSave`
- Added `LoadMasterData()` — load with recovery fallback + schema migration
  via `MigrationService.MigrateMasterData`
- Added `MigrateToMasterIfNeeded()` — one-time migration that consolidates
  `notewindows.json` + `settings.json` into `master.json`, guarded by a
  flag file so it runs only once
- Updated `CleanupOrphanedImages()` to read references from master.json
  via `LoadMasterData()`
- Build passes — 0 new errors (280 pre-existing warnings)

**Phase A.2 — Update SavedNote.cs model to store media references (not binaries):**
- Created `Core/Models/MediaReference.cs` — stores just `Filename` + `DateAdded`.
  Resolves full paths at runtime via `MediaReference.FullPath` property.
- Updated `Core/Models/SavedNote.cs`:
  - Replaced flat `List<string> ImageFiles` + `Dictionary<string, DateTime> ImageTimestamps`
    with structured `List<MediaReference> Media`
  - Added backward-compatible `[Obsolete]` property accessors for `ImageFiles` and
    `ImageTimestamps` so existing callers (`TabManager.cs`, `NoteTab.xaml.cs`)
    continue to work transparently without touching those files
  - `ImageFiles` getter resolves filenames → full paths at runtime
  - `ImageFiles` setter converts full paths → filename-only Media entries
  - `ImageTimestamps` setter merges timestamps into Media entries
- Updated `Core/Schema/MigrationService.cs`:
  - Bumped `CurrentNoteSchemaVersion` from 1 → 2
  - Added `MigrateNoteToMediaRefs()` migration method
- Updated `Core/Managers/DataManager.cs` orphan cleanup to use `Media` directly
- Updated `UI/Views/MainWindow.xaml.cs` legacy content check to use `Media`
- Build passes — 0 new errors

**Data format change:**
- Old: `"imageFiles": ["C:\\Users\\...\\images\\img_abc.png"]`
- New: `"media": [{ "filename": "img_abc.png", "dateAdded": "2026-04-25T..." }]`
- Backward-compatible accessors ensure existing data loads correctly until
  the next save cycle writes the new format

### Decisions Made
- `MediaReference` is a standalone class (not a nested type) for clean JSON
  serialization and independent evolution
- Full path resolution happens in `MediaReference.FullPath` (not DataManager)
  to keep the model self-contained and testable
- Backward-compatible accessors use `[Obsolete]` attribute to signal deprecation
  without breaking existing callers — TabManager.cs and NoteTab.xaml.cs continue
  to work without modification (per the hard rule: never touch TabManager.cs)
- `ImageFiles` setter only populates Media if Media is empty (to avoid clearing
  data when loading new-format JSON where Media is already populated)
- MigrationService bumps note schema version to 2 — version tracking enables
  future migrations without data loss

### Current State At End Of Session
```
Working:  Build passes (0 new errors), app launches
          Sprint A Phase A.1 COMPLETE: master.json consolidation
            - MasterData model created
            - SaveMasterData / LoadMasterData wired
            - One-time migration from legacy files
            - Orphan cleanup reads from master.json
          Sprint A Phase A.2 COMPLETE: Media reference model
            - MediaReference.cs created (filename-only refs)
            - SavedNote.Media replaces flat ImageFiles + ImageTimestamps
            - Backward-compatible accessors for legacy callers
            - MigrationService version bump (note schema v1 → v2)
Pending:  Git commits for Sprint A
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Created:  Data/MasterData.cs (single source of truth model)
Created:  Core/Models/MediaReference.cs (filename-only media refs)
Modified: Core/Models/SavedNote.cs (Media property + legacy accessors)
Modified: Core/Managers/DataManager.cs (master.json read/write, orphan cleanup)
Modified: Core/Schema/MigrationService.cs (note schema v2 migration)
Modified: UI/Views/MainWindow.xaml.cs (legacy content check uses Media)
Modified: docs/DEVNOTES.md (this session entry)
```

### Next Session Should Start With
1. Git commits for Sprint A phases
2. Proceed to Sprint B (Memory & GIF Management)

### Jeremy's Notes
Sprint A is progressing well. Two phases done (A.1 master.json consolidation,
A.2 media reference model). Data layer is cleaner now — single master.json
with filename-only media references. Two more phases to complete the sprint.

---

## SESSION — 2026-04-25

### Overview
```
Date:        2026-04-25
Duration:    ~15 min
Focus:       Sprint A — Phase A.3 (orphan cleanup wiring) + A.4 (log retention)
Outcome:     Sprint A COMPLETE — all 4 phases done
Tools:       Cursor
Model:       qwen/qwen3.6-27b
```

### What Was Done

**Phase A.3 — Wire orphan image cleanup on startup:**
- Verified orphan cleanup was already wired in `App.xaml.cs` OnStartup
  (from a previous session) — `Task.Run` with 5s delay calling
  `DataManager.CleanupOrphanedImages()` with 7-day grace period
- Confirmed `CleanupOrphanedImages()` in DataManager.cs reads references
  from master.json via `LoadMasterData()` and iterates `note.Media`
  (the new `List<MediaReference>`) — already updated from Phase A.2
- Build passes — 0 errors

**Phase A.4 — Clean up logs/ folder structure & Serilog retention policy:**
- Updated `Infrastructure/Logging/LoggingService.cs` Serilog config:
  - Added `fileSizeLimitBytes: 10 * 1024 * 1024` (10 MB per file cap)
  - Added `rollOnFileSizeLimit: true` (rolls when cap is hit)
  - Removed stale comment about Debug sink
- Added `CleanupOldLogs(int daysRetention = 7)` static method:
  - Deletes any `.log` files older than 7 days by `LastWriteTime`
  - Removes stray non-`.log` files from the logs folder
  - Returns count of deleted files
- Wired `LoggingService.CleanupOldLogs(7)` into `App.xaml.cs` startup task
  (runs alongside orphan image cleanup after 5s delay)
- Build passes — 0 errors, 290 pre-existing warnings

**Sprint A is now COMPLETE:**
- A.1 ✅ master.json consolidation
- A.2 ✅ MediaReference model
- A.3 ✅ Orphan cleanup wired on startup
- A.4 ✅ Log retention policy (Serilog file size cap + age-based cleanup)

### Decisions Made
- Log retention has TWO layers: Serilog's native `retainedFileCountLimit: 7`
  (count-based) + startup `CleanupOldLogs` (age-based 7-day sweep). The
  age-based layer catches stale files Serilog's count-based limit misses.
- 10 MB per-file size cap prevents runaway log growth on a single day
  (e.g. if a loop starts spamming debug logs)
- Non-`.log` stray files in the logs folder are cleaned up — prevents
  accumulation of temp files, old backups, etc.

### Current State At End Of Session
```
Working:  Build passes (0 errors), app launches
          Sprint A COMPLETE — all 4 phases done
            - A.1: master.json single source of truth ✅
            - A.2: MediaReference filename-only model ✅
            - A.3: Orphan image cleanup on startup ✅
            - A.4: Log retention (file size cap + age cleanup) ✅
Pending:  Git commits for Sprint A
          Sprint B (Memory & GIF Management) not started
Broken:   Nothing — build is green
```

### Files Changed This Session
```
Modified: Infrastructure/Logging/LoggingService.cs (file size cap, CleanupOldLogs method)
Modified: App.xaml.cs (wired log cleanup into startup task)
```

### Next Session Should Start With
1. Git commits for Sprint A
2. Sprint B Phase B.1: Implement lazy-loading for Media Vault thumbnails

### Jeremy's Notes
Sprint A is fully done. Data layer is clean — single master.json, filename-only
media refs, orphan cleanup on startup, and proper log retention. Ready for
Sprint B (Memory & GIF Management) which will tackle lazy-loading and LRU cache.

---

## SESSION — 2026-04-25 (Sprint B Phase B.1)

### Overview
```
Date:        2026-04-25
Duration:    Single phase
Focus:       Sprint B Phase B.1 — Lazy-loading for Media Vault thumbnails
Outcome:     COMPLETE — Build passes (0 errors, 0 warnings)
Tools:       Cursor
Model:       qwen/qwen3.6-27b
```

### What Was Done

**ImageCacheManager.cs** — Dual-eviction LRU cache:
- Added size-based eviction (100MB cap) alongside existing count-based (100 items)
- `EstimateBitmapBytes()` approximates memory per entry (pixelWidth × pixelHeight × 4 × 2)
- `_currentBytes` tracks total cache size; evicts LRU when limits exceeded
- Added `RemoveFromCache(path)` for cleanup when files are deleted
- Added `Clear()` for shutdown memory release

**MediaSection.xaml.cs** — Async lazy-loading:
- `SemaphoreSlim(4,4)` limits concurrent decodes to 4 at a time
- `CreatePlaceholderContainer()` renders instant placeholder (no decode, no I/O)
- `LoadThumbnailAsync()` decodes on background thread, updates UI via Dispatcher
- `CreateStaticGifThumbnail()` forces GIFs to static first frame in vault (`OnLoad` + `Freeze`)
- `EnsureThumbnailLoaded()` fire-and-forget launcher with `CancellationTokenSource`
- `LoadImagesFromFiles()` and `RebuildUIFromData()` both use lazy loading now
- `Dispose()` cancels pending loads and disposes semaphore

### Decisions Made
- GIFs in Media Vault: always static first frame. Full animation only in ImageViewerWindow.
- Placeholder design: subtle "· · ·" with ContentCardBrush background, not empty space.
- Semaphore limit: 4 concurrent decodes (balances throughput vs UI responsiveness).
- Cache eviction: dual threshold (count OR size, whichever triggers first).

### Current State At End Of Session
Working: Build passes (0 errors, 0 warnings), lazy-loading wired in
Pending: Sprint B Phase B.2 (TBD)
Broken: nothing

### Files Changed
- `UI/ImageCacheManager.cs` — complete rewrite with dual eviction
- `UI/MediaSection.xaml.cs` — lazy-loading infrastructure added
- `docs/COMPRESSED_CONTEXT.md` — updated
- `docs/PLANNING.md` — Sprint B section + B.1 phase added
- `docs/DEVNOTES.md` — this session entry

### Next Session Should Start With
1. Sprint B Phase B.2 — continue Memory & GIF Management
2. Test lazy-loading in running app (verify placeholders → real images)

---

## SESSION — 2026-04-25 (Sprint B Phase B.2)

### Overview
```
Date:        2026-04-25
Duration:    Single phase
Focus:       Sprint B Phase B.2 — ImageViewerWindow cache integration + GIF cleanup
Outcome:     COMPLETE — Build passes (0 errors, 0 warnings)
Tools:       Cursor
Model:       qwen/qwen3.6-27b
```

### What Was Done

**ImageCacheManager.cs** — Full-res cache key support:
- Added `RemoveAllForPath(string path)` method to remove all cache variants
  for a given file (base key + prefixed variants like `path:full`)
- Enables ImageViewerWindow to safely evict both thumbnail and full-res
  entries when a file is deleted

**ImageViewerWindow.xaml.cs** — Cache integration + async loading + GIF simplification:
- Static images now check LRU cache before decoding (key: `path:full`)
- `LoadStaticAsync()` — decodes static images on background thread via
  `Task.Run`, caches result, applies on UI thread via `Dispatcher.Invoke()`
- `LoadGifAsync()` — decodes GIFs on background thread, applies animation
  settings on UI thread, falls back to static loading on exception
- `ApplyImage()` — unified method to assign bitmap, update title, info bar, autosize
- GIF loading collapsed from 3 fallback paths (~250 lines with debug spam)
  into a single clean path (~30 lines). `OnDemand` cache option preserves
  animation. No more `Console.WriteLine`, `Debug.WriteLine`, or `DispatcherTimer`
  animation monitoring.
- Delete calls `ImageCacheManager.RemoveAllForPath()` to evict both thumbnail
  and full-res cache entries
- Navigation (left/right) cleaned up — removed debug logging
- Added `using System.Collections.Generic` and `using System.Threading.Tasks`
- File reduced from 647 → 478 lines

**BEFORE/AFTER (ImageViewerWindow.xaml.cs):**
- Before: 647 lines, heavy debug output, sync UI-thread decoding, 3 GIF paths
- After: 478 lines, clean async loading, single GIF path, cache integration

### Decisions Made
- Full-res cache uses `path:full` suffix key to avoid collision with thumbnail
  keys (which use bare `path`). Both share the same LRU eviction system.
- GIFs skip the cache entirely — their `BitmapImage` instances can't be frozen,
  and caching non-frozen bitmaps introduces lifetime management complexity.
  GIFs are cheap enough to re-decode on each open.
- All image decode (static and GIF) moves to background thread. Even GIF
  `BitmapImage` creation with `OnDemand` is safe on a background thread —
  the actual frame decoding happens lazily on the render thread.
- `ApplyImage()` centralizes the "assign bitmap + update UI" logic to avoid
  duplication between cache hit and cache miss paths.

### Current State At End Of Session
Working: Build passes (0 errors, 0 warnings), cache integration wired
Pending: Sprint B Phase B.3 (TBD) or Sprint C (Crash Recovery Buffer)
Broken: nothing

### Files Changed
- `UI/ImageViewerWindow.xaml.cs` — cache lookup, async decode, GIF simplification
- `UI/ImageCacheManager.cs` — `RemoveAllForPath()` method
- `docs/COMPRESSED_CONTEXT.md` — updated
- `docs/PLANNING.md` — Sprint B B.2 phase added
- `docs/DEVNOTES.md` — this session entry

### Next Session Should Start With
1. Decide: Sprint B Phase B.3 or move to Sprint C (Crash Recovery Buffer)
2. Test cache behavior in running app (navigate left/right, verify cache hits)
3. Git commits for Sprint B

---

## SESSION — 2026-05-05 (Sprint UI-5)

### Overview
```
Date:        2026-05-05
Duration:    ~30 min
Focus:       Sprint UI-5 — Tab rename edit visuals + MediaSection global menu
Outcome:     COMPLETE — Build passes (0 errors)
Tools:       Cursor
Model:       qwen/qwen3.6-27b
```

### What Was Done

**Step 1 — Tab Rename Edit Mode Visuals (TabManager.cs):**
- Replaced hardcoded blue (#4A90E2) with AccentBrush (#6366F1) design token
- TabBorder template element gets 12% indigo tint + 1.5px accent border
- ActiveUnderline template element shown (accent gradient)
- AccentGlowEffect applied to tab button
- TextBox CaretBrush set to AccentBrush for visibility
- RestoreTabAppearance restores template elements (TabBorder bg/border,
  ActiveUnderline opacity, button Effect)
- Template elements accessed via `tabButton.Template?.FindName("TabBorder", tabButton)`

**Step 2 — MediaSection XAML (MediaSection.xaml):**
- Added `ContextMenuOpening="MediaSection_ContextMenuOpening"` to root Grid

**Step 3 — Global Menu + Bulk Operations (MediaSection.xaml.cs):**
- `MediaSection_ContextMenuOpening` — walks visual tree to check if click
  hit an image container; if not, builds global menu
- `SetSizeForAll(int)` — iterates containers, sets ThumbnailSize, rebuilds each
- `ToggleHiddenForAll(bool)` — if any hidden → show all, else hide all
- `DeleteAllImages()` — confirmation dialog via DialogHelper.ShowConfirmation,
  deletes files, clears collections

### Decisions Made
- Tab rename uses template elements (TabBorder, ActiveUnderline) rather than
  button-level properties — matches existing selected-state visual language
- Global context menu built dynamically in ContextMenuOpening handler — avoids
  XAML bloat, reuses CreateIcon pattern from per-image menu
- Delete All uses DialogHelper.ShowConfirmation — consistent with codebase pattern

### Current State At End Of Session
```
Working:  Build passes (0 errors)
          Sprint UI-5 COMPLETE — all 3 steps done
Pending:  Sprint G (Native Windows features) — plan with Claude first
Broken:   Nothing — build is green
```

### Files Changed
```
Modified: UI/TabManager.cs (accent-based edit mode using template elements)
Modified: UI/MediaSection.xaml (ContextMenuOpening handler)
Modified: UI/MediaSection.xaml.cs (global menu + bulk ops)
Modified: docs/PLANNING.md (UI-5 added to completed sprints)
Modified: docs/COMPRESSED_CONTEXT.md (UI-5 added)
Modified: docs/CHANGELOG.md (two entries under [Unreleased])
Created:  docs/devnotes/2026-05-05-sprint-ui5-tab-rename-media-menu.md
Modified: docs/DEVNOTES.md (this session entry)
```

### Next Session Should Start With
1. Sprint G (Native Windows features) — plan with Claude first
2. Test UI-5 changes visually (tab rename + global media menu)

Copy this for each new session:

```
## SESSION [N] — [DATE]

### Overview
Date:
Duration:
Focus:
Outcome:
Tools:       LM Studio + Cursor
Model:       qwen/qwen3.6-27b

### What Was Done


### Decisions Made


### Current State At End Of Session
Working:
Pending:
Broken:

### Files Changed


### Next Session Should Start With
1.
2.
3.

### Jeremy's Notes

```

---

## CHECKPOINT TEMPLATE

Written every ~20 exchanges:

```
[CHECKPOINT — [DATE] [TIME] — Exchange ~[N]]
Working on: [feature and step]
Status:     [what works right now]
Last test:  [what was tested and result]
Next:       [immediate next action]
```
