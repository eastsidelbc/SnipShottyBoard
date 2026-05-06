using System;
using System.Windows;

namespace SnipShottyBoard.UI
{
    // 🎨 ThemeManager - Dark theme only
    // Light theme support has been removed. The app is dark mode only.
    public class ThemeManager
    {
        public event Action? OnThemeChanged;

        // 🌙 Apply dark theme (the only theme)
        public void ApplyTheme(bool darkMode)
        {
            // Dark mode is the only option now — no-op
        }

        // 📖 Load theme from settings
        public void LoadTheme(bool darkMode)
        {
            // Dark mode is the only option now — no-op
        }

        // 🚀 Initialize theme system (call this early in app startup)
        public void InitializeTheme()
        {
            // Dark theme is applied via App.xaml — no runtime switching needed
        }
    }
}
