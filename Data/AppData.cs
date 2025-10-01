using System.Collections.Generic;
using SnipShottyBoard.Core.Models;

namespace SnipShottyBoard.Data
{
    // 📦 AppData - The main container for ALL application data
    // 
    // WHAT THIS FILE DOES:
    // This is like the "master box" that holds everything the app needs to save.
    // When you close the app, all your notes, settings, and preferences get
    // packaged into this container and saved to a JSON file on your computer.
    // When you open the app again, this container gets unpacked to restore everything.
    // 
    // THINK OF IT LIKE:
    // A moving box that contains two smaller boxes:
    // - One box for all your notes (Notes property)
    // - One box for all your settings like theme, window size, etc. (Settings property)
    // 
    // WHY WE NEED THIS:
    // Without this container, the app would lose all your data every time you close it.
    // This ensures everything persists between app sessions.
    public class AppData
    {
        // 📝 Collection of all saved notes/tabs
        // 
        // This list contains every note tab you've created in the app.
        // Each item in this list represents one tab with its title, text content,
        // and any images you've added to it.
        // 
        // Example: If you have 3 tabs open, this list will have 3 SavedNote objects.
        public List<SavedNote> Notes { get; set; } = new List<SavedNote>();

        // ⚙️ User preferences and window state settings
        // 
        // This contains all your personal preferences like:
        // - Whether you prefer dark mode or light mode
        // - What size and position your window was when you last closed the app
        // - Which tab was selected when you closed the app
        // 
        // Think of it as your "personal preferences card" that remembers how
        // you like the app to look and behave.
        public AppSettings Settings { get; set; } = new AppSettings();
    }
} 