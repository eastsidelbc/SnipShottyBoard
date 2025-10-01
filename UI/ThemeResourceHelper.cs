using System;
using System.Windows;
using System.Windows.Media;
using SnipShottyBoard.Infrastructure.Logging;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 🎨 Safe theme resource access with fallback handling and logging
    /// Provides graceful degradation when theme resources are missing or invalid
    /// </summary>
    public static class ThemeResourceHelper
    {
        private static LoggingService? _loggingService;

        /// <summary>
        /// Initialize with logging service for error reporting
        /// </summary>
        /// <param name="loggingService">Logging service instance</param>
        public static void Initialize(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// 🔍 Safely get a theme resource with fallback and logging
        /// </summary>
        /// <typeparam name="T">Expected resource type</typeparam>
        /// <param name="resourceKey">Theme resource key</param>
        /// <param name="value">Retrieved value or default</param>
        /// <param name="fallbackValue">Default value if resource not found</param>
        /// <returns>True if resource found and correctly typed</returns>
        public static bool TryGet<T>(string resourceKey, out T value, T? fallbackValue = default)
        {
            value = fallbackValue ?? default!;
            
            try
            {
                var resource = Application.Current?.FindResource(resourceKey);
                if (resource is T typedResource)
                {
                    value = typedResource;
                    return true;
                }
                else if (resource != null)
                {
                    _loggingService?.LogWarning(
                        $"Theme resource '{resourceKey}' found but wrong type. Expected {typeof(T).Name}, got {resource.GetType().Name}", 
                        "UI");
                    return false;
                }
                else
                {
                    _loggingService?.LogWarning(
                        $"Theme resource '{resourceKey}' not found, using fallback", 
                        "UI");
                    return false;
                }
            }
            catch (ResourceReferenceKeyNotFoundException)
            {
                _loggingService?.LogWarning(
                    $"Theme resource '{resourceKey}' not found in any resource dictionary", 
                    "UI");
                return false;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError(
                    $"Error accessing theme resource '{resourceKey}'", 
                    ex, 
                    "UI");
                return false;
            }
        }

        /// <summary>
        /// 🎨 Get a brush resource safely with default fallback
        /// </summary>
        /// <param name="resourceKey">Brush resource key</param>
        /// <param name="fallbackColor">Fallback color if resource not found</param>
        /// <returns>Theme brush or solid color brush fallback</returns>
        public static Brush GetBrush(string resourceKey, Color? fallbackColor = null)
        {
            var defaultColor = fallbackColor ?? Colors.Black;
            
            if (TryGet<Brush>(resourceKey, out var brush, new SolidColorBrush(defaultColor)))
            {
                return brush;
            }
            
            return new SolidColorBrush(defaultColor);
        }

        /// <summary>
        /// 📏 Get a thickness resource safely with default fallback
        /// </summary>
        /// <param name="resourceKey">Thickness resource key</param>
        /// <param name="fallbackThickness">Fallback thickness if resource not found</param>
        /// <returns>Theme thickness or fallback value</returns>
        public static Thickness GetThickness(string resourceKey, Thickness? fallbackThickness = null)
        {
            var defaultThickness = fallbackThickness ?? new Thickness(0);
            
            TryGet<Thickness>(resourceKey, out var thickness, defaultThickness);
            return thickness;
        }

        /// <summary>
        /// 🎯 Get a style resource safely with null fallback
        /// </summary>
        /// <param name="resourceKey">Style resource key</param>
        /// <returns>Theme style or null if not found</returns>
        public static Style? GetStyle(string resourceKey)
        {
            TryGet<Style>(resourceKey, out var style, null);
            return style;
        }

        /// <summary>
        /// 🔢 Get a double resource safely with default fallback
        /// </summary>
        /// <param name="resourceKey">Double resource key</param>
        /// <param name="fallbackValue">Fallback value if resource not found</param>
        /// <returns>Theme double value or fallback</returns>
        public static double GetDouble(string resourceKey, double fallbackValue = 0.0)
        {
            TryGet<double>(resourceKey, out var value, fallbackValue);
            return value;
        }

        /// <summary>
        /// 📝 Get a string resource safely with default fallback
        /// </summary>
        /// <param name="resourceKey">String resource key</param>
        /// <param name="fallbackValue">Fallback string if resource not found</param>
        /// <returns>Theme string or fallback</returns>
        public static string GetString(string resourceKey, string fallbackValue = "")
        {
            TryGet<string>(resourceKey, out var value, fallbackValue);
            return value;
        }

        /// <summary>
        /// 🔍 Check if a resource exists and is of the expected type
        /// </summary>
        /// <typeparam name="T">Expected resource type</typeparam>
        /// <param name="resourceKey">Resource key to check</param>
        /// <returns>True if resource exists and is correct type</returns>
        public static bool ResourceExists<T>(string resourceKey)
        {
            return TryGet<T>(resourceKey, out _, default);
        }

        /// <summary>
        /// 📊 Validate multiple theme resources and report any issues
        /// </summary>
        /// <param name="resourceKeys">Array of resource keys to validate</param>
        /// <returns>Number of missing or invalid resources</returns>
        public static int ValidateResources(params (string key, Type expectedType)[] resourceKeys)
        {
            int issueCount = 0;
            
            foreach (var (key, expectedType) in resourceKeys)
            {
                try
                {
                    var resource = Application.Current?.FindResource(key);
                    if (resource == null)
                    {
                        _loggingService?.LogWarning($"Theme resource validation: '{key}' is missing", "UI");
                        issueCount++;
                    }
                    else if (!expectedType.IsAssignableFrom(resource.GetType()))
                    {
                        _loggingService?.LogWarning(
                            $"Theme resource validation: '{key}' is wrong type. Expected {expectedType.Name}, got {resource.GetType().Name}", 
                            "UI");
                        issueCount++;
                    }
                }
                catch (ResourceReferenceKeyNotFoundException)
                {
                    _loggingService?.LogWarning($"Theme resource validation: '{key}' not found in dictionaries", "UI");
                    issueCount++;
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError($"Theme resource validation error for '{key}'", ex, "UI");
                    issueCount++;
                }
            }

            if (issueCount > 0)
            {
                _loggingService?.LogWarning($"Theme validation found {issueCount} resource issues", "UI");
            }
            else
            {
                _loggingService?.LogDebug("Theme validation passed - all resources valid", "UI");
            }

            return issueCount;
        }
    }
}
