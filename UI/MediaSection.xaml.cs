using System;
using System.Collections.Generic;
using System.IO;
using SnipShottyBoard.Core.Managers;
using SnipShottyBoard.Core.Models;
using System.Linq;
using System.Threading;
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

        // ✅ Sprint B Phase B.1: Async lazy-loading for thumbnails
        private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(4, 4); // Max 4 concurrent decodes
        private readonly CancellationTokenSource _disposeToken = new(); // Cancelled only on Dispose — shared by all loads

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

        // 📦 Property to get/set media references (bridges full paths ↔ MediaReference)
        // Used by TabManager for direct Media save/load without round-trip conversion.
        public List<MediaReference> MediaReferences
        {
            get
            {
                var refs = new List<MediaReference>();
                foreach (var container in ImagePanel.Children.OfType<Grid>())
                {
                    var refData = container.Tag as MediaReference;
                    if (refData != null)
                        refs.Add(refData);
                }
                return refs;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    imageFiles.Clear();
                    imageTimestamps.Clear();
                    ImagePanel.Children.Clear();
                    return;
                }

                imageFiles.Clear();
                imageTimestamps.Clear();

                var mediaRefsDict = new Dictionary<string, MediaReference>();
                foreach (var ref_ in value)
                {
                    var fullPath = ref_.FullPath;
                    if (!string.IsNullOrEmpty(fullPath) && !imageFiles.Contains(fullPath))
                    {
                        imageFiles.Add(fullPath);
                        imageTimestamps[fullPath] = ref_.DateAdded;
                        mediaRefsDict[fullPath] = ref_;
                    }
                }
                LoadImagesFromFiles(mediaRefsDict);
                OnMediaChanged?.Invoke();
            }
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
        private BitmapImage CreateThumbnailBitmap(string imagePath, int maxWidth = SnipShottyBoard.Data.AppConstants.MediaContainerWidth)
        {
            try
            {
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

        /// <summary>
        /// Creates a static (first-frame) thumbnail for GIFs in the Media Vault.
        /// GIFs only animate in the ImageViewerWindow, not in the vault.
        /// </summary>
        private BitmapImage CreateStaticGifThumbnail(string imagePath, int maxWidth = SnipShottyBoard.Data.AppConstants.MediaContainerWidth)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Decode into memory, discard stream
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.DelayCreation;
            bitmap.DecodePixelWidth = maxWidth;
            bitmap.UriSource = new Uri(imagePath);
            bitmap.EndInit();
            bitmap.Freeze(); // Thread-safe, single frame
            return bitmap;
        }

        // === Global Context Menu (empty-space right-click) ===

        private void MediaSection_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // If right-click landed on an image container, let its own ContextMenu open
            var originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null)
            {
                if (originalSource is Grid grid && grid.Tag is MediaReference)
                {
                    // Container has its own ContextMenu — do not build global menu
                    return;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource) as DependencyObject;
            }

            // Right-click was on empty space — build global menu
            var menu = new ContextMenu();
            if (Application.Current.Resources.Contains("NativeContextMenuStyle"))
                menu.Style = (Style)Application.Current.Resources["NativeContextMenuStyle"];

            // Size submenu — apply to ALL images
            var sizeMenu = new MenuItem { Header = "Size", Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.Resize) };
            var sizes = new (string Label, int Size)[]
            {
                ("Small (60px)", AppConstants.ThumbnailSizeSmall),
                ("Medium (100px)", AppConstants.ThumbnailSizeMedium),
                ("Big (150px)", AppConstants.ThumbnailSizeBig)
            };
            foreach (var (label, size) in sizes)
            {
                var sizeItem = new MenuItem { Header = label, IsCheckable = true };
                var capturedSize = size;
                sizeItem.Click += (s, ev) => SetSizeForAll(capturedSize);
                sizeMenu.Items.Add(sizeItem);
            }
            menu.Items.Add(sizeMenu);

            // Hide toggle — apply to ALL images
            var anyHidden = ImagePanel.Children.OfType<Grid>()
                .Any(g => (g.Tag as MediaReference)?.IsHidden == true);
            var hideItem = new MenuItem
            {
                Header = anyHidden ? "Show All" : "Hide All",
                Icon = CreateIcon(anyHidden ? MaterialDesignThemes.Wpf.PackIconKind.Eye : MaterialDesignThemes.Wpf.PackIconKind.EyeOff)
            };
            hideItem.Click += (s, ev) => ToggleHiddenForAll(anyHidden);
            menu.Items.Add(hideItem);

            menu.Items.Add(new Separator());

            // Label toggle — apply to ALL images
            var anyLabel = ImagePanel.Children.OfType<Grid>()
                .Any(g => (g.Tag as MediaReference)?.ShowLabel == true);
            var labelAllItem = new MenuItem
            {
                Header = "Label",
                IsCheckable = true,
                IsChecked = anyLabel,
                Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.Tag)
            };
            labelAllItem.Click += (s, ev) => ToggleShowLabelForAll((bool)((MenuItem)s!).IsChecked!);
            menu.Items.Add(labelAllItem);

            // Date toggle — apply to ALL images
            var anyDate = ImagePanel.Children.OfType<Grid>()
                .Any(g => (g.Tag as MediaReference)?.ShowDate == true);
            var dateAllItem = new MenuItem
            {
                Header = "Date",
                IsCheckable = true,
                IsChecked = anyDate,
                Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.Calendar)
            };
            dateAllItem.Click += (s, ev) => ToggleShowDateForAll((bool)((MenuItem)s!).IsChecked!);
            menu.Items.Add(dateAllItem);

            // Time toggle — apply to ALL images
            var anyTime = ImagePanel.Children.OfType<Grid>()
                .Any(g => (g.Tag as MediaReference)?.ShowTime == true);
            var timeAllItem = new MenuItem
            {
                Header = "Time",
                IsCheckable = true,
                IsChecked = anyTime,
                Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.Clock)
            };
            timeAllItem.Click += (s, ev) => ToggleShowTimeForAll((bool)((MenuItem)s!).IsChecked!);
            menu.Items.Add(timeAllItem);

            menu.Items.Add(new Separator());

            // Delete All — with confirmation
            var deleteItem = new MenuItem
            {
                Header = "Delete All",
                Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.DeleteForever)
            };
            deleteItem.Click += (s, ev) => DeleteAllImages();
            menu.Items.Add(deleteItem);

            // Assign and open
            ((Grid)sender).ContextMenu = menu;
        }

        private void SetSizeForAll(int newSize)
        {
            foreach (var container in ImagePanel.Children.OfType<Grid>().ToList())
            {
                var mediaRef = container.Tag as MediaReference;
                if (mediaRef != null)
                {
                    mediaRef.ThumbnailSize = newSize;
                    RebuildSingleContainer(container, mediaRef);
                }
            }
            OnMediaChanged?.Invoke();
        }

        private void ToggleShowLabelForAll(bool value)
        {
            foreach (var container in ImagePanel.Children.OfType<Grid>().ToList())
            {
                var mediaRef = container.Tag as MediaReference;
                if (mediaRef != null)
                {
                    mediaRef.ShowLabel = value;
                    UpdateContainerVisibility(container, mediaRef);
                }
            }
            OnMediaChanged?.Invoke();
        }

        private void ToggleShowDateForAll(bool value)
        {
            foreach (var container in ImagePanel.Children.OfType<Grid>().ToList())
            {
                var mediaRef = container.Tag as MediaReference;
                if (mediaRef != null)
                {
                    mediaRef.ShowDate = value;
                    UpdateContainerVisibility(container, mediaRef);
                }
            }
            OnMediaChanged?.Invoke();
        }

        private void ToggleShowTimeForAll(bool value)
        {
            foreach (var container in ImagePanel.Children.OfType<Grid>().ToList())
            {
                var mediaRef = container.Tag as MediaReference;
                if (mediaRef != null)
                {
                    mediaRef.ShowTime = value;
                    UpdateContainerVisibility(container, mediaRef);
                }
            }
            OnMediaChanged?.Invoke();
        }

        private void ToggleHiddenForAll(bool currentlyAnyHidden)
        {
            var targetState = !currentlyAnyHidden; // if any hidden → show all, else hide all
            foreach (var container in ImagePanel.Children.OfType<Grid>().ToList())
            {
                var mediaRef = container.Tag as MediaReference;
                if (mediaRef != null)
                {
                    mediaRef.IsHidden = targetState;
                    var imageGrid = container.Children.OfType<Grid>().FirstOrDefault();
                    if (targetState)
                    {
                        // Hide — show placeholder
                        imageGrid!.Children.Clear();
                        imageGrid.Children.Add(new TextBlock
                        {
                            Text = "· · ·",
                            Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush"),
                            Opacity = 0.3,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16
                        });
                    }
                    else
                    {
                        // Show — rebuild with image
                        RebuildSingleContainer(container, mediaRef);
                    }
                }
            }
            OnMediaChanged?.Invoke();
        }

        private void DeleteAllImages()
        {
            var ownerWindow = Window.GetWindow(this);
            var confirmed = DialogHelper.ShowConfirmation(
                ownerWindow,
                "Delete all images in this note? This cannot be undone.",
                "Delete All Images",
                "⚠️");

            if (!confirmed)
                return;

            var containers = ImagePanel.Children.OfType<Grid>().ToList();
            foreach (var container in containers)
            {
                var mediaRef = container.Tag as MediaReference;
                if (mediaRef != null)
                {
                    try
                    {
                        if (File.Exists(mediaRef.FullPath))
                            File.Delete(mediaRef.FullPath);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogErrorStatic("Failed to delete image file", ex, "Media", new {
                            FileName = PathSanitizer.SanitizePath(mediaRef.FullPath)
                        });
                    }
                }
            }

            imageFiles.Clear();
            imageTimestamps.Clear();
            ImagePanel.Children.Clear();
            OnMediaChanged?.Invoke();
        }

        // === UI-3.4 Context Menu Methods ===

        private MaterialDesignThemes.Wpf.PackIcon CreateIcon(MaterialDesignThemes.Wpf.PackIconKind kind)
        {
            return new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = kind,
                Width = 16,
                Height = 16
            };
        }

        private void SetupContainerInteractions(Grid container, string imagePath, MediaReference mediaRef)
        {
            // Hover effects
            container.MouseEnter += (s, e) =>
            {
                if (!isDragging)
                    container.Background = (System.Windows.Media.Brush)FindResource("HoverTransparentBrush");
            };
            container.MouseLeave += (s, e) =>
            {
                if (!isDragging)
                    container.Background = Brushes.Transparent;
            };

            // Right-click context menu — lazily built on first open (LEAK-16)
            // Empty placeholder required for ContextMenuOpening to fire.
            container.ContextMenu = new ContextMenu();
            container.ContextMenuOpening += (_, _) =>
            {
                var menu = (ContextMenu)container.ContextMenu;
                if (menu.Items.Count > 0) return;

                if (Application.Current.Resources.Contains("NativeContextMenuStyle"))
                    menu.Style = (Style)Application.Current.Resources["NativeContextMenuStyle"];

                var copyItem = new MenuItem { Header = "Copy", Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.ContentCopy) };
                copyItem.Click += (s, e) => CopyImageToClipboard(mediaRef.FullPath);
                menu.Items.Add(copyItem);

                var deleteItem = new MenuItem { Header = "Delete", Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.Delete) };
                deleteItem.Click += (s, e) => DeleteImageFromMenu(container, mediaRef);
                menu.Items.Add(deleteItem);

                var sizeMenu = new MenuItem { Header = "Size", Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.Resize) };
                var sizes = new (string Label, int Size)[]
                {
                    ("Small (60px)", AppConstants.ThumbnailSizeSmall),
                    ("Medium (100px)", AppConstants.ThumbnailSizeMedium),
                    ("Big (150px)", AppConstants.ThumbnailSizeBig)
                };
                foreach (var (label, size) in sizes)
                {
                    var sizeItem = new MenuItem { Header = label, IsCheckable = true, IsChecked = mediaRef.ThumbnailSize == size };
                    var capturedSize = size;
                    sizeItem.Click += (s, e) => ChangeThumbnailSize(container, mediaRef, capturedSize);
                    sizeMenu.Items.Add(sizeItem);
                }
                menu.Items.Add(sizeMenu);

                var hideItem = new MenuItem
                {
                    Header = mediaRef.IsHidden ? "Show" : "Hide",
                    Icon = CreateIcon(mediaRef.IsHidden ? MaterialDesignThemes.Wpf.PackIconKind.Eye : MaterialDesignThemes.Wpf.PackIconKind.EyeOff)
                };
                hideItem.Click += (s, e) => ToggleHidden(container, mediaRef);
                menu.Items.Add(hideItem);

                menu.Items.Add(new Separator());

                var labelItem = new MenuItem { Header = "Label", IsCheckable = true, IsChecked = mediaRef.ShowLabel };
                labelItem.Click += (s, e) => ToggleMediaBool((MenuItem)s!, container, mediaRef, m => m.ShowLabel = (bool)((MenuItem)s!).IsChecked!);
                menu.Items.Add(labelItem);

                var dateItem = new MenuItem { Header = "Date", IsCheckable = true, IsChecked = mediaRef.ShowDate };
                dateItem.Click += (s, e) => ToggleMediaBool((MenuItem)s!, container, mediaRef, m => m.ShowDate = (bool)((MenuItem)s!).IsChecked!);
                menu.Items.Add(dateItem);

                var timeItem = new MenuItem { Header = "Time", IsCheckable = true, IsChecked = mediaRef.ShowTime };
                timeItem.Click += (s, e) => ToggleMediaBool((MenuItem)s!, container, mediaRef, m => m.ShowTime = (bool)((MenuItem)s!).IsChecked!);
                menu.Items.Add(timeItem);

                menu.Items.Add(new Separator());

                var renameItem = new MenuItem { Header = "Rename...", Icon = CreateIcon(MaterialDesignThemes.Wpf.PackIconKind.Edit) };
                renameItem.Click += (s, e) => EditLabel(container, mediaRef);
                menu.Items.Add(renameItem);
            };

            // Double-click on label row to edit
            container.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2 && e.OriginalSource is TextBlock label && Grid.GetRow(label) == 1)
                    EditLabel(container, mediaRef);
            };

            // Drag handlers
            AddDragHandlers(container, imagePath);
        }

        private void CopyImageToClipboard(string imagePath)
        {
            try
            {
                BitmapImage? bitmap = ImageCacheManager.Instance.GetFromCache(imagePath);
                if (bitmap == null)
                {
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.EndInit();
                }
                bitmap.Freeze();
                System.Windows.Clipboard.SetImage(bitmap);
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to copy image to clipboard", ex, "Media", new {
                    FileName = PathSanitizer.SanitizePath(imagePath)
                });
            }
        }

        private void DeleteImageFromMenu(Grid container, MediaReference mediaRef)
        {
            var imagePath = mediaRef.FullPath;
            try
            {
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to delete image file", ex, "Media", new {
                    FileName = PathSanitizer.SanitizePath(imagePath)
                });
            }
            RemoveImageContainer(container, imagePath);
        }

        private void ChangeThumbnailSize(Grid container, MediaReference mediaRef, int newSize)
        {
            mediaRef.ThumbnailSize = newSize;
            RebuildSingleContainer(container, mediaRef);
        }

        private void ToggleHidden(Grid container, MediaReference mediaRef)
        {
            mediaRef.IsHidden = !mediaRef.IsHidden;
            var imageGrid = container.Children.OfType<Grid>().First();
            if (mediaRef.IsHidden)
            {
                imageGrid.Children.Clear();
                imageGrid.Children.Add(new TextBlock
                {
                    Text = "· · ·",
                    Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush"),
                    Opacity = 0.3,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16
                });
            }
            else
            {
                RebuildSingleContainer(container, mediaRef);
            }
            OnMediaChanged?.Invoke();
        }

        private void ToggleMediaBool(MenuItem menuItem, Grid container, MediaReference mediaRef, Action<MediaReference> setProperty)
        {
            setProperty(mediaRef);
            UpdateContainerVisibility(container, mediaRef);
            OnMediaChanged?.Invoke();
        }

        private void UpdateContainerVisibility(Grid container, MediaReference mediaRef)
        {
            // Update label row (Row 1)
            var labelBlock = container.Children.OfType<TextBlock>()
                .FirstOrDefault(tb => Grid.GetRow(tb) == 1);
            if (labelBlock != null)
            {
                labelBlock.Visibility = (mediaRef.ShowLabel && !string.IsNullOrEmpty(mediaRef.Label))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                labelBlock.Text = mediaRef.Label;
            }

            // Update timestamp row (Row 2)
            var timestampBlock = container.Children.OfType<TextBlock>()
                .FirstOrDefault(tb => Grid.GetRow(tb) == 2);
            if (timestampBlock != null)
            {
                var newText = CreateTimestampText(mediaRef);
                int idx = container.Children.IndexOf(timestampBlock);
                container.Children.RemoveAt(idx);
                container.Children.Insert(idx, newText);
                Grid.SetRow(newText, 2);
            }
        }

        private void UpdateTimestampText(Grid container, MediaReference mediaRef)
        {
            var timestampBlock = container.Children.OfType<TextBlock>()
                .FirstOrDefault(tb => Grid.GetRow(tb) == 2);
            if (timestampBlock != null)
            {
                var newText = CreateTimestampText(mediaRef);
                int idx = container.Children.IndexOf(timestampBlock);
                container.Children.RemoveAt(idx);
                container.Children.Insert(idx, newText);
                Grid.SetRow(newText, 2);
            }
        }

        private void EditLabel(Grid container, MediaReference mediaRef)
        {
            var result = CustomInputDialog.ShowInput(
                Window.GetWindow(this),
                "Enter label for this image:",
                "Rename Image",
                mediaRef.Label,
                "✏️");

            if (result.success && result.input != null)
            {
                mediaRef.Label = result.input.Trim();
                mediaRef.ShowLabel = !string.IsNullOrEmpty(mediaRef.Label);

                var labelBlock = container.Children.OfType<TextBlock>()
                    .FirstOrDefault(tb => Grid.GetRow(tb) == 1);

                if (labelBlock != null)
                {
                    labelBlock.Text = mediaRef.Label;
                    labelBlock.Visibility = mediaRef.ShowLabel ? Visibility.Visible : Visibility.Collapsed;
                }

                OnMediaChanged?.Invoke();
            }
        }

        private void RebuildSingleContainer(Grid container, MediaReference mediaRef)
        {
            var panel = container.Parent as WrapPanel;
            if (panel == null) return;

            var oldIndex = panel.Children.IndexOf(container);
            var imagePath = mediaRef.FullPath;

            panel.Children.Remove(container);

            var newContainer = CreatePlaceholderContainer(imagePath, mediaRef);
            if (oldIndex >= panel.Children.Count)
                panel.Children.Add(newContainer);
            else
                panel.Children.Insert(oldIndex, newContainer);

            EnsureThumbnailLoaded(newContainer, imagePath, mediaRef.DateAdded, mediaRef);
            OnMediaChanged?.Invoke();
        }

        /// <summary>
        /// Creates a placeholder container (no image decoded yet). The async loader
        /// replaces the placeholder once the thumbnail is ready.
        /// </summary>
        private Grid CreatePlaceholderContainer(string imagePath, MediaReference? mediaRef = null)
        {
            var refData = mediaRef ?? new MediaReference
            {
                Filename = Path.GetFileName(imagePath),
                DateAdded = DateTime.Now
            };
            int containerWidth = refData.ThumbnailSize;

            var container = new Grid
            {
                Width = containerWidth,
                MinHeight = CalculateMinHeight(containerWidth),
                Margin = new Thickness(5),
                Background = (System.Windows.Media.Brush)FindResource("ContentCardBrush")
            };
            container.Tag = refData;

            // 3-row layout: Image (auto), Label (auto), Timestamp (auto)
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Placeholder image area
            var imageGrid = new Grid { MinHeight = containerWidth - 10 };
            imageGrid.Children.Add(new TextBlock
            {
                Text = "· · ·",
                Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush"),
                Opacity = 0.3,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16
            });
            Grid.SetRow(imageGrid, 0);
            container.Children.Add(imageGrid);

            // Row 1: Label (conditionally visible)
            var labelText = new TextBlock
            {
                Text = refData.Label,
                FontSize = SnipShottyBoard.Data.AppConstants.SmallFontSize,
                Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush"),
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2, 2, 2, 0),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Visibility = (refData.ShowLabel && !string.IsNullOrEmpty(refData.Label))
                    ? Visibility.Visible
                    : Visibility.Collapsed
            };
            Grid.SetRow(labelText, 1);
            container.Children.Add(labelText);

            // Row 2: Timestamp
            var timestampText = CreateTimestampText(refData, "loading…");
            Grid.SetRow(timestampText, 2);
            container.Children.Add(timestampText);

            SetupContainerInteractions(container, imagePath, refData);

            return container;
        }

        /// <summary>
        /// Calculate the minimum height for a container based on its width.
        /// Maintains a roughly square aspect ratio for the image area.
        /// </summary>
        private int CalculateMinHeight(int containerWidth)
        {
            return containerWidth + 20; // Image area + label + timestamp overhead
        }

        /// <summary>
        /// Asynchronously loads a thumbnail and replaces the placeholder in the container.
        /// Limited to 4 concurrent decodes via _loadSemaphore.
        /// </summary>
        private async Task LoadThumbnailAsync(Grid container, string imagePath, DateTime? timestamp, MediaReference? mediaRef = null, CancellationToken cancellationToken = default)
        {
            await _loadSemaphore.WaitAsync(cancellationToken);

            try
            {
                BitmapImage? bitmap = null;

                // Decode on a background thread
                await Task.Run(() =>
                {
                    var extension = Path.GetExtension(imagePath).ToLowerInvariant();

                    // Check cache first (cache access is thread-confined but safe for reads)
                    bitmap = Application.Current.Dispatcher.Invoke(() =>
                        ImageCacheManager.Instance.GetFromCache(imagePath));

                    if (bitmap != null)
                        return;

                    if (extension == ".gif")
                        bitmap = CreateStaticGifThumbnail(imagePath);
                    else
                        bitmap = CreateThumbnailBitmap(imagePath);
                }, cancellationToken);

                if (bitmap == null)
                    return;

                // Update UI on dispatcher thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Ensure container is still in the visual tree
                    if (container.Parent == null)
                        return;

                    // Cache the bitmap
                    ImageCacheManager.Instance.AddToCache(imagePath, bitmap);

                    // Get the MediaReference from the container tag
                    var refData = container.Tag as MediaReference
                        ?? new MediaReference { Filename = Path.GetFileName(imagePath), DateAdded = timestamp ?? DateTime.Now };

                    // Replace placeholder content with actual image
                    var imageGrid = container.Children.OfType<Grid>().First();
                    imageGrid.Children.Clear();

                    var containerWidth = refData.ThumbnailSize;
                    var image = new Image
                    {
                        Source = bitmap,
                        MaxWidth = containerWidth,
                        MaxHeight = containerWidth - 10,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    imageGrid.Children.Add(image);

                    // Update timestamp text (row 2 — the last TextBlock)
                    var timestampText = container.Children.OfType<TextBlock>()
                        .LastOrDefault(tb => Grid.GetRow(tb) == 2);
                    if (timestampText != null)
                    {
                        timestampText.Text = CreateTimestampText(refData).Text;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Expected when the MediaSection is disposed
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to load thumbnail async", ex, "Media", new {
                    FileName = PathSanitizer.SanitizePath(imagePath)
                });
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        /// <summary>
        /// Create a TextBlock displaying the timestamp based on MediaReference visibility settings.
        /// Falls back to a default combined date+time string if no ref is available.
        /// </summary>
        private TextBlock CreateTimestampText(MediaReference mediaRef, string? overrideText = null)
        {
            string displayText;
            if (overrideText != null)
            {
                displayText = overrideText;
            }
            else
            {
                var dateStr = mediaRef.ShowDate ? mediaRef.DateAdded.ToString("M/d/yy") : "";
                var timeStr = mediaRef.ShowTime ? mediaRef.DateAdded.ToString("h:mmtt").ToLower() : "";
                var parts = new[] { dateStr, timeStr }.Where(s => !string.IsNullOrEmpty(s));
                displayText = string.Join(" ", parts);
            }

            return new TextBlock
            {
                Text = displayText,
                FontSize = SnipShottyBoard.Data.AppConstants.SmallFontSize,
                Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush"),
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2, 2, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Visibility = string.IsNullOrEmpty(displayText) ? Visibility.Collapsed : Visibility.Visible
            };
        }

        /// <summary>
        /// Fire-and-forget: starts async thumbnail load for a path.
        /// Uses cancellation token to clean up on Dispose.
        /// </summary>
        private void EnsureThumbnailLoaded(Grid container, string imagePath, DateTime? timestamp, MediaReference? mediaRef = null)
        {
            // Use the shared dispose token — all thumbnail loads for this section share one
            // lifetime and are cancelled together only when the section is disposed.
            _ = Task.Run(() => LoadThumbnailAsync(container, imagePath, timestamp, mediaRef, _disposeToken.Token));
        }

        // 📦 Create image container with 3-row layout and drag support
        private Grid CreateImageContainer(Image imageControl, string imagePath, MediaReference? mediaRef = null)
        {
            var refData = mediaRef ?? new MediaReference
            {
                Filename = Path.GetFileName(imagePath),
                DateAdded = imageTimestamps.ContainsKey(imagePath) ? imageTimestamps[imagePath] : DateTime.Now
            };
            int containerWidth = refData.ThumbnailSize;

            var container = new Grid
            {
                Width = containerWidth,
                MinHeight = CalculateMinHeight(containerWidth),
                Margin = new Thickness(5),
                Background = Brushes.Transparent
            };

            // Store full MediaReference on the container tag
            container.Tag = refData;

            // 3-row layout: Image (auto), Label (auto), Timestamp (auto)
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Image area
            var imageGrid = new Grid { MinHeight = containerWidth - 10 };
            imageControl.HorizontalAlignment = HorizontalAlignment.Center;
            imageControl.VerticalAlignment = VerticalAlignment.Center;
            imageGrid.Children.Add(imageControl);
            Grid.SetRow(imageGrid, 0);
            container.Children.Add(imageGrid);

            // Row 1: Label (conditionally visible)
            var labelText = new TextBlock
            {
                Text = refData.Label,
                FontSize = SnipShottyBoard.Data.AppConstants.SmallFontSize,
                Foreground = (System.Windows.Media.Brush)FindResource("AppForegroundBrush"),
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2, 2, 2, 0),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Visibility = (refData.ShowLabel && !string.IsNullOrEmpty(refData.Label))
                    ? Visibility.Visible
                    : Visibility.Collapsed
            };
            Grid.SetRow(labelText, 1);
            container.Children.Add(labelText);

            // Row 2: Timestamp
            var timestampText = CreateTimestampText(refData);
            Grid.SetRow(timestampText, 2);
            container.Children.Add(timestampText);

            SetupContainerInteractions(container, imagePath, refData);

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
            }
            pendingClickContainer = null;
            pendingClickImagePath = null;
            dragStartPoint = default(Point);
        }

        // 🧹 Dispose timer and cancel pending async loads when control is unloaded
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

                // Cancel all pending thumbnail loads for this section
                _disposeToken.Cancel();
                _disposeToken.Dispose();
                _loadSemaphore?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Error disposing MediaSection", ex, "UI");
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
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Insertion indicator placed at UI position {insertPosition}, data index {index}");
#endif
            }
            catch (ArgumentOutOfRangeException)
            {
                // Fallback: add at end
                ImagePanel.Children.Add(insertionIndicator);
                dropTargetIndex = actualContainers.Count;
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Fallback: Insertion indicator added at end");
#endif
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

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Target calculation: dropIndex={dropTargetIndex}, originalIndex={originalDataIndex}, targetDataIndex={targetDataIndex}");
#endif
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
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Drag move: targetIndex={targetIndex}, dropTargetIndex={dropTargetIndex}, panelPos=({panelPosition.X:F1}, {panelPosition.Y:F1})");
#endif
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
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Invalid reorder indices: from={fromDataIndex}, to={toDataIndex}, count={imageFiles.Count}");
#endif
                return;
            }

            // Ensure target index is valid
            toDataIndex = Math.Max(0, Math.Min(toDataIndex, imageFiles.Count - 1));

            if (fromDataIndex == toDataIndex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("No reordering needed - same position");
#endif
                return;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Reordering image: from={fromDataIndex} to={toDataIndex}");
#endif

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
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Reordering completed successfully");
#endif
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Error during image reordering", ex, "UI");
                // Try to recover by rebuilding UI
                RebuildUIFromData();
            }
        }

        // 🏗️ Rebuild UI to match data order — uses lazy async loading (Sprint B B.1)
        // ✅ Preserves v3 metadata (Label, ThumbnailSize, IsHidden, ShowLabel, ShowDate, ShowTime)
        private void RebuildUIFromData()
        {
            CancelClickDetection(); // Clear stale container ref before rebuild

            // Store current dragged container reference
            var wasDragging = isDragging;
            var draggedPath = draggedImagePath;

            // Collect existing v3 metadata from containers BEFORE clearing
            var existingRefs = new Dictionary<string, MediaReference>();
            foreach (var container in ImagePanel.Children.OfType<Grid>())
            {
                if (ReferenceEquals(container, insertionIndicator))
                    continue;
                var refData = container.Tag as MediaReference;
                if (refData != null)
                {
                    var fullPath = refData.FullPath;
                    if (!string.IsNullOrEmpty(fullPath))
                        existingRefs[fullPath] = refData;
                }
            }

            // Clear UI (but keep drag canvas and insertion indicator logic intact)
            var containersToRemove = ImagePanel.Children
                .OfType<Grid>()
                .Where(g => !ReferenceEquals(g, insertionIndicator))
                .ToList();

            foreach (var container in containersToRemove)
            {
                ImagePanel.Children.Remove(container);
            }

            // Rebuild from data order — reuse existing refs to preserve v3 metadata
            foreach (var imagePath in imageFiles)
            {
                try
                {
                    // ✅ Layer separation: File I/O delegated to DataManager (Phase 4C P1.3)
                    if (!DataManager.ValidateImageFile(imagePath))
                        continue;

                    // Reuse existing MediaReference (preserves v3 metadata) or create new
                    var timestamp = imageTimestamps.TryGetValue(imagePath, out var ts) ? ts : DateTime.Now;
                    MediaReference mediaRef;
                    if (existingRefs.TryGetValue(imagePath, out var existingRef))
                    {
                        mediaRef = existingRef;
                    }
                    else
                    {
                        mediaRef = new MediaReference
                        {
                            Filename = Path.GetFileName(imagePath),
                            DateAdded = timestamp
                        };
                    }

                    // 📦 Add placeholder container (instant, no decode)
                    var container = CreatePlaceholderContainer(imagePath, mediaRef);
                    ImagePanel.Children.Add(container);

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

                    // 🚀 Start async thumbnail load (fire-and-forget, semaphore-limited)
                    EnsureThumbnailLoaded(container, imagePath, timestamp, mediaRef);
                }
                catch (Exception ex)
                {
                    LoggingService.LogErrorStatic("Failed to rebuild container", ex, "Media", new {
                        FileName = PathSanitizer.SanitizePath(imagePath)
                    });
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

        // 📂 Load images from file paths — uses lazy async loading (Sprint B B.1)
        private void LoadImagesFromFiles(Dictionary<string, MediaReference>? mediaRefsDict = null)
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
                        // Use provided MediaReference or build from file
                        MediaReference mediaRef;
                        if (mediaRefsDict != null && mediaRefsDict.TryGetValue(imagePath, out var existingRef))
                        {
                            mediaRef = existingRef;
                        }
                        else
                        {
                            var fileInfo = new FileInfo(imagePath);
                            var timestamp = fileInfo.CreationTime;
                            imageTimestamps[imagePath] = timestamp;
                            mediaRef = new MediaReference
                            {
                                Filename = Path.GetFileName(imagePath),
                                DateAdded = timestamp
                            };
                        }

                        // 📦 Add placeholder container (instant, no decode)
                        var container = CreatePlaceholderContainer(imagePath, mediaRef);
                        ImagePanel.Children.Add(container);

                        // 🚀 Start async thumbnail load (fire-and-forget, semaphore-limited)
                        EnsureThumbnailLoaded(container, imagePath, mediaRef.DateAdded, mediaRef);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogErrorStatic("Failed to create image placeholder", ex, "Media", new {
                        FileName = PathSanitizer.SanitizePath(imagePath)
                    });
                }
            }
        }

        // 🖼️ Show full-size image in custom viewer window
        private void ShowFullSizeImage(string imagePath)
        {
            try
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"🖼️ ===== MEDIASECTION OPENING IMAGE VIEWER =====");
                System.Diagnostics.Debug.WriteLine($"🖼️ MediaSection.ShowFullSizeImage() called");
                System.Diagnostics.Debug.WriteLine($"🖼️ Opening ImageViewer for: {imagePath}");
#endif
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"🖼️ File extension: {extension}");

                if (extension == ".gif")
                {
                    System.Diagnostics.Debug.WriteLine($"🎬 MEDIASECTION: This is a GIF file - animation should be preserved");
                    System.Diagnostics.Debug.WriteLine($"🎬 MEDIASECTION: About to create ImageViewerWindow for GIF");
                }
#endif
                // Read image paths directly from what's rendered in the UI (eliminates ghost paths)
                var validImages = ImagePanel.Children
                    .OfType<Grid>()
                    .Select(container => (container.Tag as MediaReference)?.FullPath)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Select(path => path!)
                    .ToList();

                // Find clicked image position within visible thumbnails
                var currentIndex = validImages.IndexOf(imagePath);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"🖼️ Current image index: {currentIndex} of {validImages.Count} visible images");
#endif

                // Reuse existing viewer for this path instead of opening a duplicate
                var existingViewer = Application.Current.Windows
                    .OfType<ImageViewerWindow>()
                    .FirstOrDefault(w => w.CurrentImagePath == imagePath);

                if (existingViewer != null)
                {
                    existingViewer.WindowState = System.Windows.WindowState.Normal;
                    existingViewer.Activate();
                    existingViewer.Focus();
                    return;
                }

                // 🔗 Create viewer with clean navigation list (only displayed thumbnails)
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"🖼️ Creating ImageViewerWindow...");
#endif
                var imageViewer = new ImageViewerWindow(imagePath, validImages, currentIndex, RemoveImageByPath);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"🖼️ ImageViewerWindow created successfully");
#endif
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

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"🖼️ About to show ImageViewerWindow...");
#endif
                imageViewer.Show();
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"🖼️ ImageViewerWindow.Show() completed");

                if (extension == ".gif")
                {
                    System.Diagnostics.Debug.WriteLine($"🎬 MEDIASECTION: GIF should now be visible and animating in ImageViewerWindow");
                }
                System.Diagnostics.Debug.WriteLine($"🖼️ ===== MEDIASECTION OPENING COMPLETE =====");
#endif
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to open image viewer", ex, "UI");
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
                    .FirstOrDefault(container => (container.Tag as MediaReference)?.FullPath == imagePath);
                
                if (containerToRemove != null)
                {
                    // 🔓 Quick image source clear
                    ClearImageSourceInContainer(containerToRemove);
                    
                    // 🗑️ Instant UI removal
                    RemoveImageContainer(containerToRemove, imagePath);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"✅ Instantly removed: {imagePath}");
#endif
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Error removing image", ex, "UI");
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
                LoggingService.LogErrorStatic("Error handling dropped files", ex, "UI");
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
#if DEBUG
                                System.Diagnostics.Debug.WriteLine($"❌ UI: Failed to copy dropped image: {sourcePath}");
#endif
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
#if DEBUG
                                        System.Diagnostics.Debug.WriteLine($"❌ Failed to create thumbnail for dropped image: {newFileName}");
#endif
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
#if DEBUG
                                    System.Diagnostics.Debug.WriteLine($"✅ Added dropped image: {newFileName}");
#endif
                                }
                                catch (Exception ex)
                                {
                                    LoggingService.LogErrorStatic("Error adding dropped image to UI", ex, "UI");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogErrorStatic("Error copying dropped file", ex, "UI");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Error processing dropped images", ex, "UI");
            }
        }
        
        #endregion
    }
} 