using System.Windows.Controls;

namespace SnipShottyBoard.UI
{
    // 🏷️ CustomTab - Links a tab button with its note content
    // WHAT IT DOES: Connects the clickable tab button you see at the top 
    // with the actual note content that displays when you click it.
    // THINK OF IT LIKE: A file folder that has a label on the tab 
    // and contains the actual documents inside.
    public class CustomTab
    {
        // 📝 The text shown on the tab button (e.g., "Shopping List")
        public required string Title { get; set; }

        // 📄 The actual note content (text area + images) that shows when tab is selected
        public required NoteTab Content { get; set; }

        // 🔘 The clickable button that appears in the tab strip at the top
        public required Button HeaderButton { get; set; }
    }
} 