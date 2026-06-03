using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Input;
using System.Windows.Media;

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

        // Cached plain-text — updated only on actual edits, not on every status-bar tick (LEAK-20)
        private string _cachedTextContent = string.Empty;

        // 📝 Document property for RichTextBox binding
        public FlowDocument Document
        {
            get => NoteRichTextBox.Document;
            set => NoteRichTextBox.Document = value;
        }

        // 📝 Properties for text content access (plain text)
        public string TextContent 
        { 
            get => _cachedTextContent; 
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

            // 🎯 Zero FlowDocument PagePadding — prevents hidden padding stacking
            //    on top of RichTextBox Padding, keeping text aligned with placeholder
            NoteRichTextBox.Document.PagePadding = new Thickness(0);

            // 📐 Force text wrapping by setting PageWidth to available width.
            //    RichTextBox never wraps by default (scrolls horizontal instead).
            //    PageWidth constraint makes it wrap like a normal text editor.
            NoteRichTextBox.SizeChanged += NoteRichTextBox_SizeChanged;

            // 🧠 Focus event handlers for better placeholder behavior
            NoteRichTextBox.GotFocus += NoteRichTextBox_GotFocus;
            NoteRichTextBox.LostFocus += NoteRichTextBox_LostFocus;

            // 🎯 Initialize placeholder visibility
            UpdatePlaceholderVisibility();
        }

        // 📐 Update PageWidth on resize to maintain wrapping
        private void NoteRichTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (NoteRichTextBox.ActualWidth > 0)
            {
                NoteRichTextBox.Document.PageWidth = NoteRichTextBox.ActualWidth;
            }
        }

        // 🖱️ Mouse wheel scrolling — RichTextBox internal ScrollViewer doesn't
        //    respond to wheel by default. PreviewMouseWheel scrolls manually.
        private void NoteRichTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = GetScrollViewer(NoteRichTextBox);
            if (sv != null)
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        // 📦 Walk visual tree to find RichTextBox's internal ScrollViewer
        private ScrollViewer GetScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer sv)
                    return sv;
                var found = GetScrollViewer(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        // 📝 Handle text changes to show/hide placeholder and trigger events
        private void NoteRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _cachedTextContent = GetPlainText();
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


        // 🧠 Update placeholder visibility based on text content
        private void UpdatePlaceholderVisibility()
        {
            PlaceholderText.Visibility = string.IsNullOrWhiteSpace(GetPlainText()) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        // 🎯 Focus the editor and place cursor at the beginning
        public void FocusEditor()
        {
            NoteRichTextBox.Focus();
            NoteRichTextBox.CaretPosition = NoteRichTextBox.Document.ContentStart;
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