---
Title: Sprint UI-2 — Title bar icon swap + alignment
Date: 2026-05-01
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §Sprint UI-2
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

Replace emoji content in title bar buttons with MaterialDesign PackIcons for
consistent sizing and visual weight. Define missing TitleBarButtonStyle and
TitleBarPinButtonStyle with fixed dimensions and proper alignment.

## Decisions & Alternatives

- **PackIcon over FontIcon:** PackIcon renders as vector paths from the MaterialDesign
  icon font, ensuring identical pixel footprint (16x16) across all icons. Emojis are
  colored bitmap glyphs that render at different sizes even at the same FontSize.
- **Fixed button dimensions (32x28):** Every title bar button is now the same box size,
  eliminating inconsistent sizing. VerticalAlignment=Center forces centering.
- **Pin button uses background accent:** When Tag="Pinned", the button shows
  AccentBrush background + White foreground instead of just changing foreground color.
  This provides a more visible pinned state indicator.
- **StackPanel margin changed from "4,2,0,0" to "6,0,0,0":** Removed the 2px downward
  nudge that was breaking vertical centering. 6px left margin keeps buttons closer to
  the window edge corner.

## Implementation Notes

### Files Changed
- `Resources/Themes/DarkTheme.xaml` — Updated TitleBarButtonStyle and TitleBarPinButtonStyle
- `Resources/Themes/LightTheme.xaml` — Added TitleBarButtonStyle and TitleBarPinButtonStyle (were missing)
- `UI/Views/MainWindow.xaml` — Replaced emoji Content with PackIcon elements

### Icon Mapping
| Button | Emoji | PackIcon Kind |
|--------|-------|---------------|
| New Note | + | PlusBoxOutline |
| Note Windows | 📝 | WindowMaximize |
| Delete Tab | 🗑️ | TrashCanOutline |
| Settings | ⚙️ | CogOutline |
| Toggle Theme | 🌙 | ThemeLightDark |
| Help | ? | HelpCircleOutline |
| Developer | 🔧 | WrenchOutline |
| Pin | 📌 | Pushpin |

### Style Properties
- Width=32, Height=28 (fixed dimensions)
- Padding=0 (removed inconsistent padding)
- Margin=2,0 (horizontal spacing between buttons)
- HorizontalAlignment=Center, VerticalAlignment=Center
- HorizontalContentAlignment=Center, VerticalContentAlignment=Center
- CornerRadius=4
- Hover: HoverTransparentBrush, Pressed: ActiveTransparentBrush
- Pin active: AccentBrush background + White foreground

## Testing & Acceptance

- [x] dotnet build — 0 errors
- [ ] All 8 title bar buttons render at same height/width
- [ ] Icons are 16x16, visually consistent weight
- [ ] Vertical alignment centered within title bar
- [ ] Pin button shows indigo background + white icon when Tag="Pinned"
- [ ] Hover highlights work on all buttons
- [ ] Click functionality still works (New tab, Settings, theme toggle, etc.)
- [ ] Light mode: icons remain visible (not white-on-white)

## Performance & Limits

No performance impact — PackIcons are lightweight vector rendering from the already-loaded
MaterialDesignInXAML icon font (5.3.1).

## Follow-ups

- System window buttons (min/max/close) may still be slightly off-center — acceptable
  limitation of FluentWindow library. Fixing requires custom-drawn window chrome.
- UI-2.4 smoke test pending manual verification by Jeremy.
