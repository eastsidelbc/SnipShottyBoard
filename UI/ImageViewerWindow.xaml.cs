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
using System.Windows.Threading;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 🖼️ Custom Image Viewer Window with toolbar functionality
    ///
    /// KEY FEATURES:
    /// - Window automatically sizes to fit the image at original resolution (within screen limits)
    /// - Image scales automatically when you resize the window
    /// - Copy and delete functionality with keyboard shortcuts
    /// - Clean, modern UI with status bar showing image details
    /// - LRU cache integration for static images (keyed by path:full)
    /// - GIFs load with full animation, skip cache
    /// </summary>
    public partial class ImageViewerWindow : Window
    {
        private const string FullResCacheSuffix = ":full";

        private string currentImagePath;
        private BitmapImage currentImage;
        private Action<string> onImageDeleted; // Callback when image is deleted

        // 🖼️ Navigation support
        private List<string> allImagePaths;
        private int currentImageIndex;

        public ImageViewerWindow()
        {
            InitializeComponent();
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
            // 🎨 Apply theme-aware sizing
            this.MinWidth = SnipShottyBoard.Data.AppConstants.ImageViewerMinWidth;
            this.MinHeight = SnipShottyBoard.Data.AppConstants.ImageViewerMinHeight;
            
            // 🔑 Handle keyboard shortcuts
            this.KeyDown += (s, e) =>
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        this.Close();
                        break;
                    case Key.C when Keyboard.Modifiers == ModifierKeys.Control:
                        CopyImageToClipboard();
                        break;
                    case Key.Delete:
                        DeleteCurrentImage();
                        break;
                    case Key.Left:
                        NavigateToPreviousImage();
                        break;
                    case Key.Right:
                        NavigateToNextImage();
                        break;
                }
            };

            // 📏 Handle window resize to update scaling info
            this.SizeChanged += (s, e) =>
            {
                UpdateImageInfo();
            };

            // 🎯 Set focus to allow keyboard shortcuts
            this.Focusable = true;
            this.Focus();
        }

        // 🖼️ Load and display an image (supports animated GIFs)
        public void LoadImage(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    ShowError("Image file not found.");
                    return;
                }

                currentImagePath = imagePath;
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();

                if (extension == ".gif")
                    LoadGifAsync(imagePath);
                else
                    LoadStaticAsync(imagePath);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load image: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a static image asynchronously on a background thread, then apply on UI thread.
        /// Checks the LRU cache first; caches on hit/miss using key "path:full".
        /// </summary>
        private async void LoadStaticAsync(string imagePath)
        {
            var cacheKey = imagePath + FullResCacheSuffix;

            // Check cache on UI thread
            BitmapImage? cached = ImageCacheManager.Instance.GetFromCache(cacheKey);
            if (cached != null)
            {
                ApplyImage(cached, imagePath);
                return;
            }

            // Decode on background thread
            var bitmap = await Task.Run(() =>
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(imagePath, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            });

            // Cache and apply on UI thread
            Dispatcher.Invoke(() =>
            {
                ImageCacheManager.Instance.AddToCache(cacheKey, bitmap);
                ApplyImage(bitmap, imagePath);
            });
        }

        /// <summary>
        /// Load a GIF with full animation support.
        /// GIFs skip the cache since their BitmapImages can't be frozen.
        /// Falls back to static loading if animation fails.
        /// </summary>
        private async void LoadGifAsync(string imagePath)
        {
            try
            {
                var bitmap = await Task.Run(() =>
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnDemand;
                    bmp.CreateOptions = BitmapCreateOptions.None;
                    bmp.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bmp.EndInit();
                    return bmp;
                });

                Dispatcher.Invoke(() =>
                {
                    RenderOptions.SetBitmapScalingMode(DisplayImage, BitmapScalingMode.Unspecified);
                    DisplayImage.SnapsToDevicePixels = false;
                    DisplayImage.UseLayoutRounding = false;

                    ApplyImage(bitmap, imagePath);
                });
            }
            catch
            {
                // Fallback: load GIF as a static first-frame image
                LoadStaticAsync(imagePath);
            }
        }

        /// <summary>
        /// Assign the bitmap to the UI and update window state.
        /// Must be called on the UI thread.
        /// </summary>
        private void ApplyImage(BitmapImage bitmap, string imagePath)
        {
            currentImage = bitmap;
            DisplayImage.Source = bitmap;

            var fileName = Path.GetFileName(imagePath);
            this.Title = $"🖼️ {fileName}";

            UpdateImageInfo();
            AutoSizeWindow(bitmap);
        }

        // 📐 Set window size to show image at original resolution (within screen limits)
        private void AutoSizeWindow(BitmapImage bitmap)
        {
            try
            {
                // 📺 Get screen dimensions (leave some margin)
                var screenWidth = SystemParameters.WorkArea.Width * SnipShottyBoard.Data.AppConstants.ScreenUsageRatio; // 90% of screen width
                var screenHeight = SystemParameters.WorkArea.Height * SnipShottyBoard.Data.AppConstants.ScreenUsageRatio; // 90% of screen height

                // 🎯 Calculate window chrome overhead
                var chromeHeight = SnipShottyBoard.Data.AppConstants.WindowChromeHeight; // Title bar + toolbar + status bar
                var chromeWidth = SnipShottyBoard.Data.AppConstants.WindowChromeWidth; // Side margins

                // 📏 Calculate desired window size based on original image size
                var desiredWindowWidth = bitmap.PixelWidth + chromeWidth;
                var desiredWindowHeight = bitmap.PixelHeight + chromeHeight;

                // 🔄 Set window size (prioritize showing original image size)
                this.Width = Math.Max(this.MinWidth, Math.Min(desiredWindowWidth, screenWidth));
                this.Height = Math.Max(this.MinHeight, Math.Min(desiredWindowHeight, screenHeight));

                // 🎯 Center window on screen
                this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
                this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;
            }
            catch
            {
                // 🛡️ Fallback to reasonable default size if calculation fails
                this.Width = SnipShottyBoard.Data.AppConstants.DefaultImageViewerWidth;
                this.Height = SnipShottyBoard.Data.AppConstants.DefaultImageViewerHeight;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }

        // 📋 Copy image to clipboard
        private void CopyImageToClipboard()
        {
            try
            {
                if (currentImage != null)
                {
                    // Copy the image data to clipboard for pasting
                    Clipboard.SetImage(currentImage);
                    
                    // 🎉 Show feedback
                    ShowTemporaryMessage("📋 Image copied to clipboard!");
                }
                else
                {
                    ShowError("No image to copy.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to copy image: {ex.Message}");
            }
        }

        // 📣 Show temporary success message (lightweight - no timer needed)
        private void ShowTemporaryMessage(string message)
        {
            StatusFileName.Text = message;
            StatusFileName.Foreground = new SolidColorBrush(Colors.Green);
            
            // Note: Removed timer - message will be cleared when window closes or image updates
        }

        // ❌ Show error message
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

        // 📊 Update status bar information display
        private void UpdateImageInfo()
        {
            try
            {
                if (currentImage != null && !string.IsNullOrEmpty(currentImagePath))
                {
                    var fileInfo = new FileInfo(currentImagePath);
                    var fileSizeKB = Math.Round(fileInfo.Length / SnipShottyBoard.Data.AppConstants.BytesToKB, 1);
                    
                    // 📂 File name and path
                    var fileName = Path.GetFileName(currentImagePath);
                    StatusFileName.Text = fileName;
                    StatusFileName.Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush");
                    
                    // 📐 Image dimensions (with display size if different)
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
                    
                    // 💾 File size
                    StatusFileSize.Text = $"{fileSizeKB} KB";
                    
                    // 🕒 File creation/modification date
                    var fileDate = fileInfo.LastWriteTime;
                    StatusDateTime.Text = fileDate.ToString("M/d/yyyy h:mm tt");
                }
                else
                {
                    // 🚫 No image loaded
                    StatusFileName.Text = "No image loaded";
                    StatusDimensions.Text = "-- × -- px";
                    StatusFileSize.Text = "-- KB";
                    StatusDateTime.Text = "--";
                }
            }
            catch
            {
                // 🛡️ Ignore errors during info updates
                StatusFileName.Text = "Error loading info";
                StatusDimensions.Text = "Error";
                StatusFileSize.Text = "Error";
                StatusDateTime.Text = "Error";
            }
        }

        #region Event Handlers

        // 📋 Copy button clicked
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyImageToClipboard();
        }

        // 🗑️ Delete button clicked
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteCurrentImage();
        }



        // 🖱️ Handle title bar dragging for custom window chrome
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // ✕ Handle close button click
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 🗑️ Delete current image instantly (UI-first approach)
        private void DeleteCurrentImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(currentImagePath))
                {
                    var pathToDelete = currentImagePath;

                    // 🧹 Remove from LRU cache (both thumbnail and full-res variants)
                    ImageCacheManager.Instance.RemoveAllForPath(pathToDelete);

                    if (onImageDeleted != null)
                        onImageDeleted.Invoke(pathToDelete);

                    Close();

                    // 🗑️ Delete file asynchronously (non-blocking)
                    Task.Run(() =>
                    {
                        try
                        {
                            if (File.Exists(pathToDelete))
                                File.Delete(pathToDelete);
                        }
                        catch
                        {
                            // File may already be removed — ignore
                        }
                    });
                }
                else
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Delete failed: {ex.Message}");
                Close();
            }
        }
        
        // 🔓 Quick resource cleanup for window closing
        private void ReleaseImageResources()
        {
            try
            {
                // Quick cleanup - no blocking operations
                if (DisplayImage != null)
                {
                    DisplayImage.Source = null;
                }
                
                currentImage = null;
                currentImagePath = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error during resource cleanup: {ex.Message}");
            }
        }



        #endregion

        #region Window Lifecycle

        protected override void OnClosed(EventArgs e)
        {
            // 🧹 Clean up resources using unified method
            ReleaseImageResources();
            
            base.OnClosed(e);
        }

        #endregion
    }
} 