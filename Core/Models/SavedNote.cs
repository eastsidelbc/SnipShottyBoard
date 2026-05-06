using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnipShottyBoard.Core.Models
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
        // 📌 Data Version - Format version of this saved note
        // 
        // ✅ Phase 4D P2.4: Data versioning for future migrations
        // This allows the app to detect if a note was saved by a newer or older version
        // and handle migrations gracefully without data loss.
        // 
        // Current version: 1 (initial format)
        public int DataVersion { get; set; } = 1;

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

        // 🖼️ Media References - Structured list of media items in this note
        // 
        // ✅ Sprint A Phase A.2: Replaces flat ImageFiles + ImageTimestamps
        // Each entry stores just the filename (not a full path) and when it was added.
        // Full paths are resolved at runtime via DataManager.GetImagesFolder().
        // 
        // Example stored JSON:
        //   [ { "filename": "img_20260425_001234_abc.png", "dateAdded": "2026-04-25T00:12:34" } ]
        public List<MediaReference> Media { get; set; } = new List<MediaReference>();

        // ─── Backward-compatible accessors (deprecated — use Media) ───

        // 🖼️ Legacy: full-path list — computed from Media for backward compat
        // 
        // ⚠️ Deprecated — consumers should migrate to Media directly.
        // This getter resolves filenames → full paths at runtime.
        // The setter converts full paths back into Media entries.
        [System.Obsolete("Use Media property instead. This will be removed in v1.0.")]
        public List<string> ImageFiles
        {
            get => Media.Select(m => m.FullPath).ToList();
            set
            {
                if (value == null || value.Count == 0) return;

                // Clear existing entries before repopulating — prevents duplicates
                // on repeated setter calls (e.g., LoadTabs → ImageFiles = data)
                Media.Clear();

                foreach (var path in value)
                {
                    var fileName = Path.GetFileName(path);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        // Dedup: skip if this filename already exists
                        if (!Media.Any(m => m.Filename == fileName))
                            Media.Add(new MediaReference { Filename = fileName, DateAdded = DateTime.Now });
                    }
                }
            }
        }

        // 🕒 Legacy: timestamp dict — computed from Media for backward compat
        // 
        // ⚠️ Deprecated — consumers should migrate to Media directly.
        [System.Obsolete("Use Media property instead. This will be removed in v1.0.")]
        public Dictionary<string, DateTime> ImageTimestamps
        {
            get
            {
                var dict = new Dictionary<string, DateTime>();
                foreach (var m in Media)
                    dict[m.FullPath] = m.DateAdded;
                return dict;
            }
            set
            {
                if (value == null || value.Count == 0) return;

                foreach (var kvp in value)
                {
                    var fileName = Path.GetFileName(kvp.Key);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var existing = Media.FirstOrDefault(m => m.Filename == fileName);
                        if (existing != null)
                        {
                            // Update timestamp on existing entry
                            existing.DateAdded = kvp.Value;
                        }
                        else
                        {
                            // Dedup: only add if no matching filename exists
                            // (the getter computes timestamps from Media, so this is the source of truth)
                            Media.Add(new MediaReference { Filename = fileName, DateAdded = kvp.Value });
                        }
                    }
                }
            }
        }

        // 📐 Splitter Position - Text/Media divider ratio for THIS specific tab
        // 
        // This stores the position of the splitter between TextSection and MediaSection
        // as a ratio (0.0 to 1.0) for THIS individual tab only.
        // 
        // Each tab can have its own custom splitter position:
        // - Tab 1 might be 30/70 (more focus on media)
        // - Tab 2 might be 70/30 (more focus on text)
        // - Tab 3 might be 50/50 (equal split)
        // 
        // Defaults to 0.5 (50/50) if not set.
        public double SplitterTextMediaRatio { get; set; } = 0.5;

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