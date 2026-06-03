# Memory Fix — Phase 2: Bug Fixes, Tab + Viewer Memory
# Expected result: ~150MB → ~120MB
# Part 2 of 4 — Phase 1 must be complete and building clean before starting this.

You are continuing the memory remediation of SnipShottyBoard (WPF .NET 8).
Phase 1 is done. These are the next confirmed bugs to fix — slightly more invasive
than Phase 1 but still surgical. Read each file fully before editing.

## RULES
- Read each file before editing it
- One fix at a time, build after each
- Do NOT change UI behavior, layout, or visual output
- Do NOT refactor anything outside the specified scope
- Build gate: `dotnet build` must pass 0 errors 0 warnings after every fix
- After ALL fixes pass build, write ONE devnote (format at bottom)

---

## FIX 1 — Dispose AnimationController before clearing GIF source
**File:** `UI/ImageViewerWindow.xaml.cs` — `ClearPreviousImage()`
**Finding:** LEAK-6

WpfAnimatedGif's `AnimationController` holds a reference to the `BitmapDecoder` which
holds ALL decoded frame pixel buffers. A 30-frame GIF = ~30MB of decoded frames.
If the controller is not disposed before clearing the source, those frames stay in memory.

Change:
```csharp
// Old:
try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { }
DisplayImage.Source = null;

// New:
try
{
    var ctrl = ImageBehavior.GetAnimationController(DisplayImage);
    ctrl?.Dispose();
    ImageBehavior.SetAnimatedSource(DisplayImage, null);
}
catch { }
DisplayImage.Source = null;
```

Apply this change everywhere `ClearPreviousImage()` clears the animated source.
Also apply the same pattern in `ReleaseImageResources()` if it clears animated source.

---

## FIX 2 — Limit ImageViewerWindow to 1 instance per image path
**File:** `UI/MediaSection.xaml.cs` — `ShowFullSizeImage()`
**Finding:** LEAK-9 / QW-9

Every image click creates a brand new `ImageViewerWindow` with no limit.
10 clicks = 10 open windows, each holding a full-res BitmapImage (5–50MB each).
Limit to 1 viewer per path — if already open, bring it to front instead.

Replace the viewer creation block in `ShowFullSizeImage()`:
```csharp
// Old:
var imageViewer = new ImageViewerWindow(imagePath, validImages, currentIndex, RemoveImageByPath);
// ... positioning logic ...
imageViewer.Show();

// New:
// Check if a viewer for this path is already open
var existingViewer = Application.Current.Windows
    .OfType<ImageViewerWindow>()
    .FirstOrDefault(w => w.CurrentImagePath == imagePath);

if (existingViewer != null)
{
    existingViewer.WindowState = WindowState.Normal;
    existingViewer.Activate();
    existingViewer.Focus();
    return;
}

var imageViewer = new ImageViewerWindow(imagePath, validImages, currentIndex, RemoveImageByPath);
// ... existing positioning logic unchanged ...
imageViewer.Show();
```

You will need to expose `CurrentImagePath` as a public property on `ImageViewerWindow`
that returns `currentImagePath`. Add it as a simple getter:
```csharp
public string CurrentImagePath => currentImagePath;
```

---

## FIX 3 — Cache static SolidColorBrush instances in StatusBarManager
**File:** `UI/StatusBarManager.cs` — `UpdateStatusBar()` or equivalent
**Finding:** LEAK-19 / QW-2

`UpdateStatusBar()` is called every 1 second. If it creates `new SolidColorBrush(...)` 
for save-status color changes, that's 2 allocations/second forever, preventing GC from 
quiescing. Cache them as static readonly fields instead.

Read `StatusBarManager.cs` first. Find every `new SolidColorBrush(...)` inside any
method called on a timer tick. Replace with static cached instances:

```csharp
// Add at top of class:
private static readonly SolidColorBrush SavedBrush = new SolidColorBrush(Color.FromRgb(/* existing color */)) { IsFrozen = true } ... ;
// Use Freeze() after construction to make them immutable and thread-safe:
// e.g.:
private static readonly SolidColorBrush _savedBrush;
private static readonly SolidColorBrush _unsavedBrush;

static StatusBarManager()
{
    _savedBrush = new SolidColorBrush(Colors.Green);
    _savedBrush.Freeze();
    _unsavedBrush = new SolidColorBrush(Colors.Orange);
    _unsavedBrush.Freeze();
}
```

Match the exact colors already in use — do not change any colors.
Replace all `new SolidColorBrush(...)` inside timer-driven methods with the cached instances.

---

## FIX 4 — Unsubscribe child events in NoteTab.Dispose()
**File:** `UI/Views/NoteTab.xaml.cs` — `Dispose()`
**Finding:** LEAK-13 / QW-4

`NoteTab` subscribes to child component events in its constructor:
```csharp
TextSectionControl.OnTextChanged += () => OnDataChanged?.Invoke();
MediaSectionControl.OnMediaChanged += () => OnDataChanged?.Invoke();
```

These are lambdas — they capture `this` (NoteTab). When a tab is deleted, if these
subscriptions are not removed, `TextSection` and `MediaSection` keep `NoteTab` alive
via the event subscription. Any async thumbnail completing after tab deletion holds
the entire tab object graph in memory.

In `NoteTab.Dispose()`, add unsubscription before the existing cleanup:
```csharp
public void Dispose()
{
    try
    {
        // Unsubscribe child events to allow GC collection
        TextSectionControl.OnTextChanged -= _onTextChangedHandler;
        MediaSectionControl.OnMediaChanged -= _onMediaChangedHandler;

        // existing cleanup below unchanged:
        MediaSectionControl?.Dispose();
        OnDataChanged = null;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error disposing NoteTab: {ex.Message}");
    }
}
```

Because the original subscriptions are lambdas (not named methods), you cannot unsubscribe
them directly. Refactor the constructor subscriptions to use stored handler fields:

```csharp
// Add fields:
private Action _onTextChangedHandler;
private Action _onMediaChangedHandler;

// In constructor, replace lambda subscriptions with:
_onTextChangedHandler = () => OnDataChanged?.Invoke();
_onMediaChangedHandler = () => OnDataChanged?.Invoke();
TextSectionControl.OnTextChanged += _onTextChangedHandler;
MediaSectionControl.OnMediaChanged += _onMediaChangedHandler;
```

---

## AFTER ALL 4 FIXES PASS BUILD

Write devnote to:
`docs/devnotes/2026-05-19-sprint-memory-phase2-bug-fixes.md`

Format:
```
# Memory Fix Phase 2 — Bug Fixes: Tab + Viewer Memory
Date: 2026-05-19

## Changes
- LEAK-6: AnimationController disposed before GIF source cleared (ImageViewerWindow.xaml.cs)
- LEAK-9: ImageViewerWindow limited to 1 instance per path (MediaSection.xaml.cs)
- LEAK-19: SolidColorBrush instances cached as static frozen fields (StatusBarManager.cs)
- LEAK-13: Child event handlers stored in fields + unsubscribed in NoteTab.Dispose()

## Expected RAM impact
~150MB → ~120MB idle

## Build status
0 errors, 0 warnings

## Next
Phase 3: memory-fix-phase-3.md
```
