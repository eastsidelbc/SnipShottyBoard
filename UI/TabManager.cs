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
    /// ✅ Phase 4D P2.1: Implemented IDisposable for proper cleanup
    public class TabManager : IDisposable
    {
        private bool _disposed = false;
        #region Fields & Properties
        private List<CustomTab> _tabs = new List<CustomTab>();
        private CustomTab _selectedTab = null;
        
        // UI Dependencies
        private readonly Panel _tabHeaderPanel;
        private readonly ContentPresenter _tabContentArea;
        
        // 🎯 Drag and Drop Fields
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private Button _draggedTab = null;
        private Canvas _dragCanvas = null;
        private Border _dragVisual = null;
        private Border _dropIndicator = null; // 📍 Drop indicator line
        private int _draggedTabOriginalIndex = -1;
        private int _dropTargetIndex = -1;
        private int _lastDropTargetIndex = -1; // 🎯 For hysteresis

        // 🔕 User Preferences - Settings reference for delete confirmation
        private AppSettings _appSettings;
        private bool _skipDeleteConfirmation = false;
        
        // Event handlers for MainWindow communication
        public event Action<bool> OnDataChanged;
        public event Action OnStatusUpdateRequested;
        public event Action<string, string> OnLogDebug;
        public event Action<string, Exception> OnLogError;
        public event Action OnSettingsNeedUpdate; // New event for when settings need to be updated
        
        public CustomTab SelectedTab => _selectedTab;
        public IReadOnlyList<CustomTab> Tabs => _tabs.AsReadOnly();
        public int TabCount => _tabs.Count;
        
        // 🔍 Check if delete confirmation is effectively disabled
        public bool IsDeleteConfirmationDisabled => 
            (_appSettings != null && !_appSettings.ConfirmTabDeletion) || _skipDeleteConfirmation;
        #endregion

        #region Constructor
        public TabManager(Panel _tabHeaderPanel, ContentPresenter _tabContentArea)
        {
            this._tabHeaderPanel = _tabHeaderPanel ?? throw new ArgumentNullException(nameof(_tabHeaderPanel));
            this._tabContentArea = _tabContentArea ?? throw new ArgumentNullException(nameof(_tabContentArea));
            
            // 🎯 Setup drag canvas for visual feedback
            InitializeDragCanvas();
        }
        #endregion

        #region Settings Management
        // ⚙️ Update settings reference
        /// ✅ Phase 4D P2.8: Added null guard
        public void UpdateSettings(AppSettings? settings)
        {
            // ✅ Null guard
            if (settings == null)
            {
                OnLogError?.Invoke("UpdateSettings called with null settings", new ArgumentNullException(nameof(settings)));
                return;
            }
            
            this._appSettings = settings;

            // If settings explicitly re-enable confirmation, clear the in-session skip flag.
            // Without this, the OR in IsDeleteConfirmationDisabled means re-enabling via Settings has no effect.
            if (settings.ConfirmTabDeletion)
                _skipDeleteConfirmation = false;

            OnLogDebug?.Invoke($"⚙️ TabManager settings updated - ConfirmTabDeletion: {settings.ConfirmTabDeletion}", string.Empty);
        }

        // 🔄 Reset "don't ask again" preference
        public void ResetDeleteConfirmationPreference()
        {
            _skipDeleteConfirmation = false;
            // 📝 Also update the settings to reflect this choice
            if (_appSettings != null)
            {
                _appSettings.ConfirmTabDeletion = true;
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
                _dragCanvas = new Canvas
                {
                    Background = Brushes.Transparent,
                    IsHitTestVisible = false
                };
                
                OnLogDebug?.Invoke("🎨 Drag canvas created", string.Empty);
                
            // 📍 Create drop indicator line
            _dropIndicator = new Border
            {
                Width = 3,
                Height = 30,
                Background = (SolidColorBrush)Application.Current.FindResource(ResourceKeys.AccentBrush),
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
                        
                        mainGrid.Children.Add(_dragCanvas);
                        Panel.SetZIndex(_dragCanvas, 9999); // Ensure it's on top
                        
                        // Add drop indicator to canvas
                        _dragCanvas.Children.Add(_dropIndicator);
                        
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
                
                _isDragging = true;
                _dragStartPoint = startPoint;
                _draggedTab = tabButton;
                _draggedTabOriginalIndex = GetTabIndex(tabButton);
                _lastDropTargetIndex = -1; // Reset hysteresis tracking
                
                OnLogDebug?.Invoke($"🎯 Drag started for tab at index {_draggedTabOriginalIndex}", string.Empty);
                
                // 🎨 Create visual feedback
                CreateDragVisual(tabButton);
                
                // 🖱️ Capture mouse for drag operation
                tabButton.CaptureMouse();
                
                OnLogDebug?.Invoke($"✅ Drag started for tab at index {_draggedTabOriginalIndex}", string.Empty);
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
                    Background = (SolidColorBrush)Application.Current.FindResource(ResourceKeys.DragGhostBrush),
                    BorderBrush = (SolidColorBrush)Application.Current.FindResource(ResourceKeys.DragGhostBorderBrush),
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

                _dragVisual = visualCopy;
                _dragCanvas.Children.Add(_dragVisual);
                
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
            if (_dragVisual != null)
            {
                Canvas.SetLeft(_dragVisual, currentPosition.X - _dragVisual.Width / 2);
                Canvas.SetTop(_dragVisual, currentPosition.Y - _dragVisual.Height / 2);
            }
        }

        // 📍 Update drop indicator position and visibility (row-aware for multi-row layout)
        private void UpdateDropIndicator(int targetIndex)
        {
            if (_dropIndicator == null || _tabHeaderPanel == null) 
            {
                OnLogDebug?.Invoke($"🚫 UpdateDropIndicator skipped - _dropIndicator: {_dropIndicator != null}, _tabHeaderPanel: {_tabHeaderPanel != null}", string.Empty);
                return;
            }
            
            try
            {
                OnLogDebug?.Invoke($"📍 UpdateDropIndicator called with targetIndex: {targetIndex}, _isDragging: {_isDragging}", string.Empty);
                
                if (targetIndex < 0 || !_isDragging)
                {
                    // Hide indicator
                    _dropIndicator.Visibility = Visibility.Hidden;
                    OnLogDebug?.Invoke("📍 Drop indicator hidden", string.Empty);
                    return;
                }
                
                // Show indicator
                _dropIndicator.Visibility = Visibility.Visible;
                OnLogDebug?.Invoke("📍 Drop indicator shown", string.Empty);
                
                // Calculate position for drop indicator
                double indicatorX = 0;
                double indicatorY = 0;
                Button referenceTab = null;
                
                if (targetIndex >= _tabHeaderPanel.Children.Count)
                {
                    // Drop at end - position after last tab
                    if (_tabHeaderPanel.Children.Count > 0)
                    {
                        var lastTab = _tabHeaderPanel.Children[_tabHeaderPanel.Children.Count - 1] as Button;
                        if (lastTab != null && Application.Current.MainWindow != null)
                        {
                            referenceTab = lastTab;
                            // Transform from tab to main window, then to drag canvas
                            var lastTabPosInWindow = lastTab.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                            var canvasPosInWindow = _dragCanvas.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                            indicatorX = lastTabPosInWindow.X - canvasPosInWindow.X + lastTab.ActualWidth + 2;
                            indicatorY = lastTabPosInWindow.Y - canvasPosInWindow.Y + 1;
                            OnLogDebug?.Invoke($"📍 Positioning at end: X={indicatorX}, Y={indicatorY}", string.Empty);
                        }
                    }
                }
                else if (targetIndex < _tabHeaderPanel.Children.Count)
                {
                    // Drop before target tab
                    var targetTab = _tabHeaderPanel.Children[targetIndex] as Button;
                    if (targetTab != null && Application.Current.MainWindow != null)
                    {
                        referenceTab = targetTab;
                        // Transform from tab to main window, then to drag canvas
                        var targetTabPosInWindow = targetTab.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                        var canvasPosInWindow = _dragCanvas.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
                        indicatorX = targetTabPosInWindow.X - canvasPosInWindow.X - 2;
                        indicatorY = targetTabPosInWindow.Y - canvasPosInWindow.Y + 1;
                        OnLogDebug?.Invoke($"📍 Positioning before tab {targetIndex}: X={indicatorX}, Y={indicatorY}", string.Empty);
                    }
                }
                
                // Position the drop indicator
                Canvas.SetLeft(_dropIndicator, indicatorX);
                Canvas.SetTop(_dropIndicator, indicatorY);
                
                // Set height based on reference tab
                if (referenceTab != null)
                {
                    _dropIndicator.Height = referenceTab.ActualHeight - 2;
                    OnLogDebug?.Invoke($"📍 Final position: X={indicatorX}, Y={indicatorY}, Height={_dropIndicator.Height}", string.Empty);
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error updating drop indicator", ex);
                _dropIndicator.Visibility = Visibility.Hidden;
            }
        }

        // 🎯 Find drop target index based on mouse position (row-aware for multi-row layout)
        private int FindDropTargetIndex(Point mousePosition)
        {
            try
            {
                OnLogDebug?.Invoke($"🎯 FindDropTargetIndex called with mousePosition: {mousePosition}", string.Empty);
                
                double mouseX = mousePosition.X;
                double mouseY = mousePosition.Y;
                OnLogDebug?.Invoke($"🎯 Mouse position: X={mouseX:F1}, Y={mouseY:F1}", string.Empty);
                
                if (_tabHeaderPanel.Children.Count == 0)
                {
                    return 0;
                }
                
                // 📊 Group _tabs by row (based on Y position with 5px tolerance)
                var tabPositions = new List<(int index, Button button, Point position, double rowY)>();
                
                for (int i = 0; i < _tabHeaderPanel.Children.Count; i++)
                {
                    if (_tabHeaderPanel.Children[i] is Button tabButton)
                    {
                        var tabPosition = tabButton.TransformToAncestor(_tabHeaderPanel).Transform(new Point(0, 0));
                        double rowY = Math.Round(tabPosition.Y / AppConstants.TabRowGroupingTolerance) * AppConstants.TabRowGroupingTolerance;
                        tabPositions.Add((i, tabButton, tabPosition, rowY));
                        OnLogDebug?.Invoke($"🎯 Tab {i}: X={tabPosition.X:F1}, Y={tabPosition.Y:F1}, RowY={rowY:F1}", string.Empty);
                    }
                }
                
                // 🔍 Find which row the mouse is over
                var rows = tabPositions.GroupBy(t => t.rowY).OrderBy(g => g.Key).ToList();
                OnLogDebug?.Invoke($"🎯 Found {rows.Count} rows", string.Empty);
                
                int targetRowIndex = 0;
                double minYDiff = double.MaxValue;
                
                for (int r = 0; r < rows.Count; r++)
                {
                    double rowY = rows[r].Key;
                    double yDiff = Math.Abs(mouseY - rowY);
                    
                    if (yDiff < minYDiff)
                    {
                        minYDiff = yDiff;
                        targetRowIndex = r;
                    }
                }
                
                var targetRow = rows[targetRowIndex].OrderBy(t => t.position.X).ToList();
                OnLogDebug?.Invoke($"🎯 Target row {targetRowIndex} has {targetRow.Count} _tabs", string.Empty);
                
                // 🎯 Find position within the target row
                double hysteresisBuffer = AppConstants.TabDragHysteresisBuffer;
                
                for (int i = 0; i < targetRow.Count; i++)
                {
                    var tab = targetRow[i];
                    double tabMidX = tab.position.X + tab.button.ActualWidth / 2;
                    
                    bool shouldInsertHere = false;
                    
                    if (_lastDropTargetIndex == tab.index && mouseX < tabMidX + hysteresisBuffer)
                    {
                        shouldInsertHere = true;
                    }
                    else if (_lastDropTargetIndex != tab.index && mouseX < tabMidX - hysteresisBuffer)
                    {
                        shouldInsertHere = true;
                    }
                    
                    if (shouldInsertHere)
                    {
                        OnLogDebug?.Invoke($"🎯 Inserting before tab {tab.index} in row {targetRowIndex}", string.Empty);
                        _lastDropTargetIndex = tab.index;
                        return tab.index;
                    }
                }
                
                // 📍 Drop at end of target row (or at very end if last row)
                var lastTabInRow = targetRow[targetRow.Count - 1];
                int insertIndex = lastTabInRow.index + 1;
                OnLogDebug?.Invoke($"🎯 Inserting after last tab in row {targetRowIndex}: index {insertIndex}", string.Empty);
                _lastDropTargetIndex = insertIndex;
                return insertIndex;
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("Error finding drop target index", ex);
                return _draggedTabOriginalIndex; // Default to original position
            }
        }

        // ✅ Complete drag operation and reorder _tabs
        private void CompleteDragOperation(bool performReorder = true)
        {
            try
            {
                OnLogDebug?.Invoke("🎯 Completing drag operation", string.Empty);
                
                if (performReorder && _dropTargetIndex >= 0 && 
                    _dropTargetIndex != _draggedTabOriginalIndex)
                {
                    // 🔄 Perform the actual reordering
                    ReorderTab(_draggedTabOriginalIndex, _dropTargetIndex);
                    OnLogDebug?.Invoke($"✅ Tab reordered from {_draggedTabOriginalIndex} to {_dropTargetIndex}", string.Empty);
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
                if (_draggedTab != null)
                {
                    _draggedTab.ReleaseMouseCapture();
                }
                
                // 🎨 Remove drag visual
                if (_dragVisual != null && _dragCanvas != null)
                {
                    _dragCanvas.Children.Remove(_dragVisual);
                    _dragVisual = null;
                }
                
                // 📍 Hide drop indicator
                if (_dropIndicator != null)
                {
                    _dropIndicator.Visibility = Visibility.Hidden;
                }
                
                // 🔄 Reset drag state
                _isDragging = false;
                _draggedTab = null;
                _draggedTabOriginalIndex = -1;
                _dropTargetIndex = -1;
                _lastDropTargetIndex = -1;
                
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
                if (fromIndex < 0 || fromIndex >= _tabs.Count || 
                    toIndex < 0 || toIndex > _tabs.Count) return;
                
                // 📝 Store the tab being moved
                var movingTab = _tabs[fromIndex];
                
                // 🗂️ Remove from collections
                _tabs.RemoveAt(fromIndex);
                _tabHeaderPanel.Children.RemoveAt(fromIndex);
                
                // 📌 Insert at new position
                var insertIndex = toIndex > fromIndex ? toIndex - 1 : toIndex;
                insertIndex = Math.Max(0, Math.Min(insertIndex, _tabs.Count));
                
                _tabs.Insert(insertIndex, movingTab);
                _tabHeaderPanel.Children.Insert(insertIndex, movingTab.HeaderButton);
                
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
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i].HeaderButton == tabButton)
                    return i;
            }
            return -1;
        }

        // 📝 Helper method to get tab title from button
        private string GetTabTitle(Button tabButton)
        {
            var tab = _tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
            return tab?.Title ?? "Tab";
        }
        #endregion

        #region Public Methods
        // 📝 Creates a new tab
        public void CreateNewTab()
        {
            if (_appSettings != null && _tabs.Count >= _appSettings.MaxTabs)
            {
                CustomDialog.ShowInformation(
                    Application.Current.MainWindow,
                    $"Maximum of {_appSettings.MaxTabs} _tabs reached.\n\nClose a tab before creating a new one.",
                    "Tab Limit Reached",
                    "📋");
                return;
            }

            try
            {
                OnLogDebug?.Invoke("🆕 Starting CreateNewTab", string.Empty);
                
                int tabCount = _tabs.Count + 1;
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

                // 📐 Wire up splitter ratio persistence (per-tab)
                noteTab.OnSplitterRatioChanged += (ratio) =>
                {
                    try
                    {
                        // Mark tab as changed to trigger auto-save (per-tab ratio)
                        OnDataChanged?.Invoke(true);
                        OnLogDebug?.Invoke($"📐 Tab splitter ratio changed: {ratio:F2}", string.Empty);
                    }
                    catch (Exception ex)
                    {
                        OnLogError?.Invoke("Error saving tab splitter ratio", ex);
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

                // 📐 Apply default splitter ratio after layout is ready (new _tabs use default)
                noteTab.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        double defaultRatio = _appSettings?.SplitterTextMediaRatio ?? AppConstants.SplitterDefaultRatio;
                        noteTab.ApplySplitterRatio(defaultRatio);
                        OnLogDebug?.Invoke($"📐 Applied default splitter ratio to new tab: {defaultRatio:F2}", string.Empty);
                    }
                    catch (Exception ex)
                    {
                        OnLogError?.Invoke("Error applying splitter ratio to new tab", ex);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                // 🎯 Auto-focus the text editor so user can start typing immediately
                noteTab.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        noteTab.FocusEditor();
                    }
                    catch (Exception ex)
                    {
                        OnLogError?.Invoke("Error focusing editor on new tab", ex);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                // 📍 Add to collections
                _tabs.Add(customTab);
                _tabHeaderPanel.Children.Add(customTab.HeaderButton);

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
        /// ✅ Phase 4D P2.8: Added null guard
        public void SelectTab(CustomTab? tab)
        {
            // ✅ Null guard
            if (tab == null)
            {
                OnLogError?.Invoke("SelectTab called with null tab", new ArgumentNullException(nameof(tab)));
                return;
            }
            
            // 🧠 Skip if already selected (avoid redundant updates)
            if (_selectedTab == tab) return;
            
            var previousTab = _selectedTab;
            _selectedTab = tab;

            // 🎨 Only update the _tabs that changed
            if (previousTab != null)
            {
                UpdateTabSelection(previousTab, false); // Deselect old tab
            }
            UpdateTabSelection(tab, true); // Select new tab

            // 🎬 Update content (without animation)
            _tabContentArea.Content = tab.Content;
            
            // 📊 Update status bar
            OnStatusUpdateRequested?.Invoke();
        }

        // ❌ Delete the currently selected tab
        public void DeleteCurrentTab()
        {
            if (_selectedTab == null) return;

            // 🔔 Check settings first - if ConfirmTabDeletion is false, delete immediately
            if (_appSettings != null && !_appSettings.ConfirmTabDeletion)
            {
                DeleteTab(_selectedTab);
                return;
            }

            // 🔔 Check if user previously chose "don't ask again"
            if (_skipDeleteConfirmation)
            {
                DeleteTab(_selectedTab);
                return;
            }

            // 🎭 Show custom confirmation dialog
            var (confirmed, dontAskAgain) = CustomDialog.ShowDeleteConfirmation(
                Application.Current.MainWindow,
                _selectedTab.Title,
                showDontAskAgain: true);

            if (!confirmed) return;

            // 💭 Remember user preference if they checked "don't ask again"
            if (dontAskAgain)
            {
                _skipDeleteConfirmation = true;
                // 📝 Also update the settings to reflect this choice
                if (_appSettings != null)
                {
                    _appSettings.ConfirmTabDeletion = false;
                    OnSettingsNeedUpdate?.Invoke(); // Notify that settings need to be saved
                }
                OnLogDebug?.Invoke("🔕 Delete confirmation disabled by user preference and settings updated", string.Empty);
            }

            DeleteTab(_selectedTab);
        }

        // 🗑️ Delete a specific tab
        public void DeleteSpecificTab(Button tabButton)
        {
            var tabToDelete = _tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
            if (tabToDelete == null) return;

            // 🔔 Check settings first - if ConfirmTabDeletion is false, delete immediately
            if (_appSettings != null && !_appSettings.ConfirmTabDeletion)
            {
                DeleteTab(tabToDelete);
                return;
            }

            // 🔔 Check if user previously chose "don't ask again"
            if (_skipDeleteConfirmation)
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
                _skipDeleteConfirmation = true;
                // 📝 Also update the settings to reflect this choice
                if (_appSettings != null)
                {
                    _appSettings.ConfirmTabDeletion = false;
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

            // 🎨 Create seamless textbox with accent caret for visibility
            try
            {
                editBox.Background = Brushes.Transparent;
                editBox.SetResourceReference(TextBox.ForegroundProperty, ResourceKeys.AppForegroundBrush);
                editBox.BorderThickness = new Thickness(0);
                editBox.BorderBrush = Brushes.Transparent;
                editBox.CaretBrush = (Brush)Application.Current.FindResource(ResourceKeys.AccentBrush);
                editBox.Padding = new Thickness(0);
                editBox.Margin = textBlock.Margin;
            }
            catch
            {
                editBox.Background = Brushes.Transparent;
                editBox.Foreground = textBlock.Foreground ?? new SolidColorBrush(Color.FromRgb(255, 255, 255));
                editBox.BorderThickness = new Thickness(0);
                editBox.BorderBrush = Brushes.Transparent;
                editBox.Padding = new Thickness(0);
            }

            // Find the button that contains this TextBlock
            var tabButton = _tabs.FirstOrDefault(t => t.HeaderButton.Content == textBlock)?.HeaderButton;
            if (tabButton == null) return;

            // 🎯 Store original tab appearance for restoration
            var originalBackground = tabButton.Background;
            var originalEffect = tabButton.Effect;

            // 🌟 Apply "EDITING MODE" indicators using template elements + design tokens
            // TabBorder gets accent-tinted bg + accent border; ActiveUnderline shows; glow effect
            Border? tabBorder = tabButton.Template?.FindName("TabBorder", tabButton) as Border;
            Border? activeUnderline = tabButton.Template?.FindName("ActiveUnderline", tabButton) as Border;
            var originalTabBorderBg = tabBorder?.Background;
            var originalUnderlineOpacity = activeUnderline?.Opacity;

            try
            {
                var accentBrush = (Brush)Application.Current.FindResource(ResourceKeys.AccentBrush);
                var glowEffect = Application.Current.FindResource(ResourceKeys.AccentGlowEffect) as System.Windows.Media.Effects.Effect;

                // Accent-tinted background on tab surface
                tabBorder!.Background = new SolidColorBrush(Color.FromArgb(30, 99, 102, 241)); // 12% indigo
                tabBorder.BorderBrush = accentBrush;
                tabBorder.BorderThickness = new Thickness(1.5);

                // Show accent gradient underline
                activeUnderline!.Opacity = 1;

                // Apply glow effect
                if (glowEffect != null)
                    tabButton.Effect = glowEffect;
            }
            catch
            {
                // Fallback — accent border on button level
                tabBorder!.Background = new SolidColorBrush(Color.FromArgb(40, 99, 102, 241));
                tabBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                tabBorder.BorderThickness = new Thickness(1.5);
                activeUnderline!.Opacity = 1;
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
                var tab = _tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
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
                // 🔄 Restore tab surface + underline + effect to pre-edit state
                try
                {
                    tabBorder!.Background = originalTabBorderBg;
                    tabBorder.BorderThickness = new Thickness(0);
                    activeUnderline!.Opacity = originalUnderlineOpacity ?? 0;
                    tabButton.Effect = originalEffect;

                    // Force refresh of tab selection state
                    var currentTab = _tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
                    if (currentTab != null)
                    {
                        UpdateTabSelection(currentTab, currentTab == _selectedTab);
                    }
                }
                catch
                {
                    tabBorder!.Background = Brushes.Transparent;
                    tabBorder.BorderThickness = new Thickness(0);
                    activeUnderline!.Opacity = 0;
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
            var sourceTab = _tabs.FirstOrDefault(t => t.HeaderButton == sourceTabButton);
            if (sourceTab == null) return;

            // 🧠 Create new tab with copied content
            var newNoteTab = new NoteTab();
            var duplicateTitle = $"{sourceTab.Title} (Copy)";

            // 🔔 Wire up change tracking for auto-save
            newNoteTab.OnDataChanged += () => OnDataChanged?.Invoke(true);

            // 📐 Wire up splitter ratio persistence for duplicated _tabs (per-tab)
            newNoteTab.OnSplitterRatioChanged += (ratio) =>
            {
                try
                {
                    // Mark tab as changed to trigger auto-save (per-tab ratio)
                    OnDataChanged?.Invoke(true);
                    OnLogDebug?.Invoke($"📐 Tab splitter ratio changed: {ratio:F2}", string.Empty);
                }
                catch (Exception ex)
                {
                    OnLogError?.Invoke("Error saving tab splitter ratio", ex);
                }
            };

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
            _tabs.Add(duplicateTab);
            _tabHeaderPanel.Children.Add(duplicateTab.HeaderButton);
            SelectTab(duplicateTab);

            // 📐 Copy source tab's splitter ratio to duplicated tab
            newNoteTab.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    double sourceRatio = sourceTab.Content.GetStoredSplitterRatio();
                    newNoteTab.ApplySplitterRatio(sourceRatio);
                    OnLogDebug?.Invoke($"📐 Copied splitter ratio to duplicated tab: {sourceRatio:F2}", string.Empty);
                }
                catch (Exception ex)
                {
                    OnLogError?.Invoke("Error applying splitter ratio to duplicated tab", ex);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            OnDataChanged?.Invoke(true); // 💾 Mark as changed for auto-save
        }

        // 🎯 Start renaming the currently selected tab
        public void StartRenameCurrentTab()
        {
            if (_selectedTab != null)
            {
                // Find the TextBlock within the tab button
                if (_selectedTab.HeaderButton.Content is TextBlock textBlock)
                {
                    RenameTab(textBlock);
                }
            }
        }

        // 🔄 Switch to the next tab
        public void SwitchToNextTab()
        {
            if (_tabs.Count <= 1) return; // No point switching if only one tab

            var currentIndex = _selectedTab != null ? _tabs.IndexOf(_selectedTab) : -1;
            var nextIndex = (currentIndex + 1) % _tabs.Count; // Wrap around to first tab
            
            if (nextIndex >= 0 && nextIndex < _tabs.Count)
            {
                SelectTab(_tabs[nextIndex]);
            }
        }

        // 🎯 Navigate _tabs using arrow keys (row-aware for multi-row layout)
        public void NavigateTab(string direction)
        {
            try
            {
                if (_tabs.Count == 0) return;

                var currentIndex = _selectedTab != null ? _tabs.IndexOf(_selectedTab) : -1;
                if (currentIndex < 0) return;

                OnLogDebug?.Invoke($"🎯 NavigateTab: direction={direction}, currentIndex={currentIndex}", string.Empty);

                // Handle simple Home/End cases
                if (direction == "Home")
                {
                    SelectTab(_tabs[0]);
                    OnLogDebug?.Invoke("🎯 Navigated to first tab (Home)", string.Empty);
                    return;
                }
                else if (direction == "End")
                {
                    SelectTab(_tabs[_tabs.Count - 1]);
                    OnLogDebug?.Invoke("🎯 Navigated to last tab (End)", string.Empty);
                    return;
                }

                // 📊 Build row/column layout for arrow key navigation
                var tabPositions = new List<(int index, Button button, Point position, double rowY)>();
                
                for (int i = 0; i < _tabs.Count; i++)
                {
                    if (_tabs[i].HeaderButton != null && _tabHeaderPanel != null)
                    {
                        var tabPosition = _tabs[i].HeaderButton.TransformToAncestor(_tabHeaderPanel).Transform(new Point(0, 0));
                        double rowY = Math.Round(tabPosition.Y / AppConstants.TabRowGroupingTolerance) * AppConstants.TabRowGroupingTolerance;
                        tabPositions.Add((i, _tabs[i].HeaderButton, tabPosition, rowY));
                    }
                }

                // Group into rows
                var rows = tabPositions.GroupBy(t => t.rowY).OrderBy(g => g.Key).Select(g => g.OrderBy(t => t.position.X).ToList()).ToList();
                OnLogDebug?.Invoke($"🎯 Found {rows.Count} rows", string.Empty);

                // Find current row and position within row
                int currentRow = -1;
                int currentCol = -1;

                for (int r = 0; r < rows.Count; r++)
                {
                    for (int c = 0; c < rows[r].Count; c++)
                    {
                        if (rows[r][c].index == currentIndex)
                        {
                            currentRow = r;
                            currentCol = c;
                            break;
                        }
                    }
                    if (currentRow >= 0) break;
                }

                if (currentRow < 0)
                {
                    OnLogDebug?.Invoke("🚫 Current tab not found in layout", string.Empty);
                    return;
                }

                OnLogDebug?.Invoke($"🎯 Current position: row {currentRow}, col {currentCol}", string.Empty);

                int targetIndex = currentIndex;

                switch (direction)
                {
                    case "Left":
                        if (currentCol > 0)
                        {
                            // Move left within same row
                            targetIndex = rows[currentRow][currentCol - 1].index;
                        }
                        else if (currentRow > 0)
                        {
                            // Wrap to end of previous row
                            targetIndex = rows[currentRow - 1][rows[currentRow - 1].Count - 1].index;
                        }
                        else
                        {
                            // Wrap to last tab in last row
                            targetIndex = rows[rows.Count - 1][rows[rows.Count - 1].Count - 1].index;
                        }
                        break;

                    case "Right":
                        if (currentCol < rows[currentRow].Count - 1)
                        {
                            // Move right within same row
                            targetIndex = rows[currentRow][currentCol + 1].index;
                        }
                        else if (currentRow < rows.Count - 1)
                        {
                            // Wrap to start of next row
                            targetIndex = rows[currentRow + 1][0].index;
                        }
                        else
                        {
                            // Wrap to first tab in first row
                            targetIndex = rows[0][0].index;
                        }
                        break;

                    case "Up":
                        if (currentRow > 0)
                        {
                            // Move to tab in row above (closest X position)
                            var currentX = rows[currentRow][currentCol].position.X;
                            var targetRow = rows[currentRow - 1];
                            
                            // Find closest tab by X position
                            int closestCol = 0;
                            double minDist = double.MaxValue;
                            for (int c = 0; c < targetRow.Count; c++)
                            {
                                double dist = Math.Abs(targetRow[c].position.X - currentX);
                                if (dist < minDist)
                                {
                                    minDist = dist;
                                    closestCol = c;
                                }
                            }
                            targetIndex = targetRow[closestCol].index;
                        }
                        // else stay on current tab
                        break;

                    case "Down":
                        if (currentRow < rows.Count - 1)
                        {
                            // Move to tab in row below (closest X position)
                            var currentX = rows[currentRow][currentCol].position.X;
                            var targetRow = rows[currentRow + 1];
                            
                            // Find closest tab by X position
                            int closestCol = 0;
                            double minDist = double.MaxValue;
                            for (int c = 0; c < targetRow.Count; c++)
                            {
                                double dist = Math.Abs(targetRow[c].position.X - currentX);
                                if (dist < minDist)
                                {
                                    minDist = dist;
                                    closestCol = c;
                                }
                            }
                            targetIndex = targetRow[closestCol].index;
                        }
                        // else stay on current tab
                        break;
                }

                if (targetIndex != currentIndex && targetIndex >= 0 && targetIndex < _tabs.Count)
                {
                    SelectTab(_tabs[targetIndex]);
                    OnLogDebug?.Invoke($"🎯 Navigated to tab {targetIndex} ({direction})", string.Empty);
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke($"Error navigating tab ({direction})", ex);
            }
        }

        // 📖 Load _tabs from saved data
        public void LoadTabs(List<SavedNote> savedNotes)
        {
            try
            {
                // Clear existing _tabs
                _tabs.Clear();
                _tabHeaderPanel.Children.Clear();

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
                        
                        noteTab.MediaReferences = savedNote.Media ?? new List<MediaReference>();

                        // 🔔 Wire up change tracking
                        noteTab.OnDataChanged += () => 
                        {
                            OnDataChanged?.Invoke(true);
                            OnStatusUpdateRequested?.Invoke();
                        };

                        // 📐 Wire up splitter ratio persistence for loaded _tabs (per-tab)
                        noteTab.OnSplitterRatioChanged += (ratio) =>
                        {
                            try
                            {
                                // Mark tab as changed to trigger auto-save (per-tab ratio)
                                OnDataChanged?.Invoke(true);
                                OnLogDebug?.Invoke($"📐 Tab splitter ratio changed: {ratio:F2}", string.Empty);
                            }
                            catch (Exception ex)
                            {
                                OnLogError?.Invoke("Error saving tab splitter ratio", ex);
                            }
                        };

                        var customTab = new CustomTab
                        {
                            Title = savedNote.Title,
                            Content = noteTab,
                            HeaderButton = CreateTabHeaderButton(savedNote.Title)
                        };

                        // 🔗 Click event is already wired up in CreateTabHeaderButton

                        _tabs.Add(customTab);
                        _tabHeaderPanel.Children.Add(customTab.HeaderButton);

                        // 📐 Apply this tab's saved splitter ratio after layout is ready
                        noteTab.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                double tabRatio = savedNote.SplitterTextMediaRatio;
                                noteTab.ApplySplitterRatio(tabRatio);
                                OnLogDebug?.Invoke($"📐 Applied saved splitter ratio to loaded tab '{savedNote.Title}': {tabRatio:F2}", string.Empty);
                            }
                            catch (Exception ex)
                            {
                                OnLogError?.Invoke("Error applying splitter ratio to loaded tab", ex);
                            }
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }

                    // Select the first tab
                    if (_tabs.Any())
                    {
                        SelectTab(_tabs.First());
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
                OnLogError?.Invoke("Error loading _tabs", ex);
                // Do NOT create a blank tab here — that would cause autosave to overwrite real notes.
                // Rethrow so the caller can decide how to handle the failure safely.
                throw;
            }
        }

        // 💾 Get data for saving
        // 🐛 BUG-LABELSIZE FIX (2026-06-03): explicitly carry DataVersion + TabOrder
        // through GetSaveData. Previously these fell back to defaults (1 and 0) on
        // every save, so the schema migration ran on every load forever.
        public List<SavedNote> GetSaveData()
        {
            return _tabs.Select((tab, index) => new SavedNote
            {
                DataVersion = SnipShottyBoard.Core.Schema.MigrationService.CurrentNoteSchemaVersion,
                Title = tab.Title,
                TextContent = tab.Content.TextContent,
                RichTextContent = tab.Content.RichTextContent,
                Media = tab.Content.MediaReferences,
                SplitterTextMediaRatio = tab.Content.GetStoredSplitterRatio(), // Per-tab splitter position
                TabOrder = index
            }).ToList();
        }

        // 🎨 Refresh all tab visuals (useful after theme changes)
        public void RefreshTabVisuals()
        {
            foreach (var tab in _tabs)
            {
                UpdateTabSelection(tab, tab == _selectedTab);
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
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, ResourceKeys.AppForegroundBrush);

            var tabButton = new Button
            {
                Content = textBlock,
                Style = (Style)Application.Current.FindResource(ResourceKeys.TabButtonStyle),
                Height = 32,
                MinWidth = AppConstants.TabMinWidth,
                MaxWidth = AppConstants.TabMaxWidth,
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
                var tab = _tabs.FirstOrDefault(t => t.HeaderButton == tabButton);
                if (tab != null) SelectTab(tab);
            };

            // 📋 Right-click context menu
            var contextMenu = new ContextMenu();

            // Apply native themed context menu style (from F.0 DarkTheme resources)
            var menuStyle = Application.Current.FindResource(ResourceKeys.NativeContextMenuStyle) as System.Windows.Style;
            if (menuStyle != null)
            {
                contextMenu.Style = menuStyle;
            }

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
                _dragStartPoint = e.GetPosition(tabButton);
                OnLogDebug?.Invoke("🖱️ Mouse down on tab", string.Empty);
            };

            // 🎯 Mouse move - check if we should start dragging
            tabButton.PreviewMouseMove += (sender, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
                {
                    var currentPosition = e.GetPosition(tabButton);
                    var diff = _dragStartPoint - currentPosition;
                    
                    // 📏 Check if mouse moved far enough to start drag (prevents accidental drags)
                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        StartDragOperation(tabButton, _dragStartPoint);
                    }
                }
                else if (_isDragging && _draggedTab == tabButton)
                {
                    // 🔄 Update drag visual position
                    var windowPosition = e.GetPosition(Application.Current.MainWindow);
                    UpdateDragVisual(windowPosition);
                    
                    // 🎯 Find drop target and update indicator
                    _dropTargetIndex = FindDropTargetIndex(e.GetPosition(_tabHeaderPanel));
                    UpdateDropIndicator(_dropTargetIndex);
                }
            };

            // 🖱️ Mouse up - complete drag operation
            tabButton.PreviewMouseLeftButtonUp += (sender, e) =>
            {
                if (_isDragging && _draggedTab == tabButton)
                {
                    CompleteDragOperation(true);
                }
            };

            // 🚫 Mouse leave - cancel drag if mouse leaves the window
            tabButton.MouseLeave += (sender, e) =>
            {
                if (_isDragging && _draggedTab == tabButton)
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
            _tabs.Remove(tabToRemove);
            _tabHeaderPanel.Children.Remove(tabToRemove.HeaderButton);

            // 🎯 Handle selection after removal
            if (tabToRemove == _selectedTab)
            {
                if (_tabs.Count > 0)
                {
                    // Select the last tab
                    SelectTab(_tabs.Last());
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
                    ? (Brush)Application.Current.FindResource(ResourceKeys.TabBackgroundBrush) 
                    : Brushes.Transparent;
                
                tab.HeaderButton.Background = targetBrush;
                
                // Ensure text color is properly set for contrast
                if (tab.HeaderButton.Content is TextBlock textBlock)
                {
                    try
                    {
                        textBlock.SetResourceReference(TextBlock.ForegroundProperty, ResourceKeys.AppForegroundBrush);
                    }
                    catch
                    {
                        // Fallback text color
                        textBlock.Foreground = (Brush)Application.Current.FindResource(ResourceKeys.AppForegroundBrush) ?? Brushes.White;
                    }
                }
                
                OnLogDebug?.Invoke($"🎨 Tab selection updated: {tab.Title}, Selected: {isSelected}, Tag: {tab.HeaderButton.Tag}", string.Empty);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke($"Failed to update tab selection for {tab.Title}", ex);
                
                if (isSelected)
                    tab.HeaderButton.Background = (Brush)Application.Current.FindResource(ResourceKeys.TabBackgroundBrush);
                else
                    tab.HeaderButton.Background = Brushes.Transparent;
            }
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// ✅ Phase 4D P2.1: Dispose pattern for proper cleanup
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                OnLogDebug?.Invoke("🗑️ TabManager disposing...", string.Empty);

                // Clear event subscribers
                OnDataChanged = null;
                OnStatusUpdateRequested = null;
                OnLogDebug = null;
                OnLogError = null;
                OnSettingsNeedUpdate = null;

                // Clear _tabs (Note: Individual tab disposal would require IDisposable on NoteTab - future enhancement)
                _tabs.Clear();
                _tabHeaderPanel?.Children.Clear();

                // Clear drag state
                _dragCanvas = null;
                _dragVisual = null;
                _dropIndicator = null;
                _draggedTab = null;

                _disposed = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in TabManager.Dispose: {ex.Message}");
            }
        }
        #endregion
    }
} 