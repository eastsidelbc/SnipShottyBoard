using System;
using System.Windows;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.UI
{
    // ⚙️ SettingsManager - Handles settings window and configuration management
    public class SettingsManager
    {
        #region Static Window Tracking
        // 📊 Track settings windows to prevent multiple instances
        private static SettingsWindow activeSettingsWindow = null;
        #endregion

        #region Events
        // 🔄 Events for communicating with other managers
        public event Action<string> OnLogDebug;
        public event Action<string, Exception> OnLogError;
        public event Action OnSettingsChanged;
        public event Action OnResetDeleteConfirmationRequested;
        #endregion

        #region Fields
        private AppSettings currentSettings;
        #endregion

        #region Constructor
        public SettingsManager()
        {
            OnLogDebug?.Invoke("⚙️ SettingsManager initialized");
        }
        #endregion

        #region Public Methods
        // 🏠 Show the settings window (only one at a time)
        public void ShowSettingsWindow(AppSettings currentSettings)
        {
            try
            {
                this.currentSettings = currentSettings;

                // 🔄 Close existing window if open
                if (activeSettingsWindow != null)
                {
                    activeSettingsWindow.Close();
                    activeSettingsWindow = null;
                }

                // 🆕 Create new settings window
                activeSettingsWindow = new SettingsWindow(currentSettings);
                
                // 🔗 Wire up events
                activeSettingsWindow.OnSettingsApplied += HandleSettingsApplied;
                activeSettingsWindow.OnLogDebug += (message) => OnLogDebug?.Invoke(message);
                activeSettingsWindow.OnLogError += (message, ex) => OnLogError?.Invoke(message, ex);
                activeSettingsWindow.OnResetDeleteConfirmationRequested += () => OnResetDeleteConfirmationRequested?.Invoke();

                // 🧹 Clear reference when window closes
                activeSettingsWindow.Closed += (s, e) => activeSettingsWindow = null;

                // 📍 Position window relative to main window
                activeSettingsWindow.Owner = Application.Current.MainWindow;
                activeSettingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // 🎭 Show the window
                activeSettingsWindow.Show();
                OnLogDebug?.Invoke("⚙️ Settings window opened");
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Failed to open settings window", ex);
                MessageBox.Show($"Error opening settings: {ex.Message}", 
                               "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔒 Close settings window if open
        public void CloseSettingsWindow()
        {
            try
            {
                if (activeSettingsWindow != null)
                {
                    activeSettingsWindow.Close();
                    activeSettingsWindow = null;
                    OnLogDebug?.Invoke("⚙️ Settings window closed");
                }
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error closing settings window", ex);
            }
        }
        #endregion

        #region Private Methods
        // ✅ Handle settings applied from the settings window
        private void HandleSettingsApplied(AppSettings newSettings)
        {
            try
            {
                OnLogDebug?.Invoke($"⚙️ Settings applied - AutoSave: {newSettings.AutoSaveEnabled}, SaveInterval: {newSettings.AutoSaveIntervalSeconds}s");
                
                // 🔔 Notify that settings have changed
                OnSettingsChanged?.Invoke();
                
                // 🔒 Close the settings window
                CloseSettingsWindow();
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error applying settings", ex);
            }
        }
        #endregion

        #region Cleanup
        // 🧹 Cleanup resources
        public void Dispose()
        {
            try
            {
                CloseSettingsWindow();
                OnLogDebug?.Invoke("⚙️ SettingsManager disposed");
            }
            catch (Exception ex)
            {
                OnLogError?.Invoke("❌ Error disposing SettingsManager", ex);
            }
        }
        #endregion
    }
} 