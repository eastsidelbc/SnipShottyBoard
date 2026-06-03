# SnipShottyBoard — Deep Memory Leak Audit
# Generated: 2026-05-19 | Baseline: ~300MB idle, target: ~100MB

You are performing a surgical memory leak audit on SnipShottyBoard (WPF .NET 8).
The app idles at ~300MB. Baseline should be ~100MB. Leaks are confirmed — find them all.

Read and fully understand these files before doing anything:
- UI/MediaSection.xaml.cs
- UI/ImageViewerWindow.xaml.cs
- UI/ImageCacheManager.cs
- UI/Views/MainWindow.xaml.cs
- UI/Views/NoteTab.xaml.cs
- UI/ThemeManager.cs
- Core/Managers/NoteWindowManager.cs

Do NOT fix anything yet. Audit only. Output a structured report with every finding
using the exact format below. Then wait for approval before touching a single line.

════════════════════════════════════════════════════════════════
FINDING FORMAT — use for every issue found
════════════════════════════════════════════════════════════════

### LEAK-N — [Short title]
**File:** `path/to/File.cs` — method or line reference
**Type:** BUG | RISK | SMELL | CLEANUP | QUICK WIN | BLOCKER
**IMPACT:** HIGH | MEDIUM | LOW  (RAM effect: ~XMB per occurrence)
**EFFORT:** Trivial (1-3 lines) | Small (< 30 min) | Medium (< 2h) | Large (> 2h)

**Root cause:**
[Exact technical explanation. Name the objects, the references, why GC cannot collect.]

**Proof of leak:**
[Scenario: "Open 5 images → navigate → close → repeat 10x → RAM never drops"]

**Fix:**
[Exact code change. Show old → new. Keep it surgical.]

---

════════════════════════════════════════════════════════════════
SECTION 1 — BITMAP & IMAGE CACHE LEAKS
════════════════════════════════════════════════════════════════

Focus on:

1a. ImageViewerWindow.ReleaseImageResources() — does it evict the LAST loaded image
    from ImageCacheManager before nulling currentImagePath? Check if
    `RemoveFromCache(currentImagePath + ":full")` is called. If missing → every close
    leaves one full-res BitmapImage permanently in the LRU cache.

1b. ImageCacheManager.EstimateBitmapBytes() — current formula: PixelWidth * PixelHeight * 4 * 2.
    Verify this matches actual WPF BitmapImage memory usage. WPF also allocates:
    internal pixel buffer, back-buffer, thumbnail pyramid, metadata strings.
    If estimate is too low, _currentBytes is wrong → eviction never fires when it should
    → cache grows to 200-300MB before eviction kicks in.

1c. ImageCacheManager — singleton, never cleared. Thumbnails from closed tabs/sessions
    accumulate forever until count or byte cap triggers. What is AppConstants.MaxCachedImages
    and AppConstants.MaxImageCacheBytes? If 100 items / 100MB, the cap may be too high
    for an idle app. Report current values.

1d. MediaSection.CreateThumbnailBitmap() — calls ImageCacheManager.Instance.AddToCache()
    INSIDE the method. Then LoadThumbnailAsync() calls AddToCache() again on the dispatcher
    thread (line: `ImageCacheManager.Instance.AddToCache(imagePath, bitmap)`).
    Dual-add attempt is guarded by `if (_index.ContainsKey(path)) return;` in AddToCache —
    confirm this guard is actually hit and no duplicate BitmapImage is created.

1e. MediaSection.CreateStaticGifThumbnail() — uses BitmapCreateOptions.IgnoreImageCache.
    This bypasses WPF's internal BitmapFrame cache. Combined with BitmapCacheOption.OnLoad
    and Freeze(), this is correct. Verify this is NOT going through CreateThumbnailBitmap()
    for GIFs (which would use OnDemand and skip Freeze). Trace the code path for .gif
    in LoadThumbnailAsync → CreateStaticGifThumbnail vs CreateThumbnailBitmap.

1f. ImageViewerWindow.LoadGifAsync() — BitmapImage created with CacheOption.OnDemand,
    NOT frozen. WpfAnimatedGif (ImageBehavior) holds internal state on this bitmap.
    When ClearPreviousImage() calls ImageBehavior.SetAnimatedSource(DisplayImage, null),
    does WpfAnimatedGif release all internal frame buffers? Or does it keep a reference
    to the original BitmapImage? Research WpfAnimatedGif's disposal pattern. If frames
    are not released, every GIF navigation = frame data permanently in memory.

════════════════════════════════════════════════════════════════
SECTION 2 — CANCELLATION TOKEN SOURCE (CTS) LEAKS
════════════════════════════════════════════════════════════════

2a. MediaSection._pendingLoadsCts — CRITICAL ARCHITECTURAL BUG.
    EnsureThumbnailLoaded() calls `_pendingLoadsCts?.Cancel()` then creates a NEW CTS
    every time it's called. This CTS is a SINGLE field on the class, shared across ALL
    image loads. During LoadImagesFromFiles() with N images:
    - Image 1: new CTS created
    - Image 2: CTS from image 1 canceled → disposed? No. New CTS created.
    - Image 3: CTS from image 2 canceled → ...
    Result: only the LAST image's CTS is alive. Images 1..N-1 had their tokens
    canceled immediately. All their LoadThumbnailAsync tasks catch OperationCanceledException
    and exit — but do they release _loadSemaphore? Yes (finally block). But:
    - All the canceled BitmapImage decode objects (Task.Run) are abandoned mid-decode.
    - The OLD CancellationTokenSource objects are canceled but never Disposed
      (only the final one gets Disposed in Dispose()). Each undisposed CTS = small leak.
    Fix requires per-image CTS, not one shared field.

2b. ImageViewerWindow._currentLoadCts — correctly per-load (field replaced on each
    LoadImage call). Verify the old CTS is Disposed (not just Canceled) before
    replacement. Current code: `_currentLoadCts?.Cancel(); _currentLoadCts?.Dispose();`
    then `_currentLoadCts = null;` then `_currentLoadCts = new CTS`. This is correct.
    Confirm OnClosed also disposes it.

════════════════════════════════════════════════════════════════
SECTION 3 — DISPATCHER TIMER LEAKS
════════════════════════════════════════════════════════════════

3a. MediaSection.clickTimer — SUBSCRIPTION BUG.
    clickTimer is a class-level field. In AddDragHandlers():
      if (clickTimer == null) {
          clickTimer = new DispatcherTimer(...);
          clickTimer.Tick += OnClickTimerTick;
      }
    In CancelClickDetection():
      clickTimer.Tick -= OnClickTimerTick;
    
    After the first image click: timer created, handler subscribed, then unsubscribed.
    clickTimer is now non-null but has NO handler. On next image click, the null check
    fails (timer exists), so no new subscription is done. Timer fires → nothing happens.
    Click detection silently broken after first use. This is both a logic bug AND
    means timers are running without subscribers.
    Fix: don't unsubscribe in CancelClickDetection. Stop the timer instead. Or use
    a bool flag to gate the Tick handler.

3b. MainWindow — 3 DispatcherTimers (autoSaveTimer, statusTimer, recoveryTimer).
    All correctly stopped and nulled in MainWindow_Closing. Confirm they are not
    re-created anywhere (e.g. settings reload path). Verify Stop() is called before
    null assignment (Stop() → null, not null → Stop()).

3c. MainWindow.recoveryTimer Tick lambda — creates `new MasterData { Windows =
    NoteWindowManager.Instance.GetActiveWindows() }` every 2 seconds when dirty.
    GetActiveWindows() returns `new List<NoteWindowData>()` with the full notes graph.
    Each 2s tick when dirty = one full object graph allocation. These are transient
    and GC-collectable, but with 20 tabs and large notes, each allocation could be
    several MB. Confirm these are not accumulating (they shouldn't be, but verify
    no static references capture them).

════════════════════════════════════════════════════════════════
SECTION 4 — EVENT HANDLER LEAKS (WPF #1 LEAK SOURCE)
════════════════════════════════════════════════════════════════

4a. MediaSection.SetupContainerInteractions() — each image container gets:
    - container.MouseEnter += lambda (captures container, FindResource)
    - container.MouseLeave += lambda (captures container)
    - 6+ ContextMenu MenuItem.Click handlers (capture container, mediaRef, imagePath)
    - container.MouseLeftButtonDown += lambda (captures container, mediaRef)
    These lambdas are attached to WPF UIElements in the visual tree. As long as
    the container is in the visual tree (ImagePanel.Children), this is fine.
    BUG: When RebuildUIFromData() clears ImagePanel.Children and removes containers,
    the old containers still have their event handlers. If anything else holds a
    reference to an old container (e.g., draggedContainer field, pendingClickContainer),
    those containers and their captured closures cannot be GC'd.
    Check: after RebuildUIFromData(), is draggedContainer always updated to the new
    container reference? Is pendingClickContainer always cleared?

4b. MediaSection.AddDragHandlers() — drag event handlers:
    container.MouseMove += OnDragMouseMove
    container.MouseUp += OnDragMouseUp
    container.MouseLeave += OnDragMouseLeave
    These are removed in CleanupDragOperation(). But CleanupDragOperation is only
    called from CompleteDrag() and nowhere else. If the user starts a drag then:
    - closes the window
    - switches tabs
    - the drag is abandoned (no mouse up ever fires)
    These handlers are never removed. The container (and all its event closures)
    stays rooted by the drag state. Check: is CleanupDragOperation called in Dispose()?

4c. NoteTab.Dispose() — calls MediaSectionControl.Dispose() and sets OnDataChanged = null.
    But it does NOT unsubscribe from MediaSectionControl.OnMediaChanged or
    TextSectionControl.OnTextChanged (subscribed in NoteTab constructor).
    After tab is disposed, if MediaSection fires OnMediaChanged (e.g. async thumbnail
    completes for an old container), the event still routes to NoteTab's dead handler.
    Fix: unsubscribe both events in NoteTab.Dispose().

4d. MainWindow.SetupEventHandlers() — tabManager.OnDataChanged, OnStatusUpdateRequested,
    OnLogDebug, OnLogError, OnSettingsNeedUpdate all subscribed. Are any of these
    unsubscribed in MainWindow_Closing? Currently only themeManager.OnThemeChanged
    is unsubscribed. TabManager lives as long as MainWindow (it's a field, not a
    singleton), so cross-reference leaks are unlikely. But verify TabManager doesn't
    get passed to any singleton that outlives MainWindow.

4e. MainWindow.settingsManager.OnSettingsChanged lambda — captures `this` (MainWindow)
    via `loggingService`, `tabManager`, `currentSettings`, `hasUnsavedChanges`.
    If SettingsManager outlives MainWindow (e.g. stored in a static collection),
    MainWindow cannot be GC'd. Is SettingsManager disposed in MainWindow_Closing?
    (`settingsManager?.Dispose()` — yes, confirmed). Does SettingsManager.Dispose()
    clear all event subscribers? Verify.

════════════════════════════════════════════════════════════════
SECTION 5 — VISUAL TREE & WPANEL ACCUMULATION
════════════════════════════════════════════════════════════════

5a. MediaSection — WrapPanel has NO virtualization. Every image = one Grid container
    always in memory, always in visual tree, even when scrolled out of view.
    With 30 images per tab × (thumbnail BitmapImage + 3 TextBlocks + nested Grid +
    ContextMenu with 8 MenuItems + DropShadowEffect) = significant retained memory.
    ContextMenus are instantiated eagerly (not lazily) in SetupContainerInteractions().
    30 images = 30 full ContextMenu objects with 8 MenuItem children each = 240
    MenuItem objects always in memory.
    Report: current item count per tab, estimated container memory per image.

5b. MediaSection.ShowInsertionIndicator() — creates a new Border with DropShadowEffect
    on every drag position change (every mouse move pixel). RemoveInsertionIndicator()
    removes it from the panel but DropShadowEffect is a Freezable holding GPU-managed
    resources. Is it explicitly disposed? Check if Border.Effect = null before removal
    and if DropShadowEffect implements IDisposable.

5c. MediaSection.CreateDragVisual() — creates a RotateTransform + DropShadowEffect
    per drag operation. These are cleaned up in CleanupDragOperation via
    dragCanvas.Children.Remove(dragVisual). DropShadowEffect not explicitly disposed.
    Minor but worth noting.

5d. ImageViewerWindow — opened via ShowFullSizeImage() with no singleton guard or
    limit. User can open unlimited concurrent ImageViewerWindows. Each holds a
    full-res BitmapImage in memory. 10 concurrent viewers = 10× full-res images.
    Report how many viewers can be open simultaneously and whether any limit exists.

════════════════════════════════════════════════════════════════
SECTION 6 — STATUS BAR & PERIODIC ALLOCATION
════════════════════════════════════════════════════════════════

6a. StatusBarManager — UpdateStatusBar() called every 1 second. If it calls
    `new SolidColorBrush(...)` anywhere (for save status color), that's one new
    SolidColorBrush object per second, forever. They are GC-collectable but create
    constant GC pressure. Cache static instances or use frozen brushes.
    Read UI/StatusBarManager.cs and report exact allocation sites.

6b. MainWindow.UpdateStatusBar() → statusBarManager.UpdateStatusBar() →
    tabManager.SelectedTab?.Content?.TextContent. TextContent getter on TextSection
    reads from RichTextBox — does it serialize RTF on every call? If so, that's
    RTF serialization happening 1x/second just for word count. Read TextSection.xaml.cs
    and report.

════════════════════════════════════════════════════════════════
SECTION 7 — WPFANIMATEDGIF LIBRARY LEAKS
════════════════════════════════════════════════════════════════

7a. WpfAnimatedGif library (ImageBehavior) — attached properties hold animation
    controllers internally. When ImageBehavior.SetAnimatedSource(DisplayImage, null)
    is called, does the library release:
    - The AnimationController object
    - All decoded GIF frames (could be 30-100 BitmapFrames for an animated GIF)
    - The DispatcherTimer used for frame advance
    Search for how WpfAnimatedGif handles cleanup. If it doesn't release frames on
    null source, every GIF navigation in ImageViewerWindow = 30-100 retained BitmapFrames.
    A 1MB GIF could expand to 30MB of decoded frames in memory.

7b. ImageBehavior.GetAnimationController(DisplayImage) in the GIF pause toggle handler
    — AnimationController is retrieved but never explicitly released or disposed.
    Does AnimationController implement IDisposable? Is it safe to let GC handle it?

════════════════════════════════════════════════════════════════
SECTION 8 — LOGGING & DEBUG OVERHEAD
════════════════════════════════════════════════════════════════

8a. MediaSection.ShowFullSizeImage() — contains 8+ System.Diagnostics.Debug.WriteLine
    calls including string interpolation. In Release builds, Debug.WriteLine is compiled
    out (it's a [Conditional("DEBUG")] method). But the string interpolations ARE evaluated
    before passing to Debug.WriteLine — the compiler does NOT skip them in Release.
    This means string allocations happen even in Release builds. Wrap in
    `#if DEBUG` or use `if (System.Diagnostics.Debugger.IsAttached)` guard.
    Count total Debug.WriteLine calls in MediaSection.xaml.cs.

8b. MediaSection.LoadThumbnailAsync, RebuildUIFromData, etc — multiple Debug.WriteLine
    with $"string interpolation". Same issue. Report total count in all UI files.

════════════════════════════════════════════════════════════════
SECTION 9 — LARGE OBJECT HEAP (LOH) CONCERNS
════════════════════════════════════════════════════════════════

9a. BitmapImage objects > 85KB go on the Large Object Heap (LOH). LOH is NOT
    compacted by default in .NET. Objects allocated on LOH that are freed leave
    holes — this is LOH fragmentation. Over a session of loading/unloading many
    full-res images (each likely 5-50MB decoded), LOH can grow and never shrink
    even after images are "freed."
    Estimate: what is the typical decoded size of images users work with?
    A 1920×1080 PNG = 1920 × 1080 × 4 bytes = ~8MB per image on LOH.
    Report: is GCSettings.LargeObjectHeapCompactionMode ever set?
    Is any explicit GC triggered (we removed GC.Collect — correct — but LOH
    fragmentation is a separate concern worth noting)?

9b. ImageCacheManager stores BitmapImage references in a LinkedList<CacheEntry>.
    Each BitmapImage > 85KB is on the LOH. LinkedList nodes themselves are tiny
    (heap), but the Bitmap data they reference is LOH. When entries are evicted,
    the LOH hole remains. With 100-item cache of 8MB images = up to 800MB LOH
    reserved, fragmented. Consider reducing MaxCachedImages for full-res cache
    (path:full entries) to 5-10 instead of 100.

════════════════════════════════════════════════════════════════
SECTION 10 — DISPOSAL COMPLETENESS AUDIT
════════════════════════════════════════════════════════════════

For each IDisposable in the codebase, verify Dispose() is called at the right time:

10a. SemaphoreSlim _loadSemaphore (MediaSection) — Dispose() called in MediaSection.Dispose(). 
     But are all tasks blocked on WaitAsync() properly unblocked before dispose?
     If tasks are waiting and semaphore is disposed, they throw ObjectDisposedException.
     Is this caught?

10b. CancellationTokenSource _pendingLoadsCts (MediaSection) — Dispose() called in
     MediaSection.Dispose(). But _pendingLoadsCts is replaced (not disposed) on every
     EnsureThumbnailLoaded call. All old CTSes except the final one are never disposed.

10c. CancellationTokenSource _currentLoadCts (ImageViewerWindow) — Dispose() called
     in OnClosed and before each new load. This looks correct. Verify.

10d. WindowPositionTracker _positionTracker (MainWindow) — Dispose() called in
     MainWindow_Closing. Verify it unsubscribes LocationChanged and SizeChanged.
     (Previously confirmed in audit as CLEAN — verify still true post-Hygiene-1.)

10e. LoggingService — is it IDisposable? Does it hold a Serilog ILogger that needs
     Dispose()? If yes, is it disposed in MainWindow_Closing?

10f. SettingsManager — settingsManager?.Dispose() called in MainWindow_Closing.
     Does SettingsManager.Dispose() actually clear OnSettingsChanged subscribers?

════════════════════════════════════════════════════════════════
OUTPUT REQUIREMENTS
════════════════════════════════════════════════════════════════

After reading ALL relevant files:

1. For EACH section above, report every finding in FINDING FORMAT.
2. After all findings, produce a PRIORITY TABLE:

| ID | File | Type | IMPACT | EFFORT | RAM Effect |
|----|------|------|--------|--------|------------|
| LEAK-1 | ... | BUG | HIGH | Trivial | ~5MB per open/close |
...sorted HIGH → LOW impact.

3. Produce a QUICK WIN LIST — findings fixable in < 10 min each that together
   should recover the most RAM.

4. Produce an ESTIMATE:
   - Current idle RAM: ~300MB
   - Expected idle RAM after all fixes: ~XMB
   - Biggest single fix (which LEAK-N) recovers the most.

Do NOT write any code changes yet. Do NOT modify any files.
Output the full report, then stop and wait for approval.

════════════════════════════════════════════════════════════════
CONTEXT — ARCHITECTURE RULES (do not violate in fixes)
════════════════════════════════════════════════════════════════

- Single Source of Truth: master.json
- Media stored in %AppData%\SnipShottyBoard\images\
- LRU cache: ImageCacheManager singleton (count + byte eviction)
- No GC.Collect() calls allowed (removed in Hygiene-3 — keep it out)
- DarkTheme.xaml = source of truth for all colors, never inline hex
- TabManager.cs and MediaSection.xaml.cs are protected — surgical edits only
- Build gate: dotnet build must pass 0 errors 0 warnings after any fix
- Write devnote to docs/devnotes/ after each fix using naming convention:
  2026-05-19-sprint-hygiene-N-issueN-short-name.md
