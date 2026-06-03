using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SnipShottyBoard.UI
{
    // 📊 StatusBarManager - Handles status bar updates and statistics
    public class StatusBarManager
    {
        private static readonly SolidColorBrush _savedBrush;
        private static readonly SolidColorBrush _unsavedBrush;
        private static readonly SolidColorBrush _errorBrush;

        static StatusBarManager()
        {
            _savedBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            _savedBrush.Freeze();
            _unsavedBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            _unsavedBrush.Freeze();
            _errorBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            _errorBrush.Freeze();
        }

        private readonly TextBlock tabCountStatus;
        private readonly TextBlock wordCountStatus;
        private readonly TextBlock saveStatus;
        private readonly TextBlock timeStatus;

        public StatusBarManager(TextBlock tabCountStatus, TextBlock wordCountStatus, 
                               TextBlock saveStatus, TextBlock timeStatus)
        {
            this.tabCountStatus = tabCountStatus ?? throw new ArgumentNullException(nameof(tabCountStatus));
            this.wordCountStatus = wordCountStatus ?? throw new ArgumentNullException(nameof(wordCountStatus));
            this.saveStatus = saveStatus ?? throw new ArgumentNullException(nameof(saveStatus));
            this.timeStatus = timeStatus ?? throw new ArgumentNullException(nameof(timeStatus));
        }

        // 📊 Update all status bar elements
        public void UpdateStatusBar(int tabCount, string currentTabText, bool hasUnsavedChanges)
        {
            // 📑 Update tab count
            tabCountStatus.Text = tabCount == 1 ? "1 tab" : $"{tabCount} tabs";

            // 📝 Update word count for current tab
            var wordCount = CountWords(currentTabText);
            wordCountStatus.Text = wordCount == 1 ? "1 word" : $"{wordCount} words";

            // 💾 Update save status with theme-aware colors
            saveStatus.Text = hasUnsavedChanges ? "Unsaved" : "Saved";
            saveStatus.Foreground = hasUnsavedChanges ? _unsavedBrush : _savedBrush;

            // 🕒 Update current time
            timeStatus.Text = DateTime.Now.ToString("h:mm tt");
        }

        // ⚠️ Show save error in status bar for 5 seconds, then revert
        public void ShowSaveError()
        {
            saveStatus.Text = "⚠️ Save failed";
            saveStatus.Foreground = _errorBrush;

            var revertTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            revertTimer.Tick += (s, e) =>
            {
                revertTimer.Stop();
                saveStatus.Text = "Unsaved";
                saveStatus.Foreground = _unsavedBrush;
            };
            revertTimer.Start();
        }

        // 📝 Count words in text (simple word counting)
        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
} 