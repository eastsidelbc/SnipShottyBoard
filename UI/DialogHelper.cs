using System;
using System.Windows;
using System.Windows.Controls;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.UI
{
    /// <summary>
    /// 💬 DialogHelper - Standardizes dialog creation patterns
    /// Provides consistent, themed dialogs with standard behaviors
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// ✅ Show a simple confirmation dialog
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="message">Dialog message</param>
        /// <param name="title">Dialog title</param>
        /// <param name="icon">Dialog icon (emoji)</param>
        /// <param name="confirmText">Confirm button text</param>
        /// <param name="cancelText">Cancel button text</param>
        /// <returns>True if user confirmed</returns>
        public static bool ShowConfirmation(
            Window owner,
            string message,
            string title = "Confirm",
            string icon = "❓",
            string confirmText = "✅ Yes",
            string cancelText = "❌ Cancel")
        {
            var (confirmed, _) = CustomDialog.ShowConfirmation(
                owner, message, title, icon, false, confirmText, cancelText);
            return confirmed;
        }

        /// <summary>
        /// 🗑️ Show a delete confirmation dialog with standard messaging
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="itemName">Name of item being deleted</param>
        /// <param name="showDontAskAgain">Whether to show "don't ask again" option</param>
        /// <returns>Tuple of (confirmed, dontAskAgain)</returns>
        public static (bool confirmed, bool dontAskAgain) ShowDeleteConfirmation(
            Window owner,
            string itemName,
            bool showDontAskAgain = true)
        {
            return CustomDialog.ShowDeleteConfirmation(owner, itemName, showDontAskAgain);
        }

        /// <summary>
        /// ❌ Show an error dialog with standard styling
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="message">Error message</param>
        /// <param name="title">Dialog title</param>
        /// <param name="exception">Optional exception for detailed logging</param>
        public static void ShowError(
            Window owner,
            string message,
            string title = "Error",
            Exception? exception = null)
        {
            var displayMessage = message;
            if (exception != null)
            {
                displayMessage += $"\n\nDetails: {exception.Message}";
            }

            CustomDialog.ShowError(owner, displayMessage, title);
        }

        /// <summary>
        /// ℹ️ Show an information dialog
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="message">Information message</param>
        /// <param name="title">Dialog title</param>
        /// <param name="icon">Dialog icon</param>
        public static void ShowInformation(
            Window owner,
            string message,
            string title = "Information",
            string icon = "ℹ️")
        {
            CustomDialog.ShowInformation(owner, message, title, icon);
        }

        /// <summary>
        /// 💾 Show a save confirmation dialog when closing with unsaved changes
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="itemName">Name of unsaved item</param>
        /// <returns>DialogResult indicating user choice</returns>
        public static SaveDialogResult ShowSaveConfirmation(
            Window owner,
            string itemName = "changes")
        {
            var result = MessageBox.Show(
                owner,
                $"You have unsaved {itemName}.\n\nDo you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            return result switch
            {
                MessageBoxResult.Yes => SaveDialogResult.Save,
                MessageBoxResult.No => SaveDialogResult.DontSave,
                _ => SaveDialogResult.Cancel
            };
        }

        /// <summary>
        /// 🔧 Show a custom dialog with advanced options
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="title">Dialog title</param>
        /// <param name="content">Dialog content element</param>
        /// <param name="icon">Dialog icon</param>
        /// <param name="width">Dialog width</param>
        /// <param name="height">Dialog height</param>
        /// <param name="buttons">Custom buttons configuration</param>
        /// <returns>Dialog result</returns>
        public static CustomDialogResult ShowCustomDialog(
            Window owner,
            string title,
            UIElement content,
            string icon = "🔧",
            double width = 400,
            double height = 300,
            DialogButtonConfig? buttons = null)
        {
            // Create the dialog window using UIFactory
            var dialog = UIFactory.CreateDialogWindow(title, icon, content, width, height);
            dialog.Owner = owner;

            // Add standard button panel if buttons are specified
            if (buttons != null)
            {
                AddCustomButtons(dialog, buttons);
            }

            var result = dialog.ShowDialog();
            return new CustomDialogResult 
            { 
                DialogResult = result ?? false,
                UserData = dialog.Tag
            };
        }

        /// <summary>
        /// 🎨 Show a themed message with custom styling
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Message content</param>
        /// <param name="icon">Dialog icon</param>
        /// <param name="width">Dialog width</param>
        /// <param name="height">Dialog height</param>
        public static void ShowThemedMessage(
            Window owner,
            string title,
            string message,
            string icon = "💬",
            double width = 500,
            double height = 350)
        {
                         var scrollViewer = UIFactory.CreateScrollViewer(
                 padding: new Thickness(20, 16, 20, 16));

            var messageText = UIFactory.CreateTextBlock(
                message,
                fontSize: 14,
                textWrapping: TextWrapping.Wrap);

            scrollViewer.Content = messageText;

            var dialog = UIFactory.CreateDialogWindow(title, icon, scrollViewer, width, height);

            // Add OK button
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                                 Margin = new Thickness(20, 16, 20, 16)
            };

            var okButton = UIFactory.CreateButton(
                "✅ OK",
                "PrimaryButtonStyle",
                (s, e) => dialog.Close(),
                "Close dialog");

            buttonPanel.Children.Add(okButton);

            // Add button panel to the dialog
            if (dialog.Content is Border border && border.Child is Grid grid)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var buttonBorder = UIFactory.CreateBorder(
                    backgroundKey: "HeaderBackgroundBrush",
                    borderBrushKey: "BorderBrush",
                    borderThickness: new Thickness(0, 1, 0, 0),
                    cornerRadius: new CornerRadius(0, 0, 12, 12));
                buttonBorder.Child = buttonPanel;
                Grid.SetRow(buttonBorder, 2);
                grid.Children.Add(buttonBorder);
            }

            dialog.Owner = owner;
            dialog.ShowDialog();
        }

        /// <summary>
        /// 🔧 Add custom buttons to a dialog
        /// </summary>
        private static void AddCustomButtons(Window dialog, DialogButtonConfig buttons)
        {
            // Implementation would add custom button panel to the dialog
            // This is a simplified version - full implementation would depend on dialog structure
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16)
            };

            foreach (var button in buttons.Buttons)
            {
                var btn = UIFactory.CreateButton(
                    button.Text,
                    button.Style,
                    (s, e) => {
                        dialog.Tag = button.Result;
                        dialog.DialogResult = button.IsDefault;
                        dialog.Close();
                    },
                    button.Tooltip,
                    new Thickness(8, 0, 0, 0));

                if (button.IsDefault)
                    btn.IsDefault = true;
                if (button.IsCancel)
                    btn.IsCancel = true;

                buttonPanel.Children.Add(btn);
            }

            // Add button panel to dialog (simplified - would need proper integration)
            if (dialog.Content is Panel panel)
            {
                panel.Children.Add(buttonPanel);
            }
        }
    }

    /// <summary>
    /// 📋 Configuration for custom dialog buttons
    /// </summary>
    public class DialogButtonConfig
    {
        public DialogButton[] Buttons { get; set; } = new DialogButton[0];
    }

    /// <summary>
    /// 🔘 Configuration for a single dialog button
    /// </summary>
    public class DialogButton
    {
        public string Text { get; set; } = string.Empty;
        public string Style { get; set; } = "SecondaryButtonStyle";
        public string Tooltip { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }
        public object? Result { get; set; }
    }

    /// <summary>
    /// 📊 Result from custom dialogs
    /// </summary>
    public class CustomDialogResult
    {
        public bool DialogResult { get; set; }
        public object? UserData { get; set; }
    }

    /// <summary>
    /// 💾 Result from save confirmation dialogs
    /// </summary>
    public enum SaveDialogResult
    {
        Save,
        DontSave,
        Cancel
    }
} 