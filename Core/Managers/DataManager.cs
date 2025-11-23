using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using SnipShottyBoard.Core.Models;
using SnipShottyBoard.Core.Schema;
using SnipShottyBoard.Data;
using SnipShottyBoard.Infrastructure.Logging;
using SnipShottyBoard.Infrastructure.Helpers;

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
        
        // ✅ Phase 4 Persistence Fix: Canonical snapshot migration
        private static readonly string CanonicalSnapshotPath = Path.Combine(AppDataFolder, "notewindows-20251120-172254.json");
        private static readonly string MigrationFlagPath = Path.Combine(AppDataFolder, "notewindows_snapshot_applied.flag");

        static DataManager()
        {
            EnsureDirectoryExists();
            ApplyCanonicalSnapshotIfNeeded();
        }

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(AppDataFolder))
                Directory.CreateDirectory(AppDataFolder);
            
            if (!Directory.Exists(ImagesFolder))
                Directory.CreateDirectory(ImagesFolder);
        }
        
        /// <summary>
        /// 🔧 Phase 4 Persistence Fix: One-time migration from canonical snapshot
        /// Applies notewindows-20251120-172254.json as the canonical source, ONCE ONLY
        /// </summary>
        private static void ApplyCanonicalSnapshotIfNeeded()
        {
            try
            {
                // If flag exists, migration already completed
                if (File.Exists(MigrationFlagPath))
                {
                    LoggingService.LogDebugStatic("Canonical snapshot migration already applied (flag exists)", "Data");
                    return;
                }
                
                // If canonical snapshot doesn't exist, nothing to migrate
                if (!File.Exists(CanonicalSnapshotPath))
                {
                    LoggingService.LogDebugStatic("No canonical snapshot found, skipping migration", "Data");
                    // Create flag anyway to prevent future checks
                    File.WriteAllText(MigrationFlagPath, $"No snapshot available at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    return;
                }
                
                LoggingService.LogInfoStatic("🔧 Applying canonical snapshot migration...", "Data");
                
                // Backup current notewindows.json if it exists
                if (File.Exists(NoteWindowsFilePath))
                {
                    var backupPath = Path.Combine(AppDataFolder, $"notewindows-previous-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                    File.Copy(NoteWindowsFilePath, backupPath, overwrite: true);
                    LoggingService.LogInfoStatic($"Backed up current notewindows.json to {Path.GetFileName(backupPath)}", "Data");
                }
                
                // Copy canonical snapshot to notewindows.json
                File.Copy(CanonicalSnapshotPath, NoteWindowsFilePath, overwrite: true);
                LoggingService.LogInfoStatic($"✅ Applied canonical snapshot: {Path.GetFileName(CanonicalSnapshotPath)} → notewindows.json", "Data");
                
                // Create flag file to prevent re-migration
                File.WriteAllText(MigrationFlagPath, $"Canonical snapshot applied successfully at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nSource: {CanonicalSnapshotPath}");
                LoggingService.LogInfoStatic("✅ Migration flag created - will not run again", "Data");
                
                // Verify the migration worked
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = File.ReadAllText(NoteWindowsFilePath);
                var windows = JsonSerializer.Deserialize<List<NoteWindowData>>(json, options);
                LoggingService.LogInfoStatic($"✅ Verification: {windows?.Count ?? 0} windows loaded from migrated file", "Data");
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to apply canonical snapshot migration", ex, "Data");
                // Don't throw - let app continue with whatever data exists
            }
        }

        #region Notes Management

        /// <summary>
        /// 💾 Save notes data to file
        /// </summary>
        /// <summary>
        /// 💾 Save notes data atomically with automatic backup
        /// ✅ Phase 4D P2.3: Migrated to atomic file operations
        /// ✅ Phase 4D P2.7: Added performance timing
        /// ✅ Phase 4D P2.8: Added null guard
        /// </summary>
        public static void SaveNotes(List<SavedNote>? notes)
        {
            // ✅ Null guard
            if (notes == null)
            {
                LoggingService.LogErrorStatic("SaveNotes called with null notes list", new ArgumentNullException(nameof(notes)), "Data");
                return;
            }
            
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var success = AtomicFileManager.AtomicSave(NotesFilePath, notes, maxBackups: 20);
                stopwatch.Stop();
                
                if (success)
                {
                    LoggingService.LogInfoStatic($"Notes saved in {stopwatch.ElapsedMilliseconds}ms", "Perf", new {
                        NoteCount = notes?.Count ?? 0,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                }
                else
                {
                    LoggingService.LogErrorStatic("AtomicSave returned false for notes", null, "Data", new {
                        NoteCount = notes?.Count ?? 0,
                        FilePath = PathSanitizer.SanitizePath(NotesFilePath),
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed to save notes after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    NoteCount = notes?.Count ?? 0,
                    FilePath = PathSanitizer.SanitizePath(NotesFilePath),
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// 📥 Load notes data from file with automatic backup recovery and migration
        /// ✅ Phase 4D P2.7: Added performance timing
        /// ✅ Phase 5 P1: Migrated to use LoadWithRecovery for automatic backup fallback
        /// ✅ Phase 5 P2: Added schema migration support
        /// </summary>
        public static List<SavedNote> LoadNotes()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Use AtomicFileManager.LoadWithRecovery for automatic backup recovery
                // This automatically tries: primary → .bak → rolling backups → default
                var result = AtomicFileManager.LoadWithRecovery(
                    NotesFilePath,
                    () => new List<SavedNote>()
                );
                
                // ✅ Phase 5 P2: Apply schema migration to all notes
                result = MigrationService.MigrateNotes(result);
                
                stopwatch.Stop();
                
                LoggingService.LogInfoStatic($"Notes loaded in {stopwatch.ElapsedMilliseconds}ms", "Perf", new {
                    NoteCount = result.Count,
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed to load notes after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    FilePath = PathSanitizer.SanitizePath(NotesFilePath),
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
                return new List<SavedNote>();
            }
        }

        #endregion

        #region Note Windows Management

        /// <summary>
        /// 💾 Save note windows data to file
        /// </summary>
        /// <summary>
        /// 💾 Save note windows data atomically with automatic backup
        /// ✅ Phase 4D P2.3: Migrated to atomic file operations
        /// ✅ Phase 4D P2.7: Added performance timing
        /// </summary>
        public static void SaveNoteWindows(List<NoteWindowData> noteWindows)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var success = AtomicFileManager.AtomicSave(NoteWindowsFilePath, noteWindows, maxBackups: 20);
                stopwatch.Stop();
                
                if (success)
                {
                    LoggingService.LogInfoStatic($"Note windows saved in {stopwatch.ElapsedMilliseconds}ms", "Perf", new {
                        WindowCount = noteWindows?.Count ?? 0,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                }
                else
                {
                    LoggingService.LogErrorStatic("AtomicSave returned false for note windows", null, "Data", new {
                        WindowCount = noteWindows?.Count ?? 0,
                        FilePath = PathSanitizer.SanitizePath(NoteWindowsFilePath),
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed to save note windows after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    WindowCount = noteWindows?.Count ?? 0,
                    FilePath = PathSanitizer.SanitizePath(NoteWindowsFilePath),
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// 📥 Load note windows data from file with automatic backup recovery and migration
        /// ✅ FIXED: Added PropertyNamingPolicy.CamelCase to match JSON format
        /// ✅ Phase 4 Persistence Fix: Enhanced logging
        /// ✅ Phase 5 P1: Migrated to use LoadWithRecovery for automatic backup fallback
        /// ✅ Phase 5 P2: Added schema migration support
        /// </summary>
        public static List<NoteWindowData> LoadNoteWindows()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Use AtomicFileManager.LoadWithRecovery for automatic backup recovery
                // Note: LoadWithRecovery uses CamelCase policy by default in AtomicFileManager
                var result = AtomicFileManager.LoadWithRecovery(
                    NoteWindowsFilePath,
                    () => new List<NoteWindowData>()
                );
                
                // ✅ Phase 5 P2: Apply schema migration to all windows and their notes
                result = MigrationService.MigrateNoteWindows(result);
                
                stopwatch.Stop();
                
                LoggingService.LogInfoStatic($"✅ Note windows loaded in {stopwatch.ElapsedMilliseconds}ms", "Perf", new {
                    WindowCount = result.Count,
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
                
                foreach (var window in result)
                {
                    LoggingService.LogDebugStatic($"  - {window.Title}: {window.Notes?.Count ?? 0} notes, IsActive={window.IsActive}", "Data");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed to load note windows after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    FilePath = PathSanitizer.SanitizePath(NoteWindowsFilePath),
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
                return new List<NoteWindowData>();
            }
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// 💾 Save application settings
        /// </summary>
        /// <summary>
        /// 💾 Save application settings atomically with automatic backup
        /// ✅ Phase 4D P2.3: Migrated to atomic file operations
        /// ✅ Phase 4D P2.7: Added performance timing
        /// </summary>
        public static void SaveSettings(AppSettings settings)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var success = AtomicFileManager.AtomicSave(SettingsFilePath, settings, maxBackups: 20);
                stopwatch.Stop();
                
                if (success)
                {
                    LoggingService.LogInfoStatic($"Settings saved in {stopwatch.ElapsedMilliseconds}ms", "Perf", new {
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                }
                else
                {
                    LoggingService.LogErrorStatic("AtomicSave returned false for settings", null, "Data", new {
                        FilePath = PathSanitizer.SanitizePath(SettingsFilePath),
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed to save settings after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    FilePath = PathSanitizer.SanitizePath(SettingsFilePath),
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// 📥 Load application settings with automatic backup recovery and migration
        /// ✅ Phase 5 P1: Migrated to use LoadWithRecovery for automatic backup fallback
        /// ✅ Phase 5 P2: Added schema migration support
        /// </summary>
        public static AppSettings LoadSettings()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Use AtomicFileManager.LoadWithRecovery for automatic backup recovery
                // This automatically tries: primary → .bak → rolling backups → default
                var settings = AtomicFileManager.LoadWithRecovery(
                    SettingsFilePath,
                    () => new AppSettings()
                );
                
                // ✅ Phase 5 P2: Apply schema migration to settings
                settings = MigrationService.MigrateAppSettings(settings);
                
                stopwatch.Stop();
                
                LoggingService.LogInfoStatic($"Settings loaded in {stopwatch.ElapsedMilliseconds}ms", "Perf", new {
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
                
                return settings;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed to load settings after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    FilePath = PathSanitizer.SanitizePath(SettingsFilePath),
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
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
                LoggingService.LogErrorStatic("Failed to save app data", ex, "Data", new {
                    NoteCount = appData?.Notes?.Count ?? 0,
                    HasSettings = appData?.Settings != null
                });
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
                LoggingService.LogErrorStatic("Failed to load app data", ex, "Data");
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
                LoggingService.LogErrorStatic("Failed to save clipboard image", ex, "Data");
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
                LoggingService.LogErrorStatic("Failed to copy dropped image", ex, "Data", new {
                    SourceFile = PathSanitizer.SanitizePath(sourcePath)
                });
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
                    LoggingService.LogDebugStatic("Image deleted", "Data", new {
                        FileName = PathSanitizer.SanitizePath(imagePath)
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to delete image", ex, "Data", new {
                    FileName = PathSanitizer.SanitizePath(imagePath)
                });
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
        /// <summary>
        /// 🗑️ Clean up orphaned image files (not referenced in any note)
        /// ✅ Phase 4D P2.7: Added performance timing
        /// </summary>
        /// <param name="daysGracePeriod">Only delete files older than this many days (default 7)</param>
        /// <returns>Number of orphaned images deleted</returns>
        public static int CleanupOrphanedImages(int daysGracePeriod = 7)
        {
            var deletedCount = 0;
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                if (!Directory.Exists(ImagesFolder))
                {
                    LoggingService.LogDebugStatic("Images folder does not exist, skipping cleanup", "Data");
                    return 0;
                }

                // Load all notes to get referenced image paths
                var notes = LoadNotes();
                var referencedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var note in notes)
                {
                    if (note.ImageFiles != null)
                    {
                        foreach (var imagePath in note.ImageFiles)
                        {
                            // Store full path for comparison
                            referencedImages.Add(Path.GetFullPath(imagePath));
                        }
                    }
                }

                LoggingService.LogDebugStatic($"Found {referencedImages.Count} referenced images across {notes.Count} notes", "Data");

                // Find orphaned images
                var cutoffDate = DateTime.Now.AddDays(-daysGracePeriod);
                var allImageFiles = Directory.GetFiles(ImagesFolder);
                
                foreach (var imageFile in allImageFiles)
                {
                    var fullPath = Path.GetFullPath(imageFile);
                    
                    // Skip if referenced in any note
                    if (referencedImages.Contains(fullPath))
                        continue;

                    // Check if file is old enough (grace period)
                    var fileInfo = new FileInfo(imageFile);
                    if (fileInfo.CreationTime >= cutoffDate)
                    {
                        LoggingService.LogDebugStatic($"Orphaned image too recent, skipping: {PathSanitizer.SanitizePath(imageFile)}", "Data");
                        continue;
                    }

                    // Delete orphaned image
                    try
                    {
                        File.Delete(imageFile);
                        deletedCount++;
                        LoggingService.LogDebugStatic($"Deleted orphaned image: {PathSanitizer.SanitizePath(imageFile)}", "Data");
                    }
                    catch (Exception deleteEx)
                    {
                        LoggingService.LogErrorStatic($"Failed to delete orphaned image: {PathSanitizer.SanitizePath(imageFile)}", deleteEx, "Data");
                    }
                }

                stopwatch.Stop();
                
                if (deletedCount > 0)
                {
                    LoggingService.LogInfoStatic($"Cleanup completed in {stopwatch.ElapsedMilliseconds}ms: Deleted {deletedCount} orphaned image(s)", "Perf", new {
                        DeletedCount = deletedCount,
                        GracePeriodDays = daysGracePeriod,
                        TotalImagesChecked = allImageFiles.Length,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                }
                else
                {
                    LoggingService.LogDebugStatic($"Cleanup completed in {stopwatch.ElapsedMilliseconds}ms: No orphaned images (checked {allImageFiles.Length} files)", "Perf");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed during orphaned image cleanup after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    DaysGracePeriod = daysGracePeriod,
                    DeletedBeforeError = deletedCount,
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
            }

            return deletedCount;
        }

        /// <summary>
        /// 🗑️ Legacy method - Cleans all old files regardless of reference status
        /// </summary>
        [Obsolete("Use CleanupOrphanedImages() instead - this deletes ALL old files, not just orphaned ones")]
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
                LoggingService.LogErrorStatic("Failed during cleanup", ex, "Data", new {
                    DaysToKeep = daysToKeep
                });
            }
        }

        #endregion
    }
} 