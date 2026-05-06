using SnipShottyBoard.Data;

namespace SnipShottyBoard.Core.Models
{
    // ⚙️ AppSettings - Your Personal Preferences Storage
    // 
    // WHAT THIS FILE DOES:
    // This class stores all your personal preferences and remembers how you like
    // the app to look and behave. Every time you change something (like switching
    // to dark mode or resizing the window), these settings get updated and saved.
    // 
    // THINK OF IT LIKE:
    // Your personal profile or settings card that the app reads when it starts up
    // to restore everything exactly how you left it.
    // 
    // EXAMPLES OF WHAT IT REMEMBERS:
    // - "I prefer dark mode"
    // - "I had the window positioned in the top-right corner"
    // - "I was looking at tab #2 when I closed the app"
    // - "My window was 800 pixels wide and 600 pixels tall"
    // - "I want auto-save every 10 seconds"
    // - "I prefer large font size"
    // 
    // WHY THIS IS IMPORTANT:
    // Without this, every time you open the app it would reset to default settings.
    // This makes the app remember your preferences between sessions.
    public class AppSettings
    {
        #region Schema Versioning
        /// <summary>
        /// 📌 Schema version for application settings
        /// ✅ Phase 5 P2: Added for schema versioning and migration support
        /// Current version: 1 (initial format)
        /// </summary>
        public int SettingsVersion { get; set; } = 1;
        #endregion

        #region Appearance Settings
        // 🌙 Theme Preference — always Dark
        // Light theme support has been removed. The app is dark mode only.
        public string Theme { get; set; } = "Dark";

        // 🌙 Legacy Dark Mode Support — always true
        public bool IsDarkMode 
        { 
            get => true; 
            set { /* No-op — dark mode only */ } 
        }

        // 🔤 Font Size - How big the text appears
        // 
        // This stores your preferred font size in points.
        // Common values:
        // - 12 = Small text
        // - 14 = Medium text (default)
        // - 16 = Large text
        // - 18 = Extra large text
        // 
        // Affects all text in notes and UI elements.
        public int FontSize { get; set; } = 14;
        #endregion

        #region Auto-Save Settings
        // 💾 Auto-Save Enabled - Whether automatic saving is on
        // 
        // This boolean controls if the app automatically saves your notes.
        // - true = App saves changes automatically (recommended)
        // - false = You must save manually (not recommended)
        // 
        // Auto-save prevents losing work if the app crashes or closes unexpectedly.
        public bool AutoSaveEnabled { get; set; } = true;

        // ⏰ Auto-Save Interval - How often to save automatically
        // 
        // This number controls how many seconds between auto-saves.
        // Common values:
        // - 3 = Save every 3 seconds (very frequent)
        // - 5 = Save every 5 seconds (default, good balance)
        // - 10 = Save every 10 seconds (less frequent)
        // - 30 = Save every 30 seconds (minimal)
        // 
        // Lower numbers = more frequent saves = less chance of losing work
        public int AutoSaveIntervalSeconds { get; set; } = 5;
        #endregion

        #region Window Settings
        // 💻 Always on Top - Keep window above other applications
        // 
        // This boolean controls if the app window stays above all other windows.
        // - true = Window always visible on top (useful for quick notes)
        // - false = Window can be hidden behind other apps (normal behavior)
        // 
        // Useful when you want the notes app always visible while working.
        public bool AlwaysOnTop { get; set; } = false;

        // 📏 Window Width - How wide your window was
        // 
        // This stores the width of your window in pixels.
        // For example, if your window was 800 pixels wide, this value would be 800.
        // 
        // When you reopen the app, it will restore the window to this exact width.
        public double WindowWidth { get; set; } = 400;

        // 📏 Window Height - How tall your window was
        // 
        // This stores the height of your window in pixels.
        // For example, if your window was 600 pixels tall, this value would be 600.
        // 
        // When you reopen the app, it will restore the window to this exact height.
        public double WindowHeight { get; set; } = 500;

        // 📍 Window X Position - Where your window was horizontally
        // 
        // This stores how far from the left edge of your screen the window was positioned.
        // Measured in pixels from the left edge of your monitor.
        // 
        // For example:
        // - 0 = Window was at the very left edge of screen
        // - 100 = Window was 100 pixels from the left edge
        // 
        // This helps restore the window to the exact same spot on your screen.
        public double WindowLeft { get; set; } = AppConstants.DefaultWindowLeft;

        // 📍 Window Y Position - Where your window was vertically
        // 
        // This stores how far from the top edge of your screen the window was positioned.
        // Measured in pixels from the top edge of your monitor.
        // 
        // For example:
        // - 0 = Window was at the very top of screen
        // - 50 = Window was 50 pixels from the top edge
        // 
        // Combined with WindowLeft, this restores the window to its exact position.
        public double WindowTop { get; set; } = AppConstants.DefaultWindowTop;

        // 📐 Splitter Position - Text/Media divider ratio
        // 
        // This stores the position of the splitter between TextSection and MediaSection
        // as a ratio (0.0 to 1.0) representing the proportion of the TextSection.
        // 
        // For example:
        // - 0.5 = 50/50 split (default)
        // - 0.3 = 30% text, 70% media
        // - 0.7 = 70% text, 30% media
        // 
        // Stored as ratio (not pixels) so it scales correctly across different window sizes and DPIs.
        public double SplitterTextMediaRatio { get; set; } = AppConstants.SplitterDefaultRatio;
        #endregion

        #region Tab Settings
        // 📑 Last Selected Tab - Which tab you were viewing
        // 
        // This number represents which tab was active when you last closed the app.
        // For example:
        // - 0 = First tab was selected
        // - 1 = Second tab was selected
        // - 2 = Third tab was selected, etc.
        // 
        // When you reopen the app, it will automatically select this tab for you.
        public int SelectedTabIndex { get; set; } = 0;

        // 📊 Maximum Tabs - Limit on how many tabs you can have
        // 
        // This number controls the maximum number of tabs allowed.
        // Common values:
        // - 10 = Up to 10 tabs (conservative)
        // - 20 = Up to 20 tabs (default, good for most users)
        // - 50 = Up to 50 tabs (power user)
        // - 0 = Unlimited tabs (no limit, could impact performance)
        // 
        // Helps prevent accidentally creating too many tabs and slowing down the app.
        public int MaxTabs { get; set; } = 20;

        // ❓ Confirm Tab Deletion - Ask before deleting tabs
        // 
        // This boolean controls if the app asks for confirmation before deleting tabs.
        // - true = Show "Are you sure?" dialog (recommended, prevents accidents)
        // - false = Delete immediately without asking (faster but risky)
        // 
        // Prevents accidentally losing important notes when deleting tabs.
        public bool ConfirmTabDeletion { get; set; } = true;
        #endregion
    }
} 