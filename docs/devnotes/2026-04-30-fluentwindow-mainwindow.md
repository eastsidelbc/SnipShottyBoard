---
Title: Sprint F Phase F.1 — Convert MainWindow to FluentWindow
Date: 2026-04-30
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §Sprint F Phase F.1
 - PR/SHAs: e955505
---

Context & Goal
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Convert MainWindow from a custom WindowStyle=None floating card
to WPF-UI FluentWindow with native Windows 11 caption buttons
(minimize, maximize, close with red hover), snap layouts, proper
alt-tab thumbnails, and full taskbar behavior.

This is the most complex window conversion — it has a custom title bar,
action buttons, tab strip, content area, and status bar. The conversion
requires merging the old toolbar buttons into the FluentWindow TitleBar
Header, removing the custom title bar chrome, and relying on native
Windows caption buttons.

Decisions & Alternatives
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. Use ui:FluentWindow (the only window control in WPF-UI 4.x)
   - Alternative: Keep custom WindowStyle=None — rejected, loses snap layouts and native behavior
2. Merge 3 action buttons ([+], [📂], [🗑️]) into TitleBar.Header
   - Alternative: Keep separate toolbar row — rejected, wastes vertical space
3. Keep pin button (📌) in TitleBar.TrailingContent
   - Alternative: Remove pin entirely — rejected, always-on-top is a core feature
4. Remove 4 buttons (📝, ⚙️, 🌙, ❓, 🔧) from title bar
   - These will be addressed via keyboard shortcuts or other access patterns in Sprint G/R
5. WindowBackdropType="None" to keep solid dark chrome
   - Alternative: Mica/Acrylic glass — rejected, doesn't match the deep dark aesthetic

Implementation Notes
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
XAML changes:
- Root element changed from <Window> to <ui:FluentWindow>
- Added ExtendsContentIntoTitleBar="True" (required for DarkTheme to style title bar area)
- Added WindowBackdropType="None" (solid dark, no frosted glass)
- Removed WindowStyle="None", AllowsTransparency="True", WindowChrome block
- Removed outer floating card Border (CornerRadius=12, DropShadowEffect)
- Added <ui:FluentWindow.TitleBar> with <ui:TitleBar>
  - TitleBar.Header contains 3 action buttons: [+], [📂], [🗑️]
  - TitleBar.TrailingContent contains pin button [📌]
- Removed old custom title bar Border (Row 0)
- Grid.RowDefinitions reduced from 5 rows to 4 rows:
  Row 0: TitleBar (Auto)
  Row 1: Tab Strip (Auto)
  Row 2: Main Content (*)
  Row 3: Status Bar (28)
- Moved fade-in animation from Border to Grid via Grid.Triggers

Code-behind changes:
- Base class changed from Window to FluentWindow
- Added using Wpf.Ui.Controls
- Removed Minimize_Click handler (native minimize now handles this)
- Removed Close_Click handler (native close now handles this)
- Kept Pin_Click handler (still wires to this.Topmost)
- Added OpenLogs_Click handler (opens %AppData%\SnipShottyBoard\logs in Explorer)
- Removed TitleBar_MouseDown drag code (FluentWindow handles natively)
- Fade-in storyboard moved to XAML Grid.Triggers

Resource dependencies (from F.0):
- TitleBarButtonStyle: transparent background, AppForegroundBrush foreground,
  hover = HoverTransparentBrush, 32x32 size
- TitleBarPinButtonStyle: same as above but toggled state uses AccentBrush
  when Topmost=True

Testing & Acceptance
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
All 16 test criteria passed:
1. dotnet build passes 0 errors ✅
2. App launches and title bar looks correct ✅
3. [+] creates new tab ✅
4. [📂] opens logs folder in File Explorer ✅
5. [🗑️] deletes current tab ✅
6. [📌] pin toggles always-on-top, visual state changes ✅
7. Native [─] minimize works ✅
8. Native [□] maximize works ✅
9. Double-click title bar maximizes/restores ✅
10. Native [✕] closes app ✅
11. Red X on hover (Windows 11) ✅
12. Drag window by title bar area works ✅
13. Snap to screen corner shows snap layout grid ✅
14. Alt+Tab shows live thumbnail ✅
15. Tab strip, content area, status bar look correct ✅
16. Right-click title bar shows system menu ✅

Performance & Limits
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- FluentWindow adds minimal overhead — it's a subclass of Window
- ExtendsContentIntoTitleBar="True" is the key attribute that enables
  custom styling of the title bar area
- WindowCornerPreference="Round" uses system DWM rounding (~8px)
- Drop shadow is lost (was carried by the outer Border) — this is
  the price of native behavior

Follow-ups
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Phase F.2: Convert SettingsWindow (next)
- Phase F.3: Convert ImageViewerWindow (after F.2)
- Phase F.4: Convert NoteListWindow
- Phase F.5: Native tab right-click context menus
- Phase F.6: Cleanup + backlog notes
- Removed buttons (📝, ⚙️, 🌙, ❓, 🔧) need alternative access patterns
  — will be addressed in Sprint G (native features) or Sprint R (refactor)

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
