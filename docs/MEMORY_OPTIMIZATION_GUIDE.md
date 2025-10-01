# SnipShottyBoard Memory Optimization Guide

## Current Memory Usage Analysis

**Baseline Comparison:**
- Windows Sticky Notes: ~60 MB
- SnipShottyBoard: ~135 MB (with tabs + images)
- **Difference**: +75 MB (+125% overhead)

## Memory Sources Breakdown

### 1. Framework Overhead (40-50 MB)
- **WPF Runtime**: ~25-35 MB
- **.NET 8 Self-Contained**: ~15-20 MB
- **Inevitable cost** of rich desktop framework vs native

### 2. Image Memory (30-50 MB with pictures)
- **BitmapImage objects**: Full images loaded into RAM
- **GIF animations**: Extra memory for animated frames
- **No disposal**: Images persist until garbage collection
- **Thumbnails inefficient**: Load full image, then scale

### 3. UI Control Overhead (20-30 MB)
- **Rich controls per tab**: Grid, WrapPanel, RichTextBox, TextBlock
- **Event handlers**: Multiple subscriptions per component
- **Theme resources**: WPF resource dictionaries

### 4. Logging & Debugging (5-10 MB)
- **Serilog**: In-memory buffers and structured logging
- **Debug output**: Additional overhead in debug builds

## Optimization Strategies

### 🚀 High Impact (Quick Wins)

#### 1. Image Memory Optimization
```csharp
// BEFORE: Load full image
var bitmap = new BitmapImage();
bitmap.CacheOption = BitmapCacheOption.OnLoad; // Loads entire image

// AFTER: True thumbnail generation
var bitmap = new BitmapImage();
bitmap.DecodePixelWidth = 120; // Decode only what we need
bitmap.CacheOption = BitmapCacheOption.OnLoad;
```

**Implementation:**
- Generate actual thumbnails (not scaled full images)
- Dispose BitmapImage objects when not needed
- Use WeakReference for cached images
- Lazy load images only when tab is visible

#### 2. Image Disposal Pattern
```csharp
// Add to MediaSection cleanup
public void ReleaseImageResources()
{
    foreach (var container in ImagePanel.Children.OfType<Grid>())
    {
        var images = container.Descendants<Image>();
        foreach (var img in images)
        {
            if (img.Source is BitmapImage bmp)
            {
                img.Source = null;
                // Force disposal for large images
                if (bmp.PixelWidth > 500 || bmp.PixelHeight > 500)
                {
                    bmp = null;
                }
            }
        }
    }
    GC.Collect(0, GCCollectionMode.Optimized);
}
```

#### 3. Lazy Tab Loading
```csharp
// Only create tab content when first accessed
public class LazyTab : CustomTab
{
    private bool _contentLoaded = false;
    private Func<UserControl> _contentFactory;
    
    public override UserControl Content 
    {
        get 
        {
            if (!_contentLoaded)
            {
                base.Content = _contentFactory();
                _contentLoaded = true;
            }
            return base.Content;
        }
    }
}
```

### ⚡ Medium Impact

#### 4. Image Format Optimization
- **Convert large images to JPEG** (smaller than PNG)
- **Limit GIF frame count** for animations
- **Set max image dimensions** (e.g., 1920x1080)

#### 5. UI Virtualization
```csharp
// For large image collections, use VirtualizingStackPanel
<VirtualizingStackPanel VirtualizationMode="Recycling">
    <!-- Image containers -->
</VirtualizingStackPanel>
```

#### 6. Event Handler Cleanup
```csharp
// Proper disposal in NoteTab
public void Dispose()
{
    // Clear all event subscriptions
    OnDataChanged = null;
    MediaSectionControl?.Dispose();
    
    // Clear control references
    TextSectionControl = null;
    MediaSectionControl = null;
}
```

### 🔧 Advanced Optimizations

#### 7. Memory-Mapped Image Cache
```csharp
// Use memory-mapped files for large images
public class ImageCache
{
    private readonly Dictionary<string, WeakReference<BitmapImage>> _cache;
    
    public BitmapImage GetImage(string path)
    {
        if (_cache.TryGetValue(path, out var weakRef) && 
            weakRef.TryGetTarget(out var cached))
        {
            return cached; // Reuse if still in memory
        }
        
        var image = LoadOptimizedImage(path);
        _cache[path] = new WeakReference<BitmapImage>(image);
        return image;
    }
}
```

#### 8. Release Build Optimizations
```xml
<!-- In .csproj for release builds -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
    <Optimize>true</Optimize>
    <PublishTrimmed>true</PublishTrimmed>
</PropertyGroup>
```

#### 9. Framework-Dependent Deployment
Instead of self-contained (161 MB), use framework-dependent:
```bash
dotnet publish -c Release --framework-dependent
# Results in ~15-20 MB executable (requires .NET 8 on target)
```

## Expected Memory Reduction

| Optimization | Memory Reduction | Effort |
|-------------|------------------|---------|
| Image disposal | -15-25 MB | Low |
| True thumbnails | -10-20 MB | Medium |
| Lazy tab loading | -5-15 MB | Medium |
| Framework-dependent | -30-40 MB file size | Low |
| UI virtualization | -5-10 MB | High |

## Implementation Priority

### Phase 1: Quick Wins (1-2 days)
1. Add image disposal to tab/window close
2. Force GC after image operations
3. Reduce thumbnail decode size

### Phase 2: Structural (1 week)
1. Implement proper image cache with WeakReference
2. Add lazy tab loading
3. Clean up event handlers

### Phase 3: Advanced (2+ weeks)
1. UI virtualization for large collections
2. Memory-mapped image cache
3. Consider lighter UI framework alternatives

## Monitoring

Add memory monitoring to logging:
```csharp
public void LogMemoryUsage(string context)
{
    var workingSet = Environment.WorkingSet / (1024 * 1024);
    var gcMemory = GC.GetTotalMemory(false) / (1024 * 1024);
    LogInfo($"Memory: {workingSet} MB working set, {gcMemory} MB managed", "Memory");
}
```

## Realistic Expectations

**Target:** Reduce from 135 MB to 85-100 MB (30-40% reduction)
**Comparison:** Still 40-60% higher than native Windows Sticky Notes due to framework overhead

The WPF/.NET stack will always use more memory than native Windows apps, but these optimizations can significantly reduce the overhead while maintaining the rich functionality.
