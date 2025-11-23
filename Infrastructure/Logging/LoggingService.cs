using System;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SnipShottyBoard.Infrastructure.Logging
{
    /// <summary>
    /// 🐛 Centralized logging service using Serilog with file and debug output
    /// Provides structured logging with context categories and automatic file rotation
    /// </summary>
    public class LoggingService
    {
        private static readonly Lazy<ILogger> _logger = new Lazy<ILogger>(CreateLogger);
        private static ILogger Logger => _logger.Value;
        
        // 📂 Log file path for easy access
        public static string LogFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Creates and configures the Serilog logger with file and debug sinks
        /// </summary>
        private static ILogger CreateLogger()
        {
            try
            {
                var logFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "SnipShottyBoard", 
                    "logs");
                
                Directory.CreateDirectory(logFolder);
                LogFilePath = Path.Combine(logFolder, "snipshottyboard-.log");

                var config = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(
                        LogFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Category}: {Message:lj}{NewLine}{Exception}");

                // Note: Debug sink not available in current Serilog version
                // For Visual Studio output, use Debug.WriteLine in fallback handlers

                return config.CreateLogger();
            }
            catch
            {
                // Fallback to a minimal logger if file creation fails
                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .CreateLogger();
            }
        }

        /// <summary>
        /// 🐛 Log debug messages with optional category context
        /// </summary>
        /// <param name="message">Debug message to log</param>
        /// <param name="category">Optional category (UI, Manager, Data)</param>
        public void LogDebug(string message, string category = "General")
        {
            try
            {
                Logger.ForContext("Category", category).Debug(message);
            }
            catch
            {
                // Fallback to Debug.WriteLine if logging fails
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🐛 {category}: {message}");
            }
        }

        /// <summary>
        /// ❌ Log error messages with exception details and category context
        /// </summary>
        /// <param name="message">Error description</param>
        /// <param name="ex">Exception details</param>
        /// <param name="category">Optional category (UI, Manager, Data)</param>
        public void LogError(string message, Exception ex, string category = "General")
        {
            try
            {
                Logger.ForContext("Category", category).Error(ex, message);
            }
            catch
            {
                // Fallback to Debug.WriteLine if logging fails
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ {category}: {message}: {ex.Message}");
            }
        }

        /// <summary>
        /// ⚠️ Log warning messages with optional category context
        /// </summary>
        /// <param name="message">Warning message to log</param>
        /// <param name="category">Optional category (UI, Manager, Data)</param>
        public void LogWarning(string message, string category = "General")
        {
            try
            {
                Logger.ForContext("Category", category).Warning(message);
            }
            catch
            {
                // Fallback to Debug.WriteLine if logging fails
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ {category}: {message}");
            }
        }

        /// <summary>
        /// ℹ️ Log informational messages with optional category context
        /// </summary>
        /// <param name="message">Information message to log</param>
        /// <param name="category">Optional category (UI, Manager, Data)</param>
        public void LogInfo(string message, string category = "General")
        {
            try
            {
                Logger.ForContext("Category", category).Information(message);
            }
            catch
            {
                // Fallback to Debug.WriteLine if logging fails
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ℹ️ {category}: {message}");
            }
        }

        /// <summary>
        /// 📂 Get the logs folder path for opening in file explorer
        /// </summary>
        public static string GetLogsFolder()
        {
            var logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "SnipShottyBoard", 
                "logs");
            return logFolder;
        }

        /// <summary>
        /// 🚀 Log application startup information with system details
        /// </summary>
        public void LogApplicationStart()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "Unknown";
                var location = assembly.Location;
                var osVersion = Environment.OSVersion.ToString();
                var dotnetVersion = Environment.Version.ToString();
                var workingSet = Environment.WorkingSet / (1024 * 1024); // MB

                LogInfo($"🚀 SnipShottyBoard v{version} starting", "Lifecycle");
                LogInfo($"📍 Location: {location}", "Lifecycle");
                LogInfo($"🖥️ OS: {osVersion}", "System");
                LogInfo($"⚙️ .NET Runtime: {dotnetVersion}", "System");
                LogInfo($"💾 Working Set: {workingSet} MB", "System");
                LogInfo($"📂 Data Folder: {Path.GetDirectoryName(GetLogsFolder())}", "System");
            }
            catch (Exception ex)
            {
                LogError("Failed to log application startup info", ex, "Lifecycle");
            }
        }

        /// <summary>
        /// 🛑 Log application shutdown information
        /// </summary>
        public void LogApplicationShutdown()
        {
            try
            {
                var workingSet = Environment.WorkingSet / (1024 * 1024); // MB
                LogInfo($"🛑 SnipShottyBoard shutting down gracefully", "Lifecycle");
                LogInfo($"💾 Final Working Set: {workingSet} MB", "System");
            }
            catch (Exception ex)
            {
                LogError("Failed to log application shutdown info", ex, "Lifecycle");
            }
        }

        /// <summary>
        /// 🔥 Properly dispose of logging resources when application shuts down
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                if (_logger.IsValueCreated)
                {
                    Logger.Information("Logging service shutting down");
                    Log.CloseAndFlush();
                }
            }
            catch
            {
                // Ignore shutdown errors
            }
        }

        #region Static Bridge Methods (for static DataManager)
        // These methods enable static classes like DataManager to use structured logging
        // without requiring instance management. This is a pragmatic bridge pattern
        // until full dependency injection is implemented (Phase 4 P3 item).

        /// <summary>
        /// Static bridge: Log error with structured data
        /// Complies with LOGGING_GOVERNANCE.md structured logging requirements
        /// </summary>
        public static void LogErrorStatic(string message, Exception ex, string category, object data = null)
        {
            try
            {
                if (data != null)
                {
                    // Serialize structured data to string for Serilog
                    var dataStr = System.Text.Json.JsonSerializer.Serialize(data);
                    Logger.ForContext("Category", category)
                          .ForContext("Data", dataStr)
                          .Error(ex, message);
                }
                else
                {
                    Logger.ForContext("Category", category).Error(ex, message);
                }
            }
            catch
            {
                // Fallback to Debug.WriteLine
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ {category}: {message}: {ex.Message}");
            }
        }

        /// <summary>
        /// Static bridge: Log info with structured data
        /// </summary>
        public static void LogInfoStatic(string message, string category, object data = null)
        {
            try
            {
                if (data != null)
                {
                    var dataStr = System.Text.Json.JsonSerializer.Serialize(data);
                    Logger.ForContext("Category", category)
                          .ForContext("Data", dataStr)
                          .Information(message);
                }
                else
                {
                    Logger.ForContext("Category", category).Information(message);
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ℹ️ {category}: {message}");
            }
        }

        /// <summary>
        /// Static bridge: Log debug with structured data
        /// </summary>
        public static void LogDebugStatic(string message, string category, object data = null)
        {
            try
            {
                if (data != null)
                {
                    var dataStr = System.Text.Json.JsonSerializer.Serialize(data);
                    Logger.ForContext("Category", category)
                          .ForContext("Data", dataStr)
                          .Debug(message);
                }
                else
                {
                    Logger.ForContext("Category", category).Debug(message);
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🐛 {category}: {message}");
            }
        }

        /// <summary>
        /// Static bridge: Log warning with structured data
        /// </summary>
        public static void LogWarningStatic(string message, string category, object data = null)
        {
            try
            {
                if (data != null)
                {
                    var dataStr = System.Text.Json.JsonSerializer.Serialize(data);
                    Logger.ForContext("Category", category)
                          .ForContext("Data", dataStr)
                          .Warning(message);
                }
                else
                {
                    Logger.ForContext("Category", category).Warning(message);
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ {category}: {message}");
            }
        }
        #endregion
    }
} 