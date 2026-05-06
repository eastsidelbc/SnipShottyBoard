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
using System.Windows.Interop;
using System.Windows.Threading;

using Wpf.Ui.Controls;
using Wpf.Ui.Controls;

using SnipShottyBoard.Core.Managers;
using SnipShottyBoard.Core.Models;
using SnipShottyBoard.Core.Utils;
using SnipShottyBoard.Data;
using SnipShottyBoard.Infrastructure.Logging;
using SnipShottyBoard.UI;

namespace SnipShottyBoard.UI.Views
{
    public partial class MainWindow : FluentWindow
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
        private DispatcherTimer recoveryTimer;
        private bool hasUnsavedChanges = false;

        // 💾 Debounced window position tracker (fixes choppy dragging)
        private WindowPositionTracker _positionTracker;

        // 🪟 Window data for multi-window support
        public NoteWindowData WindowData { get; private set; }

        /// <summary>
        /// 🔄 Ensures main window has note window data
        /// ✅ FIXED: Removed dangerous auto-migration logic that could overwrite data
        /// Migration now only occurs on first-ever run when notewindows.json doesn't exist
        /// </summary>
        private NoteWindowData EnsureMainWindowHasData()
        {
            var noteManager = NoteWindowManager.Instance;
            
            loggingService?.LogDebug($"🔍 EnsureMainWindowHasData: Starting data check");
            
            // 🔍 Check if we already have note windows
            var existingWindows = noteManager.GetActiveWindows();
            loggingService?.LogDebug($"🔍 EnsureMainWindowHasData: Found {existingWindows.Count} existing windows");
            
            // ✅ If we have existing windows, ALWAYS use them (never auto-migrate)
            if (existingWindows.Any())
            {
                loggingService?.LogDebug($"✅ Using existing window: {existingWindows.First().Title}");
                return existingWindows.First();
            }
            
            // 🔄 Only attempt migration if notewindows.json doesn't exist (first-ever run)
            loggingService?.LogDebug($"🔍 No existing windows found. Checking for legacy data (first-run migration only)");
            try
            {
                var legacyData = new DataManager().LoadAppData();
                
                // Check if legacy data has any meaningful content
                var legacyContentNotes = legacyData?.Notes?.Where(n => 
                    !string.IsNullOrWhiteSpace(n.TextContent) || 
                    (n.Media != null && n.Media.Any())).ToList() ?? new List<SavedNote>();
                
                if (legacyContentNotes.Any())
                {
                    loggingService?.LogDebug($"🔄 First run: Migrating {legacyContentNotes.Count} notes from legacy format");
                    
                    // Create main window with legacy data (first-run migration only)
                    var mainWindow = new NoteWindowData
                    {
                        Id = Guid.NewGuid(),
                        Title = "My Notes",
                        CreatedAt = DateTime.Now,
                        LastModified = DateTime.Now,
                        IsActive = true,
                        Notes = legacyData.Notes,
                        WindowLeft = legacyData.Settings?.WindowLeft ?? SnipShottyBoard.Data.AppConstants.DefaultWindowLeft,
                        WindowTop = legacyData.Settings?.WindowTop ?? SnipShottyBoard.Data.AppConstants.DefaultWindowTop,
                        WindowWidth = legacyData.Settings?.WindowWidth ?? SnipShottyBoard.Data.AppConstants.DefaultWindowWidth,
                        WindowHeight = legacyData.Settings?.WindowHeight ?? SnipShottyBoard.Data.AppConstants.DefaultWindowHeight
                    };
                    
                    noteManager.NoteWindows.Add(mainWindow);
                    noteManager.SaveNoteWindows();
                    
                    loggingService?.LogDebug($"✅ First-run migration completed with {mainWindow.Notes.Count} notes");
                    return mainWindow;
                }
            }
            catch (Exception ex)
            {
                loggingService?.LogDebug($"⚠️ Could not load legacy data for first-run migration: {ex.Message}");
            }
            
            // 🆕 Create new main window (truly first run, no legacy data)
            loggingService?.LogDebug($"🆕 Creating new default window (no existing data found)");
            return noteManager.CreateNewNoteWindow("My Notes");
        }

        public MainWindow() : this(null)
        {
        }

        public MainWindow(NoteWindowData windowData)
        {
            try
            {
                // 🔧 Initialize logging FIRST so EnsureMainWindowHasData can log
                loggingService = new LoggingService();
                
                // 🪟 SIMPLE APPROACH: Every window gets note window data (no special cases)
                WindowData = windowData ?? EnsureMainWindowHasData();
                
                // 🎨 Load settings to get the correct theme before initializing UI
                var settingsToLoad = DataManager.LoadSettings();
                currentSettings = settingsToLoad;
                
                // 🎨 Initialize theme manager with correct theme from settings
                themeManager = new ThemeManager();
                if (currentSettings != null)
                {
                    themeManager.LoadTheme(currentSettings.IsDarkMode);
                    loggingService.LogDebug($"🎨 Theme initialized: Dark");
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
                    
                    if (WindowData.WindowWidth > SnipShottyBoard.Data.AppConstants.MinWindowWidth && WindowData.WindowWidth < SystemParameters.VirtualScreenWidth)
                        this.Width = WindowData.WindowWidth;

                    if (WindowData.WindowHeight > SnipShottyBoard.Data.AppConstants.MinWindowHeight && WindowData.WindowHeight < SystemParameters.VirtualScreenHeight)
                        this.Height = WindowData.WindowHeight;
                        
                    loggingService.LogDebug($"🪟 Window positioned at: {this.Left},{this.Top} Size: {this.Width}x{this.Height}");
                    
                    // 💾 Set up debounced position tracking (prevents choppy dragging from disk I/O)
                    SetupPositionTracking();
                }
                
                loggingService.LogDebug("🎉 MainWindow initialization completed successfully");
            }
            catch (Exception ex)
            {
                var tempLogger = new LoggingService();
                tempLogger.LogError("FATAL ERROR in MainWindow constructor", ex);
                System.Windows.MessageBox.Show($"Fatal error starting application:\n{ex.Message}\n\nCheck console for details.",
                               "Application Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Sets the DWM composition background to our dark theme color.
        /// This is the correct fix for white flash during window resize.
        /// 
        /// WHY THIS WORKS:
        /// Every WPF window has a Win32 HWND background brush set to system white.
        /// When you resize, Windows paints this HWND brush BEFORE WPF renders.
        /// Setting Window.Background or Grid.Background is WPF-level — too late.
        /// HwndSource.CompositionTarget.BackgroundColor sets the actual DWM
        /// composition background for this HWND so any rendering gap shows dark.
        /// 
        /// WHY NOT OnSourceInitialized + WindowChrome:
        /// Replacing FluentWindow's internal WindowChrome via SetWindowChrome()
        /// breaks FluentWindow's HWND hook (WM_NCHITTEST) causing ghost/mirrored
        /// DWM caption buttons to appear when dragging the left resize edge.
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Set DWM composition background to our dark app color
            // #111113 = AppBackgroundBrush
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source?.CompositionTarget != null)
            {
                source.CompositionTarget.BackgroundColor =
                    System.Windows.Media.Color.FromRgb(0x11, 0x11, 0x13);
            }
        }

        #region Initialization
        // 🔧 Initialize all manager instances
        private void InitializeManagers()
        {
            loggingService.LogApplicationStart();
            loggingService.LogDebug("🚀 Starting MainWindow initialization", "UI");

            // DataManager is now static
            // themeManager already initialized in constructor
            
            tabManager = new TabManager(TabHeaderPanel, TabContentArea);
            statusBarManager = new StatusBarManager(TabCountStatus, WordCountStatus, SaveStatus, TimeStatus);
            keyboardHandler = new KeyboardHandler();
            helpManager = new HelpManager();
            settingsManager = new SettingsManager();

            loggingService.LogDebug("✅ All managers initialized", "UI");
        }

        // ⏰ Setup auto-save and status update timers
        private void SetupTimers()
        {
            // ⏰ Setup auto-save timer (save every 5 seconds if changes exist)
            autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(SnipShottyBoard.Data.AppConstants.DefaultAutoSaveIntervalSeconds)
            };
            autoSaveTimer.Tick += (s, e) => {
                if (hasUnsavedChanges) SaveApplicationData();
            };
            autoSaveTimer.Start();

            // 💾 Setup recovery journal timer (write snapshot every 2s when dirty)
            recoveryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(SnipShottyBoard.Data.AppConstants.RecoveryJournalIntervalSeconds)
            };
            recoveryTimer.Tick += (s, e) => {
                if (hasUnsavedChanges)
                {
                    var master = new SnipShottyBoard.Data.MasterData
                    {
                        Windows = NoteWindowManager.Instance.GetActiveWindows(),
                        Settings = currentSettings ?? new AppSettings()
                    };
                    DataManager.SaveRecoverySnapshot(master);
                }
            };
            recoveryTimer.Start();

            // 📊 Setup status bar timer (update every second)
            statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(SnipShottyBoard.Data.AppConstants.StatusUpdateIntervalSeconds)
            };
            statusTimer.Tick += (s, e) => UpdateStatusBar();
            statusTimer.Start();

            loggingService.LogDebug("✅ Timers started", "UI");
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
            tabManager.OnLogError += (message, ex) => loggingService.LogError(message, ex, "Manager");
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
            keyboardHandler.OnTabNavigationRequested += (direction) => tabManager.NavigateTab(direction);
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
            settingsManager.OnLogError += (message, ex) => loggingService.LogError(message, ex, "Manager");
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
                    loggingService.LogError("Error reloading settings after change", ex, "UI");
                }
            };

            loggingService.LogDebug("✅ Event handlers wired");
        }
        
        /// <summary>
        /// 💾 Set up debounced position tracking for window moves/resizes
        /// Prevents choppy dragging caused by saving to disk on every pixel move
        /// </summary>
        private void SetupPositionTracking()
        {
            if (WindowData == null) return;
            
            _positionTracker = new WindowPositionTracker(this, () =>
            {
                // This callback runs only after drag/resize stops (debounced)
                WindowData.WindowLeft = this.Left;
                WindowData.WindowTop = this.Top;
                WindowData.WindowWidth = this.ActualWidth;
                WindowData.WindowHeight = this.ActualHeight;
                
                NoteWindowManager.Instance.SaveNoteWindows();
                loggingService?.LogDebug($"💾 Debounced save: Window position {this.Left},{this.Top} Size: {this.Width}x{this.Height}");
            });
            
            loggingService.LogDebug("✅ Position tracking enabled (debounced)");
        }
        #endregion

        #region Rich Text Formatting
        private void ApplyRichTextFormatting(System.Windows.Controls.RichTextBox richTextBox, string formatType)
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
        /// <summary>
        /// Opens developer tools and options
        /// </summary>
        private void Developer_Click(object sender, RoutedEventArgs e)
        {
            // Show developer menu with options like "Open Logs Folder"
            var menu = new ContextMenu();

            var openLogsItem = new System.Windows.Controls.MenuItem
            {
                Header = "📁 Open Logs Folder",
                ToolTip = "Open the folder containing application logs"
            };
            openLogsItem.Click += (s, ev) =>
            {
                try
                {
                    var logsFolder = LoggingService.GetLogsFolder();
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(logsFolder)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    loggingService.LogError("Failed to open logs folder", ex, "UI");
                }
            };

            menu.Items.Add(openLogsItem);

            var auditItem = new System.Windows.Controls.MenuItem
            {
                Header = "🔍 Audit Vault",
                ToolTip = "Show vault statistics and clean orphaned files"
            };
            auditItem.Click += (s, ev) => ShowVaultAudit();
            menu.Items.Add(auditItem);

            menu.PlacementTarget = sender as FrameworkElement;
            menu.IsOpen = true;
        }

        private void ShowVaultAudit()
        {
            try
            {
                var imagesFolder = SnipShottyBoard.Core.Managers.DataManager.GetImagesFolder();
                var diskFiles = Directory.Exists(imagesFolder) ? Directory.GetFiles(imagesFolder) : Array.Empty<string>();
                int totalOnDisk = diskFiles.Length;

                // Collect referenced filenames from all open tabs
                var referencedFilenames = new HashSet<string>();
                int totalRefs = 0;
                foreach (var tab in tabManager.Tabs)
                {
                    var files = tab.Content.ImageFiles;
                    totalRefs += files.Count;
                    foreach (var f in files)
                        referencedFilenames.Add(Path.GetFileName(f));
                }

                // Find orphaned files (on disk but not referenced)
                var orphans = diskFiles.Where(f => !referencedFilenames.Contains(Path.GetFileName(f))).ToList();
                int orphanCount = orphans.Count;

                // Split orphans by age (24h grace period)
                var cutoff = DateTime.Now.AddDays(-1);
                int graceOrphans = 0;
                foreach (var orphan in orphans)
                {
                    try
                    {
                        if (File.GetCreationTime(orphan) >= cutoff)
                            graceOrphans++;
                    }
                    catch { /* skip files we can't read */ }
                }
                int oldOrphans = orphanCount - graceOrphans;

                // Check for duplicate image entries within notes
                int dupNotes = 0;
                foreach (var tab in tabManager.Tabs)
                {
                    var filenames = tab.Content.ImageFiles.Select(f => Path.GetFileName(f)).ToList();
                    if (filenames.Count != filenames.Distinct().Count())
                        dupNotes++;
                }

                var result = System.Windows.MessageBox.Show(
                    this,
                    $"Vault Audit Results\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    $"Total image files on disk:     {totalOnDisk}\n" +
                    $"Total references in notes:     {totalRefs}\n" +
                    $"Orphaned files (not referenced): {orphanCount}\n" +
                    $"  - Older than 24h:           {oldOrphans}\n" +
                    $"  - Newer than 24h (grace):    {graceOrphans}\n" +
                    $"Notes with duplicate entries:   {dupNotes}\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                    $"Do you want to delete {orphanCount} orphaned file(s) now?\n" +
                    $"(Files within 24h grace period will also be deleted)",
                    "Vault Audit",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes && orphanCount > 0)
                {
                    var deleted = SnipShottyBoard.Core.Managers.DataManager.CleanupOrphanedImages(daysGracePeriod: 0);
                    System.Windows.MessageBox.Show(
                        this,
                        $"Deleted {deleted} orphaned file(s).",
                        "Vault Cleanup Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                else if (orphanCount == 0)
                {
                    System.Windows.MessageBox.Show(
                        this,
                        "No orphaned files found. Vault is clean!",
                        "Vault Audit",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                loggingService.LogError("Vault audit failed", ex, "UI");
                System.Windows.MessageBox.Show(
                    this,
                    $"Vault audit failed: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void NewTab_Click(object sender, RoutedEventArgs e) => tabManager.CreateNewTab();
        private void DeleteTab_Click(object sender, RoutedEventArgs e) => tabManager.DeleteCurrentTab();

        // 📌 Pin button click - toggle always on top
        private void Pin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool newState = !this.Topmost;
                this.Topmost = newState;
                
                if (currentSettings != null)
                {
                    currentSettings.AlwaysOnTop = newState;
                    DataManager.SaveSettings(currentSettings);
                    loggingService.LogDebug($"📌 Always on top: {(newState ? "ON" : "OFF")}", "UI");
                }
                
                // Defer visual update to avoid conflict with button's active state
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdatePinButtonVisual(newState);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                loggingService.LogError("Failed to toggle always on top", ex, "UI");
            }
        }

        // 🎨 Update pin button visual based on state using Tag property
        // This triggers the style's Tag="Pinned" trigger for persistent visual state
        private void UpdatePinButtonVisual(bool isPinned)
        {
            try
            {
                if (PinButton != null)
                {
                    if (isPinned)
                    {
                        // Set Tag to "Pinned" to trigger the style's visual state
                        PinButton.Tag = "Pinned";
                        PinButton.ToolTip = "Always on top: On";
                        
                        loggingService?.LogDebug("📌 Pin button visual: ON (Tag set to 'Pinned')", "UI");
                    }
                    else
                    {
                        // Clear Tag to return to default state
                        PinButton.Tag = null;
                        PinButton.ToolTip = "Always on top: Off";
                        
                        loggingService?.LogDebug("📌 Pin button visual: OFF (Tag cleared)", "UI");
                    }
                }
            }
            catch (Exception ex)
            {
                loggingService?.LogError("Failed to update pin button visual", ex, "UI");
            }
        }

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
                loggingService.LogError("Error opening note list window", ex, "UI");
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            // 🔄 Get current settings from loaded data or create defaults
            var settingsToShow = currentSettings ?? new AppSettings
            {
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
            var helpItem = new System.Windows.Controls.MenuItem { Header = "📖 Help & Shortcuts" };
            helpItem.Click += (s, args) => helpManager.ShowHelpWindow();

            // Quick Tips option
            var tipsItem = new System.Windows.Controls.MenuItem { Header = "💡 Quick Tips" };
            tipsItem.Click += (s, args) => helpManager.ShowQuickTips();

            // Reset Delete Confirmations option
            var resetConfirmItem = new System.Windows.Controls.MenuItem { Header = "🔄 Reset Delete Confirmations" };
            resetConfirmItem.Click += (s, args) => {
                tabManager.ResetDeleteConfirmationPreference();
                CustomDialog.ShowInformation(
                    this,
                    "Delete confirmation dialogs have been reset.\nYou will be asked to confirm tab deletions again.",
                    "Confirmations Reset",
                    "🔄");
            };

            // About option
            var aboutItem = new System.Windows.Controls.MenuItem { Header = "ℹ️ About" };
            aboutItem.Click += (s, args) => helpManager.ShowAbout();

            contextMenu.Items.Add(helpItem);
            contextMenu.Items.Add(tipsItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(resetConfirmItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(aboutItem);

            // Show context menu at the help button
            if (sender is System.Windows.Controls.Button helpButton)
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
                    try
                    {
                        tabManager.LoadTabs(notesToLoad);
                        // 🎨 Refresh tab visuals after theme is loaded
                        tabManager.RefreshTabVisuals();
                    }
                    catch (Exception loadEx)
                    {
                        // LoadTabs threw — real notes are still safe on disk (WindowData.Notes unchanged).
                        // Show a blank tab for this session but DO NOT mark as changed.
                        // This prevents autosave from overwriting good data with a blank state.
                        loggingService.LogError("CRITICAL: LoadTabs failed — data preserved on disk, showing blank tab for this session only", loadEx, "Data");
                        tabManager.CreateNewTab();
                        hasUnsavedChanges = false;
                    }
                }
                else
                {
                    tabManager.CreateNewTab();
                }

                // 📌 Apply saved AlwaysOnTop state and update pin button visual
                if (currentSettings != null)
                {
                    this.Topmost = currentSettings.AlwaysOnTop;
                    // Defer pin button visual update until UI is fully loaded
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdatePinButtonVisual(currentSettings.AlwaysOnTop);
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                    loggingService.LogDebug($"📌 Always on top restored: {currentSettings.AlwaysOnTop}", "UI");
                }

                UpdateStatusBar();
                loggingService.LogDebug("✅ App data loaded successfully");
            }
            catch (Exception ex)
            {
                loggingService.LogError("Error loading app data", ex, "Data");
                currentSettings = new AppSettings();
                tabManager.UpdateSettings(currentSettings);
                tabManager.CreateNewTab();
                hasUnsavedChanges = false;
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
                    currentSettings.AlwaysOnTop = this.Topmost;
                    DataManager.SaveSettings(currentSettings);
                }
                
                loggingService.LogDebug($"🪟 Saved data for window: {WindowData.Title}");

                // 🗑️ Clear recovery snapshot on successful save
                DataManager.ClearRecoverySnapshot();

                hasUnsavedChanges = false;
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                loggingService.LogError("Error saving app data", ex, "Data");
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
                // 💾 Stop position tracker and force final save
                if (_positionTracker != null)
                {
                    _positionTracker.SaveNow(); // Save immediately before disposing
                    _positionTracker.Dispose();
                    _positionTracker = null;
                    loggingService.LogDebug("✅ Position tracker disposed");
                }
                
                // 💾 ALWAYS save window state (position/size) on close, regardless of content changes
                SaveApplicationData();
                loggingService.LogDebug("💾 Window state saved on close");

                // 🛑 Stop and dispose timers
                autoSaveTimer?.Stop();
                autoSaveTimer = null;
                statusTimer?.Stop();
                statusTimer = null;
                recoveryTimer?.Stop();
                recoveryTimer = null;

                // 🗑️ Cleanup managers
                settingsManager?.Dispose();
                
                loggingService.LogDebug("🧹 MainWindow cleanup completed");
            }
            catch (Exception ex)
            {
                loggingService.LogError("Error during MainWindow cleanup", ex, "UI");
            }
        }
        #endregion
    }
}