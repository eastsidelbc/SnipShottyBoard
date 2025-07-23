using System;
using System.Windows;
using System.Windows.Media;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 🎨 ResourceHelper - Standardizes resource access patterns
    /// Provides safe, consistent access to theme resources with fallbacks
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// 🔍 Get a theme resource safely with fallback
        /// </summary>
        /// <typeparam name="T">Resource type</typeparam>
        /// <param name="resourceKey">Resource key name</param>
        /// <param name="fallback">Fallback value if resource not found</param>
        /// <returns>Resource value or fallback</returns>
        public static T GetResource<T>(string resourceKey, T fallback = default(T))
        {
            try
            {
                var resource = Application.Current.FindResource(resourceKey);
                return resource is T ? (T)resource : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// 🖌️ Get a brush resource safely with automatic fallbacks
        /// </summary>
        /// <param name="resourceKey">Brush resource key</param>
        /// <param name="lightFallback">Fallback for light theme</param>
        /// <param name="darkFallback">Fallback for dark theme</param>
        /// <returns>Brush or appropriate fallback</returns>
        public static Brush GetBrushResource(string resourceKey, Brush lightFallback = null, Brush darkFallback = null)
        {
            try
            {
                return (Brush)Application.Current.FindResource(resourceKey);
            }
            catch
            {
                // Smart fallbacks based on resource name patterns
                if (resourceKey.Contains("Background"))
                {
                    return lightFallback ?? SystemColors.WindowBrush;
                }
                else if (resourceKey.Contains("Foreground"))
                {
                    return darkFallback ?? SystemColors.WindowTextBrush;
                }
                else if (resourceKey.Contains("Border"))
                {
                    return new SolidColorBrush(Colors.Gray);
                }
                
                return lightFallback ?? SystemColors.ControlBrush;
            }
        }

        /// <summary>
        /// 🎨 Get common theme brushes with smart fallbacks
        /// </summary>
        public static class CommonBrushes
        {
            public static Brush AppBackground => GetBrushResource("AppBackgroundBrush");
            public static Brush AppForeground => GetBrushResource("AppForegroundBrush");
            public static Brush HeaderBackground => GetBrushResource("HeaderBackgroundBrush");
            public static Brush TabBackground => GetBrushResource("TabBackgroundBrush");
            public static Brush Border => GetBrushResource("BorderBrush");
        }

        /// <summary>
        /// 🎯 Get style resource safely
        /// </summary>
        /// <param name="styleKey">Style resource key</param>
        /// <returns>Style or null if not found</returns>
        public static Style GetStyleResource(string styleKey)
        {
            return GetResource<Style>(styleKey, null);
        }

        /// <summary>
        /// 📏 Apply resource to framework element safely
        /// </summary>
        /// <param name="element">Element to apply resource to</param>
        /// <param name="property">Dependency property to set</param>
        /// <param name="resourceKey">Resource key</param>
        /// <returns>True if resource was applied successfully</returns>
        public static bool ApplyResource(FrameworkElement element, DependencyProperty property, string resourceKey)
        {
            try
            {
                element.SetResourceReference(property, resourceKey);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 🔍 Check if a resource exists
        /// </summary>
        /// <param name="resourceKey">Resource key to check</param>
        /// <returns>True if resource exists</returns>
        public static bool ResourceExists(string resourceKey)
        {
            try
            {
                Application.Current.FindResource(resourceKey);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 