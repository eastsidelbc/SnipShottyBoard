using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SnipShottyBoard.Core.Models;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.Core.Managers
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
                // Use static fallback logging since DataManager shouldn't depend on UI layer
                // Use minimal fallback logging for Data layer - no dependency on UI LoggingService
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error saving notes: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error loading notes: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error saving note windows: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error loading note windows: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error saving settings: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error loading settings: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error saving app data: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error loading app data: {ex.Message}");
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
        /// 📁 Save image from clipboard to managed images folder
        /// </summary>
        /// <param name="imageSource">WPF ImageSource from clipboard</param>
        /// <returns>Full path to saved image or null if failed</returns>
        public static string? SaveImageFromClipboard(System.Windows.Media.ImageSource imageSource)
        {
            try
            {
                if (imageSource == null) return null;

                // 📁 Generate unique filename
                var fileName = $"img_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}.png";
                var fullPath = Path.Combine(ImagesFolder, fileName);

                // 💾 Save image to file - convert ImageSource to BitmapSource
                using var fileStream = new FileStream(fullPath, FileMode.Create);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                
                // Convert ImageSource to BitmapSource if needed
                if (imageSource is System.Windows.Media.Imaging.BitmapSource bitmapSource)
                {
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
                }
                else
                {
                    // Handle other ImageSource types by rendering to RenderTargetBitmap
                    var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)imageSource.Width, (int)imageSource.Height, 96, 96, 
                        System.Windows.Media.PixelFormats.Pbgra32);
                    
                    var visual = new System.Windows.Media.DrawingVisual();
                    using (var context = visual.RenderOpen())
                    {
                        context.DrawImage(imageSource, new System.Windows.Rect(0, 0, imageSource.Width, imageSource.Height));
                    }
                    renderBitmap.Render(visual);
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderBitmap));
                }
                
                encoder.Save(fileStream);
                return fullPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error saving clipboard image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 📂 Copy dropped image to managed images folder
        /// </summary>
        /// <param name="sourcePath">Source image file path</param>
        /// <returns>Destination path or null if failed</returns>
        public static string? CopyDroppedImage(string sourcePath)
        {
            try
            {
                if (!File.Exists(sourcePath)) return null;

                // 📂 Generate timestamped filename
                var fileName = Path.GetFileName(sourcePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var extension = Path.GetExtension(fileName);
                var newFileName = $"dropped_{timestamp}_{fileName}";
                var destinationPath = Path.Combine(ImagesFolder, newFileName);

                // 📋 Copy the file
                File.Copy(sourcePath, destinationPath, true);
                return destinationPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error copying dropped image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🗑️ Delete image file safely
        /// </summary>
        /// <param name="imagePath">Path to image file</param>
        /// <returns>True if deleted or file didn't exist</returns>
        public static bool DeleteImage(string imagePath)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    System.Diagnostics.Debug.WriteLine($"✅ Data: Image deleted: {imagePath}");
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Data: Error deleting image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 🔍 Check if image file exists and is accessible
        /// </summary>
        /// <param name="imagePath">Path to image file</param>
        /// <returns>True if file exists and is readable</returns>
        public static bool ValidateImageFile(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath)) return false;
                
                // Try to read the file to ensure it's accessible
                using var stream = File.OpenRead(imagePath);
                return stream.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 📊 Get image file information safely
        /// </summary>
        /// <param name="imagePath">Path to image file</param>
        /// <returns>Tuple with (exists, size, extension) or null if error</returns>
        public static (bool exists, long size, string extension)? GetImageInfo(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath)) return (false, 0, "");
                
                var fileInfo = new FileInfo(imagePath);
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                return (true, fileInfo.Length, extension);
            }
            catch
            {
                return null;
            }
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