using System;
using System.Linq;
using System.Windows;

namespace SnipShottyBoard.UI
{
    // 🎨 ThemeManager - Handles theme switching and application
    public class ThemeManager
    {
        private bool isDarkMode = false;

        public bool IsDarkMode => isDarkMode;

        public event Action? OnThemeChanged;

        // 🌙 Toggle between dark and light themes
        public void ToggleTheme()
        {
            isDarkMode = !isDarkMode;
            ApplyTheme(isDarkMode);
            OnThemeChanged?.Invoke();
        }

        // 🎨 Apply the specified theme
        public void ApplyTheme(bool darkMode)
        {
            isDarkMode = darkMode;
            
            try
            {
                var themeName = darkMode ? "DarkTheme" : "LightTheme";
                System.Diagnostics.Debug.WriteLine($"🎨 Applying {themeName}...");
                
                var themeDict = new ResourceDictionary();
                themeDict.Source = new Uri($"Resources/Themes/{themeName}.xaml", UriKind.Relative);

                // 🔄 First add the new theme, then remove old ones to avoid resource gaps
                Application.Current.Resources.MergedDictionaries.Add(themeDict);
                
                // 🧠 Remove existing theme dictionaries (but keep the new one we just added)
                var existingThemes = Application.Current.Resources.MergedDictionaries
                    .Where(d => d != themeDict && d.Source?.OriginalString?.Contains("Resources/Themes/") == true)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"🔄 Removing {existingThemes.Count} existing theme dictionaries");
                foreach (var theme in existingThemes)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(theme);
                }

                System.Diagnostics.Debug.WriteLine($"✅ {themeName} applied successfully");

                // 🔄 Force UI refresh
                foreach (Window window in Application.Current.Windows)
                {
                    window.InvalidateVisual();
                    window.UpdateLayout();
                }

                OnThemeChanged?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error applying theme: {ex.Message}");
                // Fallback to light theme if there's an error
                if (darkMode)
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Falling back to light theme");
                    ApplyTheme(false);
                }
            }
        }

        // 📖 Load theme from settings
        public void LoadTheme(bool darkMode)
        {
            if (darkMode != isDarkMode)
            {
                ApplyTheme(darkMode);
            }
        }

        // 🚀 Initialize theme system (call this early in app startup)
        public void InitializeTheme()
        {
            // Ensure light theme is properly loaded at startup
            ApplyTheme(false);
        }
    }
} 