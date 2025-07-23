using System;

namespace SnipShottyBoard.UI
{
    // 🐛 LoggingService - Handles debug logging and error reporting
    public class LoggingService
    {
        // 🐛 Log debug messages
        public void LogDebug(string message)
        {
            try
            {
                // Only output to Debug stream to prevent duplicates in debug console
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🐛 {message}");
            }
            catch
            {
                // Ignore logging errors
            }
        }

        // ❌ Log error messages with exception details
        public void LogError(string message, Exception ex)
        {
            try
            {
                string errorMsg = $"❌ {message}: {ex.Message}";
                // Only output to Debug stream to prevent duplicates in debug console
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {errorMsg}");
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Stack: {ex.StackTrace}");
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
} 