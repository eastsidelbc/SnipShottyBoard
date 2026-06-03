using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;
using WpfAnimatedGif;
using SnipShottyBoard.Core.Utils;
using SnipShottyBoard.Infrastructure.Logging;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 🖼️ Custom Image Viewer Window with toolbar functionality, zoom/pan, and GIF controls
    /// </summary>
    public partial class ImageViewerWindow : Wpf.Ui.Controls.FluentWindow
    {
        private const string FullResCacheSuffix = ":full";

        private string currentImagePath;
        private BitmapImage currentImage;
        private Action<string> onImageDeleted;

        public string CurrentImagePath => currentImagePath;

        // 🖼️ Navigation support
        private List<string> allImagePaths;
        private int currentImageIndex;

        // 🔍 Zoom & Pan state
        private double currentZoomLevel = 1.0;
        private const double MIN_ZOOM = 0.25;
        private const double MAX_ZOOM = 5.0;
        private bool _isInFitMode = true;   // true = image tracks window size on resize
        private bool isGifPaused = false;
        private Point _mouseDownPos;
        private bool _isMouseDragging = false;
        private double _panStartScrollH;
        private double _panStartScrollV;
        private DispatcherTimer? _gifStatusTimer;

        // 🐛 Debug image logging (Sprint D.1a) — off in Release builds
#if DEBUG
        private static bool debugImageLogging = true;
#else
        private static bool debugImageLogging = false;
#endif
        private int imageLoadSession = 0;

        // 🛑 Cancel stale loads on rapid navigation
        private CancellationTokenSource? _currentLoadCts;

        public ImageViewerWindow()
        {
            InitializeComponent();
            WindowChromeFix.Apply(this, "ContentCardBrush");
            SetupWindow();
        }

        public ImageViewerWindow(string imagePath) : this()
        {
            LoadImage(imagePath);
        }

        public ImageViewerWindow(string imagePath, Action<string> onImageDeleted) : this()
        {
            this.onImageDeleted = onImageDeleted;
            LoadImage(imagePath);
        }

        // 🖼️ Constructor with navigation support
        public ImageViewerWindow(string imagePath, List<string> allImagePaths, int currentIndex, Action<string> onImageDeleted) : this()
        {
            this.onImageDeleted = onImageDeleted;
            this.allImagePaths = allImagePaths ?? new List<string>();
            this.currentImageIndex = currentIndex;
            LoadImage(imagePath);
        }

        // 🔧 Setup window properties and behavior
        private void SetupWindow()
        {
            this.MinWidth = SnipShottyBoard.Data.AppConstants.ImageViewerMinWidth;
            this.MinHeight = SnipShottyBoard.Data.AppConstants.ImageViewerMinHeight;
            
            this.KeyDown += (s, e) =>
            {
                switch (e.Key)
                {
                    case Key.Escape: this.Close(); break;
                    case Key.C when Keyboard.Modifiers == ModifierKeys.Control: CopyImageToClipboard(); break;
                    case Key.Delete: DeleteCurrentImage(); break;
                    case Key.Left: NavigateToPreviousImage(); break;
                    case Key.Right: NavigateToNextImage(); break;
                }
            };

            this.SizeChanged += (s, e) =>
            {
                UpdateImageInfo();
                if (_isInFitMode && currentImage != null)
                    Dispatcher.BeginInvoke(new Action(() => FitToWindow()), DispatcherPriority.Background);
            };
            // Re-focus the window every time it becomes active so arrow-key navigation
            // always works even after clicking away and back.
            this.Activated += (s, e) => this.Focus();
            this.Focusable = true;
            this.Focus();
        }

        private void LogImage(string message, Exception? ex = null)
        {
            if (!debugImageLogging) return;
            var filename = Path.GetFileName(currentImagePath) ?? "unknown";
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var sessionTag = $"IMG-sess{imageLoadSession}-{filename}";

            if (ex != null)
                LoggingService.LogErrorStatic($"[{sessionTag}] Thread={threadId} — {message}", ex, "ImageLoad");
            else
                LoggingService.LogDebugStatic($"[{sessionTag}] Thread={threadId} — {message}", "ImageLoad");
        }

        private void ClearPreviousImage()
        {
            if (string.IsNullOrEmpty(currentImagePath)) return;
            try
            {
                var ctrl = ImageBehavior.GetAnimationController(DisplayImage);
                ctrl?.Dispose(); // releases BitmapDecoder + all decoded GIF frame buffers
                ImageBehavior.SetAnimatedSource(DisplayImage, null);
            }
            catch { /* no prior GIF */ }
            DisplayImage.Source = null;
            currentImage = null;
            ImageCacheManager.Instance.RemoveFromCache(currentImagePath + FullResCacheSuffix);
            currentZoomLevel = 1.0;
            _isInFitMode = false;
        }

        // 🖼️ Load and display an image (supports animated GIFs)
        public void LoadImage(string imagePath)
        {
            try
            {
                _currentLoadCts?.Cancel();
                _currentLoadCts?.Dispose();
                _currentLoadCts = null;

                ClearPreviousImage();

                imageLoadSession++;
                currentImagePath = imagePath;
                isGifPaused = false;
                UpdateNavigationButtons();
                LogImage("📥 LoadImage START");

                if (!File.Exists(imagePath))
                {
                    ShowError("Image file not found.");
                    return;
                }

                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                LogImage($"Format detected: {extension}");

                if (extension == ".gif")
                    LoadGif(imagePath);
                else
                    LoadStaticAsync(imagePath);
            }
            catch (Exception ex)
            {
                LogImage("💥 Exception in LoadImage", ex);
                ShowError($"Failed to load image: {ex.Message}");
            }
        }

        private async void LoadStaticAsync(string imagePath)
        {
            var cacheKey = imagePath + FullResCacheSuffix;
            BitmapImage? cached = ImageCacheManager.Instance.GetFromCache(cacheKey);
            if (cached != null)
            {
                LogImage("⚡ Cache HIT for static image");
                ApplyStaticImage(cached, imagePath);
                return;
            }

            LogImage("🔄 Cache MISS, decoding on background thread");
            _currentLoadCts = new CancellationTokenSource();
            var cts = _currentLoadCts;

            var bitmap = await Task.Run(() =>
            {
                if (cts.IsCancellationRequested) return null;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(imagePath, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            });

            if (cts.IsCancellationRequested || bitmap == null)
            {
                LogImage("⚠️ Load cancelled — navigation moved on");
                cts.Dispose();
                _currentLoadCts = null;
                return;
            }

            Dispatcher.Invoke(() =>
            {
                if (currentImagePath == imagePath)
                {
                    ImageCacheManager.Instance.AddToCache(cacheKey, bitmap);
                    ApplyStaticImage(bitmap, imagePath);
                }
            });
        }

        private async void LoadGif(string imagePath)
        {
            try
            {
                // Read file bytes on background thread — keeps UI responsive on large GIFs
                byte[] gifBytes = await Task.Run(() => File.ReadAllBytes(imagePath));

                // If the user navigated away while we were loading, discard
                if (currentImagePath != imagePath) return;

                // Create BitmapImage from memory stream on UI thread (required by WPF)
                var ms = new MemoryStream(gifBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnDemand; // frames decoded on demand during playback
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                // Do NOT freeze — GIF animation requires mutable bitmap

                currentImagePath = imagePath;
                currentImage = bitmap;
                LogImage($"BitmapImage created, size={bitmap.PixelWidth}x{bitmap.PixelHeight}");

                RenderOptions.SetBitmapScalingMode(DisplayImage, BitmapScalingMode.Unspecified);
                DisplayImage.SnapsToDevicePixels = false;
                DisplayImage.UseLayoutRounding = false;

                try { ImageBehavior.SetAnimatedSource(DisplayImage, null); } catch { /* safe */ }
                ImageBehavior.SetAnimatedSource(DisplayImage, bitmap);
                LogImage("✅ SetAnimatedSource complete");

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

        private void ApplyStaticImage(BitmapImage bitmap, string imagePath)
        {
            currentImage = bitmap;
            DisplayImage.Source = bitmap;
            var fileName = Path.GetFileName(imagePath);
            this.Title = $"🖼️ {fileName}";

            AutoSizeWindow(bitmap);
            Dispatcher.BeginInvoke(new Action(() => ApplyOneToOne()), DispatcherPriority.Loaded);
        }

        // 📐 Set window size to show image at original resolution (within screen limits)
        private void AutoSizeWindow(BitmapImage bitmap)
        {
            try
            {
                var screenWidth = SystemParameters.WorkArea.Width * SnipShottyBoard.Data.AppConstants.ScreenUsageRatio;
                var screenHeight = SystemParameters.WorkArea.Height * SnipShottyBoard.Data.AppConstants.ScreenUsageRatio;

                var chromeHeight = SnipShottyBoard.Data.AppConstants.WindowChromeHeight;
                var chromeWidth = SnipShottyBoard.Data.AppConstants.WindowChromeWidth;

                var desiredWindowWidth = bitmap.PixelWidth + chromeWidth;
                var desiredWindowHeight = bitmap.PixelHeight + chromeHeight;

                this.Width = Math.Max(this.MinWidth, Math.Min(desiredWindowWidth, screenWidth));
                this.Height = Math.Max(this.MinHeight, Math.Min(desiredWindowHeight, screenHeight));

                this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
                this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;
            }
            catch
            {
                this.Width = SnipShottyBoard.Data.AppConstants.DefaultImageViewerWidth;
                this.Height = SnipShottyBoard.Data.AppConstants.DefaultImageViewerHeight;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }

        // 🔍 Show image at 1:1 (actual pixels) — default view when opening or navigating
        private void ApplyOneToOne()
        {
            currentZoomLevel = 1.0;
            _isInFitMode = false;
            ApplyCurrentZoom();
            UpdateImageInfo();
        }

        // 🔍 Fit image to current window client area (used when _isInFitMode == true on resize)
        private void FitToWindow()
        {
            if (currentImage == null) return;

            double availWidth = ImageScrollViewer.ViewportWidth - 20;
            double availHeight = ImageScrollViewer.ViewportHeight - 20;

            if (availWidth <= 0 || availHeight <= 0) return;

            double scaleX = availWidth / currentImage.PixelWidth;
            double scaleY = availHeight / currentImage.PixelHeight;

            currentZoomLevel = Math.Min(scaleX, scaleY);
            currentZoomLevel = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, currentZoomLevel));
            _isInFitMode = true;

            ApplyCurrentZoom();
        }

        // 🔍 Apply the current zoom level to the image dimensions
        private void ApplyCurrentZoom()
        {
            if (currentImage == null) return;
            ApplyCurrentZoom(center: true);
        }

        private void ApplyCurrentZoom(bool center)
        {
            if (currentImage == null) return;

            DisplayImage.Width = currentImage.PixelWidth * currentZoomLevel;
            DisplayImage.Height = currentImage.PixelHeight * currentZoomLevel;

            if (center)
            {
                Dispatcher.BeginInvoke(new Action(() => {
                    if (DisplayImage.ActualWidth > 0)
                    {
                        ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.ScrollableWidth / 2);
                        ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.ScrollableHeight / 2);
                    }
                }), DispatcherPriority.Render);
            }

            UpdateStatusZoom();
        }

        // 🔍 Zoom at a specific mouse position — keeps the point under cursor pinned
        private void ApplyZoomAtPoint(Point mouseViewportPos)
        {
            if (currentImage == null) return;

            double oldWidth = DisplayImage.ActualWidth;
            double oldHeight = DisplayImage.ActualHeight;

            if (oldWidth <= 0 || oldHeight <= 0)
            {
                ApplyCurrentZoom(center: true);
                return;
            }

            // Fractional anchor point in the image under the mouse (zoom-invariant)
            Point mousePosInImage = Mouse.GetPosition(DisplayImage);
            double fracX = Math.Clamp(mousePosInImage.X / oldWidth, 0.0, 1.0);
            double fracY = Math.Clamp(mousePosInImage.Y / oldHeight, 0.0, 1.0);

            double newWidth = currentImage.PixelWidth * currentZoomLevel;
            double newHeight = currentImage.PixelHeight * currentZoomLevel;

            DisplayImage.Width = newWidth;
            DisplayImage.Height = newHeight;

            // Snapshot scroll offsets before dispatch (used to resolve image origin in viewport)
            double snapOffsetX = ImageScrollViewer.HorizontalOffset;
            double snapOffsetY = ImageScrollViewer.VerticalOffset;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DisplayImage.ActualWidth <= 0) return;

                // Image origin in viewport space — gives us (padding - scrollOffset)
                // which is scroll-position-independent for computing content coords
                Point imageTopLeft = DisplayImage.TransformToAncestor(ImageScrollViewer).Transform(new Point(0, 0));

                // New scroll: keeps (fracX*newWidth, fracY*newHeight) under the mouse
                double newOffsetX = snapOffsetX + imageTopLeft.X + fracX * newWidth - mouseViewportPos.X;
                double newOffsetY = snapOffsetY + imageTopLeft.Y + fracY * newHeight - mouseViewportPos.Y;

                newOffsetX = Math.Max(0, Math.Min(ImageScrollViewer.ScrollableWidth, newOffsetX));
                newOffsetY = Math.Max(0, Math.Min(ImageScrollViewer.ScrollableHeight, newOffsetY));

                ImageScrollViewer.ScrollToHorizontalOffset(newOffsetX);
                ImageScrollViewer.ScrollToVerticalOffset(newOffsetY);
            }), DispatcherPriority.Render);

            UpdateStatusZoom();
        }

        // 📋 Copy image to clipboard
        private void CopyImageToClipboard()
        {
            try
            {
                if (currentImage != null)
                {
                    Clipboard.SetImage(currentImage);
                    ShowTemporaryMessage("📋 Image copied to clipboard!");
                }
                else
                    ShowError("No image to copy.");
            }
            catch (Exception ex)
            {
                ShowError($"Failed to copy image: {ex.Message}");
            }
        }

        private void ShowTemporaryMessage(string message)
        {
            StatusFileName.Text = message;
            StatusFileName.Foreground = new SolidColorBrush(Colors.Green);
        }

        private void ShowError(string message)
        {
            StatusFileName.Text = $"❌ {message}";
            StatusFileName.Foreground = new SolidColorBrush(Colors.Red);
            StatusDimensions.Text = "Error";
            StatusFileSize.Text = "Error";
            StatusDateTime.Text = "Error";
        }

        // 🖼️ Navigate to previous image
        private void NavigateToPreviousImage()
        {
            if (allImagePaths == null || allImagePaths.Count <= 1) return;
            currentImageIndex = (currentImageIndex - 1 + allImagePaths.Count) % allImagePaths.Count;
            LoadImage(allImagePaths[currentImageIndex]);
        }

        // 🖼️ Navigate to next image
        private void NavigateToNextImage()
        {
            if (allImagePaths == null || allImagePaths.Count <= 1) return;
            currentImageIndex = (currentImageIndex + 1) % allImagePaths.Count;
            LoadImage(allImagePaths[currentImageIndex]);
        }

        // 🔄 Show/hide Prev/Next buttons based on whether multiple images are available
        private void UpdateNavigationButtons()
        {
            bool hasMultiple = allImagePaths != null && allImagePaths.Count > 1;
            PrevButton.Visibility = hasMultiple ? Visibility.Visible : Visibility.Collapsed;
            NextButton.Visibility = hasMultiple ? Visibility.Visible : Visibility.Collapsed;
        }

        // 📊 Update status bar information display
        private void UpdateImageInfo()
        {
            try
            {
                if (currentImage != null && !string.IsNullOrEmpty(currentImagePath))
                {
                    var fileInfo = new FileInfo(currentImagePath);
                    var fileSizeKB = Math.Round(fileInfo.Length / SnipShottyBoard.Data.AppConstants.BytesToKB, 1);
                    
                    StatusFileName.Text = Path.GetFileName(currentImagePath);
                    StatusFileName.Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush");
                    
                    var displayWidth = (int)DisplayImage.ActualWidth;
                    var displayHeight = (int)DisplayImage.ActualHeight;
                    
                    if (displayWidth > 0 && displayHeight > 0 && 
                        (displayWidth != currentImage.PixelWidth || displayHeight != currentImage.PixelHeight))
                    {
                        StatusDimensions.Text = $"{currentImage.PixelWidth} × {currentImage.PixelHeight} px ({displayWidth} × {displayHeight})";
                    }
                    else
                    {
                        StatusDimensions.Text = $"{currentImage.PixelWidth} × {currentImage.PixelHeight} px";
                    }
                    
                    StatusFileSize.Text = $"{fileSizeKB} KB";
                    var fileDate = fileInfo.LastWriteTime;
                    StatusDateTime.Text = fileDate.ToString("M/d/yyyy h:mm tt");

                    UpdateStatusZoom();
                }
                else
                {
                    StatusFileName.Text = "No image loaded";
                    StatusDimensions.Text = "-- × -- px";
                    StatusFileSize.Text = "-- KB";
                    StatusDateTime.Text = "--";
                    StatusZoom.Text = "Fit";
                }
            }
            catch { /* ignore */ }
        }

        private void UpdateStatusZoom()
        {
            if (_isInFitMode)
                StatusZoom.Text = $"Fit ({(currentZoomLevel * 100):F0}%)";
            else if (Math.Abs(currentZoomLevel - 1.0) < 0.01)
                StatusZoom.Text = "100% (1:1)";
            else
                StatusZoom.Text = $"{(currentZoomLevel * 100):F0}%";
        }

        #region UI Event Handlers

        private void CopyButton_Click(object sender, RoutedEventArgs e) => CopyImageToClipboard();
        private void DeleteButton_Click(object sender, RoutedEventArgs e) => DeleteCurrentImage();

        private void PrevButton_Click(object sender, RoutedEventArgs e) => NavigateToPreviousImage();
        private void NextButton_Click(object sender, RoutedEventArgs e) => NavigateToNextImage();

        private void FitActualButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentImage == null) return;
            AutoSizeWindow(currentImage);   // resize window back to original dimensions
            _isInFitMode = false;
            currentZoomLevel = 1.0;
            // Defer so the window layout finishes before we center the scroll
            Dispatcher.BeginInvoke(new Action(() => ApplyCurrentZoom()), DispatcherPriority.Loaded);
        }

        // 🔍 Mouse Wheel Zoom — zooms around cursor position, keeping point under mouse pinned
        private void ImageScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (currentImage == null) return;
            e.Handled = true;
            _isInFitMode = false;

            double factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
            currentZoomLevel = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, currentZoomLevel * factor));

            var mousePos = Mouse.GetPosition(ImageScrollViewer);
            ApplyZoomAtPoint(mousePos);
        }

        // ⏸️ GIF Click Toggle, Double-Click Zoom, and Mouse-Drag Pan
        private void DisplayImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // WPF Image doesn't expose MouseDoubleClick in XAML — detect via ClickCount
            if (e.ClickCount > 1)
            {
                _isInFitMode = false;
                currentZoomLevel = 1.0;
                ApplyCurrentZoom();
                e.Handled = true;
                return;
            }

            // Record pan start position relative to the ScrollViewer viewport (stable reference frame)
            _mouseDownPos = Mouse.GetPosition(ImageScrollViewer);
            _panStartScrollH = ImageScrollViewer.HorizontalOffset;
            _panStartScrollV = ImageScrollViewer.VerticalOffset;
            _isMouseDragging = false;
            (sender as UIElement)?.CaptureMouse();
        }

        private void DisplayImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !((sender as UIElement)?.IsMouseCaptured ?? false)) return;

            var currentPos = Mouse.GetPosition(ImageScrollViewer);
            double deltaX = _mouseDownPos.X - currentPos.X;
            double deltaY = _mouseDownPos.Y - currentPos.Y;

            if (Math.Abs(deltaX) > 3 || Math.Abs(deltaY) > 3)
                _isMouseDragging = true;

            ImageScrollViewer.ScrollToHorizontalOffset(_panStartScrollH + deltaX);
            ImageScrollViewer.ScrollToVerticalOffset(_panStartScrollV + deltaY);
        }

        private void DisplayImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool wasDragging = _isMouseDragging;
            _isMouseDragging = false;
            (sender as UIElement)?.ReleaseMouseCapture();

            // Skip single-click logic for double-click sequence or if the user dragged
            if (e.ClickCount > 1 || wasDragging) return;

            var currentPos = Mouse.GetPosition(ImageScrollViewer);
            double dist = Math.Sqrt(Math.Pow(currentPos.X - _mouseDownPos.X, 2) + Math.Pow(currentPos.Y - _mouseDownPos.Y, 2));

            // If moved < 5px from press point, treat as a click → toggle GIF pause
            if (dist < 5.0 && currentImagePath != null && Path.GetExtension(currentImagePath).ToLowerInvariant() == ".gif")
            {
                isGifPaused = !isGifPaused;
                try
                {
                    var controller = ImageBehavior.GetAnimationController(DisplayImage);
                    if (isGifPaused)
                        controller?.Pause();
                    else
                        controller?.Play();
                }
                catch { /* ignore */ }

                StatusZoom.Text = isGifPaused ? "⏸️ GIF Paused" : "▶️ Playing";
                _gifStatusTimer?.Stop();
                if (_gifStatusTimer == null)
                {
                    _gifStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
                    _gifStatusTimer.Tick += (s, args) =>
                    {
                        _gifStatusTimer.Stop();
                        UpdateStatusZoom();
                    };
                }
                _gifStatusTimer.Start();
            }
        }

        private void DisplayImage_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // System stole capture (dialog, Alt-Tab, etc.) — reset drag state so nothing stays locked
            _isMouseDragging = false;
        }

        #endregion

        #region Internal Logic

        private void DeleteCurrentImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(currentImagePath))
                {
                    var pathToDelete = currentImagePath;
                    ImageCacheManager.Instance.RemoveAllForPath(pathToDelete);
                    if (onImageDeleted != null) onImageDeleted.Invoke(pathToDelete);
                    Close();

                    Task.Run(() =>
                    {
                        try
                        {
                            if (File.Exists(pathToDelete)) File.Delete(pathToDelete);
                        }
                        catch { /* ignore */ }
                    });
                }
                else Close();
            }
            catch { Close(); }
        }

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

        #endregion

        #region Window Lifecycle

        protected override void OnClosed(EventArgs e)
        {
            _currentLoadCts?.Cancel();
            _currentLoadCts?.Dispose();
            _currentLoadCts = null;

            ReleaseImageResources();
            base.OnClosed(e);

            // Reclaim LOH memory from full-res BitmapImages on a background thread.
            // Large objects (>85KB) live on the LOH which is not compacted by default.
            // Without this, Task Manager shows committed RAM ~117MB above baseline after close.
            Task.Run(() =>
            {
                System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                    System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            });
        }

        #endregion
    }
}