# Tabs — Multi-row wrapping & Edge-like styling

**Date:** 2025-10-01 (America/Chicago)  
**Owner:** SnipShottyBoard Development Team  
**Versions Affected:** 1.3.0–1.4.0  

**Links:**
- **CR Section:** [docs/CR.md § Tabs Pattern](../CR.md#15-tab-drag-and-drop-system)
- **PR/SHAs:** (to be filled)

---

> **Normative rules live in CR.md § Tabs Pattern. This note records implementation details and rationale.**

---

## Context & Goal

### Original Design Problem
- **Original**: Horizontal `ScrollViewer` with `StackPanel` → tabs scrolled horizontally when overflow occurred
- **Issue**: Hidden tabs were not visible, requiring scrolling to access
- **User Request**: Make all tabs visible simultaneously, eliminate horizontal scrolling

### Solution: Responsive Row Wrapping
- **New**: Vertical `ScrollViewer` with `WrapPanel` → tabs wrap to new rows automatically
- **Benefits**:
  - All tabs visible without horizontal scrolling
  - Automatic adaptation to window width
  - Natural Edge-like UX
  - Better space utilization

---

## Decisions & Alternatives

### Why WrapPanel?
- **Considered:** Custom panel with explicit row management
- **Chosen:** `WrapPanel` - built-in, well-tested, automatic layout
- **Trade-off:** Less control over row breaks, but simpler and more maintainable

### Why Vertical Scrollbar?
- **Considered:** Fixed tab strip height (no scroll)
- **Chosen:** Vertical scroll with 200px max height
- **Rationale:** Handles edge case of 50+ tabs without consuming entire window

### Why Row Detection via Y-Position Grouping?
- **Considered:** Track WrapPanel's internal row state
- **Chosen:** Group tabs by rounded Y-position with 5px tolerance
- **Rationale:** WrapPanel doesn't expose row information; Y-grouping is reliable and simple

---

## Implementation Notes

### XAML Layout Changes (MainWindow.xaml)

**Before:**
```xml
<ScrollViewer HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Hidden">
    <StackPanel Orientation="Horizontal" x:Name="TabHeaderPanel" />
</ScrollViewer>
```

**After:**
```xml
<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" MaxHeight="200">
    <WrapPanel Orientation="Horizontal" x:Name="TabHeaderPanel" />
</ScrollViewer>
```

### Row Detection Algorithm

Tabs are grouped into rows by comparing their Y positions with a tolerance:

```csharp
// AppConstants.TabRowGroupingTolerance = 5 (pixels)

var tabPositions = new List<(int index, Button button, Point position, double rowY)>();

for (int i = 0; i < tabHeaderPanel.Children.Count; i++)
{
    var tabPosition = tabButton.TransformToAncestor(tabHeaderPanel).Transform(new Point(0, 0));
    // Round to nearest 5px increment
    double rowY = Math.Round(tabPosition.Y / 5) * 5;
    tabPositions.Add((i, tabButton, tabPosition, rowY));
}

// Group tabs by rounded Y position
var rows = tabPositions.GroupBy(t => t.rowY).OrderBy(g => g.Key).ToList();
```

**Why 5px Tolerance?**
- WrapPanel may not align tabs perfectly (sub-pixel rendering)
- Small variations in Y position (±2px) should be considered same row
- 5px provides enough tolerance while distinguishing actual rows

### Coordinate Transform Strategy

**Problem:** `tabHeaderPanel` and `dragCanvas` are in different branches of the visual tree.

**Solution:** Use `MainWindow` as common ancestor for coordinate transforms.

```csharp
// UpdateDropIndicator() in TabManager.cs

// 1. Get target tab position in MainWindow coordinates
var targetTabPosInWindow = targetTab.TransformToAncestor(Application.Current.MainWindow)
                                    .Transform(new Point(0, 0));

// 2. Get dragCanvas position in MainWindow coordinates
var canvasPosInWindow = dragCanvas.TransformToAncestor(Application.Current.MainWindow)
                                  .Transform(new Point(0, 0));

// 3. Calculate relative position (dragCanvas coordinates)
double indicatorX = targetTabPosInWindow.X - canvasPosInWindow.X - 2;
double indicatorY = targetTabPosInWindow.Y - canvasPosInWindow.Y + 1;

// 4. Position the drop indicator
Canvas.SetLeft(dropIndicator, indicatorX);
Canvas.SetTop(dropIndicator, indicatorY);
dropIndicator.Height = targetTab.ActualHeight - 2;
```

### Finding Drop Target Index (Row-Aware)

When dragging, the system must determine which tab position the mouse is over:

```csharp
// FindDropTargetIndex() in TabManager.cs

// 1. Group tabs by row
var rows = tabPositions.GroupBy(t => t.rowY).OrderBy(g => g.Key).ToList();

// 2. Find which row mouse is over (closest Y position)
int targetRowIndex = 0;
double minYDiff = double.MaxValue;
for (int r = 0; r < rows.Count; r++)
{
    double yDiff = Math.Abs(mouseY - rows[r].Key);
    if (yDiff < minYDiff)
    {
        minYDiff = yDiff;
        targetRowIndex = r;
    }
}

// 3. Within target row, find insertion point using midpoints + hysteresis
var targetRow = rows[targetRowIndex].OrderBy(t => t.position.X).ToList();
double hysteresisBuffer = AppConstants.TabDragHysteresisBuffer; // 5.0px

for (int i = 0; i < targetRow.Count; i++)
{
    double tabMidX = targetRow[i].position.X + targetRow[i].button.ActualWidth / 2;
    
    bool shouldInsertHere = false;
    
    // Hysteresis prevents flicker when mouse hovers near boundaries
    if (lastDropTargetIndex == i && mouseX < tabMidX + hysteresisBuffer)
    {
        shouldInsertHere = true; // Stick to current target
    }
    else if (lastDropTargetIndex != i && mouseX < tabMidX - hysteresisBuffer)
    {
        shouldInsertHere = true; // Switch to new target
    }
    
    if (shouldInsertHere)
    {
        return targetRow[i].index;
    }
}

// Mouse past all tabs in row → insert at end of row
return targetRow[targetRow.Count - 1].index + 1;
```

### Keyboard Navigation Grid Calculation

**Left/Right:** Sequential tab traversal with row wrapping
```csharp
// NavigateTab() in TabManager.cs

case "Left":
    if (currentCol > 0)
    {
        // Move left within same row
        targetIndex = rows[currentRow][currentCol - 1].index;
    }
    else if (currentRow > 0)
    {
        // Wrap to end of previous row
        targetIndex = rows[currentRow - 1][rows[currentRow - 1].Count - 1].index;
    }
    else
    {
        // Wrap to last tab in last row
        targetIndex = rows[rows.Count - 1][rows[rows.Count - 1].Count - 1].index;
    }
    break;
```

**Up/Down:** Move to adjacent row, maintain horizontal position
```csharp
case "Up":
    if (currentRow > 0)
    {
        // Find tab in row above with closest X position
        var currentX = rows[currentRow][currentCol].position.X;
        var targetRow = rows[currentRow - 1];
        
        int closestCol = 0;
        double minDist = double.MaxValue;
        for (int c = 0; c < targetRow.Count; c++)
        {
            double dist = Math.Abs(targetRow[c].position.X - currentX);
            if (dist < minDist)
            {
                minDist = dist;
                closestCol = c;
            }
        }
        targetIndex = targetRow[closestCol].index;
    }
    break;
```

### Tab Sizing Constants (AppConstants.cs)

```csharp
public const int TabMinWidth = 80;          // Minimum readable width
public const int TabMaxWidth = 200;         // Prevent excessive width
public const int TabStripMaxHeight = 200;   // Max height before vertical scroll
public const int TabRowGroupingTolerance = 5;  // Y-position tolerance for row detection
public const double TabDragHysteresisBuffer = 5.0;  // Prevents flicker during drag
```

**Why These Values?**
- **MinWidth (80px)**: Ensures tab labels remain readable even with many tabs
- **MaxWidth (200px)**: Prevents tabs from becoming too wide in multi-row layout
- **MaxHeight (200px)**: Balances visibility vs vertical space consumption
- **Tolerance (5px)**: Accounts for sub-pixel rendering variations
- **Hysteresis (5px)**: Sweet spot between responsiveness and stability

---

## Testing & Acceptance Criteria

### Manual Testing Checklist

- [x] **Multi-row wrapping**: Narrow window → tabs wrap to 2-3 rows
- [x] **Drag within row**: Indicator shows correctly, tabs reorder
- [x] **Drag across rows**: Indicator moves to correct row, reorder works
- [x] **Keyboard Left/Right**: Sequential navigation with row wrapping
- [x] **Keyboard Up/Down**: Moves to adjacent row, maintains X position
- [x] **Home/End keys**: Jump to first/last tab
- [x] **Text input protection**: Arrow keys work in text, not tabs
- [x] **Theme toggle**: Visual consistency maintained
- [x] **Tab operations**: Create/delete/rename/duplicate all work
- [x] **Window resize**: Tabs re-wrap smoothly
- [x] **Vertical scroll**: Appears when many tabs exceed 200px height

### Edge Cases Verified

1. **Single Tab**: Row detection works (single row with one tab), no navigation
2. **Window Resize During Drag**: Drag operation continues, row layout recalculates
3. **Theme Toggle During Drag**: Drag visual uses neutral gray (theme-independent)
4. **Tab Creation/Deletion During Navigation**: Uses current tab indices, selects adjacent
5. **Very Many Tabs (>50)**: WrapPanel wraps to many rows, vertical scrollbar appears

---

## Performance & Limitations

### Performance Characteristics

- **Row Calculation Frequency**:
  - On drag move: Recalculated every mouse move (~60 times/second during drag)
  - On keyboard nav: Calculated once per arrow key press
  - Not cached: Layout can change dynamically (window resize, tab creation)

- **Complexity**: O(n) where n = number of tabs
- **Current Performance**: Excellent for typical usage (<50 tabs)

### Optimization Opportunities (Future)

1. **Cache row layout**: Invalidate on window resize or tab count change
2. **Throttle drag updates**: Update indicator every 16ms instead of every mouse move
3. **Use visual state manager**: Pre-calculate row boundaries for faster lookup

### Known Limitations

- **No tab scrolling animation**: Tabs wrap instantly (no smooth scroll)
- **Fixed max height**: 200px limit not user-configurable
- **No row labels**: Can't name/organize tabs into labeled rows
- **Single-line tab titles**: Long titles truncate, no multi-line wrapping

---

## Follow-ups

### Potential Future Enhancements

1. **Tab Pinning**: Pin tabs to always show in first row
2. **Row Animations**: Smooth transitions when tabs wrap/unwrap
3. **Custom Row Height**: User-configurable max height for tab strip
4. **Touch Support**: Swipe gestures for tab navigation on touch screens
5. **Tab Groups**: Visual separators between tab groups across rows

---

## Graduated to CR.md

**Date:** 2025-10-01

The following normative rules were promoted to CR.md § Tabs Pattern:

1. **Multi-row wrapping requirement**: Tabs must wrap into multiple rows when horizontal space is constrained (no hidden horizontal scrollbar)
2. **Edge-like styling musts**: Rounded top corners, accent underline on active tab, hover/pressed states, drag ghost, blue drop indicator
3. **Behavioral guarantees**: Row-aware drag & drop, keyboard Left/Right/Up/Down/Home/End semantics across rows
4. **Design token usage**: All styling uses theme resources (no inline hex codes)
5. **Constant references**: Use AppConstants for all configuration values (by name, not value)
6. **No hidden horizontal resizer**: Removed hidden drag bar beneath tabs

See CR.md for normative language; this Dev Note retains implementation details, rationale, and testing procedures.

---

**References:**
- **Main Implementation**: `UI/TabManager.cs` (lines 314-405 for drag & drop, 968-1136 for keyboard nav)
- **Constants**: `Data/AppConstants.cs` (lines 147-177)
- **XAML Layout**: `UI/Views/MainWindow.xaml` (lines 145-157)
- **Keyboard Handler**: `UI/KeyboardHandler.cs` (lines 143-184)
- **Theme Styles**: `Resources/Themes/DarkTheme.xaml` & `LightTheme.xaml`

