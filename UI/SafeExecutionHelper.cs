using System;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 🛡️ SafeExecutionHelper - Standardizes exception handling and logging patterns
    /// Reduces boilerplate code for try-catch blocks with consistent logging
    /// </summary>
    public static class SafeExecutionHelper
    {
        /// <summary>
        /// 🔒 Execute an action safely with automatic exception handling and logging
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="errorMessage">Error message to log if exception occurs</param>
        /// <param name="onLogError">Error logging callback</param>
        /// <param name="onLogDebug">Debug logging callback (optional)</param>
        /// <param name="suppressExceptions">Whether to suppress exceptions (default: true)</param>
        /// <returns>True if execution succeeded, false if exception occurred</returns>
        public static bool Execute(
            Action action, 
            string errorMessage,
            Action<string, Exception> onLogError = null,
            Action<string> onLogDebug = null,
            bool suppressExceptions = true)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                onLogError?.Invoke(errorMessage, ex);
                
                if (!suppressExceptions)
                    throw;
                    
                return false;
            }
        }

        /// <summary>
        /// 🔒 Execute a function safely with automatic exception handling and logging
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="errorMessage">Error message to log if exception occurs</param>
        /// <param name="defaultValue">Default value to return on exception</param>
        /// <param name="onLogError">Error logging callback</param>
        /// <param name="onLogDebug">Debug logging callback (optional)</param>
        /// <param name="suppressExceptions">Whether to suppress exceptions (default: true)</param>
        /// <returns>Function result or default value on exception</returns>
        public static T Execute<T>(
            Func<T> func,
            string errorMessage,
            T defaultValue = default(T),
            Action<string, Exception> onLogError = null,
            Action<string> onLogDebug = null,
            bool suppressExceptions = true)
        {
            try
            {
                return func != null ? func.Invoke() : defaultValue;
            }
            catch (Exception ex)
            {
                onLogError?.Invoke(errorMessage, ex);
                
                if (!suppressExceptions)
                    throw;
                    
                return defaultValue;
            }
        }

        /// <summary>
        /// 🎯 Execute with both debug entry/exit logging and error handling
        /// </summary>
        public static bool ExecuteWithLogging(
            Action action,
            string operationName,
            Action<string, Exception> onLogError = null,
            Action<string> onLogDebug = null,
            bool suppressExceptions = true)
        {
            onLogDebug?.Invoke($"🚀 Starting {operationName}");
            
            var success = Execute(action, $"Error in {operationName}", onLogError, onLogDebug, suppressExceptions);
            
            if (success)
                onLogDebug?.Invoke($"✅ {operationName} completed successfully");
            else
                onLogDebug?.Invoke($"❌ {operationName} failed");
                
            return success;
        }
    }
} 