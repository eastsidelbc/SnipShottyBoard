using System;
using System.Windows;
using System.Windows.Controls;
using SnipShottyBoard.Core.Models;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.UI.Views
{
    public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
    {
        #region Events
        // 🔄 Events for communicating with SettingsManager
        public event Action<AppSettings> OnSettingsApplied;
        public event Action<string> OnLogDebug;
        public event Action<string, Exception> OnLogError;
        public event Action OnResetDeleteConfirmationRequested;
        #endregion

        #region Fields
        private AppSettings originalSettings;
        private AppSettings workingSettings;
        private bool isInitializing = true;
        private Button currentActiveTab;
        #endregion

        #region Constructor
        public SettingsWindow(AppSettings currentSettings)
        {
            try
            {
                InitializeComponent();
                
                // 🔄 Clone settings so we can work with a copy
                originalSettings = currentSettings;
                workingSettings = CloneSettings(currentSettings);
                
                // 🎨 Initialize UI with current settings
                InitializeControls();
                
                isInitializing = false;
                OnLogDebug?.Invoke("⚙️ SettingsWindow initialized");
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error initializing SettingsWindow", ex);
            }
        }
        #endregion

        #region Initialization
        // 🎨 Initialize all controls with current settings
        private void InitializeControls()
        {
            try
            {
                // 💾 Auto-Save Settings
                AutoSaveEnabledCheckBox.IsChecked = workingSettings.AutoSaveEnabled;
                SetSelectedComboBoxItem(SaveIntervalComboBox, workingSettings.AutoSaveIntervalSeconds.ToString());
                
                // 🎨 Appearance Settings
                SetSelectedComboBoxItem(FontSizeComboBox, workingSettings.FontSize.ToString());
                
                // 💻 Window Settings
                AlwaysOnTopCheckBox.IsChecked = workingSettings.AlwaysOnTop;
                
                // 📋 Tab Settings
                SetSelectedComboBoxItem(MaxTabsComboBox, workingSettings.MaxTabs.ToString());
                ConfirmTabDeletionCheckBox.IsChecked = workingSettings.ConfirmTabDeletion;
                
                // 🔄 Update control states
                UpdateControlStates();
                
                // 🗂️ Initialize tab selection (default to General)
                InitializeTabSelection();
                
                OnLogDebug?.Invoke("🎨 Controls initialized with current settings");
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error initializing controls", ex);
            }
        }

        // 🔧 Helper method to set ComboBox selection by tag
        private void SetSelectedComboBoxItem(ComboBox comboBox, string tagValue)
        {
            try
            {
                foreach (ComboBoxItem item in comboBox.Items)
                {
                    if (item.Tag?.ToString() == tagValue)
                    {
                        comboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke($"❌ Error setting ComboBox selection for {comboBox.Name}", ex);
            }
        }

        // 🔄 Update control enable/disable states based on settings
        private void UpdateControlStates()
        {
            try
            {
                // 💾 Disable save interval when auto-save is off
                SaveIntervalComboBox.IsEnabled = workingSettings.AutoSaveEnabled;
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error updating control states", ex);
            }
        }

        // 🗂️ Initialize tab selection
        private void InitializeTabSelection()
        {
            try
            {
                // Set General tab as default active tab
                TabButton_Click(GeneralTabButton, null);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error initializing tab selection", ex);
            }
        }
        #endregion

        #region Tab Management
        // 🗂️ Handle tab button clicks
        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button clickedTab)
                {
                    // 🔄 Reset all tab buttons to default state
                    ResetTabButtonStates();
                    
                    // 🎯 Set clicked tab as active
                    SetActiveTab(clickedTab);
                    
                    // 📄 Show corresponding panel
                    ShowTabPanel(clickedTab.Tag?.ToString());
                    
                    OnLogDebug?.Invoke($"🗂️ Switched to tab: {clickedTab.Tag}");
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error handling tab click", ex);
            }
        }

        // 🔄 Reset all tab buttons to default state
        private void ResetTabButtonStates()
        {
            try
            {
                var tabButtons = new[] { GeneralTabButton, AppearanceTabButton, WindowTabButton, TabSettingsTabButton };
                
                foreach (var button in tabButtons)
                {
                    button.Background = System.Windows.Media.Brushes.Transparent;
                    button.FontWeight = FontWeights.Normal;
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error resetting tab button states", ex);
            }
        }

        // 🎯 Set the active tab styling
        private void SetActiveTab(Button tabButton)
        {
            try
            {
                currentActiveTab = tabButton;
                tabButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)); // 20% white
                tabButton.FontWeight = FontWeights.SemiBold;
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error setting active tab", ex);
            }
        }

        // 📄 Show the corresponding settings panel
        private void ShowTabPanel(string tabName)
        {
            try
            {
                // 🙈 Hide all panels
                GeneralPanel.Visibility = Visibility.Collapsed;
                AppearancePanel.Visibility = Visibility.Collapsed;
                WindowPanel.Visibility = Visibility.Collapsed;
                TabSettingsPanel.Visibility = Visibility.Collapsed;

                // 👁️ Show the selected panel
                switch (tabName)
                {
                    case "General":
                        GeneralPanel.Visibility = Visibility.Visible;
                        break;
                    case "Appearance":
                        AppearancePanel.Visibility = Visibility.Visible;
                        break;
                    case "Window":
                        WindowPanel.Visibility = Visibility.Visible;
                        break;
                    case "TabSettings":
                        TabSettingsPanel.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error showing tab panel", ex);
            }
        }
        #endregion

        #region Event Handlers
        // 💾 Auto-Save Settings Changed
        private void AutoSaveEnabled_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;
            
            try
            {
                workingSettings.AutoSaveEnabled = AutoSaveEnabledCheckBox.IsChecked ?? true;
                UpdateControlStates();
                OnLogDebug?.Invoke($"⚙️ Auto-save enabled changed: {workingSettings.AutoSaveEnabled}");
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error changing auto-save enabled setting", ex);
            }
        }

        private void SaveInterval_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;
            
            try
            {
                if (SaveIntervalComboBox.SelectedItem is ComboBoxItem selected)
                {
                    workingSettings.AutoSaveIntervalSeconds = int.Parse(selected.Tag.ToString());
                    OnLogDebug?.Invoke($"⚙️ Save interval changed: {workingSettings.AutoSaveIntervalSeconds}s");
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error changing save interval", ex);
            }
        }

        // 🔤 Font Settings Changed
        private void FontSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;
            
            try
            {
                if (FontSizeComboBox.SelectedItem is ComboBoxItem selected)
                {
                    workingSettings.FontSize = int.Parse(selected.Tag.ToString());
                    OnLogDebug?.Invoke($"⚙️ Font size changed: {workingSettings.FontSize}");
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error changing font size", ex);
            }
        }

        // 💻 Window Settings Changed
        private void AlwaysOnTop_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;
            
            try
            {
                workingSettings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked ?? false;
                OnLogDebug?.Invoke($"⚙️ Always on top changed: {workingSettings.AlwaysOnTop}");
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error changing always on top setting", ex);
            }
        }

        // 📋 Tab Settings Changed
        private void MaxTabs_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;
            
            try
            {
                if (MaxTabsComboBox.SelectedItem is ComboBoxItem selected)
                {
                    workingSettings.MaxTabs = int.Parse(selected.Tag.ToString());
                    OnLogDebug?.Invoke($"⚙️ Max tabs changed: {workingSettings.MaxTabs}");
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error changing max tabs", ex);
            }
        }

        private void ConfirmTabDeletion_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;
            
            try
            {
                workingSettings.ConfirmTabDeletion = ConfirmTabDeletionCheckBox.IsChecked ?? true;
                OnLogDebug?.Invoke($"⚙️ Confirm tab deletion changed: {workingSettings.ConfirmTabDeletion}");
                
                // 🔄 If the user is enabling confirmation, also reset the "don't ask again" preference
                if (workingSettings.ConfirmTabDeletion)
                {
                    // Find the TabManager and reset its preference
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        // We need to access the TabManager - let's add this via an event
                        OnResetDeleteConfirmationRequested?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error changing confirm tab deletion setting", ex);
            }
        }

        // 🔘 Button Event Handlers
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnLogDebug?.Invoke("✅ Applying settings changes");
                OnSettingsApplied?.Invoke(workingSettings);
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error applying settings", ex);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnLogDebug?.Invoke("❌ Settings cancelled - no changes applied");
                this.Close();
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error cancelling settings", ex);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var (confirmed, _) = CustomDialog.ShowConfirmation(
                    this,
                    "Are you sure you want to reset all settings to their default values?\n\nThis action cannot be undone.",
                    "Reset Settings",
                    "🔄",
                    showDontAskAgain: false,
                    "🔄 Reset",
                    "❌ Cancel");

                if (confirmed)
                {
                    // 🔄 Reset to default settings
                    workingSettings = new AppSettings();
                    isInitializing = true;
                    InitializeControls();
                    isInitializing = false;
                    
                    OnLogDebug?.Invoke("🔄 Settings reset to defaults");
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error resetting settings", ex);
            }
        }

        #endregion

        #region Helper Methods
        // 🔄 Create a deep copy of settings
        private AppSettings CloneSettings(AppSettings original)
        {
            try
            {
                return new AppSettings
                {
                    // 💾 Auto-Save Settings
                    AutoSaveEnabled = original.AutoSaveEnabled,
                    AutoSaveIntervalSeconds = original.AutoSaveIntervalSeconds,
                    
                    // 🎨 Appearance Settings
                    FontSize = original.FontSize,
                    
                    // 💻 Window Settings
                    AlwaysOnTop = original.AlwaysOnTop,
                    WindowLeft = original.WindowLeft,
                    WindowTop = original.WindowTop,
                    WindowWidth = original.WindowWidth,
                    WindowHeight = original.WindowHeight,
                    
                    // 📋 Tab Settings
                    MaxTabs = original.MaxTabs,
                    ConfirmTabDeletion = original.ConfirmTabDeletion
                };
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error cloning settings", ex);
                return new AppSettings(); // Return defaults if cloning fails
            }
        }
        #endregion
    }
} 