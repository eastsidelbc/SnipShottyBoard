# Memory Hotfix 2 — GIF Frames Not Released on Viewer Close/Navigate
# Symptom: After hotfix 1, idle ~110MB. Viewer open/cycle (incl GIFs)/close → stuck at ~180MB.
# Root cause: AnimationController holds BitmapDecoder → all decoded GIF frames in memory.
# Apply immediately, before Phase 2.

Read `UI/ImageViewerWindow.xaml.cs` IN FULL before touching anything.

## ROOT CAUSE

WpfAnimatedGif works by decoding ALL frames of a GIF upfront into pixel buffers.
A 30-frame 800×600 GIF = 800 × 600 × 4 × 30 bytes = ~57MB of decoded frame data.

The `AnimationController` object holds a reference to the `BitmapDecoder` which owns
all those frames. When navigating or closing:

Current code in ClearPreviousImage():
  `try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { }`

SetAnimatedSource(null) stops playback but does NOT dispose the AnimationController.
The controller stays alive, BitmapDecoder stays alive, all frames stay in memory.

The AnimationController IS disposable — calling `ctrl.Dispose()` releases the decoder
and all frame buffers. This must happen BEFORE SetAnimatedSource(null).

## TWO PLACES TO FIX

Both `ClearPreviousImage()` and `ReleaseImageResources()` clear the animated source.
Both need the controller disposed first. Apply the same pattern to both.

---

## FIX 1 — ClearPreviousImage(): dispose controller before clearing source
**File:** `UI/ImageViewerWindow.xaml.cs` — `ClearPreviousImage()`

```csharp
// Old:
private void ClearPreviousImage()
{
    if (string.IsNullOrEmpty(currentImagePath)) return;
    try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { /* no prior GIF */ }
    DisplayImage.Source = null;
    currentImage = null;
    ImageCacheManager.Instance.RemoveFromCache(currentImagePath + FullResCacheSuffix);
    currentZoomLevel = 1.0;
    _isInFitMode = false;
}

// New:
private void ClearPreviousImage()
{
    if (string.IsNullOrEmpty(currentImagePath)) return;
    try
    {
        var ctrl = ImageBehavior.GetAnimationController(DisplayImage);
        ctrl?.Dispose(); // releases BitmapDecoder + all decoded GIF frame buffers
        ImageBehavior.SetAnimatedSource(DisplayImage, null);
    }
    catch { /* no prior GIF — safe to ignore */ }
    DisplayImage.Source = null;
    currentImage = null;
    ImageCacheManager.Instance.RemoveFromCache(currentImagePath + FullResCacheSuffix);
    currentZoomLevel = 1.0;
    _isInFitMode = false;
}
```

---

## FIX 2 — ReleaseImageResources(): same pattern on window close
**File:** `UI/ImageViewerWindow.xaml.cs` — `ReleaseImageResources()`

```csharp
// Old:
private void ReleaseImageResources()
{
    try
    {
        try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { /* safe */ }

        if (currentImage != null && !currentImage.IsFrozen)
        {
            try { currentImage.StreamSource?.Dispose(); } catch { /* safe */ }
        }

        if (DisplayImage != null) DisplayImage.Source = null;
        currentImage = null;

        if (!string.IsNullOrEmpty(currentImagePath))
            ImageCacheManager.Instance.RemoveFromCache(currentImagePath + FullResCacheSuffix);

        currentImagePath = null;
    }
    catch (Exception ex)
    {
        LoggingService.LogErrorStatic($"[CLS] Error during resource cleanup: {ex.Message}", ex, "ImageLoad");
    }
}

// New:
private void ReleaseImageResources()
{
    try
    {
        try
        {
            var ctrl = ImageBehavior.GetAnimationController(DisplayImage);
            ctrl?.Dispose(); // releases BitmapDecoder + all decoded GIF frame buffers
            ImageBehavior.SetAnimatedSource(DisplayImage, null);
        }
        catch { /* safe */ }

        if (currentImage != null && !currentImage.IsFrozen)
        {
            try { currentImage.StreamSource?.Dispose(); } catch { /* safe */ }
        }

        if (DisplayImage != null) DisplayImage.Source = null;
        currentImage = null;

        if (!string.IsNullOrEmpty(currentImagePath))
            ImageCacheManager.Instance.RemoveFromCache(currentImagePath + FullResCacheSuffix);

        currentImagePath = null;
    }
    catch (Exception ex)
    {
        LoggingService.LogErrorStatic($"[CLS] Error during resource cleanup: {ex.Message}", ex, "ImageLoad");
    }
}
```

---

## BUILD + TEST

Build: `dotnet build` — 0 errors, 0 warnings.

Manual test:
1. Note idle RAM (~110MB)
2. Open viewer on a GIF, let it play a few seconds
3. Navigate through 5+ images including other GIFs
4. Close viewer (ESC)
5. Wait 5 seconds
6. RAM should return to ~110–115MB

Test static images only too — behaviour should be identical.

---

## DEVNOTE

Write to: `docs/devnotes/2026-05-19-sprint-memory-hotfix2-gif-frames.md`

```
# Memory Hotfix 2 — GIF Frame Buffers Not Released
Date: 2026-05-19

## Symptom
After hotfix 1: idle ~110MB. Viewer open/cycle (GIFs included)/close → stuck at ~180MB.
~70MB not returning to baseline.

## Root Cause
WpfAnimatedGif AnimationController holds BitmapDecoder → all decoded GIF frames.
30-frame GIF = ~57MB decoded frame data retained until ctrl.Dispose() called.
Previous code called SetAnimatedSource(null) without disposing the controller first.
Frames never released → accumulate across navigation cycles.

## Fix
Both ClearPreviousImage() and ReleaseImageResources() now call:
  var ctrl = ImageBehavior.GetAnimationController(DisplayImage);
  ctrl?.Dispose();
  ImageBehavior.SetAnimatedSource(DisplayImage, null);

Disposing the controller releases the BitmapDecoder and all frame pixel buffers
before the source is cleared. Combined with the LOH compaction in OnClosed()
(hotfix 1), RAM should return to baseline within 5 seconds of closing the viewer.

## Changes
- ImageViewerWindow.xaml.cs — ClearPreviousImage(): dispose AnimationController first
- ImageViewerWindow.xaml.cs — ReleaseImageResources(): dispose AnimationController first

## Expected result
RAM returns to ~110MB baseline after viewer close including GIF sessions.

## Build status
0 errors, 0 warnings
```
