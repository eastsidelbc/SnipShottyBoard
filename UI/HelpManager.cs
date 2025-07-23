using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SnipShottyBoard.UI
{
    // ℹ️ HelpManager - Handles help window and documentation display
    public class HelpManager
    {
        private const string HELP_CONTENT = @"🎯 SnipShottyBoard - Features & Shortcuts

✨ FEATURES:
• 📝 Multiple sticky note tabs
• 🖼️ Paste images with Ctrl+V
• 🌙 Dark/Light theme toggle
• 💾 Auto-save every 5 seconds
• 🖱️ Right-click tabs for context menu
• 🔄 Tab duplication and renaming

⌨️ KEYBOARD SHORTCUTS:
• Ctrl + T → New tab
• Ctrl + W → Delete current tab
• Ctrl + Tab → Switch to next tab
• Ctrl + R → Rename current tab
• Ctrl + V → Paste image from clipboard

🖱️ MOUSE ACTIONS:
• Double-click tab → Rename tab
• Right-click tab → Context menu
• Click image → View full size

🎨 INTERFACE:
• ➕ New tab
• 🗑️ Delete current tab
• 🌙 Toggle dark/light theme
• ℹ️ Show this help
• ❌ Close app

💾 Data is automatically saved to:
%AppData%\Roaming\SnipShottyBoard\";

        // 📖 Show main help window
        public void ShowHelpWindow()
        {
            var helpText = @"🎯 SnipShottyBoard - Enhanced Tab Management

📝 TEXT EDITING:
• Type directly in the main area
• Ctrl+Z = Undo, Ctrl+Y = Redo  
• Ctrl+A = Select All
• Ctrl+C/V/X = Copy/Paste/Cut

🏷️ TAB MANAGEMENT:
• Ctrl+T = New tab
• Ctrl+W = Delete current tab
• Ctrl+Tab = Switch to next tab
• F2 = Rename current tab
• Double-click tab = Rename tab

🎯 DRAG & DROP REORDERING:
• Click and drag any tab to reorder
• Blue visual feedback shows drag state
• Drop between tabs to reorder
• Drag outside window to cancel
• Works with mouse or trackpad

📋 TAB CONTEXT MENU (Right-click):
• 📝 Rename Tab
• ➕ New Tab  
• 📋 Duplicate Tab
• ❌ Delete Tab

🖼️ IMAGES:
• Ctrl+V = Paste images from clipboard
• Images auto-save with your notes

⚙️ OTHER FEATURES:
• 🌙 Theme toggle (Dark/Light)
• ⏰ Auto-save every 5 seconds
• 📊 Live word count in status bar
• 📍 Window position saves automatically

💡 PRO TIPS:
• Drag tabs to organize your workflow
• Use Ctrl+Tab to quickly switch between notes
• Right-click tabs for quick actions
• Images paste directly where your cursor is
• Your layout and tab order save automatically";

            ShowThemedDialog("📖 SnipShottyBoard Help", helpText);
        }

        // 🏗️ Create and configure the help window
        private Window CreateHelpWindow()
        {
            var helpWindow = new Window
            {
                Title = "SnipShottyBoard - Help",
                Width = 500,
                Height = 600,
                ResizeMode = ResizeMode.CanResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = GetThemeResource("AppBackgroundBrush"),
                Icon = Application.Current.MainWindow?.Icon
            };

            helpWindow.Content = CreateHelpContent();
            return helpWindow;
        }

        // 📝 Create the scrollable help content
        private ScrollViewer CreateHelpContent()
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(20)
            };

            var textBlock = new TextBlock
            {
                Text = HELP_CONTENT,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                Foreground = GetThemeResource("AppForegroundBrush"),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };

            scrollViewer.Content = textBlock;
            return scrollViewer;
        }

        // 🎨 Get theme resource safely
        private Brush GetThemeResource(string resourceKey)
        {
            try
            {
                return (Brush)Application.Current.FindResource(resourceKey);
            }
            catch
            {
                // Fallback to system defaults if theme resource not found
                return resourceKey.Contains("Background") 
                    ? SystemColors.WindowBrush 
                    : SystemColors.WindowTextBrush;
            }
        }

        // 📋 Get help content as plain text (for potential future use)
        public string GetHelpText()
        {
            return HELP_CONTENT;
        }

        // 💡 Show quick tips using themed dialog
        public void ShowQuickTips()
        {
            var tipsText = @"💡 QUICK TIPS & SHORTCUTS

⚡ SPEED TIPS:
• Ctrl+T = Instant new tab
• Ctrl+Tab = Quick tab switching  
• F2 = Fast tab rename
• Double-click tab = Rename tab
• Right-click tabs = Context menu

🎯 DRAG & DROP MASTERY:
• Click + drag tabs to reorder
• Blue preview shows where tab will go
• Drop between tabs for precise placement
• Drag outside window = cancel operation
• Tab order saves automatically

🖼️ IMAGE WORKFLOW:
• Copy image (Ctrl+C) → Switch to app → Paste (Ctrl+V)
• Screenshots paste instantly
• Images auto-save with notes

💾 AUTO-SAVE MAGIC:
• Saves every 5 seconds automatically
• Window position/size remembered
• Tab order preserved between sessions
• Never lose your work

🎨 PERSONALIZATION:
• 🌙 Toggle between dark/light themes
• Themes apply instantly
• Settings save automatically";

            ShowThemedDialog("💡 Quick Tips", tipsText);
        }

        // 🎨 Show themed dialog with proper sizing for content
        private void ShowThemedDialog(string title, string content)
        {
            // 🎨 Create custom styled window (matching CustomDialog appearance)
            var dialog = new Window
            {
                Height = 600,
                Width = 650,
                MinHeight = 400,
                MinWidth = 500,
                MaxHeight = 700,
                MaxWidth = 800,
                ResizeMode = ResizeMode.CanResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = Brushes.Transparent,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false
            };

            // 🎨 Create the styled border container (like CustomDialog)
            var mainBorder = new Border
            {
                Background = GetThemeResource("AppBackgroundBrush"),
                BorderBrush = GetThemeResource("BorderBrush"),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(8, 8, 8, 8)
            };

            // ✨ Add drop shadow effect
            mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.3,
                BlurRadius = 10,
                ShadowDepth = 3,
                Direction = 270
            };

            // 🏗️ Create main grid layout
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Button

            // 📊 Header Section
            var headerBorder = new Border
            {
                Background = GetThemeResource("HeaderBackgroundBrush"),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                Padding = new Thickness(16, 12, 16, 12)
            };
            Grid.SetRow(headerBorder, 0);

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };

            // 🎯 Dialog Icon
            var iconText = new TextBlock
            {
                Text = title.Split(' ')[0], // First emoji from title
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = GetThemeResource("AppForegroundBrush"),
                Margin = new Thickness(0, 0, 12, 0)
            };

            // 📍 Dialog Title  
            var titleText = new TextBlock
            {
                Text = title.Substring(title.IndexOf(' ') + 1), // Title without emoji
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = GetThemeResource("AppForegroundBrush")
            };

            headerStack.Children.Add(iconText);
            headerStack.Children.Add(titleText);
            headerBorder.Child = headerStack;

            // 📝 Content Section
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(20, 16, 20, 16)
            };
            Grid.SetRow(scrollViewer, 1);

            var contentText = new TextBlock
            {
                Text = content,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Foreground = GetThemeResource("AppForegroundBrush"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            scrollViewer.Content = contentText;

            // 🔘 Button Section
            var buttonBorder = new Border
            {
                Background = GetThemeResource("HeaderBackgroundBrush"),
                BorderBrush = GetThemeResource("BorderBrush"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                CornerRadius = new CornerRadius(0, 0, 12, 12),
                Padding = new Thickness(20, 16, 20, 16)
            };
            Grid.SetRow(buttonBorder, 2);

            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "✅ OK",
                Style = (Style)Application.Current.FindResource("PrimaryButtonStyle")
            };
            okButton.Click += (s, e) => dialog.Close();

            buttonStack.Children.Add(okButton);
            buttonBorder.Child = buttonStack;

            // 🏗️ Assemble the layout
            mainGrid.Children.Add(headerBorder);
            mainGrid.Children.Add(scrollViewer);
            mainGrid.Children.Add(buttonBorder);

            mainBorder.Child = mainGrid;
            dialog.Content = mainBorder;
            dialog.ShowDialog();
        }



        // ℹ️ Show about dialog
        public void ShowAbout()
        {
            var aboutText = @"📝 SnipShottyBoard

A simple, elegant sticky notes application with:
• Multiple tabs for organization
• Image support via clipboard
• Dark/Light themes
• Auto-save functionality
• Drag & drop tab reordering
• Double-click tab renaming

Built with WPF and .NET 8";

            ShowThemedDialog("📝 About SnipShottyBoard", aboutText);
        }


    }
} 