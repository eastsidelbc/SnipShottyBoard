using System;
using System.Windows;
using System.Windows.Media;

namespace SnipShottyBoard.UI
{
    // 🎭 CustomDialog - Beautiful, themed dialog windows
    public partial class CustomDialog : Window
    {
        #region Dialog Result Properties
        public new bool DialogResult { get; private set; } = false;
        public bool DontAskAgain { get; private set; } = false;
        #endregion

        #region Constructor
        public CustomDialog()
        {
            InitializeComponent();
            
            // 🎯 Set up keyboard navigation
            this.KeyDown += (s, e) => {
                if (e.Key == System.Windows.Input.Key.Escape) Cancel_Click(null, null);
                if (e.Key == System.Windows.Input.Key.Enter) Confirm_Click(null, null);
            };
        }
        #endregion

        #region Static Factory Methods
        // ❓ Show confirmation dialog with "don't ask again" option
        public static (bool confirmed, bool dontAskAgain) ShowConfirmation(
            Window owner,
            string message,
            string title = "Confirmation",
            string icon = "❓",
            bool showDontAskAgain = false,
            string confirmText = "✅ Yes",
            string cancelText = "❌ Cancel")
        {
            var dialog = new CustomDialog();
            dialog.Owner = owner;
            dialog.SetupDialog(message, title, icon, showDontAskAgain, confirmText, cancelText);
            
            dialog.ShowDialog();
            return (dialog.DialogResult, dialog.DontAskAgain);
        }

        // 🗑️ Show delete confirmation (specialized for delete operations)
        public static (bool confirmed, bool dontAskAgain) ShowDeleteConfirmation(
            Window owner,
            string itemName,
            bool showDontAskAgain = true)
        {
            var message = $"Are you sure you want to delete '{itemName}'?\n\nThis action cannot be undone.";
            return ShowConfirmation(
                owner, 
                message, 
                "Delete Confirmation", 
                "🗑️", 
                showDontAskAgain,
                "🗑️ Delete",
                "❌ Cancel");
        }

        // ℹ️ Show information dialog
        public static void ShowInformation(
            Window owner,
            string message,
            string title = "Information",
            string icon = "ℹ️")
        {
            var dialog = new CustomDialog();
            dialog.Owner = owner;
            dialog.SetupInformationDialog(message, title, icon);
            dialog.ShowDialog();
        }

        // ⚠️ Show warning dialog
        public static void ShowWarning(
            Window owner,
            string message,
            string title = "Warning",
            string icon = "⚠️")
        {
            var dialog = new CustomDialog();
            dialog.Owner = owner;
            dialog.SetupInformationDialog(message, title, icon);
            
            // 🟡 Change confirm button to warning color
            dialog.ConfirmButton.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            dialog.ConfirmButton.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            dialog.ConfirmButton.Foreground = new SolidColorBrush(Colors.Black);
            
            dialog.ShowDialog();
        }

        // ❌ Show error dialog
        public static void ShowError(
            Window owner,
            string message,
            string title = "Error",
            string icon = "❌")
        {
            var dialog = new CustomDialog();
            dialog.Owner = owner;
            dialog.SetupInformationDialog(message, title, icon);
            
            // 🔴 Change confirm button to error color
            dialog.ConfirmButton.Background = new SolidColorBrush(Color.FromRgb(220, 38, 127));
            dialog.ConfirmButton.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 127));
            
            dialog.ShowDialog();
        }
        #endregion

        #region Setup Methods
        // 🔧 Setup confirmation dialog
        private void SetupDialog(
            string message, 
            string title, 
            string icon, 
            bool showDontAskAgain,
            string confirmText,
            string cancelText)
        {
            DialogIcon.Text = icon;
            DialogTitle.Text = title;
            DialogMessage.Text = message;
            ConfirmButton.Content = confirmText;
            CancelButton.Content = cancelText;
            
            // 📋 Show/hide checkbox based on parameter
            CheckboxSection.Visibility = showDontAskAgain ? Visibility.Visible : Visibility.Collapsed;
            
            // 📏 Adjust height based on content
            AdjustDialogSize();
        }

        // 🔧 Setup information-only dialog (OK button only)
        private void SetupInformationDialog(string message, string title, string icon)
        {
            DialogIcon.Text = icon;
            DialogTitle.Text = title;
            DialogMessage.Text = message;
            
            // 🔄 Hide cancel button and change confirm to "OK"
            CancelButton.Visibility = Visibility.Collapsed;
            ConfirmButton.Content = "✅ OK";
            ConfirmButton.Margin = new Thickness(0);
            
            // 🚫 Hide checkbox for info dialogs
            CheckboxSection.Visibility = Visibility.Collapsed;
            
            // 📏 Adjust height based on content
            AdjustDialogSize();
        }

        // 📏 Automatically adjust dialog size based on content
        private void AdjustDialogSize()
        {
            try
            {
                // 📐 Measure text to determine required height
                var textLength = DialogMessage.Text?.Length ?? 0;
                var lineCount = Math.Max(1, textLength / 50); // Rough estimate
                
                var baseHeight = 200;
                var contentHeight = Math.Max(60, lineCount * 25);
                var checkboxHeight = CheckboxSection.Visibility == Visibility.Visible ? 40 : 0;
                
                this.Height = Math.Min(300, baseHeight + contentHeight + checkboxHeight);
                
                // 📐 Adjust width for longer messages
                if (textLength > 100)
                {
                    this.Width = Math.Min(600, Math.Max(400, textLength * 4));
                }
            }
            catch
            {
                // 🛡️ Fallback to default size if calculation fails
                this.Height = 200;
                this.Width = 400;
            }
        }
        #endregion

        #region Event Handlers
        // ✅ Confirm button clicked
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            DontAskAgain = DontAskAgainCheckBox.IsChecked ?? false;
            this.Close();
        }

        // ❌ Cancel button clicked
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            DontAskAgain = false;
            this.Close();
        }
        #endregion

        #region Window Event Overrides
        // 🚫 Prevent closing with Alt+F4 or X button (force button usage)
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Allow closing - we'll handle the result appropriately
            base.OnClosing(e);
        }

        // 🎯 Center dialog on owner when shown
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 📍 Center on owner window
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.Width - Width) / 2;
                Top = Owner.Top + (Owner.Height - Height) / 2;
            }
        }
        #endregion
    }
} 