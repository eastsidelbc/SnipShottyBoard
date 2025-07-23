using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 📝 TextSection - Pure text input component with placeholder functionality
    /// Simple, standalone text field that behaves naturally
    /// </summary>
    public partial class TextSection : UserControl
    {
        // 🔔 Event to notify when text content changes
        public event Action OnTextChanged;

        // 📝 Properties for text content access
        public string TextContent 
        { 
            get => NoteTextBox.Text; 
            set 
            { 
                NoteTextBox.Text = value;
                UpdatePlaceholderVisibility();
            } 
        }

        // ✅ Expose the TextBox for external access if needed
        public TextBox TextBox => NoteTextBox;

        public TextSection()
        {
            InitializeComponent();
            
            // 🧠 Focus event handlers for better placeholder behavior
            NoteTextBox.GotFocus += NoteTextBox_GotFocus;
            NoteTextBox.LostFocus += NoteTextBox_LostFocus;
            
            // 🎯 Cursor tracking for better visibility when resized
            NoteTextBox.SelectionChanged += NoteTextBox_SelectionChanged;
            NoteTextBox.SizeChanged += NoteTextBox_SizeChanged;
            
            // 🖱️ Mouse wheel support for scrolling
            this.PreviewMouseWheel += TextSection_PreviewMouseWheel;
            
            // 🎯 Initialize placeholder visibility
            UpdatePlaceholderVisibility();
        }

        // 📝 Handle text changes to show/hide placeholder and trigger events
        private void NoteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholderVisibility();
            OnTextChanged?.Invoke(); // 🔔 Notify parent component
        }

        // 🎯 Handle focus to hide placeholder
        private void NoteTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        // 🎯 Handle focus loss to show placeholder if empty
        private void NoteTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        // 🎯 Handle cursor position changes to ensure visibility
        private void NoteTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // 📍 Scroll to cursor position when selection changes (typing/moving cursor)
            EnsureCursorVisible();
        }

        // 📏 Handle size changes to maintain cursor visibility
        private void NoteTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 📍 Ensure cursor stays visible when text area is resized
            if (NoteTextBox.IsFocused)
            {
                EnsureCursorVisible();
            }
        }

        // 🖱️ Handle mouse wheel events for scrolling
        private void TextSection_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                // 📜 Forward mouse wheel events to the TextBox's scroll viewer
                if (NoteTextBox.Template.FindName("PART_ContentHost", NoteTextBox) is ScrollViewer scrollViewer)
                {
                    // 🎯 Calculate scroll amount (negative for natural scrolling direction)
                    double scrollAmount = -e.Delta * 0.1; // Adjust sensitivity
                    
                    // 📜 Apply the scroll
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + scrollAmount);
                    
                    // 🛑 Mark event as handled to prevent bubbling
                    e.Handled = true;
                }
            }
            catch
            {
                // 🛡️ Ignore errors in mouse wheel handling
            }
        }

        // 📍 Ensure the cursor/caret is always visible
        private void EnsureCursorVisible()
        {
            try
            {
                // 🎯 Get the caret position and scroll to it
                var caretIndex = NoteTextBox.CaretIndex;
                var rect = NoteTextBox.GetRectFromCharacterIndex(caretIndex);
                
                // 📜 Scroll to make the caret visible
                if (rect.Y < 0 || rect.Y + rect.Height > NoteTextBox.ActualHeight)
                {
                    NoteTextBox.ScrollToVerticalOffset(rect.Y);
                }
            }
            catch
            {
                // 🛡️ Ignore errors in cursor positioning
            }
        }

        // 🧠 Update placeholder visibility based on text content
        private void UpdatePlaceholderVisibility()
        {
            PlaceholderText.Visibility = string.IsNullOrWhiteSpace(NoteTextBox.Text) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }
} 