using System;
using System.Threading.Tasks;
using System.Windows;
using SnipShottyBoard.Core.Managers;
using SnipShottyBoard.Infrastructure.Logging;

namespace SnipShottyBoard
{
    /// <summary>
    /// 🚀 Application entry point with global exception handling
    /// Ensures graceful error handling and proper logging shutdown
    /// </summary>
    public partial class App : Application
    {
        private LoggingService? loggingService;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Initialize logging early
            loggingService = new LoggingService();
            
            // Setup global exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            
            loggingService.LogInfo("🚀 Application starting with global exception handling", "Lifecycle");

            // 💾 Silent crash recovery — merge recovery snapshot before any window loads
            if (DataManager.TryRestoreFromRecovery())
            {
                loggingService.LogInfo("💾 Unsaved text recovered silently on startup", "Lifecycle");
            }

            // Schedule startup cleanup tasks (background, after startup)
            Task.Run(async () =>
            {
                try
                {
                    // Wait 5 seconds after startup to avoid impacting initial load
                    await Task.Delay(5000);

                    // Clean up old log files
                    var logsDeleted = LoggingService.CleanupOldLogs(7);
                    if (logsDeleted > 0)
                    {
                        LoggingService.LogInfoStatic(
                            $"🗑️ Startup log cleanup: Removed {logsDeleted} old log file(s)",
                            "Lifecycle"
                        );
                    }

                    // Clean up orphaned images (24h grace period protects against crash-race conditions)
                    var imagesDeleted = DataManager.CleanupOrphanedImages(daysGracePeriod: 1);
                    if (imagesDeleted > 0)
                    {
                        LoggingService.LogInfoStatic(
                            $"🗑️ Startup cleanup: Removed {imagesDeleted} orphaned image(s)",
                            "Lifecycle"
                        );
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogErrorStatic("Failed to run startup cleanup", ex, "Lifecycle");
                }
            });
            
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                loggingService?.LogInfo("🛑 Application exiting gracefully", "Lifecycle");
                LoggingService.Shutdown();
            }
            catch
            {
                // Silent - app is shutting down
            }
            
            base.OnExit(e);
        }

        /// <summary>
        /// 💥 Handle unhandled exceptions from background threads
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                loggingService?.LogError("💥 Unhandled exception in background thread", 
                    exception ?? new Exception("Unknown exception"), "System");

                if (e.IsTerminating)
                {
                    var result = MessageBox.Show(
                        "SnipShottyBoard encountered a fatal error and must close.\n" +
                        "Your data has been auto-saved.\n\n" +
                        "Would you like to view the error details?",
                        "Fatal Error",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (result == MessageBoxResult.Yes && exception != null)
                    {
                        MessageBox.Show($"Error: {exception.Message}\n\nDetails logged to: {LoggingService.GetLogsFolder()}",
                            "Error Details", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch
            {
                // Last resort - can't even log
                MessageBox.Show("A critical error occurred and could not be logged.", 
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 💥 Handle unhandled exceptions from UI thread
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                loggingService?.LogError("💥 Unhandled exception in UI thread", e.Exception, "UI");

                var result = MessageBox.Show(
                    "SnipShottyBoard encountered an error but can continue running.\n" +
                    "Your data has been auto-saved.\n\n" +
                    $"Error: {e.Exception.Message}\n\n" +
                    "Would you like to view the logs folder?",
                    "Application Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = LoggingService.GetLogsFolder(),
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        MessageBox.Show($"Logs are located at: {LoggingService.GetLogsFolder()}",
                            "Logs Location", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Mark as handled so app continues
                e.Handled = true;
            }
            catch
            {
                // Last resort - let the default handler take over
                e.Handled = false;
            }
        }
    }
}