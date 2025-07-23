using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnipShottyBoard.Data;

namespace SnipShottyBoard
{
    public partial class NoteTab : UserControl
    {
        // ✅ Expose child components for external access
        public TextBox TextBox => TextSectionControl.TextBox;
        public WrapPanel ImagePanel => MediaSectionControl.ImagePanel;

        // 💾 Properties for save/load functionality
        private string title = "";

        public string Title 
        { 
            get => title; 
            set 
            { 
                title = value;
                OnDataChanged?.Invoke();
            } 
        }

        // 📝 Delegate text content to TextSection
        public string TextContent 
        { 
            get => TextSectionControl.TextContent; 
            set => TextSectionControl.TextContent = value;
        }

        // 🖼️ Delegate image files to MediaSection
        public List<string> ImageFiles 
        { 
            get => MediaSectionControl.ImageFiles;
            set => MediaSectionControl.ImageFiles = value;
        }

        // 🕒 Delegate image timestamps to MediaSection
        public Dictionary<string, DateTime> ImageTimestamps 
        { 
            get => MediaSectionControl.ImageTimestamps;
            set => MediaSectionControl.ImageTimestamps = value;
        }

        // 🔔 Event to notify when data changes (for auto-save)
        public event Action OnDataChanged;

        // 🎛️ Splitter state tracking
        private bool isDraggingSplitter = false;
        private Point lastMousePosition;

        public NoteTab()
        {
            InitializeComponent();
            
            // 🔗 Wire up events from child components
            TextSectionControl.OnTextChanged += () => OnDataChanged?.Invoke();
            MediaSectionControl.OnMediaChanged += () => OnDataChanged?.Invoke();
            
            // 🎯 Initialize proportional sizing (50/50 split by default)
            InitializeSplitterSizing();
        }

        // 🎯 Initialize default proportional sizing
        private void InitializeSplitterSizing()
        {
            // Set equal proportions initially (50/50 split)
            TextSectionRow.Height = new GridLength(1, GridUnitType.Star);
            MediaSectionRow.Height = new GridLength(1, GridUnitType.Star);
        }

        #region Splitter Event Handlers

        // 🖱️ Start splitter drag operation
        private void Splitter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var splitter = sender as Border;
            if (splitter != null)
            {
                isDraggingSplitter = true;
                lastMousePosition = e.GetPosition(this);
                
                // 🎯 Capture mouse to ensure we get move/up events
                splitter.CaptureMouse();
                e.Handled = true;
            }
        }

        // 🖱️ Handle splitter dragging
        private void Splitter_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingSplitter && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(this);
                var deltaY = currentPosition.Y - lastMousePosition.Y;
                
                // 📊 Apply incremental change to row heights
                if (Math.Abs(deltaY) > 2) // Deadzone to prevent micro-movements
                {
                    AdjustRowHeights(deltaY);
                    lastMousePosition = currentPosition;
                }
                
                e.Handled = true;
            }
        }

        // 🖱️ End splitter drag operation
        private void Splitter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingSplitter)
            {
                isDraggingSplitter = false;
                var splitter = sender as Border;
                splitter?.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        // 🖱️ Handle mouse leaving splitter area
        private void Splitter_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isDraggingSplitter && e.LeftButton != MouseButtonState.Pressed)
            {
                // 🛑 Cancel drag if mouse leaves without being pressed
                isDraggingSplitter = false;
                var splitter = sender as Border;
                splitter?.ReleaseMouseCapture();
            }
        }

        // 📊 Adjust row heights directly based on mouse movement
        private void AdjustRowHeights(double deltaY)
                {
                    try
                    {
                // 📏 Get current actual heights of the sections
                var totalHeight = this.ActualHeight - 40; // Account for margins and splitter
                if (totalHeight <= 100) return; // Need minimum space
                
                // 📐 Get current star values
                var textStarValue = TextSectionRow.Height.Value;
                var mediaStarValue = MediaSectionRow.Height.Value;
                var totalStarValue = textStarValue + mediaStarValue;
                
                // 🧮 Convert current star values to actual pixel heights
                var currentTextHeight = (textStarValue / totalStarValue) * totalHeight;
                var currentMediaHeight = (mediaStarValue / totalStarValue) * totalHeight;
                
                // 🎯 Apply direct pixel change - natural divider movement
                // Drag down = text gets bigger, drag up = text gets smaller
                var newTextHeight = Math.Max(50, Math.Min(totalHeight - 50, currentTextHeight + deltaY));
                var newMediaHeight = totalHeight - newTextHeight;
                
                // 📊 Convert back to proportional star values
                var newTextStar = newTextHeight / totalHeight;
                var newMediaStar = newMediaHeight / totalHeight;
                
                // 🔄 Apply the new proportions
                TextSectionRow.Height = new GridLength(newTextStar, GridUnitType.Star);
                MediaSectionRow.Height = new GridLength(newMediaStar, GridUnitType.Star);
                    }
                    catch (Exception ex)
                    {
                System.Diagnostics.Debug.WriteLine($"Error adjusting row heights: {ex.Message}");
            }
        }



        #endregion

        // 🖼️ Delegate image addition to MediaSection
        public void AddImage(Image imageControl, string imagePath)
        {
            MediaSectionControl.AddImage(imageControl, imagePath);
        }

        // 🖼️ Delegate image removal to MediaSection
        public void RemoveImage(Image imageControl)
        {
            MediaSectionControl.RemoveImage(imageControl);
        }

        // 🧹 Cleanup method called when tab is being closed
        public void Dispose()
        {
            try
            {
                // 🧹 Dispose MediaSection if it implements IDisposable
                MediaSectionControl?.Dispose();
                
                // 🔄 Clear any event subscriptions
                OnDataChanged = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing NoteTab: {ex.Message}");
            }
        }
    }
}
