using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 📝 TextSection - Rich text input component with placeholder functionality
    /// Simple, standalone rich text field that behaves naturally
    /// </summary>
    public partial class TextSection : UserControl
    {
        // 🔔 Event to notify when text content changes
        public event Action OnTextChanged;

        // 📝 Document property for RichTextBox binding
        public FlowDocument Document
        {
            get => NoteRichTextBox.Document;
            set => NoteRichTextBox.Document = value;
        }

        // 📝 Properties for text content access (plain text)
        public string TextContent 
        { 
            get => GetPlainText(); 
            set 
            { 
                SetPlainText(value);
                UpdatePlaceholderVisibility();
            } 
        }

        // 📝 Properties for rich text content access (RTF)
        public string RichTextContent
        {
            get => GetRtfText();
            set => SetRtfText(value);
        }

        // ✅ Expose the RichTextBox for external access if needed
        public RichTextBox RichTextBox => NoteRichTextBox;

        public TextSection()
        {
            InitializeComponent();
            
            // 🧠 Focus event handlers for better placeholder behavior
            NoteRichTextBox.GotFocus += NoteRichTextBox_GotFocus;
            NoteRichTextBox.LostFocus += NoteRichTextBox_LostFocus;
            
            // 🎯 Cursor tracking for better visibility when resized
            NoteRichTextBox.SelectionChanged += NoteRichTextBox_SelectionChanged;
            NoteRichTextBox.SizeChanged += NoteRichTextBox_SizeChanged;
            
            // 🖱️ Mouse wheel support for scrolling
            this.PreviewMouseWheel += TextSection_PreviewMouseWheel;
            
            // 🎯 Initialize placeholder visibility
            UpdatePlaceholderVisibility();
        }

        // 📝 Handle text changes to show/hide placeholder and trigger events
        private void NoteRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholderVisibility();
            OnTextChanged?.Invoke(); // 🔔 Notify parent component
        }

        // 🎯 Handle focus to hide placeholder
        private void NoteRichTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        // 🎯 Handle focus loss to show placeholder if empty
        private void NoteRichTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        // 🎯 Handle cursor position changes to ensure visibility
        private void NoteRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // 📍 Scroll to cursor position when selection changes (typing/moving cursor)
            EnsureCursorVisible();
        }

        // 📏 Handle size changes to maintain cursor visibility
        private void NoteRichTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 📍 Ensure cursor stays visible when text area is resized
            if (NoteRichTextBox.IsFocused)
            {
                EnsureCursorVisible();
            }
        }

        // 🖱️ Handle mouse wheel events for scrolling
        private void TextSection_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                // 📜 Forward mouse wheel events to the RichTextBox's scroll viewer
                if (NoteRichTextBox.Template.FindName("PART_ContentHost", NoteRichTextBox) is ScrollViewer scrollViewer)
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
                var caretPosition = NoteRichTextBox.CaretPosition;
                var rect = caretPosition.GetCharacterRect(LogicalDirection.Forward);
                
                // 📜 Scroll to make the caret visible
                if (rect.Y < 0 || rect.Y + rect.Height > NoteRichTextBox.ActualHeight)
                {
                    NoteRichTextBox.ScrollToVerticalOffset(rect.Y);
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
            PlaceholderText.Visibility = string.IsNullOrWhiteSpace(GetPlainText()) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        // 📝 Get plain text from RichTextBox
        private string GetPlainText()
        {
            try
            {
                return new TextRange(NoteRichTextBox.Document.ContentStart, NoteRichTextBox.Document.ContentEnd).Text;
            }
            catch
            {
                return string.Empty;
            }
        }

        // 📝 Set plain text to RichTextBox
        private void SetPlainText(string text)
        {
            try
            {
                NoteRichTextBox.Document.Blocks.Clear();
                if (!string.IsNullOrEmpty(text))
                {
                    NoteRichTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
                }
            }
            catch
            {
                // 🛡️ Ignore errors in text setting
            }
        }

        // 📝 Get RTF text from RichTextBox
        private string GetRtfText()
        {
            try
            {
                var range = new TextRange(NoteRichTextBox.Document.ContentStart, NoteRichTextBox.Document.ContentEnd);
                using var stream = new MemoryStream();
                range.Save(stream, DataFormats.Rtf);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }

        // 📝 Set RTF text to RichTextBox
        private void SetRtfText(string rtfText)
        {
            try
            {
                if (string.IsNullOrEmpty(rtfText))
                {
                    NoteRichTextBox.Document.Blocks.Clear();
                    return;
                }

                var range = new TextRange(NoteRichTextBox.Document.ContentStart, NoteRichTextBox.Document.ContentEnd);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rtfText));
                range.Load(stream, DataFormats.Rtf);
            }
            catch
            {
                // 🛡️ If RTF loading fails, try as plain text
                SetPlainText(rtfText);
            }
        }

        // 🎨 Apply formatting to selected text
        public void ApplyBold()
        {
            if (NoteRichTextBox.Selection.GetPropertyValue(TextElement.FontWeightProperty).Equals(FontWeights.Bold))
            {
                NoteRichTextBox.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            }
            else
            {
                NoteRichTextBox.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
        }

        public void ApplyItalic()
        {
            if (NoteRichTextBox.Selection.GetPropertyValue(TextElement.FontStyleProperty).Equals(FontStyles.Italic))
            {
                NoteRichTextBox.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
            }
            else
            {
                NoteRichTextBox.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
            }
        }

        public void ApplyUnderline()
        {
            if (NoteRichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty).Equals(TextDecorations.Underline))
            {
                NoteRichTextBox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
            }
            else
            {
                NoteRichTextBox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
            }
        }

        public void ApplyStrikethrough()
        {
            if (NoteRichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty).Equals(TextDecorations.Strikethrough))
            {
                NoteRichTextBox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
            }
            else
            {
                NoteRichTextBox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
            }
        }

        public void ApplyBulletList()
        {
            var selection = NoteRichTextBox.Selection;
            var start = selection.Start;
            var end = selection.End;

            // Get the paragraphs in the selection
            var startBlock = start.Paragraph;
            var endBlock = end.Paragraph;

            if (startBlock != null && endBlock != null)
            {
                var blocks = NoteRichTextBox.Document.Blocks;
                var startIndex = -1;
                var endIndex = -1;

                // Find indices manually
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks.ElementAt(i) == startBlock) startIndex = i;
                    if (blocks.ElementAt(i) == endBlock) endIndex = i;
                }

                if (startIndex >= 0 && endIndex >= 0)
                {
                    for (int i = startIndex; i <= endIndex && i < blocks.Count; i++)
                    {
                        if (blocks.ElementAt(i) is Paragraph paragraph)
                        {
                            var text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Trim();
                            
                            if (paragraph.Margin.Left == 0 && !text.StartsWith("• "))
                            {
                                // Add bullet
                                paragraph.Margin = new Thickness(20, 0, 0, 0);
                                paragraph.TextAlignment = TextAlignment.Left;
                                
                                // Add bullet character if text doesn't start with one
                                if (!string.IsNullOrEmpty(text) && !text.StartsWith("• "))
                                {
                                    var range = new TextRange(paragraph.ContentStart, paragraph.ContentStart);
                                    range.Text = "• " + text;
                                }
                            }
                            else if (paragraph.Margin.Left > 0 && text.StartsWith("• "))
                            {
                                // Remove bullet
                                paragraph.Margin = new Thickness(0);
                                
                                // Remove bullet character
                                if (text.StartsWith("• "))
                                {
                                    var range = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                                    range.Text = text.Substring(2); // Remove "• "
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ApplyNumberedList()
        {
            var selection = NoteRichTextBox.Selection;
            var start = selection.Start;
            var end = selection.End;

            // Get the paragraphs in the selection
            var startBlock = start.Paragraph;
            var endBlock = end.Paragraph;

            if (startBlock != null && endBlock != null)
            {
                var blocks = NoteRichTextBox.Document.Blocks;
                var startIndex = -1;
                var endIndex = -1;

                // Find indices manually
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks.ElementAt(i) == startBlock) startIndex = i;
                    if (blocks.ElementAt(i) == endBlock) endIndex = i;
                }

                if (startIndex >= 0 && endIndex >= 0)
                {
                    var numberedItems = 0;
                    for (int i = startIndex; i <= endIndex && i < blocks.Count; i++)
                    {
                        if (blocks.ElementAt(i) is Paragraph paragraph)
                        {
                            var text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Trim();
                            
                            // Check if it's already a numbered item
                            var isNumbered = System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+\.\s");
                            
                            if (paragraph.Margin.Left == 0 && !isNumbered)
                            {
                                // Add numbering
                                numberedItems++;
                                paragraph.Margin = new Thickness(20, 0, 0, 0);
                                paragraph.TextAlignment = TextAlignment.Left;
                                
                                // Add number if text doesn't start with one
                                if (!string.IsNullOrEmpty(text) && !isNumbered)
                                {
                                    var range = new TextRange(paragraph.ContentStart, paragraph.ContentStart);
                                    range.Text = $"{numberedItems}. " + text;
                                }
                            }
                            else if (paragraph.Margin.Left > 0 && isNumbered)
                            {
                                // Remove numbering
                                paragraph.Margin = new Thickness(0);
                                
                                // Remove number
                                if (isNumbered)
                                {
                                    var range = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                                    var match = System.Text.RegularExpressions.Regex.Match(text, @"^\d+\.\s");
                                    if (match.Success)
                                    {
                                        range.Text = text.Substring(match.Length);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void IndentText()
        {
            var selection = NoteRichTextBox.Selection;
            var start = selection.Start;
            var end = selection.End;

            var startBlock = start.Paragraph;
            var endBlock = end.Paragraph;

            if (startBlock != null && endBlock != null)
            {
                var blocks = NoteRichTextBox.Document.Blocks;
                var startIndex = -1;
                var endIndex = -1;

                // Find indices manually
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks.ElementAt(i) == startBlock) startIndex = i;
                    if (blocks.ElementAt(i) == endBlock) endIndex = i;
                }

                if (startIndex >= 0 && endIndex >= 0)
                {
                    for (int i = startIndex; i <= endIndex && i < blocks.Count; i++)
                    {
                        if (blocks.ElementAt(i) is Paragraph paragraph)
                        {
                            paragraph.Margin = new Thickness(paragraph.Margin.Left + 20, 0, 0, 0);
                        }
                    }
                }
            }
        }

        public void UnindentText()
        {
            var selection = NoteRichTextBox.Selection;
            var start = selection.Start;
            var end = selection.End;

            var startBlock = start.Paragraph;
            var endBlock = end.Paragraph;

            if (startBlock != null && endBlock != null)
            {
                var blocks = NoteRichTextBox.Document.Blocks;
                var startIndex = -1;
                var endIndex = -1;

                // Find indices manually
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks.ElementAt(i) == startBlock) startIndex = i;
                    if (blocks.ElementAt(i) == endBlock) endIndex = i;
                }

                if (startIndex >= 0 && endIndex >= 0)
                {
                    for (int i = startIndex; i <= endIndex && i < blocks.Count; i++)
                    {
                        if (blocks.ElementAt(i) is Paragraph paragraph)
                        {
                            var newLeft = Math.Max(0, paragraph.Margin.Left - 20);
                            paragraph.Margin = new Thickness(newLeft, 0, 0, 0);
                        }
                    }
                }
            }
        }
    }
} 