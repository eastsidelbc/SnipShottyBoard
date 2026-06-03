---
Title: Sprint UI-1 — Layer Cleanup + Glow Polish (Phases UI-1.1, UI-1.2)
Date: 2026-05-01
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §Sprint UI-1
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

Remove redundant nested card layers creating a "card inside a card" effect.
Replace grey hover with purple ambient glow on hover (not just keyboard focus).

## Phase UI-1.1 — ContentCardHoverStyle + SectionHoverGlow

**File:** `Resources/Themes/DarkTheme.xaml`

### Changes
1. Added `SectionHoverGlow` — DropShadowEffect, purple (#6366F1), ShadowDepth=0, BlurRadius=12, Opacity=0.12
2. Replaced `ContentCardHoverStyle`:
   - `Background=Transparent` — removes nested #18181B card visible at rest
   - `Padding=0` — removes 8px inset that shrank thumbnails
   - Hover trigger uses `SectionHoverGlow` (purple) instead of `HoverGlow` (white)
   - Focus trigger uses `EditorFocusGlow` (purple, 0.25 opacity) — unchanged

### Rationale
ContentCardHoverStyle inherited ContentCardStyle which set Background=#18181B and Padding=8.
ContentArea also has Background=#18181B, creating a visible nested card.
Transparent background + zero padding makes sections blend seamlessly.
WPF last-trigger-wins: focus always beats hover automatically.

## Phase UI-1.2 — Remove Redundant TextBorder

**Files:** `UI/TextSection.xaml`, `UI/TextSection.xaml.cs`

### Changes
1. Removed `TextBorder` Border wrapper — Grid is now direct child of TextSection
2. Removed `NoteRichTextBox_GotKeyboardFocus` and `NoteRichTextBox_LostKeyboardFocus` handlers
3. Removed `_borderAnimBrush` field
4. Removed unused imports: `System.Windows.Input`, `System.Windows.Media.Animation`
5. Removed `GotKeyboardFocus`/`LostKeyboardFocus` event handlers from XAML RichTextBox

### Rationale
TextBorder used `EditorSurfaceStyle` which is just: Background=Transparent, BorderBrush=Transparent,
BorderThickness=1, CornerRadius=4. It did nothing — no effects, no background, no visible border.
The keyboard focus animation code was animating TextBorder's border color, but this is now handled
by ContentCardHoverStyle's IsKeyboardFocusWithin trigger on the outer border in NoteTab.xaml.

## Testing & Acceptance

- dotnet build: 0 errors ✅
- Sections invisible/seamless at rest ✅
- Hover either section → soft purple glow ✅
- Click text → brighter purple glow + accent border ✅
- Thumbnails fill more space, less padding ✅
- Placeholder shows/hides correctly ✅
- No regressions ✅

## Next

Sprint G — Native Windows Features (plan with Claude first)
