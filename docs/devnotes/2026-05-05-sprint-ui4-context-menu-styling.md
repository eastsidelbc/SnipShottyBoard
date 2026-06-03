---
Title: Sprint UI-4 — Context Menu Styling + Toggle Checkboxes
Date: 2026-05-05
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §Sprint UI-4
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

Right-click context menus on media thumbnails showed the default Windows grey
menu, clashing with the deep dark chrome. Toggle items ("Show Label"/"Hide Date")
used dynamic text instead of professional static labels with checkmarks.

**Goal:** Dark themed context menus matching tab menus. Static toggle labels with
indigo checkmarks (VS Code / Photoshop style).

## Decisions & Alternatives

- **Used existing `NativeContextMenuStyle`** (already in DarkTheme.xaml from Sprint F)
  rather than creating a new `ContextMenuStyle`. Avoids duplicate styles.
- **Added checkmark to existing implicit `MenuItem` template** rather than creating
  a separate `ContextMenuItemStyle`. The implicit template already handles icons,
  submenu arrows, hover states, and disabled states. Adding a checkmark Path +
  IsChecked trigger was a minimal 2-line change.
- **Replaced 3 individual toggle methods** with a single generic `ToggleMediaBool`
  helper + `UpdateContainerVisibility`. Reduces code from ~30 lines to ~25 lines
  and eliminates duplication.

## Implementation Notes

### UI-4.1 — DarkTheme.xaml

Added checkmark Path to the MenuItem template (Grid.Column="0", same as icon):

```xml
<Path x:Name="CheckMark"
      Grid.Column="0"
      Width="16" Height="16"
      Margin="0,0,8,0"
      Data="M0,4 L3,8 L9,1"
      Stroke="#6366F1"
      StrokeThickness="2"
      Visibility="Collapsed" />
```

Added trigger:
```xml
<Trigger Property="IsChecked" Value="True">
    <Setter TargetName="CheckMark" Property="Visibility" Value="Visible" />
</Trigger>
```

Checkmark uses `#6366F1` (AccentBrush indigo) for consistency with app aesthetic.
Hidden by default (Collapsed), shown only when IsChecked=True.

### UI-4.2 — MediaSection.xaml.cs

Changed toggle items from dynamic text to static labels + IsCheckable:

**Before:**
```csharp
var labelItem = new MenuItem { Header = mediaRef.ShowLabel ? "Hide Label" : "Show Label" };
labelItem.Click += (s, e) => ToggleLabelVisibility(container, mediaRef);
```

**After:**
```csharp
var labelItem = new MenuItem
{
    Header = "Label",
    IsCheckable = true,
    IsChecked = mediaRef.ShowLabel
};
labelItem.Click += (s, e) => ToggleMediaBool((MenuItem)s!, container, mediaRef,
    m => m.ShowLabel = (bool)((MenuItem)s!).IsChecked!);
```

Added generic helper:
```csharp
private void ToggleMediaBool(MenuItem menuItem, Grid container, MediaReference mediaRef,
    Action<MediaReference> setProperty)
{
    setProperty(mediaRef);
    UpdateContainerVisibility(container, mediaRef);
    OnMediaChanged?.Invoke();
}
```

WPF auto-toggles IsChecked before firing Click, so the lambda reads the NEW value
from the MenuItem and syncs it to the MediaReference property.

### UI-4.3 — MediaSection.xaml.cs

Applied NativeContextMenuStyle to context menus created in code:

```csharp
var contextMenu = new ContextMenu();
if (Application.Current.Resources.Contains("NativeContextMenuStyle"))
    contextMenu.Style = (Style)Application.Current.Resources["NativeContextMenuStyle"];
```

Null-check via `Contains()` prevents crashes if theme hasn't loaded yet.

## Testing & Acceptance

- `dotnet build` — 0 errors after each phase (3 builds total)
- Pre-existing warnings (CS8618, CS8604) in unrelated files — not touched
- Manual testing needed:
  - Right-click thumbnail → dark background, rounded corners
  - "Label"/"Date"/"Time" show static text with indigo checkmarks when ON
  - Clicking toggles checkmark and updates UI instantly
  - State persists through save/load

## Performance & Limits

- No performance impact — style lookup happens once per context menu creation
- Checkmark Path is lightweight vector drawing (no image loading)
- Generic toggle handler reduces code size by ~5 lines

## Follow-ups

- LightTheme.xaml mirror style not included (acceptable — light mode hidden from users)
- Custom MenuItem template doesn't show PackIcon icons on toggle items (toggle items
  don't need icons — they have checkmarks)
