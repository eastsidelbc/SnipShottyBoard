using System;
using System.Collections.Generic;
using System.IO;
using SnipShottyBoard.Core.Managers;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using SnipShottyBoard.Data;
using SnipShottyBoard.Infrastructure.Logging;
using SnipShottyBoard.Infrastructure.Helpers;

namespace SnipShottyBoard.UI
{
    public partial class MediaSection : UserControl
    {
        // 💾 Properties for save/load functionality
        private List<string> imageFiles = new List<string>();
        private Dictionary<string, DateTime> imageTimestamps = new Dictionary<string, DateTime>(); // 🕒 Track when images were added
        
        // ✅ Phase 4C P1.5: Lazy loading support
        private bool _isActive = true; // Default to active for main tab

        // 🖱️ Drag and Drop state tracking
        private bool isDragging = false;
        private Grid draggedContainer = null;
        private string draggedImagePath = null;
        private Point dragStartPoint;
        private FrameworkElement dragVisual = null; // Changed to FrameworkElement to handle both Border and Image
        private Canvas dragCanvas = null;
        private int draggedOriginalIndex = -1;
        private int dropTargetIndex = -1;
        private Border insertionIndicator = null;

        // 🖱️ Click vs Drag detection
        private System.Windows.Threading.DispatcherTimer clickTimer = null;
        private Grid pendingClickContainer = null;
        private string pendingClickImagePath = null;

        // 🎯 Supported image formats
        private readonly string[] supportedFormats = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

        // 🔔 Event to notify when media content changes
        public event Action OnMediaChanged;

        // ✅ Expose the ImagePanel for external access
        public WrapPanel ImagePanel => MyImagePanel;

        // 📝 Properties for data access
        public List<string> ImageFiles 
        { 
            get => imageFiles.ToList(); // Return copy to prevent external modification
            set 
            { 
                imageFiles = value?.ToList() ?? new List<string>();
                LoadImagesFromFiles();
                OnMediaChanged?.Invoke();
            } 
        }

        // 🕒 Property to get/set image timestamps
        public Dictionary<string, DateTime> ImageTimestamps 
        { 
            get => new Dictionary<string, DateTime>(imageTimestamps);
            set => imageTimestamps = value ?? new Dictionary<string, DateTime>();
        }

        public MediaSection()
        {
            InitializeComponent();
            
            // 🎯 Reference the drag canvas from XAML
            dragCanvas = (Canvas)FindName("DragCanvas");
        }

        // 🖼️ Add an image and track its file path and timestamp for saving (new images)
        public void AddImage(Image imageControl, string imagePath)
        {
            // ✅ Phase 4D P2.6: Validate GIF limit for new images
            if (!ValidateGifLimit(imagePath, isLoadingExisting: false))
                return;

            AddImage(imageControl, imagePath, DateTime.Now);
        }

        // 🖼️ Add an image with specific timestamp (for loading existing images)
        public void AddImage(Image imageControl, string imagePath, DateTime timestamp)
        {
            imageFiles.Add(imagePath);
            imageTimestamps[imagePath] = timestamp; // 🕒 Use provided timestamp

            var container = CreateImageContainer(imageControl, imagePath);
            ImagePanel.Children.Add(container);
            OnMediaChanged?.Invoke(); // 🔔 Trigger data change notification
        }

        /// <summary>
        /// ✅ Phase 4D P2.6: Validate GIF animation limit per note
        /// </summary>
        /// <param name="imagePath">Path to image being added</param>
        /// <param name="isLoadingExisting">True if loading from saved data (no enforcement), false if user adding new image</param>
        /// <returns>True if image can be added, false if GIF limit reached</returns>
        private bool ValidateGifLimit(string imagePath, bool isLoadingExisting)
        {
            // Only enforce limit for newly added images (not when loading existing notes)
            if (isLoadingExisting)
                return true;

            var extension = Path.GetExtension(imagePath);
            if (string.IsNullOrEmpty(extension))
                return true;

            // Check if this is a GIF
            if (!extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
                return true;

            // Count existing GIFs
            var currentGifCount = imageFiles.Count(path => 
                Path.GetExtension(path).Equals(".gif", StringComparison.OrdinalIgnoreCase));

            // Check if limit reached
            if (currentGifCount >= AppConstants.MaxAnimatedGifsPerNote)
            {
                var ownerWindow = Window.GetWindow(this);
                DialogHelper.ShowInformation(
                    ownerWindow,
                    $"Maximum {AppConstants.MaxAnimatedGifsPerNote} animated GIFs allowed per note.\n\n" +
                    "GIF animations consume significant memory. Consider using static images (PNG/JPG) or reduce the number of GIFs.",
                    "GIF Limit Reached",
                    "⚠️" // Warning icon
                );

                LoggingService.LogInfoStatic(
                    $"GIF limit reached: User attempted to add GIF when {currentGifCount} already present (limit: {AppConstants.MaxAnimatedGifsPerNote})",
                    "Media"
                );

                return false;
            }

            return true;
        }

        // 🎬 Helper method to create thumbnails that preserves GIF animation
        // ✅ Phase 4C P1.4: Integrated with ImageCacheManager for memory safety
        private BitmapImage CreateThumbnailBitmap(string imagePath, int maxWidth = SnipShottyBoard.Data.AppConstants.DefaultThumbnailWidth)
        {
            try
            {
                // Check cache first (Phase 4C P1.4)
                var cached = ImageCacheManager.Instance.GetFromCache(imagePath);
                if (cached != null)
                    return cached;

                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                
                // 🎬 Special handling for GIFs - don't freeze to preserve animation
                if (extension == ".gif")
                {
                    bitmap.CacheOption = BitmapCacheOption.OnDemand; // Allow animation
                    bitmap.CreateOptions = BitmapCreateOptions.None; // Preserve animation
                }
                else
                {
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load static images into memory
                }
                
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = maxWidth;
                bitmap.EndInit();
                
                // 🎬 Only freeze static images, not GIFs
                if (extension != ".gif")
                {
                    bitmap.Freeze(); // Make static images thread-safe
                }

                // Add to cache (Phase 4C P1.4)
                ImageCacheManager.Instance.AddToCache(imagePath, bitmap);
                
                return bitmap;
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to create thumbnail", ex, "Media", new {
                    FileName = PathSanitizer.SanitizePath(imagePath)
                });
                return null;
            }
        }

        // 📦 Create image container with timestamp and drag support (delete button handled separately)
        private Grid CreateImageContainer(Image imageControl, string imagePath)
        {
            var container = new Grid
            {
                Width = SnipShottyBoard.Data.AppConstants.MediaContainerWidth,
                MinHeight = SnipShottyBoard.Data.AppConstants.MediaContainerMinHeight,
                Margin = new Thickness(5),
                Background = Brushes.Transparent
            };

            // 📋 Store image path as tag for identification
            container.Tag = imagePath;

            // Add row definitions for image and timestamp
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(SnipShottyBoard.Data.AppConstants.MediaThumbnailHeight) });
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 🖼️ Image area
            var imageGrid = new Grid();
            imageControl.HorizontalAlignment = HorizontalAlignment.Center;
            imageControl.VerticalAlignment = VerticalAlignment.Center;
            imageGrid.Children.Add(imageControl);
            Grid.SetRow(imageGrid, 0);
            container.Children.Add(imageGrid);

            // 🕒 Timestamp footer
            var timestampText = new TextBlock
            {
                FontSize = SnipShottyBoard.Data.AppConstants.SmallFontSize,
                Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush"),
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2, 4, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            // Get timestamp for this image
            if (imageTimestamps.ContainsKey(imagePath))
            {
                var timestamp = GetTimeAgoString(imageTimestamps[imagePath]);
                timestampText.Text = timestamp;
            }
            else
            {
                // Fallback to current time if timestamp not found
                var currentTime = DateTime.Now.ToString("M/d/yy h:mmtt").ToLower();
                timestampText.Text = currentTime;
            }

            Grid.SetRow(timestampText, 1);
            container.Children.Add(timestampText);

            // 🖱️ Hover effects 
            container.MouseEnter += (s, e) =>
            {
                if (!isDragging)
                {
                    container.Background = (System.Windows.Media.Brush)FindResource("HoverTransparentBrush");
                }
            };

            container.MouseLeave += (s, e) =>
            {
                if (!isDragging)
                {
                    container.Background = Brushes.Transparent;
                }
            };

            // 🖱️ Single-click anywhere in container to view full size (handled in drag logic)

            // 🎯 Add drag and drop event handlers
            AddDragHandlers(container, imagePath);

            return container;
        }

        // 🎯 Add drag and drop event handlers to an image container
        private void AddDragHandlers(Grid container, string imagePath)
        {
            // 🖱️ Mouse down - start click vs drag detection
            container.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                // Only handle if not clicking delete button
                if (!(e.OriginalSource is Button))
                {
                    dragStartPoint = e.GetPosition(container);
                    pendingClickContainer = container;
                    pendingClickImagePath = imagePath;
                    
                    // Start timer for click detection (200ms window)
                    if (clickTimer == null)
                    {
                        clickTimer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(SnipShottyBoard.Data.AppConstants.ClickDetectionWindowMs)
                        };
                        clickTimer.Tick += OnClickTimerTick;
                    }
                    clickTimer.Start();
                    
                    // Capture mouse to ensure we get move/up events
                    container.CaptureMouse();
                }
            };

            // 🎯 Mouse move - check if we should start dragging or cancel click
            container.PreviewMouseMove += (sender, e) =>
            {
                // Don't start drag if over delete button
                if (e.OriginalSource is Button)
                {
                    return;
                }
                
                if (e.LeftButton == MouseButtonState.Pressed && !isDragging && 
                    dragStartPoint != default(Point) && pendingClickContainer == container)
                {
                    Point currentPosition = e.GetPosition(container);
                    
                    // Check if mouse moved enough to start drag (deadzone)
                    if (Math.Abs(currentPosition.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(currentPosition.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        // Cancel click detection and start drag
                        CancelClickDetection();
                        StartDrag(container, imagePath, e);
                    }
                }
            };

            // 🖱️ Mouse up - handle click or complete drag
            container.PreviewMouseLeftButtonUp += (sender, e) =>
            {
                // Don't handle if clicking delete button
                if (e.OriginalSource is Button)
                {
                    CancelClickDetection();
                    container.ReleaseMouseCapture();
                    return;
                }
                
                if (isDragging)
                {
                    CompleteDrag();
                    e.Handled = true;
                }
                else if (pendingClickContainer == container)
                {
                    // This was a click - show full-size image
                    CancelClickDetection();
                    ShowFullSizeImage(imagePath);
                    e.Handled = true;
                }
                
                // Release mouse capture
                container.ReleaseMouseCapture();
            };
        }

        // ⏰ Handle click timer expiration
        private void OnClickTimerTick(object sender, EventArgs e)
        {
            // Timer expired - this was a click, not a drag start
            if (pendingClickContainer != null && pendingClickImagePath != null)
            {
                var container = pendingClickContainer;
                var imagePath = pendingClickImagePath;
                
                CancelClickDetection();
                ShowFullSizeImage(imagePath);
                
                // Release any mouse capture
                container.ReleaseMouseCapture();
            }
        }

        // 🚫 Cancel click detection and cleanup timer
        private void CancelClickDetection()
        {
            if (clickTimer != null)
            {
                clickTimer.Stop();
                clickTimer.Tick -= OnClickTimerTick; // Prevent memory leaks
            }
            pendingClickContainer = null;
            pendingClickImagePath = null;
            dragStartPoint = default(Point);
        }

        // 🧹 Dispose timer when control is unloaded
        public void Dispose()
        {
            try
            {
                CancelClickDetection();
                if (clickTimer != null)
                {
                    clickTimer.Stop();
                    clickTimer.Tick -= OnClickTimerTick;
                    clickTimer = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing MediaSection: {ex.Message}");
            }
        }

        // 🚀 Start drag operation with visual feedback
        private void StartDrag(Grid container, string imagePath, MouseEventArgs e)
        {
            isDragging = true;
            draggedContainer = container;
            draggedImagePath = imagePath;
            draggedOriginalIndex = GetDataIndexFromImagePath(imagePath);

            // 🎨 Create drag visual
            CreateDragVisual(container, e.GetPosition(this));

            // 🖱️ Capture mouse for drag operation
            container.CaptureMouse();

            // 🎨 Apply dragging visual state to original container
            ApplyDraggingVisualState(container);

            // 🔗 Wire up mouse events for drag tracking
            container.MouseMove += OnDragMouseMove;
            container.MouseUp += OnDragMouseUp;
            container.MouseLeave += OnDragMouseLeave;
        }

        // 🎨 Create visual feedback for dragging (clean, minimal)
        private void CreateDragVisual(Grid sourceContainer, Point currentPosition)
        {
            if (dragCanvas == null) return;

            // 🖼️ Find the original image to copy its source
            var imageGrid = sourceContainer.Children.OfType<Grid>().FirstOrDefault();
            var originalImage = imageGrid?.Children.OfType<Image>().FirstOrDefault();
            
            if (originalImage?.Source != null)
            {
                // 🎨 Create a completely new image element for drag visual
                var dragImage = new Image
                {
                    Source = originalImage.Source,
                    Width = 80,  // Smaller than original for clean drag feel
                    Height = 80,
                    Stretch = Stretch.Uniform,
                    Opacity = 0.9, // Slightly transparent
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                // ✨ Add subtle drop shadow for lifted effect
                dragImage.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.3,
                    BlurRadius = 8,
                    ShadowDepth = 4,
                    Direction = 270
                };

                // 🎯 Add very slight rotation for natural feel
                var transform = new RotateTransform(1.5); // Subtle 1.5 degree rotation
                dragImage.RenderTransform = transform;
                
                dragVisual = dragImage;
                dragCanvas.Children.Add(dragVisual);
                
                // Position the visual
                UpdateDragVisualPosition(currentPosition);
            }
        }

        // 🎨 Apply visual state to show container is being dragged
        private void ApplyDraggingVisualState(Grid container)
        {
            // 👻 Make original container semi-transparent
            container.Opacity = 0.5;
            
            // 📐 Add subtle scale transform
            var scaleTransform = new ScaleTransform(0.95, 0.95);
            container.RenderTransform = scaleTransform;
            
            // 🎨 Change background to indicate it's being moved
                            container.Background = (System.Windows.Media.Brush)FindResource("ActiveTransparentBrush");
        }

        // 🔄 Update drag visual position
        private void UpdateDragVisualPosition(Point position)
        {
            if (dragVisual != null && dragCanvas != null)
            {
                Canvas.SetLeft(dragVisual, position.X - dragVisual.Width / 2);
                Canvas.SetTop(dragVisual, position.Y - dragVisual.Height / 2);
            }
        }

        // 🎯 Calculate insertion index based on mouse position (WrapPanel-aware)
        private int GetInsertionIndex(Point position)
        {
            // 📊 Get all actual image containers (excluding insertion indicator and dragged container)
            var actualContainers = ImagePanel.Children
                .OfType<Grid>()
                .Where(g => !ReferenceEquals(g, insertionIndicator) && !ReferenceEquals(g, draggedContainer))
                .ToList();

            if (actualContainers.Count == 0)
                return 0;

            // 🎯 Find the best insertion position by comparing with each container
            var bestInsertionIndex = 0;
            var minDistance = double.MaxValue;
            
            for (int i = 0; i < actualContainers.Count; i++)
            {
                var container = actualContainers[i];
                try
                {
                    // Get container position relative to the MediaSection
                    var containerPos = container.TransformToAncestor(ImagePanel).Transform(new Point(0, 0));
                    var containerRect = new Rect(containerPos, container.RenderSize);
                    
                    // Check if mouse is directly over this container
                    if (containerRect.Contains(position))
                    {
                        // Determine if closer to left or right edge
                        var centerX = containerRect.Left + containerRect.Width / 2;
                        return position.X < centerX ? i : i + 1;
                    }
                    
                    // Calculate distance to this container for fallback positioning
                    var containerCenter = new Point(
                        containerRect.Left + containerRect.Width / 2,
                        containerRect.Top + containerRect.Height / 2
                    );
                    var distance = GetDistance(position, containerCenter);
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        // If mouse is to the left of container center, insert before; otherwise after
                        bestInsertionIndex = position.X < containerCenter.X ? i : i + 1;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Handle case where container is not in visual tree
                    continue;
                }
            }

            // 📍 Clamp to valid range
            return Math.Max(0, Math.Min(bestInsertionIndex, actualContainers.Count));
        }

        // 📏 Calculate distance between two points
        private double GetDistance(Point p1, Point p2)
        {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // 📍 Show insertion indicator at the correct visual position
        private void ShowInsertionIndicator(int index)
        {
            RemoveInsertionIndicator();

            var actualContainers = ImagePanel.Children
                .OfType<Grid>()
                .Where(g => !ReferenceEquals(g, insertionIndicator) && !ReferenceEquals(g, draggedContainer))
                .ToList();

            if (index < 0) return;

            // 📏 Create insertion line indicator
            insertionIndicator = new Border
            {
                Width = 4,
                Height = 140,
                Background = (System.Windows.Media.Brush)FindResource("AppForegroundBrush"),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(2, 5, 2, 5)
            };

            // ✨ Add glow effect
            insertionIndicator.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = ((SolidColorBrush)FindResource("AppForegroundBrush")).Color,
                Opacity = 0.8,
                BlurRadius = 8,
                ShadowDepth = 0
            };

            // 📍 Calculate where to insert in the ImagePanel.Children collection
            int insertPosition = 0;
            
            if (index >= actualContainers.Count)
            {
                // Insert at the end
                insertPosition = ImagePanel.Children.Count;
            }
            else if (index <= 0)
            {
                // Insert at the beginning
                insertPosition = 0;
            }
            else
            {
                // Insert before the container at the target index
                var targetContainer = actualContainers[index];
                insertPosition = ImagePanel.Children.IndexOf(targetContainer);
            }

            // 🎯 Insert the indicator
            try
            {
                ImagePanel.Children.Insert(insertPosition, insertionIndicator);
                dropTargetIndex = index;
                
                System.Diagnostics.Debug.WriteLine($"Insertion indicator placed at UI position {insertPosition}, data index {index}");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Fallback: add at end
                ImagePanel.Children.Add(insertionIndicator);
                dropTargetIndex = actualContainers.Count;
                System.Diagnostics.Debug.WriteLine($"Fallback: Insertion indicator added at end");
            }
        }

        // 🎯 Calculate target data index from UI drop target index (improved accuracy)
        private int CalculateTargetDataIndex(int dropTargetIndex, int originalDataIndex)
        {
            // Get current actual containers (excluding indicators and dragged item)
            var actualContainers = ImagePanel.Children
                .OfType<Grid>()
                .Where(g => !ReferenceEquals(g, insertionIndicator) && !ReferenceEquals(g, draggedContainer))
                .ToList();

            // Validate indices
            if (dropTargetIndex < 0) return 0;
            if (dropTargetIndex >= actualContainers.Count) return imageFiles.Count - 1;
            if (originalDataIndex < 0) return dropTargetIndex;

            // 🎯 Simple mapping: the drop target index IS the data index
            // After we remove the dragged item, all indices shift naturally
            var targetDataIndex = dropTargetIndex;
            
            // If we're moving the item to a later position, we need to account for the removal
            if (originalDataIndex < targetDataIndex)
            {
                targetDataIndex = Math.Min(targetDataIndex, imageFiles.Count - 1);
            }

            System.Diagnostics.Debug.WriteLine($"Target calculation: dropIndex={dropTargetIndex}, originalIndex={originalDataIndex}, targetDataIndex={targetDataIndex}");
            
            return Math.Max(0, Math.Min(targetDataIndex, imageFiles.Count - 1));
        }

        // 🖱️ Handle mouse move during drag
        private void OnDragMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            // Get position relative to the MediaSection for drag visual
            var sectionPosition = e.GetPosition(this);
            UpdateDragVisualPosition(sectionPosition);

            // Get position relative to the WrapPanel for insertion calculation
            var panelPosition = e.GetPosition(ImagePanel);

            // 🎯 Determine insertion point
            var targetIndex = GetInsertionIndex(panelPosition);
            if (targetIndex != dropTargetIndex)
            {
                System.Diagnostics.Debug.WriteLine($"Drag move: targetIndex={targetIndex}, dropTargetIndex={dropTargetIndex}, panelPos=({panelPosition.X:F1}, {panelPosition.Y:F1})");
                ShowInsertionIndicator(targetIndex);
            }
        }

        // 🖱️ Handle mouse up during drag
        private void OnDragMouseUp(object sender, MouseButtonEventArgs e)
        {
            CompleteDrag();
        }

        // 🖱️ Handle mouse leaving container during drag
        private void OnDragMouseLeave(object sender, MouseEventArgs e)
        {
            // Don't cancel drag when leaving container - let it continue
        }

        // 🏁 Complete drag operation
        private void CompleteDrag()
        {
            if (!isDragging) return;

            // 🔍 Calculate the actual target index based on current drop target
            var shouldReorder = dropTargetIndex >= 0;
            var originalDataIndex = GetDataIndexFromImagePath(draggedImagePath);
            var targetDataIndex = CalculateTargetDataIndex(dropTargetIndex, originalDataIndex);

            // 🔄 Perform reordering if needed
            if (shouldReorder && targetDataIndex != originalDataIndex && originalDataIndex >= 0)
            {
                ReorderImage(originalDataIndex, targetDataIndex);
            }

            // 🧹 Clean up drag operation
            CleanupDragOperation();
        }

        // 🔍 Get data index from image path
        private int GetDataIndexFromImagePath(string imagePath)
        {
            return imageFiles.IndexOf(imagePath);
        }

        // 🔄 Reorder image in both UI and data structures (simplified and more reliable)
        private void ReorderImage(int fromDataIndex, int toDataIndex)
        {
            if (fromDataIndex < 0 || toDataIndex < 0 || fromDataIndex >= imageFiles.Count)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid reorder indices: from={fromDataIndex}, to={toDataIndex}, count={imageFiles.Count}");
                return;
            }

            // Ensure target index is valid
            toDataIndex = Math.Max(0, Math.Min(toDataIndex, imageFiles.Count - 1));

            if (fromDataIndex == toDataIndex)
            {
                System.Diagnostics.Debug.WriteLine("No reordering needed - same position");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Reordering image: from={fromDataIndex} to={toDataIndex}");

            try
            {
                // 🔄 Reorder data structures
                var imagePath = imageFiles[fromDataIndex];
                imageFiles.RemoveAt(fromDataIndex);
                imageFiles.Insert(toDataIndex, imagePath);

                // 🔄 Rebuild UI to match data order
                RebuildUIFromData();

                // 🔔 Trigger change notification
                OnMediaChanged?.Invoke();

                System.Diagnostics.Debug.WriteLine("Reordering completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during reordering: {ex.Message}");
                // Try to recover by rebuilding UI
                RebuildUIFromData();
            }
        }

        // 🏗️ Rebuild UI to match data order (ensures synchronization)
        private void RebuildUIFromData()
        {
            // Store current dragged container reference
            var wasDragging = isDragging;
            var draggedPath = draggedImagePath;

            // Clear UI (but keep drag canvas and insertion indicator logic intact)
            var containersToRemove = ImagePanel.Children
                .OfType<Grid>()
                .Where(g => !ReferenceEquals(g, insertionIndicator))
                .ToList();

            foreach (var container in containersToRemove)
            {
                ImagePanel.Children.Remove(container);
            }

            // Rebuild from data order
            foreach (var imagePath in imageFiles)
            {
                try
                {
                    // ✅ Layer separation: File I/O delegated to DataManager (Phase 4C P1.3)
                    if (DataManager.ValidateImageFile(imagePath))
                    {
                        var bitmap = CreateThumbnailBitmap(imagePath);
                        if (bitmap == null) continue; // Skip if thumbnail creation failed

                        var image = new Image
                        {
                            Source = bitmap,
                            MaxWidth = 80,
                            MaxHeight = 80,
                            Stretch = Stretch.Uniform,
                            Cursor = System.Windows.Input.Cursors.Hand
                        };

                        var container = CreateImageContainer(image, imagePath);
                        
                        // Restore dragged container reference if this is the dragged item
                        if (wasDragging && imagePath == draggedPath)
                        {
                            draggedContainer = container;
                            if (isDragging)
                            {
                                ApplyDraggingVisualState(container);
                                // Re-wire drag events
                                container.MouseMove += OnDragMouseMove;
                                container.MouseUp += OnDragMouseUp;
                                container.MouseLeave += OnDragMouseLeave;
                                container.CaptureMouse();
                            }
                        }

                        ImagePanel.Children.Add(container);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to rebuild container for {imagePath}: {ex.Message}");
                }
            }
        }

        // 📊 Get container index in ImagePanel (excluding indicators)
        private int GetContainerIndex(Grid container)
        {
            var actualContainers = ImagePanel.Children
                .OfType<Grid>()
                .Where(g => !ReferenceEquals(g, insertionIndicator))
                .ToList();
            
            return actualContainers.IndexOf(container);
        }

        // 🧹 Clean up drag operation
        private void CleanupDragOperation()
        {
            if (draggedContainer != null)
            {
                // 🔄 Restore original visual state
                draggedContainer.Opacity = 1.0;
                draggedContainer.RenderTransform = null;
                draggedContainer.Background = Brushes.Transparent;

                // 🖱️ Release mouse capture
                draggedContainer.ReleaseMouseCapture();

                // 🔗 Remove drag event handlers
                draggedContainer.MouseMove -= OnDragMouseMove;
                draggedContainer.MouseUp -= OnDragMouseUp;
                draggedContainer.MouseLeave -= OnDragMouseLeave;
            }

            // 🎨 Remove drag visual
            if (dragVisual != null && dragCanvas != null)
            {
                dragCanvas.Children.Remove(dragVisual);
                dragVisual = null;
            }

            // 🧹 Remove insertion indicator
            RemoveInsertionIndicator();

            // 🚫 Cancel any pending click detection
            CancelClickDetection();

            // 🔄 Reset drag state
            isDragging = false;
            draggedContainer = null;
            draggedImagePath = null;
            draggedOriginalIndex = -1;
            dropTargetIndex = -1;
            dragStartPoint = default(Point);
        }

        // 🕒 Format timestamp for display
        private string GetTimeAgoString(DateTime addedDate)
        {
            // 📅 Show actual date/time instead of relative time
            return addedDate.ToString("M/d/yy h:mmtt").ToLower();
        }

        // 🗑️ Remove image container and update tracking
        private void RemoveImageContainer(Grid container, string imagePath)
        {
            ImagePanel.Children.Remove(container);
            imageFiles.Remove(imagePath);
            imageTimestamps.Remove(imagePath);
            OnMediaChanged?.Invoke(); // 🔔 Trigger data change notification
        }

        // 📂 Load images from file paths
        private void LoadImagesFromFiles()
        {
            ImagePanel.Children.Clear();
            
            foreach (var imagePath in imageFiles)
            {
                try
                {
                    // ✅ Layer separation: File I/O delegated to DataManager (Phase 4C P1.3)
                    var imageInfo = DataManager.GetImageInfo(imagePath);
                    if (imageInfo.HasValue && imageInfo.Value.exists)
                    {
                        var bitmap = CreateThumbnailBitmap(imagePath);
                        if (bitmap == null) continue; // Skip if thumbnail creation failed

                        var image = new Image
                        {
                            Source = bitmap,
                            MaxWidth = 120,
                            MaxHeight = 120,
                            Stretch = Stretch.Uniform,
                            Cursor = System.Windows.Input.Cursors.Hand
                        };

                        // 🕒 Use file creation time as timestamp for existing images
                        // ✅ File info from DataManager (Phase 4C P1.3)
                        var fileInfo = new FileInfo(imagePath);
                        var timestamp = fileInfo.CreationTime;
                        
                        // 🖼️ Add image with file timestamp (don't use current time)
                        imageTimestamps[imagePath] = timestamp;
                        var container = CreateImageContainer(image, imagePath);
                        ImagePanel.Children.Add(container);
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Add proper logging
                    System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
                }
            }
        }

        // 🖼️ Show full-size image in custom viewer window
        private void ShowFullSizeImage(string imagePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🖼️ ===== MEDIASECTION OPENING IMAGE VIEWER =====");
                System.Diagnostics.Debug.WriteLine($"🖼️ MediaSection.ShowFullSizeImage() called");
                System.Diagnostics.Debug.WriteLine($"🖼️ Opening ImageViewer for: {imagePath}");
                
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                System.Diagnostics.Debug.WriteLine($"🖼️ File extension: {extension}");
                
                if (extension == ".gif")
                {
                    System.Diagnostics.Debug.WriteLine($"🎬 MEDIASECTION: This is a GIF file - animation should be preserved");
                    System.Diagnostics.Debug.WriteLine($"🎬 MEDIASECTION: About to create ImageViewerWindow for GIF");
                }
                
                // 🔍 Find current image index for navigation
                var currentIndex = imageFiles.IndexOf(imagePath);
                System.Diagnostics.Debug.WriteLine($"🖼️ Current image index: {currentIndex} of {imageFiles.Count} total images");
                
                // 🔗 Create viewer with navigation support
                System.Diagnostics.Debug.WriteLine($"🖼️ Creating ImageViewerWindow...");
                var imageViewer = new ImageViewerWindow(imagePath, imageFiles, currentIndex, RemoveImageByPath);
                System.Diagnostics.Debug.WriteLine($"🖼️ ImageViewerWindow created successfully");
                
                // 🎯 Position window to not cover the main app
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // 📐 Calculate position to the right of main window
                    imageViewer.Left = mainWindow.Left + mainWindow.Width + 20;
                    imageViewer.Top = mainWindow.Top;
                    
                    // 📺 If positioned off-screen, center on screen instead
                    if (imageViewer.Left + imageViewer.Width > SystemParameters.VirtualScreenWidth)
                    {
                        imageViewer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🖼️ About to show ImageViewerWindow...");
                imageViewer.Show();
                System.Diagnostics.Debug.WriteLine($"🖼️ ImageViewerWindow.Show() completed");
                
                if (extension == ".gif")
                {
                    System.Diagnostics.Debug.WriteLine($"🎬 MEDIASECTION: GIF should now be visible and animating in ImageViewerWindow");
                }
                System.Diagnostics.Debug.WriteLine($"🖼️ ===== MEDIASECTION OPENING COMPLETE =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to open ImageViewer: {ex.Message}");
                // 🔔 Show proper error dialog using our custom dialog system
                CustomDialog.ShowError(
                    Application.Current.MainWindow,
                    $"Failed to open image viewer: {ex.Message}",
                    "Image Viewer Error");
            }
        }

        // 🗑️ Instantly remove image from UI (called from ImageViewer when deleting)
        private void RemoveImageByPath(string imagePath)
        {
            try
            {
                // 🔍 Find and remove container instantly
                var containerToRemove = ImagePanel.Children.OfType<Grid>()
                    .FirstOrDefault(container => container.Tag as string == imagePath);
                
                if (containerToRemove != null)
                {
                    // 🔓 Quick image source clear
                    ClearImageSourceInContainer(containerToRemove);
                    
                    // 🗑️ Instant UI removal
                    RemoveImageContainer(containerToRemove, imagePath);
                    System.Diagnostics.Debug.WriteLine($"✅ Instantly removed: {imagePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error removing image: {ex.Message}");
            }
        }
        
        // 🔓 Quick helper to clear image sources
        private void ClearImageSourceInContainer(Grid container)
        {
            try
            {
                // Quick clear - no complex searching
                var imageControls = container.Children.OfType<Grid>()
                    .SelectMany(g => g.Children.OfType<Image>())
                    .ToList();
                    
                foreach (var imageControl in imageControls)
                {
                    imageControl.Source = null;
                }
            }
            catch
            {
                // Ignore errors - just continue with removal
            }
        }

        // 🖼️ Remove image by control reference
        public void RemoveImage(Image imageControl)
        {
            var containerToRemove = ImagePanel.Children.OfType<Grid>()
                .FirstOrDefault(container => container.Children.OfType<Grid>().Any(grid => 
                    grid.Children.Contains(imageControl)));
            
            if (containerToRemove != null)
            {
                var imagePath = imageFiles.FirstOrDefault(); // This is a simplified approach
                RemoveImageContainer(containerToRemove, imagePath);
            }
        }

        // 🧹 Remove insertion indicator
        private void RemoveInsertionIndicator()
        {
            if (insertionIndicator != null && ImagePanel.Children.Contains(insertionIndicator))
            {
                ImagePanel.Children.Remove(insertionIndicator);
                insertionIndicator = null;
                dropTargetIndex = -1;
            }
        }

        #region Drag and Drop from External Sources
        
        // 🖱️ Handle drag enter - show drop indicator
        private void MediaSection_DragEnter(object sender, DragEventArgs e)
        {
            if (HasImageFiles(e))
            {
                e.Effects = DragDropEffects.Copy;
                ShowDropIndicator();
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        // 🖱️ Handle drag over - maintain drop indicator
        private void MediaSection_DragOver(object sender, DragEventArgs e)
        {
            if (HasImageFiles(e))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        // 🖱️ Handle drag leave - hide drop indicator
        private void MediaSection_DragLeave(object sender, DragEventArgs e)
        {
            HideDropIndicator();
        }
        
        // 📁 Handle file drop - process external images
        private void MediaSection_Drop(object sender, DragEventArgs e)
        {
            try
            {
                HideDropIndicator();
                
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var imageFiles = files.Where(f => IsImageFile(f)).ToArray();
                    
                    if (imageFiles.Length > 0)
                    {
                        ProcessDroppedImages(imageFiles);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error handling dropped files: {ex.Message}");
            }
        }
        
        // 🔍 Check if drag data contains image files
        private bool HasImageFiles(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                return files.Any(f => IsImageFile(f));
            }
            return false;
        }
        
        // 📋 Check if file is a supported image format
        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return supportedFormats.Contains(extension);
        }
        
        // 🎨 Show drop indicator overlay
        private void ShowDropIndicator()
        {
            if (DropIndicator != null)
            {
                DropIndicator.Visibility = Visibility.Visible;
            }
        }
        
        // 🎨 Hide drop indicator overlay
        private void HideDropIndicator()
        {
            if (DropIndicator != null)
            {
                DropIndicator.Visibility = Visibility.Collapsed;
            }
        }
        
        // 📁 Process dropped image files
        private async void ProcessDroppedImages(string[] imagePaths)
        {
            try
            {
                foreach (var sourcePath in imagePaths)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            // 📂 Copy file using DataManager
                            var destinationPath = DataManager.CopyDroppedImage(sourcePath);
                            if (destinationPath == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ UI: Failed to copy dropped image: {sourcePath}");
                                return;
                            }
                            
                            var newFileName = Path.GetFileName(destinationPath);
                            
                            // 🖼️ Add to UI on main thread
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    var bitmap = CreateThumbnailBitmap(destinationPath);
                                    if (bitmap == null)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"❌ Failed to create thumbnail for dropped image: {newFileName}");
                                        return;
                                    }

                                    var image = new Image
                                    {
                                        Source = bitmap,
                                        MaxWidth = 120,
                                        MaxHeight = 120,
                                        Stretch = Stretch.Uniform,
                                        Cursor = System.Windows.Input.Cursors.Hand
                                    };

                                    // 📝 Add to MediaSection
                                    AddImage(image, destinationPath, DateTime.Now);
                                    
                                    System.Diagnostics.Debug.WriteLine($"✅ Added dropped image: {newFileName}");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"❌ Error adding dropped image to UI: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Error copying dropped file: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error processing dropped images: {ex.Message}");
            }
        }
        
        #endregion
    }
} 