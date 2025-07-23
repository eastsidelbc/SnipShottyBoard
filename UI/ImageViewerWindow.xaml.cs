using System;
using System.IO;
using System.Linq;
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
    /// </summary>
    public partial class ImageViewerWindow : Window
    {
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
            this.MinWidth = 400;
            this.MinHeight = 300;
            
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
                var fileInfo = new FileInfo(imagePath);
                
                System.Diagnostics.Debug.WriteLine($"🖼️ Loading image: {imagePath}");
                System.Diagnostics.Debug.WriteLine($"📊 File size: {fileInfo.Length} bytes, Extension: {extension}");

                // 🎬 Special handling for animated GIFs
                if (extension == ".gif")
                {
                    System.Diagnostics.Debug.WriteLine($"🎬 Loading animated GIF: {imagePath}");
                    
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnDemand; // Don't freeze for animation
                        bitmap.CreateOptions = BitmapCreateOptions.None; // Allow animation
                        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                        bitmap.EndInit();
                        
                        // 🎬 Check if GIF is actually animated
                        var decoder = BitmapDecoder.Create(new Uri(imagePath, UriKind.Absolute), BitmapCreateOptions.None, BitmapCacheOption.OnDemand);
                        var frameCount = decoder.Frames.Count;
                        System.Diagnostics.Debug.WriteLine($"🎬 GIF frame count: {frameCount}");
                        
                        if (frameCount > 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Animated GIF detected with {frameCount} frames");
                            
                            // 🎬 Use BitmapImage directly for animation - this is what worked before
                            currentImage = bitmap;
                            DisplayImage.Source = bitmap;
                            System.Diagnostics.Debug.WriteLine($"🎬 Using BitmapImage for animated GIF (should animate automatically)");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ GIF has only {frameCount} frame - may not be animated");
                            currentImage = bitmap;
                            DisplayImage.Source = bitmap;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"✅ GIF loaded successfully");
                    }
                    catch (Exception gifEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ GIF loading error: {gifEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"🔍 Stack trace: {gifEx.StackTrace}");
                        
                        // Try alternative GIF loading method
                        System.Diagnostics.Debug.WriteLine($"🔄 Trying alternative GIF loading method");
                        try
                        {
                            LoadGifWithAnimation(imagePath);
                        }
                        catch (Exception altEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Alternative GIF loading also failed: {altEx.Message}");
                            // Final fallback to static image loading
                            System.Diagnostics.Debug.WriteLine($"🔄 Final fallback to static image loading for GIF");
                            LoadAsStaticImage(imagePath);
                        }
                    }
                }
                else
                {
                    // 📥 Load static images fully into memory to avoid file locking
                    LoadAsStaticImage(imagePath);
                }

                // 📏 Update window title with filename
                var fileName = Path.GetFileName(imagePath);
                this.Title = $"🖼️ {fileName}";

                // 📊 Update image information display
                UpdateImageInfo();

                // 🔄 Set reasonable initial window size that allows resizing
                AutoSizeWindow(currentImage);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load image: {ex.Message}");
            }
        }

        // 🎬 Alternative GIF loading method using GifBitmapDecoder
        private void LoadGifWithAnimation(string imagePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🎬 Loading GIF with animation support: {imagePath}");
                
                // Use GifBitmapDecoder for better animation support
                var decoder = new System.Windows.Media.Imaging.GifBitmapDecoder(
                    new Uri(imagePath, UriKind.Absolute),
                    System.Windows.Media.Imaging.BitmapCreateOptions.None,
                    System.Windows.Media.Imaging.BitmapCacheOption.OnDemand);
                
                var frameCount = decoder.Frames.Count;
                System.Diagnostics.Debug.WriteLine($"🎬 GifBitmapDecoder frame count: {frameCount}");
                
                if (frameCount > 1)
                {
                    // 🎬 Use BitmapImage for animation instead of decoder frames
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnDemand;
                    bitmap.CreateOptions = BitmapCreateOptions.None;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    
                    currentImage = bitmap;
                    DisplayImage.Source = bitmap;
                    System.Diagnostics.Debug.WriteLine($"✅ GifBitmapDecoder + BitmapImage loaded animated GIF successfully");
                }
                else
                {
                    LoadAsStaticImage(imagePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GifBitmapDecoder failed: {ex.Message}");
                LoadAsStaticImage(imagePath);
            }
        }

        // 📥 Load image as static (non-animated)
        private void LoadAsStaticImage(string imagePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📥 Loading as static image: {imagePath}");
                
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Critical: Load fully into memory and close file handle
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze(); // Make it thread-safe and prevent further changes

                currentImage = bitmap;
                DisplayImage.Source = bitmap;
                
                System.Diagnostics.Debug.WriteLine($"✅ Static image loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Static image loading error: {ex.Message}");
                ShowError($"Failed to load image: {ex.Message}");
            }
        }

        // 📐 Set window size to show image at original resolution (within screen limits)
        private void AutoSizeWindow(BitmapImage bitmap)
        {
            try
            {
                // 📺 Get screen dimensions (leave some margin)
                var screenWidth = SystemParameters.WorkArea.Width * 0.9; // 90% of screen width
                var screenHeight = SystemParameters.WorkArea.Height * 0.9; // 90% of screen height

                // 🎯 Calculate window chrome overhead
                var chromeHeight = 35 + 55 + 25; // Title bar + toolbar + status bar
                var chromeWidth = 20; // Side margins

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
                this.Width = 700;
                this.Height = 500;
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
            var previousImagePath = allImagePaths[currentImageIndex];
            
            System.Diagnostics.Debug.WriteLine($"🔄 Navigating to previous image: {previousImagePath} (index: {currentImageIndex})");
            LoadImage(previousImagePath);
        }

        // 🖼️ Navigate to next image
        private void NavigateToNextImage()
        {
            if (allImagePaths == null || allImagePaths.Count <= 1) return;
            
            currentImageIndex = (currentImageIndex + 1) % allImagePaths.Count;
            var nextImagePath = allImagePaths[currentImageIndex];
            
            System.Diagnostics.Debug.WriteLine($"🔄 Navigating to next image: {nextImagePath} (index: {currentImageIndex})");
            LoadImage(nextImagePath);
        }

        // 📊 Update status bar information display
        private void UpdateImageInfo()
        {
            try
            {
                if (currentImage != null && !string.IsNullOrEmpty(currentImagePath))
                {
                    var fileInfo = new FileInfo(currentImagePath);
                    var fileSizeKB = Math.Round(fileInfo.Length / 1024.0, 1);
                    
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
                    System.Diagnostics.Debug.WriteLine($"🗑️ Instant delete for: {pathToDelete}");
                    
                    // 🔔 INSTANT: Remove from MediaSection UI immediately
                    if (onImageDeleted != null)
                    {
                        onImageDeleted.Invoke(pathToDelete);
                    }
                    
                    // 🚪 INSTANT: Close window immediately
                    Close();
                    
                    // 🗑️ BACKGROUND: Clean up file asynchronously (non-blocking)
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            // Simple file deletion - if it fails, just log it
                            if (File.Exists(pathToDelete))
                            {
                                File.Delete(pathToDelete);
                                System.Diagnostics.Debug.WriteLine($"✅ Background file deletion successful: {pathToDelete}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Background file deletion failed (image already removed from UI): {ex.Message}");
                        }
                    });
                }
                else
                {
                    Close(); // Just close if no path
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Delete failed: {ex.Message}");
                Close(); // Close anyway to avoid hanging window
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