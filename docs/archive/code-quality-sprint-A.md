# Code Quality Sprint A — Inline Colors, Debug Noise, MouseLeave, GIF Async
# Issues: §6, §8, §20 from audit
# Part 1 of 2 — apply before Sprint B

Read every file listed before touching anything.

## RULES
- One fix at a time, build after each
- No behavior changes beyond what's specified
- Colors: match existing values exactly — do not change any visual appearance
- Build gate: `dotnet build` — 0 errors, 0 warnings after every fix
- Write devnote after all fixes in this file pass build (format at bottom)

---

## FIX 1 — Replace inline hex/RGB colors with theme resource references
**Files:** `UI/TabManager.cs`, `UI/Views/SettingsWindow.xaml.cs`
**Finding:** §6 Architectural Violations

Inline color values bypass the theme system. All colors must come from DarkTheme.xaml
resources. Read `Resources/DarkTheme.xaml` first to find the closest matching
resource keys for each color below.

### TabManager.cs — 3 locations

**Location 1 — `InitializeDragCanvas()` — drop indicator blue**
```csharp
// Old:
Background = new SolidColorBrush(Color.FromRgb(74, 144, 226))

// New — use AccentBrush or closest accent resource from DarkTheme.xaml:
Background = (SolidColorBrush)Application.Current.FindResource("AccentBrush")
```

**Location 2 — `CreateDragVisual()` — drag ghost gray**
```csharp
// Old:
Background = new SolidColorBrush(Color.FromArgb(140, 128, 128, 128))
BorderBrush = new SolidColorBrush(Color.FromArgb(120, 96, 96, 96))

// New — use overlay/card brush from DarkTheme.xaml:
// Find the closest semi-transparent surface resource. If none exists, add to DarkTheme.xaml:
// <SolidColorBrush x:Key="DragGhostBrush" Color="#8C808080"/>
// <SolidColorBrush x:Key="DragGhostBorderBrush" Color="#78606060"/>
// Then reference:
Background = (SolidColorBrush)Application.Current.FindResource("DragGhostBrush")
BorderBrush = (SolidColorBrush)Application.Current.FindResource("DragGhostBorderBrush")
```

**Location 3 — `UpdateTabSelection()` fallback block — multiple hardcoded colors**
```csharp
// Old — entire fallback block has hardcoded RGB:
Color.FromRgb(36, 52, 71)    // dark selected
Color.FromRgb(232, 240, 245) // light selected  
Color.FromRgb(64, 64, 64)    // final fallback gray

// New — use TabBackgroundBrush from DarkTheme.xaml (already used in the try block above):
tab.HeaderButton.Background = (Brush)Application.Current.FindResource("TabBackgroundBrush")
// For the final catch fallback, use ContentCardBrush or AppBackgroundBrush.
// Remove the theme-detection branch entirely — dark mode only.
```

### SettingsWindow.xaml.cs — 1 location

**Location — `SetActiveTab()` — active tab highlight**
```csharp
// Old:
tabButton.Background = new SolidColorBrush(
    System.Windows.Media.Color.FromArgb(51, 255, 255, 255)); // 20% white

// New — add to DarkTheme.xaml if not present:
// <SolidColorBrush x:Key="SettingsActiveTabBrush" Color="#33FFFFFF"/>
// Then:
tabButton.Background = (SolidColorBrush)Application.Current.FindResource("SettingsActiveTabBrush")
```

Do NOT change `Brushes.Transparent` — that's a system brush, not a theme violation.

---

## FIX 2 — Replace Debug.WriteLine with LoggingService in production paths
**Files:** `UI/NoteListWindow.xaml.cs`, `UI/MediaSection.xaml.cs`
**Finding:** §6 Architectural Violations

`System.Diagnostics.Debug.WriteLine` is compiled out in Release but string interpolation
IS still evaluated — allocations happen even when output is dropped. More critically, these
log production errors that are silently lost in Release builds.

### NoteListWindow.xaml.cs — 2 locations

Both are in catch blocks in `OpenNoteWindow()` and `CloseNoteWindow()`.
Replace with proper logging. NoteListWindow already has access to static logging:

```csharp
// Old:
System.Diagnostics.Debug.WriteLine($"❌ Error opening note window: {ex.Message}");

// New:
LoggingService.LogErrorStatic("Failed to open note window", ex, "UI");
```

```csharp
// Old:
System.Diagnostics.Debug.WriteLine($"❌ Error closing note window: {ex.Message}");

// New:
LoggingService.LogErrorStatic("Failed to close note window", ex, "UI");
```

### MediaSection.xaml.cs — scan and replace

Read MediaSection.xaml.cs. Find every `System.Diagnostics.Debug.WriteLine` call.
Rules:
- Inside catch blocks → replace with `LoggingService.LogErrorStatic(...)`
- Drag operation debug traces (mouse move, index logging) → wrap in `#if DEBUG` block
- Any remaining non-catch Debug.WriteLine → wrap in `#if DEBUG` block

Do NOT remove the drag logging entirely — it's useful in debug builds.
Do NOT change any logic, only the logging calls.

---

## FIX 3 — MediaBorder MouseLeave handler accumulates on rapid clicks
**File:** `UI/Views/NoteTab.xaml.cs` — `MediaBorder_PreviewMouseDown()`, `MediaBorder_MouseLeave()`
**Finding:** §8 XAML Issues

Every `PreviewMouseDown` subscribes `MediaBorder_MouseLeave`. Rapid clicks before the
mouse leaves = N subscriptions. Each leave fires N times, sets brushes N times.

Add a bool guard to prevent multiple subscriptions:

```csharp
// Add field to NoteTab class:
private bool _mediaBorderLeaveSubscribed = false;

// Old MediaBorder_PreviewMouseDown:
private void MediaBorder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
{
    MediaBorder.BorderBrush = (Brush)FindResource("AccentBrush");
    MediaBorder.Effect = (Effect)FindResource("EditorFocusGlow");
    MediaBorder.MouseLeave += MediaBorder_MouseLeave; // ← unbounded accumulation
}

// New:
private void MediaBorder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
{
    MediaBorder.BorderBrush = (Brush)FindResource("AccentBrush");
    MediaBorder.Effect = (Effect)FindResource("EditorFocusGlow");
    if (!_mediaBorderLeaveSubscribed)
    {
        MediaBorder.MouseLeave += MediaBorder_MouseLeave;
        _mediaBorderLeaveSubscribed = true;
    }
}

// Old MediaBorder_MouseLeave:
private void MediaBorder_MouseLeave(object sender, MouseEventArgs e)
{
    MediaBorder.BorderBrush = Brushes.Transparent;
    MediaBorder.Effect = null;
    MediaBorder.MouseLeave -= MediaBorder_MouseLeave;
}

// New:
private void MediaBorder_MouseLeave(object sender, MouseEventArgs e)
{
    MediaBorder.BorderBrush = Brushes.Transparent;
    MediaBorder.Effect = null;
    MediaBorder.MouseLeave -= MediaBorder_MouseLeave;
    _mediaBorderLeaveSubscribed = false;
}
```

---

## FIX 4 — SettingsWindow button styles use StaticResource (throws at parse if missing)
**File:** `UI/Views/SettingsWindow.xaml`
**Finding:** §8 XAML Issues

Read `SettingsWindow.xaml`. Find every `{StaticResource ...}` reference on Button
`Style` attributes. `StaticResource` throws `XamlParseException` at window load time
if the key is missing. `DynamicResource` defers to runtime and fails gracefully.

For every Button `Style="{StaticResource SomeButtonStyle}"` in SettingsWindow.xaml:
```xaml
<!-- Old: -->
<Button Style="{StaticResource SomeButtonStyle}" .../>

<!-- New: -->
<Button Style="{DynamicResource SomeButtonStyle}" .../>
```

Only change button Style attributes. Do NOT change other StaticResource references
(brushes, converters, etc. — those are fine as Static).

---

## FIX 5 — LoadGifAsync is synchronous and blocks UI thread on large GIFs
**File:** `UI/ImageViewerWindow.xaml.cs` — `LoadGifAsync()`
**Finding:** §20 Async/Await

`LoadGifAsync` is a synchronous `private void` method despite its name. `bitmap.EndInit()`
opens the file and initializes the decoder on the UI thread — large GIFs cause visible
freeze. File I/O must move to a background thread.

Rename to `LoadGif` and restructure as `async void` matching the `LoadStaticAsync` pattern:

```csharp
// Old:
private void LoadGifAsync(string imagePath)
{
    var bitmap = new BitmapImage();
    bitmap.BeginInit();
    bitmap.CacheOption = BitmapCacheOption.OnDemand;
    bitmap.CreateOptions = BitmapCreateOptions.None;
    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
    bitmap.EndInit(); // ← blocks UI thread on large files
    // ...rest of setup
}

// New:
private async void LoadGif(string imagePath)
{
    try
    {
        // Read file bytes on background thread — keeps UI responsive
        byte[] gifBytes = await Task.Run(() => File.ReadAllBytes(imagePath));

        // Check if navigation moved on while we were loading
        if (currentImagePath != imagePath) return;

        // Create BitmapImage from memory stream on UI thread (required for WPF)
        var ms = new MemoryStream(gifBytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnDemand; // Frames decoded on demand during playback
        bitmap.CreateOptions = BitmapCreateOptions.None;
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        // Do NOT freeze — GIF animation requires mutable bitmap

        currentImagePath = imagePath;
        currentImage = bitmap;

        RenderOptions.SetBitmapScalingMode(DisplayImage, BitmapScalingMode.Unspecified);
        DisplayImage.SnapsToDevicePixels = false;
        DisplayImage.UseLayoutRounding = false;

        try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { }
        ImageBehavior.SetAnimatedSource(DisplayImage, bitmap);

        var fileName = Path.GetFileName(imagePath);
        this.Title = $"🖼️ {fileName}";

        AutoSizeWindow(bitmap);
        Dispatcher.BeginInvoke(new Action(() => ApplyOneToOne()), DispatcherPriority.Loaded);
        UpdateImageInfo();
    }
    catch (Exception ex)
    {
        LogImage("💥 Exception in LoadGif", ex);
        ShowError($"Failed to load GIF: {ex.Message}");
    }
}
```

Update the call site in `LoadImage()`:
```csharp
// Old:
if (extension == ".gif")
    LoadGifAsync(imagePath);

// New:
if (extension == ".gif")
    LoadGif(imagePath);
```

Note: `MemoryStream ms` is intentionally not disposed here. `BitmapImage` with
`StreamSource` and `OnDemand` reads from the stream during animation playback —
disposing the stream would break subsequent frame reads. The stream is owned by
the bitmap and will be GC'd with it.

---

## AFTER ALL 5 FIXES PASS BUILD

Write devnote to:
`docs/devnotes/2026-05-19-code-quality-sprint-A.md`

```
# Code Quality Sprint A — Colors, Logging, MouseLeave, GIF Async
Date: 2026-05-19

## Fixes

FIX-1: Inline hex/RGB colors replaced with DarkTheme.xaml resource references
       New keys added to DarkTheme.xaml: DragGhostBrush, DragGhostBorderBrush,
       SettingsActiveTabBrush (if not already present)
       Files: TabManager.cs, SettingsWindow.xaml.cs

FIX-2: Debug.WriteLine replaced with LoggingService in NoteListWindow (2 catch blocks)
       Debug.WriteLine in MediaSection drag traces wrapped in #if DEBUG
       Files: NoteListWindow.xaml.cs, MediaSection.xaml.cs

FIX-3: MediaBorder MouseLeave subscription guarded with _mediaBorderLeaveSubscribed bool
       Rapid clicks no longer accumulate duplicate handler subscriptions
       File: NoteTab.xaml.cs

FIX-4: SettingsWindow button Style attributes changed from StaticResource to DynamicResource
       Prevents XamlParseException at window load if key is missing at parse time
       File: SettingsWindow.xaml

FIX-5: LoadGifAsync renamed to LoadGif, restructured as async void
       File I/O moved to Task.Run — large GIFs no longer block UI thread
       File: ImageViewerWindow.xaml.cs

## Build status
0 errors, 0 warnings

## Next
Code Quality Sprint B: docs/code-quality-sprint-B.md
```
