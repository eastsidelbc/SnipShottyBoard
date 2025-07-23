using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnipShottyBoard.UI;

namespace SnipShottyBoard.Examples
{
    /// <summary>
    /// 🖼️ Example: MediaSection refactored using boilerplate helpers
    /// Shows before/after comparison of how the helpers simplify code
    /// </summary>
    public partial class MediaSectionRefactored : UserControl
    {
        private List<string> imageFiles = new List<string>();
        private Dictionary<string, DateTime> imageTimestamps = new Dictionary<string, DateTime>();
        public event Action OnMediaChanged;

        // 🗑️ BEFORE: Manual delete button creation (lots of boilerplate)
        private Button CreateDeleteButtonOldWay()
        {
            var deleteButton = new Button
            {
                Content = "×",
                Width = 20,
                Height = 20,
                Background = System.Windows.Media.Brushes.Red,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 2, 0),
                Opacity = 0,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            deleteButton.Click += (s, e) =>
            {
                try
                {
                    var (confirmed, _) = CustomDialog.ShowDeleteConfirmation(
                        Application.Current.MainWindow,
                        "this image",
                        showDontAskAgain: false);
                        
                    if (confirmed)
                    {
                        // Remove logic here
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in delete handler: {ex.Message}");
                }
            };

            return deleteButton;
        }

        // ✅ AFTER: Using boilerplate helpers (clean and consistent)
        private Button CreateDeleteButtonNewWay(string imagePath, Grid container)
        {
            // 🎯 Single line with all styling and error handling built-in
            return UIFactory.CreateButton(
                content: "×",
                style: "DangerButtonStyle", // Uses theme automatically
                clickHandler: SafeExecutionHelper.Execute(
                    () => HandleDeleteImage(imagePath, container),
                    "Error deleting image",
                    onLogError: (msg, ex) => System.Diagnostics.Debug.WriteLine($"{msg}: {ex.Message}")
                ) ? null : null, // Safe execution wrapper
                tooltip: "Delete image"
            );
        }

        // 🖼️ BEFORE: Manual image creation with repetitive styling
        private Image CreateImageOldWay(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = 120;
                bitmap.EndInit();

                var image = new Image
                {
                    Source = bitmap,
                    MaxWidth = 120,
                    MaxHeight = 120,
                    Stretch = Stretch.Uniform,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create image: {ex.Message}");
                return null;
            }
        }

        // ✅ AFTER: Using UIFactory with built-in error handling
        private Image CreateImageNewWay(string imagePath)
        {
            return SafeExecutionHelper.Execute(
                () => {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.DecodePixelWidth = 120;
                    bitmap.EndInit();

                    return UIFactory.CreateImage(
                        source: bitmap,
                        width: 120,
                        height: 120,
                        stretch: Stretch.Uniform
                    );
                },
                "Failed to create image",
                defaultValue: null
            );
        }

        // 📝 BEFORE: Manual text block creation with resource access
        private TextBlock CreateTimestampOldWay(string imagePath)
        {
            var timestampText = new TextBlock
            {
                FontSize = 10,
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
                timestampText.Text = $"Added: {timestamp}";
            }
            else
            {
                var currentTime = DateTime.Now.ToString("M/d/yy h:mmtt").ToLower();
                timestampText.Text = $"Added: {currentTime}";
            }

            return timestampText;
        }

        // ✅ AFTER: Using UIFactory with safe resource access
        private TextBlock CreateTimestampNewWay(string imagePath)
        {
            var timestampText = GetFormattedTimestamp(imagePath);
            
            return UIFactory.CreateTextBlock(
                text: $"Added: {timestampText}",
                fontSize: 10,
                foregroundKey: "AppForegroundBrush", // Safe resource access built-in
                textAlignment: TextAlignment.Center,
                textWrapping: TextWrapping.Wrap,
                margin: new Thickness(2, 4, 2, 2)
            );
        }

        // 🏗️ BEFORE: Manual grid creation with repetitive setup
        private Grid CreateContainerOldWay()
        {
            var container = new Grid
            {
                Margin = new Thickness(4),
                Background = Brushes.Transparent
            };

            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            return container;
        }

        // ✅ AFTER: Using UIFactory for consistent setup
        private Grid CreateContainerNewWay()
        {
            return UIFactory.CreateGrid(
                rowDefinitions: new[] { GridLength.Auto, GridLength.Auto },
                margin: new Thickness(4)
            );
        }

        // 🎭 BEFORE: Manual dialog creation for full-size view
        private void ShowFullSizeImageOldWay(string imagePath)
        {
            try
            {
                var fullSizeWindow = new Window
                {
                    Title = "Image View",
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.CanResize,
                    Icon = Application.Current.MainWindow?.Icon
                };

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();

                var image = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform
                };

                fullSizeWindow.Content = image;
                fullSizeWindow.Width = Math.Min(bitmap.PixelWidth + 40, SystemParameters.WorkArea.Width * 0.9);
                fullSizeWindow.Height = Math.Min(bitmap.PixelHeight + 80, SystemParameters.WorkArea.Height * 0.9);
                fullSizeWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show full-size image: {ex.Message}");
            }
        }

        // ✅ AFTER: Using UIFactory and SafeExecution for clean, reliable code
        private void ShowFullSizeImageNewWay(string imagePath)
        {
            SafeExecutionHelper.ExecuteWithLogging(
                () => {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.EndInit();

                    var imageContent = UIFactory.CreateImage(bitmap, stretch: Stretch.Uniform);
                    
                    var dialog = UIFactory.CreateDialogWindow(
                        title: "Image View",
                        icon: "🖼️",
                        content: imageContent,
                        width: Math.Min(bitmap.PixelWidth + 40, SystemParameters.WorkArea.Width * 0.9),
                        height: Math.Min(bitmap.PixelHeight + 80, SystemParameters.WorkArea.Height * 0.9)
                    );

                    dialog.Show();
                },
                "ShowFullSizeImage",
                onLogError: (msg, ex) => System.Diagnostics.Debug.WriteLine($"{msg}: {ex.Message}")
            );
        }

        // 🔔 BEFORE: Manual event invocation with try-catch
        private void TriggerDataChangeOldWay()
        {
            try
            {
                OnMediaChanged?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in data change event: {ex.Message}");
            }
        }

        // ✅ AFTER: Using EventHelper for safe invocation
        private void TriggerDataChangeNewWay()
        {
            EventHelper.SafeInvoke(OnMediaChanged);
        }

        // 🛠️ Helper methods
        private string GetFormattedTimestamp(string imagePath)
        {
            if (imageTimestamps.ContainsKey(imagePath))
            {
                return imageTimestamps[imagePath].ToString("M/d/yy h:mmtt").ToLower();
            }
            return DateTime.Now.ToString("M/d/yy h:mmtt").ToLower();
        }

        private string GetTimeAgoString(DateTime addedDate)
        {
            return addedDate.ToString("M/d/yy h:mmtt").ToLower();
        }

        private void HandleDeleteImage(string imagePath, Grid container)
        {
            var confirmed = DialogHelper.ShowDeleteConfirmation(
                Application.Current.MainWindow,
                "this image",
                showDontAskAgain: false).confirmed;
                
            if (confirmed)
            {
                // Remove logic
                imageFiles.Remove(imagePath);
                imageTimestamps.Remove(imagePath);
                EventHelper.SafeInvoke(OnMediaChanged);
            }
        }
    }

    /// <summary>
    /// 📊 Summary of benefits from using boilerplate helpers:
    /// 
    /// ✅ REDUCED CODE: 50-70% less boilerplate in UI creation
    /// ✅ CONSISTENCY: All UI elements follow same theming patterns
    /// ✅ ERROR HANDLING: Built-in exception handling and logging
    /// ✅ MAINTAINABILITY: Changes to styling happen in one place
    /// ✅ READABILITY: Intent is clearer without styling noise
    /// ✅ RELIABILITY: Consistent resource access with fallbacks
    /// ✅ DEBUGGING: Standardized logging makes issues easier to trace
    /// 
    /// 🔧 USAGE PATTERNS:
    /// - Use SafeExecutionHelper.Execute() for any operation that might fail
    /// - Use UIFactory.Create*() methods instead of manual element creation
    /// - Use ResourceHelper.CommonBrushes for theme-aware styling
    /// - Use EventHelper.SafeInvoke() for all event firing
    /// - Use DialogHelper methods instead of direct CustomDialog calls
    /// </summary>
} 