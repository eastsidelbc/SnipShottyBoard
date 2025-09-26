using System;
using System.Collections.Generic;

namespace SnipShottyBoard.Data
{
    // 📝 SavedNote - Represents ONE individual note tab
    // 
    // WHAT THIS FILE DOES:
    // This class represents a single note tab that you see in the app.
    // Each tab you create gets converted into one of these SavedNote objects
    // when the app saves your data.
    // 
    // THINK OF IT LIKE:
    // A digital index card that contains:
    // - The title written on the tab (like "Shopping List" or "Meeting Notes")
    // - All the text you typed in that note
    // - References to any images you pasted into that note
    // - The position/order of the tab (first tab, second tab, etc.)
    // 
    // REAL-WORLD EXAMPLE:
    // If you have a tab called "Grocery List" with the text "Milk, Bread, Eggs"
    // and you pasted a picture of a shopping cart, this class would store:
    // - Title: "Grocery List"
    // - TextContent: "Milk, Bread, Eggs"
    // - ImageFiles: ["path/to/shopping_cart_image.png"]
    // - TabOrder: 0 (if it's the first tab)
    // 
    // WHY WE NEED THIS:
    // This is how the app remembers each individual note when you close and reopen it.
    // Without this, all your notes would disappear every time you close the app.
    public class SavedNote
    {
        // 🏷️ Tab Title - The name displayed on the tab
        // 
        // This is the text you see on the actual tab button at the top of the app.
        // Examples: "Shopping List", "Meeting Notes", "Ideas", "To Do"
        // 
        // When you right-click a tab and choose "Rename", you're changing this value.
        // If you don't rename a tab, it gets a default name like "Note 1", "Note 2", etc.
        public string Title { get; set; } = string.Empty;

        // 📄 Note Content - All the text you typed in this note
        // 
        // This contains everything you've typed in the main text area of the note.
        // It preserves:
        // - All your text exactly as you typed it
        // - Line breaks and paragraphs
        // - Spacing and formatting
        // 
        // Example: If you typed "Buy milk\nCall mom\nFinish project", 
        // this property would contain exactly that text with the line breaks.
        public string TextContent { get; set; } = string.Empty;

        // 📄 Rich Text Content - RTF formatted text with styling
        // 
        // This contains the rich text formatting (bold, italic, underline, etc.)
        // stored in RTF (Rich Text Format) format. This allows for:
        // - Bold, italic, underline, strikethrough formatting
        // - Bullet points and indentation
        // - Preserved formatting when saving/loading
        // 
        // If this is empty, the app falls back to TextContent for plain text.
        public string RichTextContent { get; set; } = string.Empty;

        // 🖼️ Image References - List of images you pasted into this note
        // 
        // This list contains the file paths to any images you've pasted into this note.
        // When you press Ctrl+V to paste an image, the app:
        // 1. Saves the image as a file on your computer
        // 2. Adds the path to that file to this list
        // 
        // Example paths might look like:
        // - "C:\Users\YourName\AppData\Roaming\SnipShottyBoard\images\image_20231215_143022_123.png"
        // - "C:\Users\YourName\AppData\Roaming\SnipShottyBoard\images\image_20231215_143055_456.png"
        // 
        // WHY STORE PATHS INSTEAD OF THE ACTUAL IMAGES:
        // Image files can be very large. Instead of storing the actual image data
        // in this text file, we store references (paths) to where the images are saved.
        // This keeps the save file small and fast to load.
        public List<string> ImageFiles { get; set; } = new List<string>();

        // 🕒 Image Timestamps - When each image was added
        // 
        // This dictionary maps image file paths to when they were added to the note.
        // This allows us to show "Added: 2 hours ago" under each image for better organization.
        public Dictionary<string, DateTime> ImageTimestamps { get; set; } = new Dictionary<string, DateTime>();

        // 📊 Tab Order - The position of this tab in the tab strip
        // 
        // This number determines where this tab appears in the row of tabs.
        // - 0 = First tab (leftmost)
        // - 1 = Second tab
        // - 2 = Third tab, and so on...
        // 
        // When you drag and drop tabs to reorder them, this value gets updated
        // to reflect the new position.
        // 
        // WHY THIS MATTERS:
        // When you reopen the app, the tabs need to appear in the same order
        // you had them arranged. This number ensures that happens.
        public int TabOrder { get; set; } = 0;
    }
} 