using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SnipShottyBoard.Core.Managers;
using SnipShottyBoard.Infrastructure.Logging;
using SnipShottyBoard.UI.Views;

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
            // Initialize DataManager first — creates app directories and runs one-time migration.
            // Must be explicit (not static ctor) so errors surface as real exceptions, not
            // TypeInitializationException. Logging not yet available — errors go to MessageBox.
            try
            {
                DataManager.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize application data folder:\n{ex.Message}\n\n" +
                    $"Check that the app has write access to %AppData%\\SnipShottyBoard.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Shutdown(1);
                return;
            }

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

            // 🪟 Sticky-Notes-style window restore: reopen exactly the windows that
            // were open at last shutdown. Replaces the old StartupUri approach which
            // only ever opened the first window. Falls back to a single default
            // window if no IsOpen windows exist (fresh install or all closed).
            RestoreOpenWindows();
        }

        /// <summary>
        /// 🪟 Iterate saved note windows and instantiate a MainWindow for each
        /// one flagged IsOpen=true at last shutdown. Mirrors Windows Sticky Notes
        /// app behavior. If nothing has IsOpen=true (rare — e.g. user closed every
        /// individual window before exiting), open the first active one or a fresh
        /// default so the user is never left without a window.
        /// </summary>
        private void RestoreOpenWindows()
        {
            try
            {
                var noteManager = NoteWindowManager.Instance;
                var allActiveWindows = noteManager.GetActiveWindows();
                var openWindows = allActiveWindows.Where(w => w.IsOpen).ToList();

                if (openWindows.Count == 0 && allActiveWindows.Count > 0)
                {
                    // No window was flagged open (legacy data or user closed all
                    // individual windows in NoteListWindow). Fall back to opening
                    // the first active window so the user isn't stranded.
                    openWindows.Add(allActiveWindows[0]);
                    loggingService?.LogInfo(
                        "🪟 No windows flagged IsOpen=true — falling back to first active window",
                        "Lifecycle");
                }

                if (openWindows.Count == 0)
                {
                    // Truly empty — fresh install path. MainWindow() with no args
                    // runs EnsureMainWindowHasData() which creates a default.
                    loggingService?.LogInfo("🪟 No saved windows — opening fresh default window", "Lifecycle");
                    var fresh = new MainWindow();
                    fresh.Show();
                    return;
                }

                loggingService?.LogInfo(
                    $"🪟 Restoring {openWindows.Count} window(s) from last session",
                    "Lifecycle");

                foreach (var windowData in openWindows)
                {
                    try
                    {
                        var win = new MainWindow(windowData);
                        win.Show();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogErrorStatic(
                            $"Failed to restore window '{windowData.Title}'", ex, "Lifecycle");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("RestoreOpenWindows failed catastrophically — opening default window", ex, "Lifecycle");
                try
                {
                    var fallback = new MainWindow();
                    fallback.Show();
                }
                catch (Exception fallbackEx)
                {
                    LoggingService.LogErrorStatic("Fallback MainWindow also failed", fallbackEx, "Lifecycle");
                    MessageBox.Show(
                        $"Failed to open any window:\n{ex.Message}\n\nFallback also failed:\n{fallbackEx.Message}",
                        "Startup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    this.Shutdown(1);
                }
            }
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