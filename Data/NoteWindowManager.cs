using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.Data
{
    /// <summary>
    /// 📝 Manages multiple note windows like Windows Sticky Notes
    /// </summary>
    public class NoteWindowManager
    {
        private static NoteWindowManager _instance;
        public static NoteWindowManager Instance => _instance ??= new NoteWindowManager();

        public ObservableCollection<NoteWindowData> NoteWindows { get; }
        public event Action<NoteWindowData> WindowCreated;
        public event Action<NoteWindowData> WindowClosed;

        private NoteWindowManager()
        {
            NoteWindows = new ObservableCollection<NoteWindowData>();
            LoadNoteWindows();
        }

        // 🆕 Create a new note window
        public NoteWindowData CreateNewNoteWindow(string title = null)
        {
            var windowData = new NoteWindowData
            {
                Id = Guid.NewGuid(),
                Title = title ?? $"Note {NoteWindows.Count + 1}",
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                IsActive = true
            };

            NoteWindows.Add(windowData);
            WindowCreated?.Invoke(windowData);
            SaveNoteWindows();
            return windowData;
        }

        // 🗑️ Close a note window
        public void CloseNoteWindow(Guid windowId)
        {
            var window = NoteWindows.FirstOrDefault(w => w.Id == windowId);
            if (window != null)
            {
                window.IsActive = false;
                WindowClosed?.Invoke(window);
                SaveNoteWindows();
            }
        }

        // 💾 Save note windows data
        public void SaveNoteWindows()
        {
            try
            {
                DataManager.SaveNoteWindows(NoteWindows.ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving note windows: {ex.Message}");
            }
        }

        // 📥 Load note windows data
        private void LoadNoteWindows()
        {
            try
            {
                var windows = DataManager.LoadNoteWindows();
                
                foreach (var window in windows.Where(w => w.IsActive))
                {
                    NoteWindows.Add(window);
                }

                // Don't create default window automatically - let the main window handle existing data first
                System.Diagnostics.Debug.WriteLine($"📥 Loaded {NoteWindows.Count} note windows");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading note windows: {ex.Message}");
                // Don't create default window on error either
            }
        }

        // 🔍 Get active note windows
        public List<NoteWindowData> GetActiveWindows()
        {
            return NoteWindows.Where(w => w.IsActive).ToList();
        }
    }

    /// <summary>
    /// 📄 Data structure for a note window
    /// </summary>
    public class NoteWindowData
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsActive { get; set; }
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;
        public double WindowWidth { get; set; } = 800;
        public double WindowHeight { get; set; } = 600;
        public List<SavedNote> Notes { get; set; } = new List<SavedNote>();
    }
} 