using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace SnipShottyBoard.Data
{
    /// <summary>
    /// 📁 Central data management for application data persistence
    /// </summary>
    public class DataManager
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnipShottyBoard");
        
        private static readonly string NotesFilePath = Path.Combine(AppDataFolder, "notes.json");
        private static readonly string NoteWindowsFilePath = Path.Combine(AppDataFolder, "notewindows.json");
        private static readonly string ImagesFolder = Path.Combine(AppDataFolder, "images");
        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

        static DataManager()
        {
            EnsureDirectoryExists();
        }

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(AppDataFolder))
                Directory.CreateDirectory(AppDataFolder);
            
            if (!Directory.Exists(ImagesFolder))
                Directory.CreateDirectory(ImagesFolder);
        }

        #region Notes Management

        /// <summary>
        /// 💾 Save notes data to file
        /// </summary>
        public static void SaveNotes(List<SavedNote> notes)
        {
            try
            {
                var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(NotesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving notes: {ex.Message}");
            }
        }

        /// <summary>
        /// 📥 Load notes data from file
        /// </summary>
        public static List<SavedNote> LoadNotes()
        {
            try
            {
                if (!File.Exists(NotesFilePath))
                    return new List<SavedNote>();

                var json = File.ReadAllText(NotesFilePath);
                var result = JsonSerializer.Deserialize<List<SavedNote>>(json) ?? new List<SavedNote>();
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading notes: {ex.Message}");
                return new List<SavedNote>();
            }
        }

        #endregion

        #region Note Windows Management

        /// <summary>
        /// 💾 Save note windows data to file
        /// </summary>
        public static void SaveNoteWindows(List<NoteWindowData> noteWindows)
        {
            try
            {
                var json = JsonSerializer.Serialize(noteWindows, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(NoteWindowsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving note windows: {ex.Message}");
            }
        }

        /// <summary>
        /// 📥 Load note windows data from file
        /// </summary>
        public static List<NoteWindowData> LoadNoteWindows()
        {
            try
            {
                if (!File.Exists(NoteWindowsFilePath))
                    return new List<NoteWindowData>();

                var json = File.ReadAllText(NoteWindowsFilePath);
                var result = JsonSerializer.Deserialize<List<NoteWindowData>>(json) ?? new List<NoteWindowData>();
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading note windows: {ex.Message}");
                return new List<NoteWindowData>();
            }
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// 💾 Save application settings
        /// </summary>
        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// 📥 Load application settings
        /// </summary>
        public static AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading settings: {ex.Message}");
                return new AppSettings();
            }
        }

        #endregion

        #region AppData Management (Combined Notes + Settings)

        /// <summary>
        /// 💾 Save complete application data (notes + settings)
        /// </summary>
        public bool SaveAppData(AppData appData)
        {
            try
            {
                // Save notes separately for backward compatibility
                SaveNotes(appData.Notes);
                
                // Save settings separately
                SaveSettings(appData.Settings);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving app data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 📥 Load complete application data (notes + settings)
        /// </summary>
        public AppData LoadAppData()
        {
            try
            {
                var appData = new AppData
                {
                    Notes = LoadNotes(),
                    Settings = LoadSettings()
                };
                
                return appData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading app data: {ex.Message}");
                return new AppData(); // Return default data on error
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 📂 Get the images folder path
        /// </summary>
        public static string GetImagesFolder()
        {
            return ImagesFolder;
        }

        /// <summary>
        /// 🧹 Clean up old or unused data files
        /// </summary>
        public static void CleanupOldData(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                
                if (Directory.Exists(ImagesFolder))
                {
                    var files = Directory.GetFiles(ImagesFolder);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error during cleanup: {ex.Message}");
            }
        }

        #endregion
    }
} 