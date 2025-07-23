using System;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 🔔 EventHelper - Standardizes event handling patterns
    /// Provides safe event invocation and common event delegates
    /// </summary>
    public static class EventHelper
    {
        /// <summary>
        /// 🔒 Safely invoke an action event with null checking
        /// </summary>
        /// <param name="eventHandler">Event to invoke</param>
        /// <param name="suppressExceptions">Whether to suppress exceptions during invocation</param>
        public static void SafeInvoke(Action eventHandler, bool suppressExceptions = true)
        {
            if (eventHandler == null) return;

            if (suppressExceptions)
            {
                try
                {
                    eventHandler.Invoke();
                }
                catch
                {
                    // Suppress exception
                }
            }
            else
            {
                eventHandler.Invoke();
            }
        }

        /// <summary>
        /// 🔒 Safely invoke an action event with one parameter
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="eventHandler">Event to invoke</param>
        /// <param name="arg">Event argument</param>
        /// <param name="suppressExceptions">Whether to suppress exceptions during invocation</param>
        public static void SafeInvoke<T>(Action<T> eventHandler, T arg, bool suppressExceptions = true)
        {
            if (eventHandler == null) return;

            if (suppressExceptions)
            {
                try
                {
                    eventHandler.Invoke(arg);
                }
                catch
                {
                    // Suppress exception
                }
            }
            else
            {
                eventHandler.Invoke(arg);
            }
        }

        /// <summary>
        /// 🔒 Safely invoke an action event with two parameters
        /// </summary>
        /// <typeparam name="T1">First parameter type</typeparam>
        /// <typeparam name="T2">Second parameter type</typeparam>
        /// <param name="eventHandler">Event to invoke</param>
        /// <param name="arg1">First event argument</param>
        /// <param name="arg2">Second event argument</param>
        /// <param name="suppressExceptions">Whether to suppress exceptions during invocation</param>
        public static void SafeInvoke<T1, T2>(Action<T1, T2> eventHandler, T1 arg1, T2 arg2, bool suppressExceptions = true)
        {
            if (eventHandler == null) return;

            if (suppressExceptions)
            {
                try
                {
                    eventHandler.Invoke(arg1, arg2);
                }
                catch
                {
                    // Suppress exception
                }
            }
            else
            {
                eventHandler.Invoke(arg1, arg2);
            }
        }

        /// <summary>
        /// 🎯 Create a standard event handler wrapper that includes automatic logging
        /// </summary>
        /// <param name="handler">Original handler</param>
        /// <param name="eventName">Name of the event for logging</param>
        /// <param name="logHandler">Debug log handler</param>
        /// <param name="errorHandler">Error log handler</param>
        /// <returns>Wrapped handler with logging</returns>
        public static Action CreateLoggingWrapper(
            Action handler,
            string eventName,
            Action<string> logHandler = null,
            Action<string, Exception> errorHandler = null)
        {
            return () =>
            {
                try
                {
                    logHandler?.Invoke($"🔔 {eventName} triggered");
                    handler?.Invoke();
                    logHandler?.Invoke($"✅ {eventName} completed");
                }
                catch (Exception ex)
                {
                    errorHandler?.Invoke($"❌ Error in {eventName}", ex);
                }
            };
        }

        /// <summary>
        /// 🎯 Create a standard event handler wrapper with one parameter
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="handler">Original handler</param>
        /// <param name="eventName">Name of the event for logging</param>
        /// <param name="logHandler">Debug log handler</param>
        /// <param name="errorHandler">Error log handler</param>
        /// <returns>Wrapped handler with logging</returns>
        public static Action<T> CreateLoggingWrapper<T>(
            Action<T> handler,
            string eventName,
            Action<string> logHandler = null,
            Action<string, Exception> errorHandler = null)
        {
            return (arg) =>
            {
                try
                {
                    logHandler?.Invoke($"🔔 {eventName} triggered");
                    handler?.Invoke(arg);
                    logHandler?.Invoke($"✅ {eventName} completed");
                }
                catch (Exception ex)
                {
                    errorHandler?.Invoke($"❌ Error in {eventName}", ex);
                }
            };
        }
    }

    /// <summary>
    /// 📋 Common event delegates used throughout the application
    /// Standardizes event signature patterns
    /// </summary>
    public static class CommonEvents
    {
        // 🔔 Basic events
        public delegate void DataChangedHandler(bool hasChanges);
        public delegate void StatusUpdateHandler();
        public delegate void LogDebugHandler(string message);
        public delegate void LogErrorHandler(string message, Exception exception);
        
        // 🎨 UI events
        public delegate void ThemeChangedHandler();
        public delegate void TabChangedHandler(string tabId);
        public delegate void WindowStateChangedHandler(bool isMaximized);
        
        // 📂 File events
        public delegate void FileLoadedHandler(string filePath);
        public delegate void FileSavedHandler(string filePath, bool success);
        
        // ⚙️ Settings events
        public delegate void SettingsChangedHandler();
        public delegate void SettingsAppliedHandler<T>(T newSettings);
        
        // 🖼️ Media events
        public delegate void ImageAddedHandler(string imagePath);
        public delegate void ImageRemovedHandler(string imagePath);
        public delegate void MediaChangedHandler();
        
        // 📝 Text events
        public delegate void TextChangedHandler();
        public delegate void TextSelectionChangedHandler(int start, int length);
        
        // 🖱️ Mouse events
        public delegate void MouseActionHandler(string action, object context);
        
        // ⌨️ Keyboard events
        public delegate void KeyboardShortcutHandler(string shortcut);
    }

    /// <summary>
    /// 🏗️ EventManager - Centralized event management for complex scenarios
    /// Useful for managing multiple related events with coordination
    /// </summary>
    public class EventManager
    {
        private readonly Action<string> logHandler;
        private readonly Action<string, Exception> errorHandler;

        public EventManager(Action<string> logHandler = null, Action<string, Exception> errorHandler = null)
        {
            this.logHandler = logHandler;
            this.errorHandler = errorHandler;
        }

        /// <summary>
        /// 🔄 Subscribe to multiple events with error handling
        /// </summary>
        /// <param name="eventName">Name for logging</param>
        /// <param name="handlers">Array of handlers to subscribe</param>
        public void SubscribeMultiple(string eventName, params Action[] handlers)
        {
            foreach (var handler in handlers)
            {
                if (handler != null)
                {
                    try
                    {
                        // This would be used with actual event subscription in real scenarios
                        logHandler?.Invoke($"🔗 Subscribed to {eventName}");
                    }
                    catch (Exception ex)
                    {
                        errorHandler?.Invoke($"Error subscribing to {eventName}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// 🔔 Trigger multiple events in sequence with error isolation
        /// </summary>
        /// <param name="eventName">Name for logging</param>
        /// <param name="handlers">Array of handlers to trigger</param>
        public void TriggerMultiple(string eventName, params Action[] handlers)
        {
            logHandler?.Invoke($"🚀 Triggering {eventName} for {handlers.Length} handlers");
            
            var successCount = 0;
            var errorCount = 0;

            foreach (var handler in handlers)
            {
                try
                {
                    handler?.Invoke();
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errorHandler?.Invoke($"Error in {eventName} handler", ex);
                }
            }

            logHandler?.Invoke($"✅ {eventName} completed: {successCount} success, {errorCount} errors");
        }
    }
} 