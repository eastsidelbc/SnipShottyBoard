using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SnipShottyBoard.Core.Models;
using SnipShottyBoard.Data;
using SnipShottyBoard.Infrastructure.Logging;

namespace SnipShottyBoard.Core.Managers
{
    /// <summary>
    /// 📝 Manages multiple note windows like Windows Sticky Notes
    /// </summary>
    public class NoteWindowManager
    {
        private static NoteWindowManager? _instance;
        public static NoteWindowManager Instance => _instance ??= new NoteWindowManager();

        public ObservableCollection<NoteWindowData> NoteWindows { get; }
        public event Action<NoteWindowData>? WindowCreated;
        public event Action<NoteWindowData>? WindowClosed;

        private AppSettings? _cachedSettings;

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

        // 💾 Save note windows data via master.json
        public void SaveNoteWindows()
        {
            try
            {
                var master = new MasterData
                {
                    Windows = NoteWindows.ToList(),
                    Settings = _cachedSettings ?? new AppSettings()
                };
                DataManager.SaveMasterData(master);
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic($"Error saving note windows: {ex.Message}", ex, "Data");
            }
        }

        // 📥 Load note windows data from master.json
        private void LoadNoteWindows()
        {
            try
            {
                var master = DataManager.LoadMasterData();
                _cachedSettings = master.Settings;
                var windows = master.Windows ?? new List<NoteWindowData>();

                foreach (var window in windows.Where(w => w.IsActive))
                {
                    NoteWindows.Add(window);
                }

                // Don't create default window automatically - let the main window handle existing data first
                LoggingService.LogDebugStatic($"Loaded {NoteWindows.Count} note windows from master.json", "Data");
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic($"Error loading note windows: {ex.Message}", ex, "Data");
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
        /// <summary>
        /// 📌 Schema version for this note window data structure
        /// ✅ Phase 5 P2: Added for schema versioning and migration support
        /// Current version: 1 (initial format)
        /// </summary>
        public int SchemaVersion { get; set; } = 1;

        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// 🪟 Windows-Sticky-Notes-style "was this window open at last shutdown?"
        /// Defaults to true so existing data restores cleanly on first launch
        /// after the multi-window-restore fix. Set to false only when the user
        /// explicitly closes ONE window while others remain open. Last window
        /// closing the whole app preserves IsOpen=true so it reopens next launch.
        /// </summary>
        public bool IsOpen { get; set; } = true;

        public double WindowLeft { get; set; } = AppConstants.DefaultWindowLeft;
        public double WindowTop { get; set; } = AppConstants.DefaultWindowTop;
        public double WindowWidth { get; set; } = AppConstants.DefaultWindowWidth;
        public double WindowHeight { get; set; } = AppConstants.DefaultWindowHeight;
        public List<SavedNote> Notes { get; set; } = new List<SavedNote>();
    }
} 