using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 📝 Window for managing multiple note windows (like Windows Sticky Notes)
    /// </summary>
    public partial class NoteListWindow : Window
    {
        private readonly NoteWindowManager noteManager;

        public NoteListWindow()
        {
            InitializeComponent();
            noteManager = NoteWindowManager.Instance;
            SetupEvents();
            RefreshNoteWindowsList();
        }

        private void SetupEvents()
        {
            // 🔔 Listen for note window changes
            noteManager.WindowCreated += OnNoteWindowCreated;
            noteManager.WindowClosed += OnNoteWindowClosed;

            // 🔑 Handle keyboard shortcuts
            this.PreviewKeyDown += (s, e) =>
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        this.Close();
                        break;
                    case Key.N when Keyboard.Modifiers == ModifierKeys.Control:
                        CreateNewNoteWindow();
                        break;
                }
            };
        }

        private void RefreshNoteWindowsList()
        {
            NoteWindowsList.Children.Clear();
            var activeWindows = noteManager.GetActiveWindows();

            if (!activeWindows.Any())
            {
                StatusText.Text = "No active note windows";
                
                // 📝 Show helpful message
                var emptyMessage = new TextBlock
                {
                    Text = "🗒️ Click 'New Window' to create your first note window",
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(16),
                    Foreground = ResourceHelper.CommonBrushes.AppForeground,
                    Opacity = 0.6,
                    TextWrapping = TextWrapping.Wrap
                };
                NoteWindowsList.Children.Add(emptyMessage);
                return;
            }

            StatusText.Text = $"{activeWindows.Count} active window{(activeWindows.Count == 1 ? "" : "s")}";

            foreach (var window in activeWindows.OrderBy(w => w.CreatedAt))
            {
                var windowCard = CreateNoteWindowCard(window);
                NoteWindowsList.Children.Add(windowCard);
            }
        }

        private Border CreateNoteWindowCard(NoteWindowData windowData)
        {
            var card = new Border
            {
                Background = ResourceHelper.CommonBrushes.AppBackground,
                BorderBrush = ResourceHelper.CommonBrushes.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(4),
                Padding = new Thickness(12),
                Cursor = Cursors.Hand
            };

            // 🖱️ Make the whole card clickable to open the note
            card.MouseLeftButtonUp += (s, e) => OpenNoteWindow(windowData.Id);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 📄 Window Info
            var infoStack = new StackPanel { Orientation = Orientation.Vertical };

            var titleText = new TextBlock
            {
                Text = windowData.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = ResourceHelper.CommonBrushes.AppForeground
            };

            // 📝 Show preview of note content instead of generic details
            var previewText = GetNotePreview(windowData);
            var detailsText = new TextBlock
            {
                Text = previewText,
                FontSize = 11,
                Foreground = ResourceHelper.CommonBrushes.AppForeground,
                Opacity = 0.7,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 32 // Limit height to show only a couple lines
            };

            infoStack.Children.Add(titleText);
            infoStack.Children.Add(detailsText);

            // 🎛️ Actions
            var actionStack = new StackPanel { Orientation = Orientation.Horizontal };

            var renameButton = new Button
            {
                Content = "✏️ Rename",
                Margin = new Thickness(4, 0, 4, 0),
                Tag = windowData.Id
            };
            renameButton.Click += RenameWindowButton_Click;

            var closeButton = new Button
            {
                Content = "✕",
                Width = 32,
                Margin = new Thickness(4, 0, 4, 0),
                Tag = windowData.Id
            };
            closeButton.Click += CloseWindowButton_Click;

            actionStack.Children.Add(renameButton);
            actionStack.Children.Add(closeButton);

            Grid.SetColumn(infoStack, 0);
            Grid.SetColumn(actionStack, 1);

            grid.Children.Add(infoStack);
            grid.Children.Add(actionStack);
            card.Child = grid;

            return card;
        }

        // 📖 Get a preview of the note's content (first few words)
        private string GetNotePreview(NoteWindowData windowData)
        {
            // Get the first note with text content
            var firstNoteWithText = windowData.Notes
                .Where(n => !string.IsNullOrWhiteSpace(n.TextContent))
                .OrderBy(n => n.TabOrder)
                .FirstOrDefault();

            if (firstNoteWithText != null)
            {
                // Extract first 50 characters or until first line break
                var text = firstNoteWithText.TextContent.Trim();
                var firstLine = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                
                if (!string.IsNullOrEmpty(firstLine))
                {
                    if (firstLine.Length > 50)
                    {
                        return firstLine.Substring(0, 47) + "...";
                    }
                    return firstLine;
                }
            }

            // Fallback to showing creation info if no text content
            return $"Created: {windowData.CreatedAt:M/d/yyyy h:mm tt} • {windowData.Notes.Count} notes";
        }

        #region Event Handlers

        private void NewWindowButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewNoteWindow();
        }

        private void RenameWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid windowId)
            {
                RenameNoteWindow(windowId);
            }
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid windowId)
            {
                CloseNoteWindow(windowId);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void OnNoteWindowCreated(NoteWindowData window)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RefreshNoteWindowsList();
            });
        }

        private void OnNoteWindowClosed(NoteWindowData window)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RefreshNoteWindowsList();
            });
        }

        #endregion

        #region Helper Methods

        private void CreateNewNoteWindow()
        {
            var newWindow = noteManager.CreateNewNoteWindow();
            
            // 🚀 Automatically open the new window
            OpenNoteWindow(newWindow.Id);
        }

        private void OpenNoteWindow(Guid windowId)
        {
            try
            {
                var windowData = noteManager.NoteWindows.FirstOrDefault(w => w.Id == windowId);
                if (windowData == null) return;

                // 🔍 Check if window is already open
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow mainWindow && mainWindow.Tag?.ToString() == windowId.ToString())
                    {
                        // Window already open, just bring it to front
                        window.Activate();
                        window.Focus();
                        return;
                    }
                }

                // 🆕 Create new MainWindow for this note window with its specific data
                var noteWindow = new MainWindow(windowData);
                noteWindow.Tag = windowId.ToString(); // Store window ID for identification
                noteWindow.Title = windowData.Title;
                
                // 📍 Set window position and size
                noteWindow.Left = windowData.WindowLeft;
                noteWindow.Top = windowData.WindowTop;
                noteWindow.Width = windowData.WindowWidth;
                noteWindow.Height = windowData.WindowHeight;
                
                noteWindow.Show();
                noteWindow.Activate();

                // 💾 Save window position when it moves/resizes
                noteWindow.LocationChanged += (s, e) =>
                {
                    windowData.WindowLeft = noteWindow.Left;
                    windowData.WindowTop = noteWindow.Top;
                    noteManager.SaveNoteWindows();
                };

                noteWindow.SizeChanged += (s, e) =>
                {
                    windowData.WindowWidth = noteWindow.ActualWidth;
                    windowData.WindowHeight = noteWindow.ActualHeight;
                    noteManager.SaveNoteWindows();
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error opening note window: {ex.Message}");
                MessageBox.Show($"Error opening note window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameNoteWindow(Guid windowId)
        {
            var windowData = noteManager.NoteWindows.FirstOrDefault(w => w.Id == windowId);
            if (windowData == null) return;

            // 📝 Show beautiful themed input dialog
            var (success, newName) = CustomInputDialog.ShowRename(this, windowData.Title, "note window");
            
            if (success && !string.IsNullOrWhiteSpace(newName) && newName.Trim() != windowData.Title)
            {
                windowData.Title = newName.Trim();
                windowData.LastModified = DateTime.Now;
                noteManager.SaveNoteWindows();
                
                // 🔄 Update the UI
                RefreshNoteWindowsList();
                
                // 📝 Update the actual window title if it's open
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow mainWindow && mainWindow.Tag?.ToString() == windowId.ToString())
                    {
                        mainWindow.Title = windowData.Title;
                        break;
                    }
                }
            }
        }

        private void CloseNoteWindow(Guid windowId)
        {
            try
            {
                // 🗑️ Close the actual window if it's open
                foreach (Window window in Application.Current.Windows.Cast<Window>().ToList())
                {
                    if (window is MainWindow mainWindow && mainWindow.Tag?.ToString() == windowId.ToString())
                    {
                        window.Close();
                        break;
                    }
                }

                // 💾 Mark as inactive in data
                noteManager.CloseNoteWindow(windowId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error closing note window: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 🧹 Clean up event subscriptions
            noteManager.WindowCreated -= OnNoteWindowCreated;
            noteManager.WindowClosed -= OnNoteWindowClosed;
            base.OnClosed(e);
        }

        #endregion
    }
} 