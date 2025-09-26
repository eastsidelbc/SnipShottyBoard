using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using SnipShottyBoard.Data;
using SnipShottyBoard.UI;

namespace SnipShottyBoard
{
    public partial class MainWindow : Window
    {
        // 🏷️ Manager instances
        private TabManager tabManager;
        private ThemeManager themeManager;
        private StatusBarManager statusBarManager;
        private KeyboardHandler keyboardHandler;
        private LoggingService loggingService;
        private HelpManager helpManager;
        private SettingsManager settingsManager;
        
        // ⚙️ Current Settings
        private AppSettings currentSettings;
        
        // ⏰ Timers
        private DispatcherTimer autoSaveTimer;
        private DispatcherTimer statusTimer;
        private bool hasUnsavedChanges = false;

        // 🪟 Window data for multi-window support
        public NoteWindowData WindowData { get; private set; }

        /// <summary>
        /// 🔄 Ensures main window has note window data (migration from old format if needed)
        /// </summary>
        private NoteWindowData EnsureMainWindowHasData()
        {
            var noteManager = NoteWindowManager.Instance;
            
            loggingService?.LogDebug($"🔍 EnsureMainWindowHasData: Starting data check");
            
            // 🔍 Check if we already have note windows
            var existingWindows = noteManager.GetActiveWindows();
            loggingService?.LogDebug($"🔍 EnsureMainWindowHasData: Found {existingWindows.Count} existing windows");
            
            // 🔄 Always check for legacy data to compare content
            loggingService?.LogDebug($"🔍 EnsureMainWindowHasData: Checking for legacy data");
            try
            {
                var legacyData = new DataManager().LoadAppData();
                
                // Count meaningful content in legacy data
                var legacyContentNotes = legacyData?.Notes?.Where(n => 
                    !string.IsNullOrWhiteSpace(n.TextContent) || 
                    (n.ImageFiles != null && n.ImageFiles.Any())).ToList() ?? new List<SavedNote>();
                
                // Count meaningful content in current windows
                var currentContentNotes = existingWindows
                    .SelectMany(w => w.Notes ?? new List<SavedNote>())
                    .Where(n => !string.IsNullOrWhiteSpace(n.TextContent) || 
                               (n.ImageFiles != null && n.ImageFiles.Any()))
                    .ToList();
                
                loggingService?.LogDebug($"🔍 Legacy content: {legacyContentNotes.Count} notes, Current content: {currentContentNotes.Count} notes");
                
                // If legacy has significantly more content, perform migration
                if (legacyContentNotes.Count > currentContentNotes.Count + 1) // Allow for some recent changes
                {
                    loggingService?.LogDebug($"🔄 Migrating legacy data ({legacyContentNotes.Count} vs {currentContentNotes.Count} notes)");
                    
                    // Backup current content that's not empty
                    var recentContent = currentContentNotes.ToList();
                    
                    // Clear existing windows
                    noteManager.NoteWindows.Clear();
                    
                    // Create main window with legacy data
                    var mainWindow = new NoteWindowData
                    {
                        Id = Guid.NewGuid(),
                        Title = "My Notes",
                        CreatedAt = DateTime.Now,
                        LastModified = DateTime.Now,
                        IsActive = true,
                        Notes = legacyData.Notes.ToList(), // Start with all legacy notes
                        WindowLeft = legacyData.Settings?.WindowLeft ?? 100,
                        WindowTop = legacyData.Settings?.WindowTop ?? 100,
                        WindowWidth = legacyData.Settings?.WindowWidth ?? 800,
                        WindowHeight = legacyData.Settings?.WindowHeight ?? 600
                    };
                    
                    // If there was recent meaningful content that's not in legacy, add it
                    foreach (var recentNote in recentContent)
                    {
                        var existingNote = mainWindow.Notes.FirstOrDefault(n => n.Title == recentNote.Title);
                        if (existingNote != null)
                        {
                            // Update with recent content if it's more recent
                            if (!string.IsNullOrWhiteSpace(recentNote.TextContent) && 
                                recentNote.TextContent != existingNote.TextContent)
                            {
                                existingNote.TextContent = recentNote.TextContent;
                                loggingService?.LogDebug($"🔄 Updated note '{recentNote.Title}' with recent content");
                            }
                        }
                        else
                        {
                            // Add completely new recent note
                            mainWindow.Notes.Add(recentNote);
                            loggingService?.LogDebug($"🔄 Added recent note '{recentNote.Title}'");
                        }
                    }
                    
                    noteManager.NoteWindows.Add(mainWindow);
                    noteManager.SaveNoteWindows();
                    
                    loggingService?.LogDebug($"✅ Migration completed with {mainWindow.Notes.Count} total notes");
                    return mainWindow;
                }
                else if (existingWindows.Any())
                {
                    // Current windows have sufficient content, use them
                    loggingService?.LogDebug($"✅ Using existing windows");
                    return existingWindows.First();
                }
                else if (legacyContentNotes.Any())
                {
                    // No current windows but legacy has content
                    loggingService?.LogDebug($"🔄 No current windows, migrating legacy content");
                    
                    var mainWindow = new NoteWindowData
                    {
                        Id = Guid.NewGuid(),
                        Title = "My Notes",
                        CreatedAt = DateTime.Now,
                        LastModified = DateTime.Now,
                        IsActive = true,
                        Notes = legacyData.Notes,
                        WindowLeft = legacyData.Settings?.WindowLeft ?? 100,
                        WindowTop = legacyData.Settings?.WindowTop ?? 100,
                        WindowWidth = legacyData.Settings?.WindowWidth ?? 800,
                        WindowHeight = legacyData.Settings?.WindowHeight ?? 600
                    };
                    
                    noteManager.NoteWindows.Add(mainWindow);
                    noteManager.SaveNoteWindows();
                    
                    loggingService?.LogDebug("✅ Migrated legacy data to note window format");
                    return mainWindow;
                }
            }
            catch (Exception ex)
            {
                loggingService?.LogDebug($"⚠️ Could not load legacy data for migration: {ex.Message}");
            }
            
            // 🆕 If we have existing windows (even empty), use the first one; otherwise create new
            if (existingWindows.Any())
            {
                loggingService?.LogDebug($"🔄 Using existing window: {existingWindows.First().Title}");
                return existingWindows.First();
            }
            
            // 🆕 Create new main window as last resort
            loggingService?.LogDebug($"🆕 Creating new default window");
            return noteManager.CreateNewNoteWindow("My Notes");
        }

        public MainWindow() : this(null)
        {
        }

        public MainWindow(NoteWindowData windowData)
        {
            try
            {
                // 🪟 SIMPLE APPROACH: Every window gets note window data (no special cases)
                WindowData = windowData ?? EnsureMainWindowHasData();
                
                // 🎨 Load settings FIRST to get the correct theme before initializing UI
                loggingService = new LoggingService();
                var settingsToLoad = DataManager.LoadSettings();
                currentSettings = settingsToLoad;
                
                // 🎨 Initialize theme manager with correct theme from settings
                themeManager = new ThemeManager();
                if (currentSettings != null)
                {
                    themeManager.LoadTheme(currentSettings.IsDarkMode);
                    loggingService.LogDebug($"🎨 Theme initialized: {(currentSettings.IsDarkMode ? "Dark" : "Light")}");
                }
                else
                {
                    themeManager.InitializeTheme(); // Fallback to default
                }
                
                InitializeComponent();
                InitializeManagers();
                SetupTimers();
                SetupEventHandlers();
                LoadApplicationData();
                
                // 🪟 Set window title for multiple windows
                if (WindowData != null)
                {
                    this.Title = WindowData.Title;
                    
                    // 🔍 Validate window position before setting
                    if (WindowData.WindowLeft >= 0 && WindowData.WindowLeft < SystemParameters.VirtualScreenWidth)
                        this.Left = WindowData.WindowLeft;
                    
                    if (WindowData.WindowTop >= 0 && WindowData.WindowTop < SystemParameters.VirtualScreenHeight)
                        this.Top = WindowData.WindowTop;
                    
                    if (WindowData.WindowWidth > 200 && WindowData.WindowWidth < SystemParameters.VirtualScreenWidth)
                        this.Width = WindowData.WindowWidth;
                    
                    if (WindowData.WindowHeight > 200 && WindowData.WindowHeight < SystemParameters.VirtualScreenHeight)
                        this.Height = WindowData.WindowHeight;
                        
                    loggingService.LogDebug($"🪟 Window positioned at: {this.Left},{this.Top} Size: {this.Width}x{this.Height}");
                }
                
                loggingService.LogDebug("🎉 MainWindow initialization completed successfully");
            }
            catch (Exception ex)
            {
                var tempLogger = new LoggingService();
                tempLogger.LogError("FATAL ERROR in MainWindow constructor", ex);
                MessageBox.Show($"Fatal error starting application:\n{ex.Message}\n\nCheck console for details.", 
                               "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Initialization
        // 🔧 Initialize all manager instances
        private void InitializeManagers()
        {
            loggingService.LogDebug("🚀 Starting MainWindow initialization");

            // DataManager is now static
            // themeManager already initialized in constructor
            
            tabManager = new TabManager(TabHeaderPanel, TabContentArea);
            statusBarManager = new StatusBarManager(TabCountStatus, WordCountStatus, SaveStatus, TimeStatus);
            keyboardHandler = new KeyboardHandler();
            helpManager = new HelpManager();
            settingsManager = new SettingsManager();

            loggingService.LogDebug("✅ All managers initialized");
        }

        // ⏰ Setup auto-save and status update timers
        private void SetupTimers()
        {
            // ⏰ Setup auto-save timer (save every 5 seconds if changes exist)
            autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            autoSaveTimer.Tick += (s, e) => {
                if (hasUnsavedChanges) SaveApplicationData();
            };
            autoSaveTimer.Start();

            // 📊 Setup status bar timer (update every second)
            statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            statusTimer.Tick += (s, e) => UpdateStatusBar();
            statusTimer.Start();

            loggingService.LogDebug("✅ Timers started");
        }

        // 🔗 Wire up all event handlers
        private void SetupEventHandlers()
        {
            // Window events
            this.PreviewKeyDown += (s, e) => keyboardHandler.HandleKeyDown(e);
            this.Closing += MainWindow_Closing;

            // TabManager events
            tabManager.OnDataChanged += (hasChanges) => hasUnsavedChanges = hasChanges;
            tabManager.OnStatusUpdateRequested += UpdateStatusBar;
            tabManager.OnLogDebug += (message, _) => loggingService.LogDebug(message);
            tabManager.OnLogError += loggingService.LogError;
            tabManager.OnSettingsNeedUpdate += () => {
                hasUnsavedChanges = true;
                SaveApplicationData(); // Save immediately when settings change
                loggingService.LogDebug("⚙️ Settings updated due to TabManager preference change");
            };

            // ThemeManager events
            themeManager.OnThemeChanged += () => {
                hasUnsavedChanges = true;
                UpdateStatusBar();
                this.InvalidateVisual();
                
                // 🎨 Refresh tab visuals when theme changes (only when needed)
                tabManager.RefreshTabVisuals();
            };

            // KeyboardHandler events
            keyboardHandler.OnNewTabRequested += () => tabManager.CreateNewTab();
            keyboardHandler.OnDeleteTabRequested += () => tabManager.DeleteCurrentTab();
            keyboardHandler.OnRenameTabRequested += () => tabManager.StartRenameCurrentTab();
            keyboardHandler.OnSwitchTabRequested += () => tabManager.SwitchToNextTab();
            keyboardHandler.OnImagePasted += (imgControl, imagePath) => {
                if (tabManager.SelectedTab != null)
                {
                    tabManager.SelectedTab.Content.AddImage(imgControl, imagePath);
                }
            };
            keyboardHandler.OnRichTextFormattingRequested += (formatType) => {
                if (tabManager.SelectedTab?.Content?.RichTextBox != null)
                {
                    ApplyRichTextFormatting(tabManager.SelectedTab.Content.RichTextBox, formatType);
                }
            };

            // SettingsManager events
            settingsManager.OnLogDebug += (message) => loggingService.LogDebug(message);
            settingsManager.OnLogError += loggingService.LogError;
            settingsManager.OnResetDeleteConfirmationRequested += () => {
                tabManager.ResetDeleteConfirmationPreference();
                loggingService.LogDebug("🔄 Delete confirmation preference reset via settings");
            };
            settingsManager.OnSettingsChanged += () => {
                hasUnsavedChanges = true;
                UpdateStatusBar();
                this.InvalidateVisual();
                
                // 🔄 Reload settings and update TabManager
                try
                {
                    var appData = new DataManager().LoadAppData();
                    currentSettings = appData.Settings ?? new AppSettings();
                    tabManager.UpdateSettings(currentSettings);
                    loggingService.LogDebug("⚙️ Settings reloaded and applied to TabManager");
                }
                catch (Exception ex)
                {
                    loggingService.LogError("Error reloading settings after change", ex);
                }
            };

            loggingService.LogDebug("✅ Event handlers wired");
        }
        #endregion

        #region Rich Text Formatting
        private void ApplyRichTextFormatting(RichTextBox richTextBox, string formatType)
        {
            try
            {
                // Get the TextSection from the RichTextBox's parent
                var textSection = FindVisualParent<TextSection>(richTextBox);
                if (textSection == null) return;

                switch (formatType.ToLower())
                {
                    case "bold":
                        textSection.ApplyBold();
                        break;
                    case "italic":
                        textSection.ApplyItalic();
                        break;
                    case "underline":
                        textSection.ApplyUnderline();
                        break;
                    case "strikethrough":
                        textSection.ApplyStrikethrough();
                        break;
                    case "bullet":
                        textSection.ApplyBulletList();
                        break;
                    case "numbered":
                        textSection.ApplyNumberedList();
                        break;
                    case "indent":
                        textSection.IndentText();
                        break;
                    case "unindent":
                        textSection.UnindentText();
                        break;
                }

                // Trigger data change for auto-save
                hasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                loggingService?.LogError($"Error applying rich text formatting: {formatType}", ex);
            }
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }
        #endregion

        #region Button Event Handlers
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
        private void NewTab_Click(object sender, RoutedEventArgs e) => tabManager.CreateNewTab();
        private void DeleteTab_Click(object sender, RoutedEventArgs e) => tabManager.DeleteCurrentTab();
        private void ToggleTheme_Click(object sender, RoutedEventArgs e) => themeManager.ToggleTheme();

        private void NoteWindows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔍 Check if NoteListWindow is already open
                var existingWindow = Application.Current.Windows
                    .OfType<NoteListWindow>()
                    .FirstOrDefault();

                if (existingWindow != null)
                {
                    // 🎯 Bring existing window to front
                    existingWindow.WindowState = WindowState.Normal;
                    existingWindow.Activate();
                    existingWindow.Focus();
                }
                else
                {
                    // 🆕 Create new NoteListWindow
                    var noteListWindow = new NoteListWindow();
                    noteListWindow.Show();
                }
            }
            catch (Exception ex)
            {
                loggingService.LogError("Error opening note list window", ex);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            // 🔄 Get current settings from loaded data or create defaults
            var settingsToShow = currentSettings ?? new AppSettings
            {
                Theme = themeManager.IsDarkMode ? "Dark" : "Light",
                AutoSaveEnabled = true, // Get from current auto-save timer state
                AutoSaveIntervalSeconds = 5, // Current interval
                AlwaysOnTop = this.Topmost,
                WindowLeft = this.Left,
                WindowTop = this.Top,
                WindowWidth = this.Width,
                WindowHeight = this.Height,
                MaxTabs = 20, // Default
                ConfirmTabDeletion = true // Default
            };

            // 🔍 Update the ConfirmTabDeletion to reflect the current effective state
            // This ensures the checkbox shows the true state (unchecked if "don't ask again" was chosen)
            settingsToShow.ConfirmTabDeletion = !tabManager.IsDeleteConfirmationDisabled;
            
            settingsManager.ShowSettingsWindow(settingsToShow);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            // 📋 Show help context menu with useful options
            var contextMenu = new ContextMenu();

            // Help option
            var helpItem = new MenuItem { Header = "📖 Help & Shortcuts" };
            helpItem.Click += (s, args) => helpManager.ShowHelpWindow();

            // Quick Tips option
            var tipsItem = new MenuItem { Header = "💡 Quick Tips" };
            tipsItem.Click += (s, args) => helpManager.ShowQuickTips();

            // Reset Delete Confirmations option
            var resetConfirmItem = new MenuItem { Header = "🔄 Reset Delete Confirmations" };
            resetConfirmItem.Click += (s, args) => {
                tabManager.ResetDeleteConfirmationPreference();
                CustomDialog.ShowInformation(
                    this,
                    "Delete confirmation dialogs have been reset.\nYou will be asked to confirm tab deletions again.",
                    "Confirmations Reset",
                    "🔄");
            };

            // About option
            var aboutItem = new MenuItem { Header = "ℹ️ About" };
            aboutItem.Click += (s, args) => helpManager.ShowAbout();

            contextMenu.Items.Add(helpItem);
            contextMenu.Items.Add(tipsItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(resetConfirmItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(aboutItem);

            // Show context menu at the help button
            if (sender is Button helpButton)
            {
                contextMenu.PlacementTarget = helpButton;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                contextMenu.IsOpen = true;
            }
        }
        #endregion

        #region Data Management
        // 📖 Load application data and initialize UI
        private void LoadApplicationData()
        {
            try
            {
                // 🪟 SIMPLE: All windows now use note window data
                var notesToLoad = WindowData.Notes ?? new List<SavedNote>();
                var settingsToLoad = DataManager.LoadSettings();
                
                loggingService.LogDebug($"🪟 Loading data for window: {WindowData.Title}");

                // 📋 Settings already loaded in constructor, just update if needed
                if (currentSettings == null)
                {
                    currentSettings = settingsToLoad;
                }

                // ⚙️ Update TabManager with current settings
                tabManager.UpdateSettings(currentSettings);

                if (notesToLoad?.Any() == true)
                {
                    tabManager.LoadTabs(notesToLoad);
                    
                    // 🎨 Refresh tab visuals after theme is loaded
                    tabManager.RefreshTabVisuals();
                }
                else
                {
                    tabManager.CreateNewTab();
                }

                UpdateStatusBar();
                loggingService.LogDebug("✅ App data loaded successfully");
            }
            catch (Exception ex)
            {
                loggingService.LogError("Error loading app data", ex);
                // Create default settings for fallback
                currentSettings = new AppSettings();
                tabManager.UpdateSettings(currentSettings);
                tabManager.CreateNewTab(); // Fallback
            }
        }

        // 💾 Save application data
        private void SaveApplicationData()
        {
            try
            {
                var currentNotes = tabManager.GetSaveData();

                // 🪟 SIMPLE: All windows save the same way now
                WindowData.Notes = currentNotes;
                WindowData.LastModified = DateTime.Now;
                WindowData.WindowLeft = this.Left;
                WindowData.WindowTop = this.Top;
                WindowData.WindowWidth = this.Width;
                WindowData.WindowHeight = this.Height;

                // Save the updated window data back to the manager
                var noteManager = NoteWindowManager.Instance;
                noteManager.SaveNoteWindows();
                
                // Also save global settings (shared across all windows)
                if (currentSettings != null)
                {
                    currentSettings.Theme = themeManager.IsDarkMode ? "Dark" : "Light";
                    currentSettings.AlwaysOnTop = this.Topmost;
                    DataManager.SaveSettings(currentSettings);
                }
                
                loggingService.LogDebug($"🪟 Saved data for window: {WindowData.Title}");

                hasUnsavedChanges = false;
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                loggingService.LogError("Error saving app data", ex);
            }
        }
        #endregion

        #region UI Updates
        // 📊 Update status bar
        private void UpdateStatusBar()
        {
            var currentTabText = tabManager.SelectedTab?.Content?.TextContent ?? "";
            statusBarManager.UpdateStatusBar(tabManager.TabCount, currentTabText, hasUnsavedChanges);
        }


        #endregion

        #region Cleanup
        // 🧹 Handle window closing and cleanup resources
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // 💾 Save data if needed
                if (hasUnsavedChanges) SaveApplicationData();

                // 🛑 Stop and dispose timers
                autoSaveTimer?.Stop();
                autoSaveTimer = null;
                statusTimer?.Stop();
                statusTimer = null;

                // 🗑️ Cleanup managers
                settingsManager?.Dispose();
                
                loggingService.LogDebug("🧹 MainWindow cleanup completed");
            }
            catch (Exception ex)
            {
                loggingService.LogError("Error during MainWindow cleanup", ex);
            }
        }
        #endregion
    }
}