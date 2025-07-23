using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.UI
{
    // ⌨️ KeyboardHandler - Handles keyboard shortcuts and clipboard operations
    public class KeyboardHandler
    {
        // Using static DataManager - no instance needed

        // Events for communicating with MainWindow
        public event Action OnNewTabRequested;
        public event Action OnDeleteTabRequested;
        public event Action OnRenameTabRequested;
        public event Action OnSwitchTabRequested;
        public event Action<Image, string> OnImagePasted;

        public KeyboardHandler()
        {
            // No DataManager instance needed since it's static
        }

        // 🧠 Handle keyboard shortcuts
        public void HandleKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Check if focus is in a text input once for all cases
                var focusedElement = Keyboard.FocusedElement;
                var isInTextInput = focusedElement is TextBox || focusedElement is RichTextBox;
                
                switch (e.Key)
                {
                    case Key.T: // Ctrl+T: New tab
                        OnNewTabRequested?.Invoke();
                        e.Handled = true;
                        break;

                    case Key.W: // Ctrl+W: Delete current tab
                        OnDeleteTabRequested?.Invoke();
                        e.Handled = true;
                        break;

                    case Key.R: // Ctrl+R: Rename current tab
                        OnRenameTabRequested?.Invoke();
                        e.Handled = true;
                        break;

                    case Key.Tab: // Ctrl+Tab: Switch to next tab
                        // Only handle tab switching if not in a text input
                        if (!isInTextInput)
                        {
                            OnSwitchTabRequested?.Invoke();
                            e.Handled = true;
                        }
                        // Otherwise, let standard tab behavior work in text inputs
                        break;

                    case Key.V: // Ctrl+V: Paste image (only if not in text input)
                        // Only handle image paste if not in a text input
                        if (!isInTextInput)
                        {
                            HandleImagePaste();
                            e.Handled = true;
                        }
                        // Otherwise, let standard text paste work normally
                        break;
                }
            }
        }

        // 🖼️ Handle image paste from clipboard
        private void HandleImagePaste()
        {
            if (!Clipboard.ContainsImage()) return;

            try
            {
                var imagePath = SaveImageFromClipboard();
                if (string.IsNullOrEmpty(imagePath)) return;

                // 🖼️ Create thumbnail for display
                var thumbBitmap = new BitmapImage();
                thumbBitmap.BeginInit();
                thumbBitmap.UriSource = new Uri(imagePath);
                thumbBitmap.DecodePixelWidth = 150;
                thumbBitmap.EndInit();
                thumbBitmap.Freeze();

                var imgControl = new Image
                {
                    Source = thumbBitmap,
                    Width = 150,
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand
                };

                // 🔍 Click to view full size - removed since MediaSection handles this
                // imgControl.MouseLeftButtonUp += (s, args) =>
                // {
                //     ShowFullSizeImage(imagePath);
                // };

                // 📝 Notify that image was pasted
                OnImagePasted?.Invoke(imgControl, imagePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to paste image: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Note: ShowFullSizeImage removed - MediaSection handles image viewing with proper delete callbacks

        // 💾 Save image from clipboard to file
        private string SaveImageFromClipboard()
        {
            try
            {
                var imageSource = Clipboard.GetImage();
                if (imageSource == null) return null;

                // 📁 Generate unique filename
                var imagesFolder = DataManager.GetImagesFolder();
                var fileName = $"img_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}.png";
                var fullPath = System.IO.Path.Combine(imagesFolder, fileName);

                // 💾 Save image to file
                using var fileStream = new System.IO.FileStream(fullPath, System.IO.FileMode.Create);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(imageSource));
                encoder.Save(fileStream);

                return fullPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving clipboard image: {ex.Message}");
                return null;
            }
        }
    }
} 