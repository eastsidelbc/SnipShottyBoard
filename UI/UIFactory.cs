using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 🏗️ UIFactory - Standardizes UI element creation patterns
    /// Reduces boilerplate code for common UI elements with consistent theming
    /// </summary>
    public static class UIFactory
    {
        /// <summary>
        /// 🔘 Create a themed button with standard styling
        /// </summary>
        /// <param name="content">Button content</param>
        /// <param name="style">Style key (e.g., "PrimaryButtonStyle")</param>
        /// <param name="clickHandler">Click event handler</param>
        /// <param name="tooltip">Tooltip text</param>
        /// <param name="margin">Button margin</param>
        /// <returns>Configured button</returns>
        public static Button CreateButton(
            object content,
            string style = "SecondaryButtonStyle",
            RoutedEventHandler clickHandler = null,
            string tooltip = null,
            Thickness? margin = null)
        {
            var button = new Button
            {
                Content = content,
                Cursor = Cursors.Hand
            };

            if (margin.HasValue)
                button.Margin = margin.Value;

            if (!string.IsNullOrEmpty(tooltip))
                button.ToolTip = tooltip;

            // Apply style safely
            var buttonStyle = ResourceHelper.GetStyleResource(style);
            if (buttonStyle != null)
                button.Style = buttonStyle;

            if (clickHandler != null)
                button.Click += clickHandler;

            return button;
        }

        /// <summary>
        /// 📦 Create a themed border with standard styling
        /// </summary>
        /// <param name="cornerRadius">Corner radius</param>
        /// <param name="backgroundKey">Background resource key</param>
        /// <param name="borderBrushKey">Border brush resource key</param>
        /// <param name="borderThickness">Border thickness</param>
        /// <param name="padding">Inner padding</param>
        /// <param name="margin">Outer margin</param>
        /// <returns>Configured border</returns>
        public static Border CreateBorder(
            CornerRadius? cornerRadius = null,
            string backgroundKey = "AppBackgroundBrush",
            string borderBrushKey = "BorderBrush",
            Thickness? borderThickness = null,
            Thickness? padding = null,
            Thickness? margin = null)
        {
            var border = new Border();

            if (cornerRadius.HasValue)
                border.CornerRadius = cornerRadius.Value;

            if (borderThickness.HasValue)
                border.BorderThickness = borderThickness.Value;
            else
                border.BorderThickness = new Thickness(1);

            if (padding.HasValue)
                border.Padding = padding.Value;

            if (margin.HasValue)
                border.Margin = margin.Value;

            // Apply themed resources
            ResourceHelper.ApplyResource(border, Border.BackgroundProperty, backgroundKey);
            ResourceHelper.ApplyResource(border, Border.BorderBrushProperty, borderBrushKey);

            return border;
        }

        /// <summary>
        /// 📝 Create a themed text block with standard styling
        /// </summary>
        /// <param name="text">Text content</param>
        /// <param name="fontSize">Font size</param>
        /// <param name="fontWeight">Font weight</param>
        /// <param name="foregroundKey">Foreground resource key</param>
        /// <param name="textAlignment">Text alignment</param>
        /// <param name="textWrapping">Text wrapping</param>
        /// <param name="margin">Margin</param>
        /// <returns>Configured text block</returns>
        public static TextBlock CreateTextBlock(
            string text,
            double fontSize = 14,
            FontWeight? fontWeight = null,
            string foregroundKey = "AppForegroundBrush",
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            Thickness? margin = null)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                TextAlignment = textAlignment,
                TextWrapping = textWrapping
            };

            if (fontWeight.HasValue)
                textBlock.FontWeight = fontWeight.Value;

            if (margin.HasValue)
                textBlock.Margin = margin.Value;

            // Apply themed foreground
            ResourceHelper.ApplyResource(textBlock, TextBlock.ForegroundProperty, foregroundKey);

            return textBlock;
        }

        /// <summary>
        /// 📊 Create a grid with predefined row/column definitions
        /// </summary>
        /// <param name="rowDefinitions">Array of row height definitions</param>
        /// <param name="columnDefinitions">Array of column width definitions</param>
        /// <param name="margin">Grid margin</param>
        /// <returns>Configured grid</returns>
        public static Grid CreateGrid(
            GridLength[] rowDefinitions = null,
            GridLength[] columnDefinitions = null,
            Thickness? margin = null)
        {
            var grid = new Grid();

            if (margin.HasValue)
                grid.Margin = margin.Value;

            // Add row definitions
            if (rowDefinitions != null)
            {
                foreach (var height in rowDefinitions)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = height });
                }
            }

            // Add column definitions
            if (columnDefinitions != null)
            {
                foreach (var width in columnDefinitions)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
                }
            }

            return grid;
        }

        /// <summary>
        /// 📜 Create a themed scroll viewer with standard settings
        /// </summary>
        /// <param name="content">Content to scroll</param>
        /// <param name="verticalScrollBarVisibility">Vertical scroll bar visibility</param>
        /// <param name="horizontalScrollBarVisibility">Horizontal scroll bar visibility</param>
        /// <param name="padding">Content padding</param>
        /// <returns>Configured scroll viewer</returns>
        public static ScrollViewer CreateScrollViewer(
            UIElement content = null,
            ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Auto,
            ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Thickness? padding = null)
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = verticalScrollBarVisibility,
                HorizontalScrollBarVisibility = horizontalScrollBarVisibility
            };

            if (padding.HasValue)
                scrollViewer.Padding = padding.Value;

            if (content != null)
                scrollViewer.Content = content;

            return scrollViewer;
        }

        /// <summary>
        /// 🎨 Create a dialog container with standard styling (like CustomDialog)
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="icon">Dialog icon (emoji)</param>
        /// <param name="content">Dialog content</param>
        /// <param name="width">Dialog width</param>
        /// <param name="height">Dialog height</param>
        /// <returns>Configured window</returns>
        public static Window CreateDialogWindow(
            string title,
            string icon = "ℹ️",
            UIElement content = null,
            double width = 400,
            double height = 300)
        {
            var window = new Window
            {
                Width = width,
                Height = height,
                MinWidth = 300,
                MinHeight = 200,
                ResizeMode = ResizeMode.CanResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = Brushes.Transparent,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false
            };

            // Create main container with drop shadow
            var mainBorder = CreateBorder(
                cornerRadius: new CornerRadius(12),
                margin: new Thickness(8));

            // Add drop shadow effect
            mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.3,
                BlurRadius = 10,
                ShadowDepth = 3,
                Direction = 270
            };

            // Create header
            var headerBorder = CreateBorder(
                backgroundKey: "HeaderBackgroundBrush",
                cornerRadius: new CornerRadius(12, 12, 0, 0),
                padding: new Thickness(16, 12, 16, 12));

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
            headerStack.Children.Add(CreateTextBlock(icon, fontSize: 20, margin: new Thickness(0, 0, 12, 0)));
            headerStack.Children.Add(CreateTextBlock(title, fontSize: 16, fontWeight: FontWeights.SemiBold));
            headerBorder.Child = headerStack;

            // Create main grid
            var mainGrid = CreateGrid(
                rowDefinitions: new[] { GridLength.Auto, new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(headerBorder, 0);
            mainGrid.Children.Add(headerBorder);

            if (content != null)
            {
                Grid.SetRow(content, 1);
                mainGrid.Children.Add(content);
            }

            mainBorder.Child = mainGrid;
            window.Content = mainBorder;

            return window;
        }

        /// <summary>
        /// 🔍 Create an image with standard properties
        /// </summary>
        /// <param name="source">Image source</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="stretch">Stretch mode</param>
        /// <param name="margin">Image margin</param>
        /// <returns>Configured image</returns>
        public static Image CreateImage(
            ImageSource source,
            double? width = null,
            double? height = null,
            Stretch stretch = Stretch.Uniform,
            Thickness? margin = null)
        {
            var image = new Image
            {
                Source = source,
                Stretch = stretch
            };

            if (width.HasValue)
                image.Width = width.Value;

            if (height.HasValue)
                image.Height = height.Value;

            if (margin.HasValue)
                image.Margin = margin.Value;

            return image;
        }
    }
} 