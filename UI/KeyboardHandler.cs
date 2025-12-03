using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SnipShottyBoard.UI.WindowManagement;
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
        public event Action<string> OnRichTextFormattingRequested; // New event for rich text formatting
        public event Action<string> OnTabNavigationRequested; // Arrow key navigation: "Left", "Right", "Up", "Down", "Home", "End"

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

                    // 🎨 Rich text formatting shortcuts (only when in RichTextBox)
                    case Key.B: // Ctrl+B: Bold
                        if (focusedElement is RichTextBox)
                        {
                            OnRichTextFormattingRequested?.Invoke("bold");
                            e.Handled = true;
                        }
                        break;

                    case Key.I: // Ctrl+I: Italic
                        if (focusedElement is RichTextBox)
                        {
                            OnRichTextFormattingRequested?.Invoke("italic");
                            e.Handled = true;
                        }
                        break;

                    case Key.U: // Ctrl+U: Underline
                        if (focusedElement is RichTextBox)
                        {
                            OnRichTextFormattingRequested?.Invoke("underline");
                            e.Handled = true;
                        }
                        break;

                    case Key.S: // Ctrl+S: Strikethrough
                        if (focusedElement is RichTextBox)
                        {
                            OnRichTextFormattingRequested?.Invoke("strikethrough");
                            e.Handled = true;
                        }
                        break;

                    case Key.OemPeriod: // Ctrl+.: Bullet list
                        if (focusedElement is RichTextBox)
                        {
                            OnRichTextFormattingRequested?.Invoke("bullet");
                            e.Handled = true;
                        }
                        break;

                    case Key.L: // Ctrl+L: Numbered list
                        if (focusedElement is RichTextBox)
                        {
                            OnRichTextFormattingRequested?.Invoke("numbered");
                            e.Handled = true;
                        }
                        break;
                }
            }
            else if (e.Key == Key.Tab)
            {
                // Tab/Shift+Tab: Indent/Unindent (only in RichTextBox)
                var focusedElement = Keyboard.FocusedElement;
                if (focusedElement is RichTextBox)
                {
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        OnRichTextFormattingRequested?.Invoke("unindent");
                    }
                    else
                    {
                        OnRichTextFormattingRequested?.Invoke("indent");
                    }
                    e.Handled = true;
                }
            }
            else
            {
                // 🎯 Arrow key navigation (only if not in text input)
                var focusedElement = Keyboard.FocusedElement;
                var isInTextInput = focusedElement is TextBox || focusedElement is RichTextBox;
                
                if (!isInTextInput)
                {
                    switch (e.Key)
                    {
                        case Key.Left:
                            OnTabNavigationRequested?.Invoke("Left");
                            e.Handled = true;
                            break;
                            
                        case Key.Right:
                            OnTabNavigationRequested?.Invoke("Right");
                            e.Handled = true;
                            break;
                            
                        case Key.Up:
                            OnTabNavigationRequested?.Invoke("Up");
                            e.Handled = true;
                            break;
                            
                        case Key.Down:
                            OnTabNavigationRequested?.Invoke("Down");
                            e.Handled = true;
                            break;
                            
                        case Key.Home:
                            OnTabNavigationRequested?.Invoke("Home");
                            e.Handled = true;
                            break;
                            
                        case Key.End:
                            OnTabNavigationRequested?.Invoke("End");
                            e.Handled = true;
                            break;
                    }
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

        // 💾 Save image from clipboard to file using DataManager
        private string? SaveImageFromClipboard()
        {
            try
            {
                var imageSource = Clipboard.GetImage();
                return DataManager.SaveImageFromClipboard(imageSource);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UI: Error accessing clipboard image: {ex.Message}");
                return null;
            }
        }
    }
} 