using System;
using System.Windows;
using System.Windows.Input;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 📝 CustomInputDialog - Beautiful, themed input dialog windows
    /// Provides consistent styling with other app dialogs
    /// </summary>
    public partial class CustomInputDialog : Window
    {
        #region Properties
        public new bool DialogResult { get; private set; } = false;
        public string UserInput { get; private set; } = string.Empty;
        #endregion

        #region Constructor
        public CustomInputDialog()
        {
            InitializeComponent();
            
            // 🎯 Set up keyboard navigation
            this.KeyDown += (s, e) => {
                if (e.Key == Key.Escape) Cancel_Click(null, null);
                if (e.Key == Key.Enter) Ok_Click(null, null);
            };

            // 🎯 Focus the input textbox when dialog loads
            this.Loaded += (s, e) => {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }
        #endregion

        #region Static Factory Methods
        /// <summary>
        /// 📝 Show input dialog for text entry
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="message">Prompt message</param>
        /// <param name="title">Dialog title</param>
        /// <param name="defaultValue">Default input value</param>
        /// <param name="icon">Icon emoji (optional)</param>
        /// <returns>Tuple of (success, userInput)</returns>
        public static (bool success, string input) ShowInput(
            Window owner,
            string message,
            string title = "Input Required",
            string defaultValue = "",
            string icon = "✏️")
        {
            var dialog = new CustomInputDialog();
            dialog.Owner = owner;
            dialog.SetupDialog(message, title, defaultValue, icon);
            
            dialog.ShowDialog();
            return (dialog.DialogResult, dialog.UserInput);
        }

        /// <summary>
        /// 📝 Show rename dialog (specialized for rename operations)
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="currentName">Current name to rename</param>
        /// <param name="itemType">Type of item being renamed (optional)</param>
        /// <returns>Tuple of (success, newName)</returns>
        public static (bool success, string newName) ShowRename(
            Window owner,
            string currentName,
            string itemType = "item")
        {
            var message = $"Enter new name for the {itemType}:";
            return ShowInput(
                owner, 
                message, 
                $"Rename {char.ToUpper(itemType[0])}{itemType.Substring(1)}", 
                currentName,
                "✏️");
        }
        #endregion

        #region Setup Methods
        /// <summary>
        /// 🔧 Setup the input dialog
        /// </summary>
        /// <param name="message">Prompt message</param>
        /// <param name="title">Dialog title</param>
        /// <param name="defaultValue">Default input value</param>
        /// <param name="icon">Icon emoji</param>
        private void SetupDialog(string message, string title, string defaultValue, string icon)
        {
            DialogIcon.Text = icon;
            DialogTitle.Text = title;
            DialogMessage.Text = message;
            InputTextBox.Text = defaultValue;

            // 📏 Adjust dialog size based on content
            AdjustDialogSize();
        }

        /// <summary>
        /// 📏 Automatically adjust dialog size based on content
        /// </summary>
        private void AdjustDialogSize()
        {
            try
            {
                // 📐 Measure text to determine required height
                var messageLength = DialogMessage.Text?.Length ?? 0;
                var titleLength = DialogTitle.Text?.Length ?? 0;
                
                // Base height + content adjustments
                var baseHeight = 280;
                var extraHeight = Math.Max(0, (messageLength - 50) / 50) * 20;
                
                this.Height = Math.Min(350, baseHeight + extraHeight);
                
                // 📐 Adjust width for longer content
                var maxLength = Math.Max(messageLength, titleLength);
                if (maxLength > 50)
                {
                    this.Width = Math.Min(650, Math.Max(450, maxLength * 6));
                }
            }
            catch
            {
                // 🛡️ Fallback to default size if calculation fails
                this.Height = 280;
                this.Width = 450;
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// ✅ OK button clicked
        /// </summary>
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            UserInput = InputTextBox.Text ?? string.Empty;
            DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// ❌ Cancel button clicked
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            UserInput = string.Empty;
            DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// 🔒 Prevent closing without user action
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Allow closing through buttons or keyboard shortcuts
            base.OnClosing(e);
        }

        /// <summary>
        /// 🎨 Apply theme styling when dialog is shown
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            try
            {
                // 🎨 Ensure proper theme application
                this.InvalidateVisual();
                this.UpdateLayout();
            }
            catch
            {
                // 🛡️ Silently handle any theme application errors
            }
        }
        #endregion
    }
} 