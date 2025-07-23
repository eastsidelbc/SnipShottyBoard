using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace SnipShottyBoard.UI
{
    // 📊 StatusBarManager - Handles status bar updates and statistics
    public class StatusBarManager
    {
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
            if (hasUnsavedChanges)
            {
                saveStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Orange for unsaved
            }
            else
            {
                saveStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green for saved
            }

            // 🕒 Update current time
            timeStatus.Text = DateTime.Now.ToString("h:mm tt");
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