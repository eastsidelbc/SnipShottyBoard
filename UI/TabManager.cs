using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using SnipShottyBoard.Core.Models;
using SnipShottyBoard.Data;
using SnipShottyBoard.UI.Views;

namespace SnipShottyBoard.UI
{
    // 🏷️ TabManager - Handles all tab-related operations including drag & drop reordering
    public class TabManager
    {
        #region Fields & Properties
        private List<CustomTab> tabs = new List<CustomTab>();
        private CustomTab selectedTab = null;
        
        // UI Dependencies
        private readonly Panel tabHeaderPanel;
        private readonly ContentPresenter tabContentArea;
        
        // 🎯 Drag and Drop Fields
        private bool isDragging = false;
        private Point dragStartPoint;
        private Button draggedTab = null;
        private Canvas dragCanvas = null;
        private Border dragVisual = null;
        private Border dropIndicator = null; // 📍 Drop indicator line
        private int draggedTabOriginalIndex = -1;
        private int dropTargetIndex = -1;
        private int lastDropTargetIndex = -1; // 🎯 For hysteresis

        // 🔕 User Preferences - Settings reference for delete confirmation
        private AppSettings appSettings;
        private bool skipDeleteConfirmation = false;
        
        // Event handlers for MainWindow communication
        public event Action<bool> OnDataChanged;
        public event Action OnStatusUpdateRequested;
        public event Action<string, string> OnLogDebug;
        public event Action<string, Exception> OnLogError;
        public event Action OnSettingsNeedUpdate; // New event for when settings need to be updated
        
        public CustomTab SelectedTab => selectedTab;
        public IReadOnlyList<CustomTab> Tabs => tabs.AsReadOnly();
        public int TabCount => tabs.Count;
        
        // 🔍 Check if delete confirmation is effectively disabled
        public bool IsDeleteConfirmationDisabled => 
            (appSettings != null && !appSettings.ConfirmTabDeletion) || skipDeleteConfirmation;
        #endregion

        #region Constructor
        public TabManager(Panel tabHeaderPanel, ContentPresenter tabContentArea)
        {
            this.tabHeaderPanel = tabHeaderPanel ?? throw new ArgumentNullException(nameof(tabHeaderPanel));
            this.tabContentArea = tabContentArea ?? throw new ArgumentNullException(nameof(tabContentArea));
            
            // 🎯 Setup drag canvas for visual feedback
            InitializeDragCanvas();
        }
        #endregion

        #region Settings Management
        // ⚙️ Update settings reference
        public void UpdateSettings(AppSettings settings)
        {
            this.appSettings = settings;
            OnLogDebug?.Invoke($"⚙️ TabManager settings updated - ConfirmTabDeletion: {settings?.ConfirmTabDeletion}", string.Empty);
        }

        // 🔄 Reset "don't ask again" preference
        public void ResetDeleteConfirmationPreference()
        {
            skipDeleteConfirmation = false;
            // 📝 Also update the settings to reflect this choice
            if (appSettings != null)
            {
                appSettings.ConfirmTabDeletion = true;
                OnSettingsNeedUpdate?.Invoke(); // Notify that settings need to be saved
            }
            OnLogDebug?.Invoke("🔄 Delete confirmation preference reset and settings updated - will show confirmation dialogs again", string.Empty);
        }
        #endregion

        #region Drag and Drop Infrastructure
        // 🎨 Initialize drag canvas for visual feedback
        private void InitializeDragCanvas()
        {
            try
            {
                OnLogDebug?.Invoke("🎨 Starting drag canvas initialization", string.Empty);
                
                // Create a canvas that covers the entire window for drag visuals
                dragCanvas = new Canvas
                {
                    Background = Brushes.Transparent,
                    IsHitTestVisible = false
                };
                
                OnLogDebug?.Invoke("🎨 Drag canvas created", string.Empty);
                
            // 📍 Create drop indicator line
            dropIndicator = new Border
            {
                Width = 3,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(74, 144, 226)), // Blue indicator
                CornerRadius = new CornerRadius(1.5),
                Visibility = Visibility.Hidden, // Hidden by default, shown during drag
                Opacity = 0.8
            };
                
                OnLogDebug?.Invoke("🎨 Drop indicator created", string.Empty);
                
                // Add to the main window (we'll need to add this to the MainWindow's grid)
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    OnLogDebug?.Invoke($"🎨 Found MainWindow: {mainWindow.GetType().Name}", string.Empty);
                    OnLogDebug?.Invoke($"🎨 MainWindow.Content type: {mainWindow.Content?.GetType().Name}", string.Empty);
                    
                    // Find the main grid and add our drag canvas
                    if (mainWindow.Content is Border border && border.Child is Grid mainGrid)
                    {
                        OnLogDebug?.Invoke($"🎨 Found main grid with {mainGrid.Children.Count} children", string.Empty);
                        
                        mainGrid.Children.Add(dragCanvas);
                        Panel.SetZIndex(dragCanvas, 9999); // Ensure it's on top
                        
                        // Add drop indicator to canvas
                        dragCanvas.Children.Add(dropIndicator);
                        
                        OnLogDebug?.Invoke("✅ Drag canvas and drop indicator added to main grid successfully", string.Empty);
                    }
                    else
                    {
                        OnLogError?.Invoke("❌ Could not find main grid structure in MainWindow", new Exception("MainWindow structure mismatch"));
                    }
                }
                else
                {
                    OnLogError?.Invoke("❌ Could not find MainWindow instance", new Exception("MainWindow not found"));
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error initializing drag canvas", ex);
            }
        }

        // 🎯 Start drag operation
        private void StartDragOperation(Button tabButton, Point startPoint)
        {
            try
            {
                OnLogDebug?.Invoke("🎯 Starting drag operation", string.Empty);
                
                isDragging = true;
                dragStartPoint = startPoint;
                draggedTab = tabButton;
                draggedTabOriginalIndex = GetTabIndex(tabButton);
                lastDropTargetIndex = -1; // Reset hysteresis tracking
                
                OnLogDebug?.Invoke($"🎯 Drag started for tab at index {draggedTabOriginalIndex}", string.Empty);
                
                // 🎨 Create visual feedback
                CreateDragVisual(tabButton);
                
                // 🖱️ Capture mouse for drag operation
                tabButton.CaptureMouse();
                
                OnLogDebug?.Invoke($"✅ Drag started for tab at index {draggedTabOriginalIndex}", string.Empty);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error starting drag operation", ex);
            }
        }

        // 🎨 Create visual feedback for dragging
        private void CreateDragVisual(Button sourceButton)
        {
            try
            {
                // 📏 Create a visual copy of the tab button with neutral color so blue drop indicator is visible
                var visualCopy = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(200, 128, 128, 128)), // Semi-transparent gray
                    BorderBrush = new SolidColorBrush(Color.FromArgb(180, 96, 96, 96)),   // Darker gray border
                    BorderThickness = new Thickness(2, 2, 2, 2),
                    CornerRadius = new CornerRadius(3, 3, 0, 0), // Match Edge-like rounded top corners
                    Width = sourceButton.ActualWidth,
                    Height = sourceButton.ActualHeight,
                    Child = new TextBlock
                    {
                        Text = GetTabTitle(sourceButton),
                        Foreground = Brushes.White, // White text for contrast on gray
                        FontWeight = FontWeights.Medium,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(8, 4, 8, 4)
                    }
                };

                dragVisual = visualCopy;
                dragCanvas.Children.Add(dragVisual);
                
                OnLogDebug?.Invoke("✅ Drag visual created", string.Empty);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error creating drag visual", ex);
            }
        }

        // 🔄 Update drag visual position
        private void UpdateDragVisual(Point currentPosition)
        {
            if (dragVisual != null)
            {
                Canvas.SetLeft(dragVisual, currentPosition.X - dragVisual.Width / 2);
                Canvas.SetTop(dragVisual, currentPosition.Y - dragVisual.Height / 2);
            }
        }

        // 📍 Update drop indicator position and visibility
        private void UpdateDropIndicator(int targetIndex)
        {
            if (dropIndicator == null || tabHeaderPanel == null) 
            {
                OnLogDebug?.Invoke($"🚫 UpdateDropIndicator skipped - dropIndicator: {dropIndicator != null}, tabHeaderPanel: {tabHeaderPanel != null}", string.Empty);
                return;
            }
            
            try
            {
                OnLogDebug?.Invoke($"📍 UpdateDropIndicator called with targetIndex: {targetIndex}, isDragging: {isDragging}", string.Empty);
                
                if (targetIndex < 0 || !isDragging)
                {
                    // Hide indicator
                    dropIndicator.Visibility = Visibility.Hidden;
                    OnLogDebug?.Invoke("📍 Drop indicator hidden", string.Empty);
                    return;
                }
                
                // Show indicator
                dropIndicator.Visibility = Visibility.Visible;
                OnLogDebug?.Invoke("📍 Drop indicator shown", string.Empty);
                
                // Calculate position for drop indicator
                double indicatorX = 0;
                
                if (targetIndex >= tabHeaderPanel.Children.Count)
                {
                    // Drop at end - position after last tab
                    if (tabHeaderPanel.Children.Count > 0)
                    {
                        var lastTab = tabHeaderPanel.Children[tabHeaderPanel.Children.Count - 1] as Button;
                        if (lastTab != null && Application.Current.MainWindow != null)
                        {
                            // Transform from tab to main window, then to drag canvas
                            var lastTabPosInWindow = lastTab.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                            var canvasPosInWindow = dragCanvas.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                            indicatorX = lastTabPosInWindow.X - canvasPosInWindow.X + lastTab.ActualWidth + 2;
                            OnLogDebug?.Invoke($"📍 Positioning at end: X={indicatorX}", string.Empty);
                        }
                    }
                }
                else if (targetIndex < tabHeaderPanel.Children.Count)
                {
                    // Drop before target tab
                    var targetTab = tabHeaderPanel.Children[targetIndex] as Button;
                    if (targetTab != null && Application.Current.MainWindow != null)
                    {
                        // Transform from tab to main window, then to drag canvas
                        var targetTabPosInWindow = targetTab.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                        var canvasPosInWindow = dragCanvas.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                        indicatorX = targetTabPosInWindow.X - canvasPosInWindow.X - 2;
                        OnLogDebug?.Invoke($"📍 Positioning before tab {targetIndex}: X={indicatorX}", string.Empty);
                    }
                }
                
                // Position the drop indicator
                Canvas.SetLeft(dropIndicator, indicatorX);
                
                // Position vertically aligned with tabs
                if (tabHeaderPanel.Children.Count > 0)
                {
                    var firstTab = tabHeaderPanel.Children[0] as Button;
                    if (firstTab != null && Application.Current.MainWindow != null)
                    {
                        // Transform from tab to main window, then to drag canvas  
                        var tabPosInWindow = firstTab.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                        var canvasPosInWindow = dragCanvas.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                        var indicatorY = tabPosInWindow.Y - canvasPosInWindow.Y + 1;
                        Canvas.SetTop(dropIndicator, indicatorY);
                        dropIndicator.Height = firstTab.ActualHeight - 2;
                        OnLogDebug?.Invoke($"📍 Final position: X={indicatorX}, Y={indicatorY}, Height={dropIndicator.Height}", string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error updating drop indicator", ex);
                dropIndicator.Visibility = Visibility.Hidden;
            }
        }

        // 🎯 Find drop target index based on mouse position
        private int FindDropTargetIndex(Point mousePosition)
        {
            try
            {
                OnLogDebug?.Invoke($"🎯 FindDropTargetIndex called with mousePosition: {mousePosition}", string.Empty);
                
                // Use the mouse position directly - it's already relative to tabHeaderPanel
                double mouseX = mousePosition.X;
                OnLogDebug?.Invoke($"🎯 Mouse X position: {mouseX}", string.Empty);
                
                // 🔍 Find which tab position we're over
                for (int i = 0; i < tabHeaderPanel.Children.Count; i++)
                {
                    if (tabHeaderPanel.Children[i] is Button tabButton)
                    {
                        // Get tab's position relative to tabHeaderPanel
                        var tabPosition = tabButton.TransformToAncestor(tabHeaderPanel)
                            .Transform(new Point(0, 0));
                        
                        double tabStartX = tabPosition.X;
                        double tabMidX = tabPosition.X + tabButton.ActualWidth / 2;
                        double tabEndX = tabPosition.X + tabButton.ActualWidth;
                        
                        OnLogDebug?.Invoke($"🎯 Tab {i}: StartX={tabStartX:F1}, MidX={tabMidX:F1}, EndX={tabEndX:F1}", string.Empty);
                        
                        // If mouse is before the midpoint of this tab, insert before it
                        // Add hysteresis: require at least 5px movement to change drop target
                        double hysteresisBuffer = 5.0;
                        bool shouldInsertHere = false;
                        
                        if (lastDropTargetIndex == i && mouseX < tabMidX + hysteresisBuffer)
                        {
                            // Stick to current target if we're within hysteresis buffer
                            shouldInsertHere = true;
                        }
                        else if (lastDropTargetIndex != i && mouseX < tabMidX - hysteresisBuffer)
                        {
                            // Switch to new target only if we're outside hysteresis buffer
                            shouldInsertHere = true;
                        }
                        
                        if (shouldInsertHere)
                        {
                            OnLogDebug?.Invoke($"🎯 Mouse before tab {i} midpoint - returning index {i}", string.Empty);
                            lastDropTargetIndex = i;
                            return i;
                        }
                    }
                }
                
                // 📍 If past all tabs, drop at end
                OnLogDebug?.Invoke($"🎯 Mouse past all tabs - returning end index {tabHeaderPanel.Children.Count}", string.Empty);
                lastDropTargetIndex = tabHeaderPanel.Children.Count;
                return tabHeaderPanel.Children.Count;
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error finding drop target index", ex);
                return draggedTabOriginalIndex; // Default to original position
            }
        }

        // ✅ Complete drag operation and reorder tabs
        private void CompleteDragOperation(bool performReorder = true)
        {
            try
            {
                OnLogDebug?.Invoke("🎯 Completing drag operation", string.Empty);
                
                if (performReorder && dropTargetIndex >= 0 && 
                    dropTargetIndex != draggedTabOriginalIndex)
                {
                    // 🔄 Perform the actual reordering
                    ReorderTab(draggedTabOriginalIndex, dropTargetIndex);
                    OnLogDebug?.Invoke($"✅ Tab reordered from {draggedTabOriginalIndex} to {dropTargetIndex}", string.Empty);
                }
                
                // 🧹 Cleanup drag operation
                CleanupDragOperation();
                
                OnLogDebug?.Invoke("✅ Drag operation completed", string.Empty);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error completing drag operation", ex);
                CleanupDragOperation();
            }
        }

        // 🧹 Clean up drag operation state
        private void CleanupDragOperation()
        {
            try
            {
                // 🖱️ Release mouse capture
                if (draggedTab != null)
                {
                    draggedTab.ReleaseMouseCapture();
                }
                
                // 🎨 Remove drag visual
                if (dragVisual != null && dragCanvas != null)
                {
                    dragCanvas.Children.Remove(dragVisual);
                    dragVisual = null;
                }
                
                // 📍 Hide drop indicator
                if (dropIndicator != null)
                {
                    dropIndicator.Visibility = Visibility.Hidden;
                }
                
                // 🔄 Reset drag state
                isDragging = false;
                draggedTab = null;
                draggedTabOriginalIndex = -1;
                dropTargetIndex = -1;
                lastDropTargetIndex = -1;
                
                OnLogDebug?.Invoke("🧹 Drag operation cleaned up", string.Empty);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error cleaning up drag operation", ex);
            }
        }

        // 🔄 Actually reorder the tab in collections and UI
        private void ReorderTab(int fromIndex, int toIndex)
        {
            try
            {
                if (fromIndex < 0 || fromIndex >= tabs.Count || 
                    toIndex < 0 || toIndex > tabs.Count) return;
                
                // 📝 Store the tab being moved
                var movingTab = tabs[fromIndex];
                
                // 🗂️ Remove from collections
                tabs.RemoveAt(fromIndex);
                tabHeaderPanel.Children.RemoveAt(fromIndex);
                
                // 📌 Insert at new position
                var insertIndex = toIndex > fromIndex ? toIndex - 1 : toIndex;
                insertIndex = Math.Max(0, Math.Min(insertIndex, tabs.Count));
                
                tabs.Insert(insertIndex, movingTab);
                tabHeaderPanel.Children.Insert(insertIndex, movingTab.HeaderButton);
                
                // 💾 Mark as changed
                OnDataChanged?.Invoke(true);
                OnStatusUpdateRequested?.Invoke();
                
                OnLogDebug?.Invoke($"✅ Tab reordered from position {fromIndex} to {insertIndex}", string.Empty);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error reordering tab", ex);
            }
        }

        // 🔍 Helper method to get tab index from button
        private int GetTabIndex(Button tabButton)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].HeaderButton == tabButton)
                    return i;
            }
            return -1;
        }

        // 📝 Helper method to get tab title from button
        private string GetTabTitle(Button tabButton)
        {
            var tab = tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
            return tab?.Title ?? "Tab";
        }
        #endregion

        #region Public Methods
        // 📝 Creates a new tab
        public void CreateNewTab()
        {
            try
            {
                OnLogDebug?.Invoke("🆕 Starting CreateNewTab", string.Empty);
                
                int tabCount = tabs.Count + 1;
                var noteTab = new NoteTab();
                var tabTitle = $"Note {tabCount}";

                OnLogDebug?.Invoke($"📝 Creating tab: {tabTitle}", string.Empty);

                // 🔔 Wire up change tracking for auto-save and status updates
                noteTab.OnDataChanged += () => 
                {
                    try
                    {
                        OnDataChanged?.Invoke(true);
                        OnStatusUpdateRequested?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        OnLogError?.Invoke("Error in OnDataChanged handler", ex);
                    }
                };

                OnLogDebug?.Invoke("✅ Data changed handler wired", string.Empty);

                // 🧠 Create custom tab object
                var customTab = new CustomTab
                {
                    Title = tabTitle,
                    Content = noteTab,
                    HeaderButton = CreateTabHeaderButton(tabTitle)
                };

                OnLogDebug?.Invoke("✅ Custom tab object created", string.Empty);

                // 🏷️ Set the note tab's title to match
                noteTab.Title = tabTitle;

                // 🔗 Wire up click event for tab selection
                customTab.HeaderButton.Click += (s, e) => 
                {
                    try
                    {
                        OnLogDebug?.Invoke($"🖱️ Tab clicked: {tabTitle}", string.Empty);
                        SelectTab(customTab);
                    }
                    catch (Exception ex)
                    {
                        OnLogError?.Invoke("Error in tab click handler", ex);
                    }
                };

                OnLogDebug?.Invoke("✅ Click handler wired", string.Empty);

                // 📍 Add to collections
                tabs.Add(customTab);
                tabHeaderPanel.Children.Add(customTab.HeaderButton);

                OnLogDebug?.Invoke("✅ Added to collections", string.Empty);

                // 🎯 Select the new tab
                SelectTab(customTab);
                
                OnLogDebug?.Invoke("✅ Tab selected", string.Empty);
                
                // 💾 Mark as changed
                OnDataChanged?.Invoke(true);
                OnStatusUpdateRequested?.Invoke();
                
                OnLogDebug?.Invoke($"🎉 CreateNewTab completed successfully: {tabTitle}", string.Empty);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("CRITICAL ERROR in CreateNewTab", ex);
                CustomDialog.ShowError(
                    Application.Current.MainWindow,
                    $"Error creating new tab:\n{ex.Message}\n\nCheck console for details.",
                    "Tab Creation Error");
            }
        }

        // 🎯 Selects the specified tab
        public void SelectTab(CustomTab tab)
        {
            // 🧠 Skip if already selected (avoid redundant updates)
            if (selectedTab == tab) return;
            
            var previousTab = selectedTab;
            selectedTab = tab;

            // 🎨 Only update the tabs that changed
            if (previousTab != null)
            {
                UpdateTabSelection(previousTab, false); // Deselect old tab
            }
            UpdateTabSelection(tab, true); // Select new tab

            // 🎬 Update content (without animation)
            tabContentArea.Content = tab.Content;
            
            // 📊 Update status bar
            OnStatusUpdateRequested?.Invoke();
        }

        // ❌ Delete the currently selected tab
        public void DeleteCurrentTab()
        {
            if (selectedTab == null) return;

            // 🔔 Check settings first - if ConfirmTabDeletion is false, delete immediately
            if (appSettings != null && !appSettings.ConfirmTabDeletion)
            {
                DeleteTab(selectedTab);
                return;
            }

            // 🔔 Check if user previously chose "don't ask again"
            if (skipDeleteConfirmation)
            {
                DeleteTab(selectedTab);
                return;
            }

            // 🎭 Show custom confirmation dialog
            var (confirmed, dontAskAgain) = CustomDialog.ShowDeleteConfirmation(
                Application.Current.MainWindow,
                selectedTab.Title,
                showDontAskAgain: true);

            if (!confirmed) return;

            // 💭 Remember user preference if they checked "don't ask again"
            if (dontAskAgain)
            {
                skipDeleteConfirmation = true;
                // 📝 Also update the settings to reflect this choice
                if (appSettings != null)
                {
                    appSettings.ConfirmTabDeletion = false;
                    OnSettingsNeedUpdate?.Invoke(); // Notify that settings need to be saved
                }
                OnLogDebug?.Invoke("🔕 Delete confirmation disabled by user preference and settings updated", string.Empty);
            }

            DeleteTab(selectedTab);
        }

        // 🗑️ Delete a specific tab
        public void DeleteSpecificTab(Button tabButton)
        {
            var tabToDelete = tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
            if (tabToDelete == null) return;

            // 🔔 Check settings first - if ConfirmTabDeletion is false, delete immediately
            if (appSettings != null && !appSettings.ConfirmTabDeletion)
            {
                DeleteTab(tabToDelete);
                return;
            }

            // 🔔 Check if user previously chose "don't ask again"
            if (skipDeleteConfirmation)
            {
                DeleteTab(tabToDelete);
                return;
            }

            // 🎭 Show custom confirmation dialog
            var (confirmed, dontAskAgain) = CustomDialog.ShowDeleteConfirmation(
                Application.Current.MainWindow,
                tabToDelete.Title,
                showDontAskAgain: true);

            if (!confirmed) return;

            // 💭 Remember user preference if they checked "don't ask again"
            if (dontAskAgain)
            {
                skipDeleteConfirmation = true;
                // 📝 Also update the settings to reflect this choice
                if (appSettings != null)
                {
                    appSettings.ConfirmTabDeletion = false;
                    OnSettingsNeedUpdate?.Invoke(); // Notify that settings need to be saved
                }
                OnLogDebug?.Invoke("🔕 Delete confirmation disabled by user preference and settings updated", string.Empty);
            }

            DeleteTab(tabToDelete);
        }

        // ✏️ Rename tab functionality
        public void RenameTab(TextBlock textBlock)
        {
            var editBox = new TextBox
            {
                Text = textBlock.Text,
                // 📏 Dynamic width that exactly matches the text space needed
                MinWidth = 60,
                MaxWidth = 200,
                Width = Math.Max(60, textBlock.ActualWidth + 15), // Small buffer for typing
                Height = textBlock.ActualHeight > 0 ? textBlock.ActualHeight : Double.NaN,
                Margin = textBlock.Margin,
                VerticalAlignment = textBlock.VerticalAlignment,
                HorizontalAlignment = textBlock.HorizontalAlignment,
                FontSize = textBlock.FontSize,
                FontFamily = textBlock.FontFamily,
                FontWeight = textBlock.FontWeight,
                FontStyle = textBlock.FontStyle,
                TextAlignment = TextAlignment.Center, // Center align like tab text
                VerticalContentAlignment = VerticalAlignment.Center
            };

            // 🎨 Create completely seamless, invisible textbox styling
            try
            {
                // Make background completely transparent to blend with tab
                editBox.Background = Brushes.Transparent;
                editBox.SetResourceReference(TextBox.ForegroundProperty, "AppForegroundBrush");
                
                // 🌟 Remove all visual boundaries for seamless appearance
                editBox.BorderThickness = new Thickness(0); // No border at all
                editBox.BorderBrush = Brushes.Transparent;
                
                // 🎯 Advanced styling for modern inline editing experience
                editBox.CaretBrush = null; // Use system default caret
                editBox.SelectionBrush = null; // Use system default selection
                
                // Remove default textbox styling  
                editBox.Padding = new Thickness(0); // No internal padding
                editBox.Margin = textBlock.Margin; // Match original margin exactly
            }
            catch
            {
                // Fallback - still make it as seamless as possible
                editBox.Background = Brushes.Transparent;
                editBox.Foreground = textBlock.Foreground ?? new SolidColorBrush(Color.FromRgb(255, 255, 255));
                editBox.BorderThickness = new Thickness(0);
                editBox.BorderBrush = Brushes.Transparent;
                editBox.Padding = new Thickness(0);
            }

            // Find the button that contains this TextBlock
            var tabButton = tabs.FirstOrDefault(t => t.HeaderButton.Content == textBlock)?.HeaderButton;
            if (tabButton == null) return;

            // 🎯 Store original tab appearance for restoration
            var originalBackground = tabButton.Background;
            var originalBorderThickness = tabButton.BorderThickness;
            var originalBorderBrush = tabButton.BorderBrush;

            // 🌟 Apply clear "EDITING MODE" visual indicators to the tab itself
            try
            {
                // Make the entire tab clearly show it's in edit mode
                tabButton.SetResourceReference(Button.BackgroundProperty, "TabBackgroundBrush");
                
                // Add a distinctive border to show editing state
                tabButton.BorderThickness = new Thickness(2);
                tabButton.BorderBrush = new SolidColorBrush(Color.FromRgb(74, 144, 226)); // Nice blue
                
                // Add subtle glow effect by using a slightly different background
                var editBg = new SolidColorBrush(Color.FromArgb(40, 74, 144, 226)); // Subtle blue tint
                tabButton.Background = editBg;
                
                // Keep textbox transparent so tab styling shows through
                editBox.Background = Brushes.Transparent;
            }
            catch
            {
                // Fallback - at least make it visually different
                tabButton.Background = new SolidColorBrush(Color.FromArgb(60, 100, 150, 255));
                tabButton.BorderThickness = new Thickness(1);
                tabButton.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 150, 255));
                editBox.Background = Brushes.Transparent;
            }

            bool isEditing = true;

            void SaveRename()
            {
                if (!isEditing) return; // Prevent double-execution
                isEditing = false;
                
                string newName = editBox.Text.Trim();
                if (string.IsNullOrEmpty(newName)) newName = "Untitled";
                
                textBlock.Text = newName;
                tabButton.Content = textBlock;

                // 🎨 Restore original tab appearance
                RestoreTabAppearance();

                // Update tab title in our collection
                var tab = tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
                if (tab != null)
                {
                    tab.Title = newName;
                    tab.Content.Title = newName; // Update the NoteTab title too
                    OnDataChanged?.Invoke(true); // 💾 Mark as changed for auto-save
                }
            }

            void CancelRename()
            {
                if (!isEditing) return; // Prevent double-execution
                isEditing = false;
                tabButton.Content = textBlock; // Restore original content
                
                // 🎨 Restore original tab appearance
                RestoreTabAppearance();
            }

            void RestoreTabAppearance()
            {
                // 🔄 Restore the tab to its normal appearance
                try
                {
                    tabButton.Background = originalBackground;
                    tabButton.BorderThickness = originalBorderThickness;
                    tabButton.BorderBrush = originalBorderBrush;
                    
                    // Force refresh of tab selection state to ensure proper styling
                    var currentTab = tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
                    if (currentTab != null)
                    {
                        UpdateTabSelection(currentTab, currentTab == selectedTab);
                    }
                }
                catch
                {
                    // Fallback - set to transparent/default
                    tabButton.Background = Brushes.Transparent;
                    tabButton.BorderThickness = new Thickness(0);
                    tabButton.BorderBrush = Brushes.Transparent;
                }
            }

            // Handle keyboard events
            editBox.KeyDown += (k, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    SaveRename();
                    args.Handled = true;
                }
                else if (args.Key == Key.Escape)
                {
                    CancelRename();
                    args.Handled = true;
                }
            };

            // Handle losing focus (clicking outside)
            editBox.LostFocus += (s2, e2) => 
            {
                // Use Dispatcher to ensure this runs after any pending mouse events
                Application.Current.Dispatcher.BeginInvoke(new Action(() => SaveRename()));
            };

            // Replace content and focus
            tabButton.Content = editBox;
            
            // 🎯 Ensure smooth focus and selection with better timing
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                editBox.Focus();
                editBox.SelectAll();
                
                // 🔍 Ensure the textbox is properly visible and ready for editing
                editBox.CaretIndex = editBox.Text.Length; // Position cursor at end initially
                editBox.SelectAll(); // Then select all for easy replacement
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // 📋 Duplicate the specified tab
        public void DuplicateTab(Button sourceTabButton)
        {
            var sourceTab = tabs.FirstOrDefault(t => t.HeaderButton == sourceTabButton);
            if (sourceTab == null) return;

            // 🧠 Create new tab with copied content
            var newNoteTab = new NoteTab();
            var duplicateTitle = $"{sourceTab.Title} (Copy)";

            // 🔔 Wire up change tracking for auto-save
            newNoteTab.OnDataChanged += () => OnDataChanged?.Invoke(true);

            var duplicateTab = new CustomTab
            {
                Title = duplicateTitle,
                Content = newNoteTab,
                HeaderButton = CreateTabHeaderButton(duplicateTitle)
            };

            // 📝 Copy content from source tab
            newNoteTab.Title = duplicateTitle;
            newNoteTab.TextContent = sourceTab.Content.TextContent;
            newNoteTab.ImageFiles = new List<string>(sourceTab.Content.ImageFiles); // Copy image references
            newNoteTab.ImageTimestamps = new Dictionary<string, DateTime>(sourceTab.Content.ImageTimestamps); // Copy image timestamps

            // 🔗 Wire up click event for tab selection
            duplicateTab.HeaderButton.Click += (s, e) => SelectTab(duplicateTab);

            // 📍 Add to collections and select the new tab
            tabs.Add(duplicateTab);
            tabHeaderPanel.Children.Add(duplicateTab.HeaderButton);
            SelectTab(duplicateTab);

            OnDataChanged?.Invoke(true); // 💾 Mark as changed for auto-save
        }

        // 🎯 Start renaming the currently selected tab
        public void StartRenameCurrentTab()
        {
            if (selectedTab != null)
            {
                // Find the TextBlock within the tab button
                if (selectedTab.HeaderButton.Content is TextBlock textBlock)
                {
                    RenameTab(textBlock);
                }
            }
        }

        // 🔄 Switch to the next tab
        public void SwitchToNextTab()
        {
            if (tabs.Count <= 1) return; // No point switching if only one tab

            var currentIndex = selectedTab != null ? tabs.IndexOf(selectedTab) : -1;
            var nextIndex = (currentIndex + 1) % tabs.Count; // Wrap around to first tab
            
            if (nextIndex >= 0 && nextIndex < tabs.Count)
            {
                SelectTab(tabs[nextIndex]);
            }
        }

        // 📖 Load tabs from saved data
        public void LoadTabs(List<SavedNote> savedNotes)
        {
            try
            {
                // Clear existing tabs
                tabs.Clear();
                tabHeaderPanel.Children.Clear();

                if (savedNotes?.Any() == true)
                {
                    foreach (var savedNote in savedNotes)
                    {
                        var noteTab = new NoteTab();
                        noteTab.Title = savedNote.Title;
                        
                        // Load rich text content if available, otherwise fall back to plain text
                        if (!string.IsNullOrEmpty(savedNote.RichTextContent))
                        {
                            noteTab.RichTextContent = savedNote.RichTextContent;
                        }
                        else
                        {
                            noteTab.TextContent = savedNote.TextContent;
                        }
                        
                        noteTab.ImageFiles = savedNote.ImageFiles ?? new List<string>();
                        noteTab.ImageTimestamps = savedNote.ImageTimestamps ?? new Dictionary<string, DateTime>();

                        // 🔔 Wire up change tracking
                        noteTab.OnDataChanged += () => 
                        {
                            OnDataChanged?.Invoke(true);
                            OnStatusUpdateRequested?.Invoke();
                        };

                        var customTab = new CustomTab
                        {
                            Title = savedNote.Title,
                            Content = noteTab,
                            HeaderButton = CreateTabHeaderButton(savedNote.Title)
                        };

                        // 🔗 Click event is already wired up in CreateTabHeaderButton

                        tabs.Add(customTab);
                        tabHeaderPanel.Children.Add(customTab.HeaderButton);
                    }

                    // Select the first tab
                    if (tabs.Any())
                    {
                        SelectTab(tabs.First());
                    }
                }
                else
                {
                    // Create default tab if no saved data
                    CreateNewTab();
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error loading tabs", ex);
                CreateNewTab(); // Fallback to default tab
            }
        }

        // 💾 Get data for saving
        public List<SavedNote> GetSaveData()
        {
            return tabs.Select(tab => new SavedNote
            {
                Title = tab.Title,
                TextContent = tab.Content.TextContent,
                RichTextContent = tab.Content.RichTextContent,
                ImageFiles = tab.Content.ImageFiles,
                ImageTimestamps = tab.Content.ImageTimestamps
            }).ToList();
        }

        // 🎨 Refresh all tab visuals (useful after theme changes)
        public void RefreshTabVisuals()
        {
            foreach (var tab in tabs)
            {
                UpdateTabSelection(tab, tab == selectedTab);
            }
        }
        #endregion

        #region Private Methods
        
        // 📑 Create a new tab header button
        private Button CreateTabHeaderButton(string title)
        {
            var textBlock = new TextBlock 
            { 
                Text = title, 
                Margin = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "AppForegroundBrush");

            var tabButton = new Button
            {
                Content = textBlock,
                Style = (Style)Application.Current.FindResource("TabButtonStyle"),
                Height = 32,
                Margin = new Thickness(2, 0, 2, 0)
            };

            // 🎯 Add tooltip on hover
            tabButton.MouseEnter += (s, e) =>
            {
                if (tabButton.Content is TextBlock) // Only if not currently editing
                {
                    tabButton.ToolTip = "Double-click to rename";
                }
            };

            tabButton.MouseLeave += (s, e) =>
            {
                tabButton.ToolTip = null;
            };

            tabButton.Click += (s, e) =>
            {
                var tab = tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
                if (tab != null) SelectTab(tab);
            };

            // 📋 Right-click context menu
            var contextMenu = new ContextMenu();

            // ✏️ Rename Tab
            var renameItem = new MenuItem { Header = "📝 Rename Tab" };
            renameItem.Click += (s, e) => RenameTab(textBlock);

            // ➕ New Tab  
            var newTabItem = new MenuItem { Header = "➕ New Tab" };
            newTabItem.Click += (s, e) => CreateNewTab();

            // 📋 Duplicate Tab
            var duplicateItem = new MenuItem { Header = "📋 Duplicate Tab" };
            duplicateItem.Click += (s, e) => DuplicateTab(tabButton);

            // ❌ Delete Tab
            var deleteItem = new MenuItem { Header = "❌ Delete Tab" };
            deleteItem.Click += (s, e) => DeleteSpecificTab(tabButton);

            contextMenu.Items.Add(renameItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(newTabItem);
            contextMenu.Items.Add(duplicateItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteItem);

            tabButton.ContextMenu = contextMenu;

            // ✏️ Double-click to rename tab
            tabButton.MouseDoubleClick += (s, e) =>
            {
                if (tabButton.Content is TextBlock textBlock)
                {
                    RenameTab(textBlock);
                    e.Handled = true; // Prevent other handlers from processing this event
                }
            };

            // 🎯 Ensure normal arrow cursor
            tabButton.Cursor = Cursors.Arrow; // Explicitly set to default arrow cursor

            // 🎯 Add drag and drop event handlers
            AddDragHandlers(tabButton);

            return tabButton;
        }

        // 🎯 Add drag and drop event handlers to a tab button
        private void AddDragHandlers(Button tabButton)
        {
            // 🖱️ Mouse down - potential start of drag
            tabButton.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                dragStartPoint = e.GetPosition(tabButton);
                OnLogDebug?.Invoke("🖱️ Mouse down on tab", string.Empty);
            };

            // 🎯 Mouse move - check if we should start dragging
            tabButton.PreviewMouseMove += (sender, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
                {
                    var currentPosition = e.GetPosition(tabButton);
                    var diff = dragStartPoint - currentPosition;
                    
                    // 📏 Check if mouse moved far enough to start drag (prevents accidental drags)
                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        StartDragOperation(tabButton, dragStartPoint);
                    }
                }
                else if (isDragging && draggedTab == tabButton)
                {
                    // 🔄 Update drag visual position
                    var windowPosition = e.GetPosition(Application.Current.MainWindow);
                    UpdateDragVisual(windowPosition);
                    
                    // 🎯 Find drop target and update indicator
                    dropTargetIndex = FindDropTargetIndex(e.GetPosition(tabHeaderPanel));
                    UpdateDropIndicator(dropTargetIndex);
                }
            };

            // 🖱️ Mouse up - complete drag operation
            tabButton.PreviewMouseLeftButtonUp += (sender, e) =>
            {
                if (isDragging && draggedTab == tabButton)
                {
                    CompleteDragOperation(true);
                }
            };

            // 🚫 Mouse leave - cancel drag if mouse leaves the window
            tabButton.MouseLeave += (sender, e) =>
            {
                if (isDragging && draggedTab == tabButton)
                {
                    // Only cancel if mouse truly left the application area
                    var mousePos = Mouse.GetPosition(Application.Current.MainWindow);
                    var windowBounds = new Rect(0, 0, 
                        Application.Current.MainWindow.ActualWidth, 
                        Application.Current.MainWindow.ActualHeight);
                    
                    if (!windowBounds.Contains(mousePos))
                    {
                        CompleteDragOperation(false); // Cancel reorder
                    }
                }
            };
        }

        // 🗑️ Internal delete tab logic
        private void DeleteTab(CustomTab tabToRemove)
        {
            // 🧹 Dispose tab content to prevent memory leaks
            if (tabToRemove.Content is IDisposable disposableContent)
            {
                disposableContent.Dispose();
            }
            
            // 🧠 Remove from collections
            tabs.Remove(tabToRemove);
            tabHeaderPanel.Children.Remove(tabToRemove.HeaderButton);

            // 🎯 Handle selection after removal
            if (tabToRemove == selectedTab)
            {
                if (tabs.Count > 0)
                {
                    // Select the last tab
                    SelectTab(tabs.Last());
                }
                else
                {
                    // Create a new tab if none left
                    CreateNewTab();
                }
            }
            
            // 📊 Update status bar after deletion
            OnDataChanged?.Invoke(true);
            OnStatusUpdateRequested?.Invoke();
        }

        // 🎨 Update tab selection visuals (without animation)
        private void UpdateTabSelection(CustomTab tab, bool isSelected)
        {
            try
            {
                // 🎯 Set Tag property for Edge-like styling triggers
                tab.HeaderButton.Tag = isSelected ? "Selected" : null;
                
                // 🎨 Set background brush (kept for fallback compatibility)
                var targetBrush = isSelected 
                    ? (Brush)Application.Current.FindResource("TabBackgroundBrush") 
                    : Brushes.Transparent;
                
                tab.HeaderButton.Background = targetBrush;
                
                // Ensure text color is properly set for contrast
                if (tab.HeaderButton.Content is TextBlock textBlock)
                {
                    try
                    {
                        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "AppForegroundBrush");
                    }
                    catch
                    {
                        // Fallback text color
                        textBlock.Foreground = (Brush)Application.Current.FindResource("AppForegroundBrush") ?? Brushes.White;
                    }
                }
                
                OnLogDebug?.Invoke($"🎨 Tab selection updated: {tab.Title}, Selected: {isSelected}, Tag: {tab.HeaderButton.Tag}", string.Empty);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke($"Failed to update tab selection for {tab.Title}", ex);
                
                // Fallback with proper contrast - detect if we're in dark mode
                if (isSelected)
                {
                    try
                    {
                        // Try to detect current theme by checking background color
                        var appBg = (Brush)Application.Current.FindResource("AppBackgroundBrush");
                        if (appBg is SolidColorBrush solidBrush)
                        {
                            var bgColor = solidBrush.Color;
                            // If background is dark (low luminance), use darker tab color
                            var isDarkTheme = (bgColor.R + bgColor.G + bgColor.B) < 384; // 128 * 3
                            
                            var tabColor = isDarkTheme
                                ? Color.FromRgb(36, 52, 71)    // Dark blue for dark theme  
                                : Color.FromRgb(232, 240, 245); // Light blue for light theme
                                
                            tab.HeaderButton.Background = new SolidColorBrush(tabColor);
                            System.Diagnostics.Debug.WriteLine($"🎨 Using fallback color: {tabColor} (Dark theme: {isDarkTheme})");
                        }
                        else
                        {
                            // Default to subtle gray
                            tab.HeaderButton.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                            System.Diagnostics.Debug.WriteLine("🎨 Using default gray fallback");
                        }
                    }
                    catch (Exception ex2)
                    {
                        // Final fallback - subtle gray
                        tab.HeaderButton.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                        System.Diagnostics.Debug.WriteLine($"🎨 Using final fallback due to: {ex2.Message}");
                    }
                }
                else
                {
                    tab.HeaderButton.Background = Brushes.Transparent;
                }
            }
        }
        #endregion
    }
} 