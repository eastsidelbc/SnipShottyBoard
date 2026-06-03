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
        
        private static readonly string MasterFilePath = Path.Combine(AppDataFolder, "master.json");
        private static readonly string NotesFilePath = Path.Combine(AppDataFolder, "notes.json");
        private static readonly string NoteWindowsFilePath = Path.Combine(AppDataFolder, "notewindows.json");
        private static readonly string ImagesFolder = Path.Combine(AppDataFolder, "images");
        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

        // ✅ Sprint A Phase A.1: Legacy consolidation flag
        private static readonly string MasterMigrationFlagPath = Path.Combine(AppDataFolder, "master_migration_applied.flag");

        static DataManager()
        {
            // Path constants only — no file I/O here.
            // File I/O happens in Initialize(), called explicitly from App.OnStartup.
        }

        /// <summary>
        /// Called once from App.OnStartup before anything else.
        /// Creates app directories and runs one-time legacy migration.
        /// Errors surface as real exceptions (not TypeInitializationException).
        /// </summary>
        public static void Initialize()
        {
            EnsureDirectoryExists();
            MigrateToMasterIfNeeded();
        }

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(AppDataFolder))
                Directory.CreateDirectory(AppDataFolder);
            
            if (!Directory.Exists(ImagesFolder))
                Directory.CreateDirectory(ImagesFolder);
        }
        
        /// <summary>
        /// Sprint A Phase A.1: One-time migration from legacy files to master.json
        /// On first run after this change, consolidates notewindows.json + settings.json into master.json.
        /// </summary>
        private static void MigrateToMasterIfNeeded()
        {
            try
            {
                if (File.Exists(MasterMigrationFlagPath))
                {
                    LoggingService.LogDebugStatic("Master migration already applied (flag exists)", "Data");
                    return;
                }

                if (File.Exists(MasterFilePath))
                {
                    // master.json already exists — no migration needed
                    File.WriteAllText(MasterMigrationFlagPath, $"Master.json already present at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    return;
                }

                // Try to consolidate legacy files
                var windows = new List<NoteWindowData>();
                if (File.Exists(NoteWindowsFilePath))
                {
                    windows = LoadNoteWindows();
                    LoggingService.LogInfoStatic($"Migrating {windows.Count} windows from notewindows.json to master.json", "Data");
                }

                var settings = LoadSettings();
                LoggingService.LogInfoStatic("Migrating settings from settings.json to master.json", "Data");

                var master = new MasterData
                {
                    Version = 1,
                    Windows = windows,
                    Settings = settings
                };

                SaveMasterData(master);
                File.WriteAllText(MasterMigrationFlagPath, $"Legacy files consolidated to master.json at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                LoggingService.LogInfoStatic("✅ Master migration complete", "Data");
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to migrate to master.json", ex, "Data");
            }
        }

        #region Master Data Management

        /// <summary>
        /// 💾 Save complete application state to master.json atomically
        /// </summary>
        public static void SaveMasterData(MasterData masterData)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var success = AtomicFileManager.AtomicSave(MasterFilePath, masterData, maxBackups: 20);
                stopwatch.Stop();

                if (success)
                {
                    LoggingService.LogInfoStatic($"Master data saved in {stopwatch.ElapsedMilliseconds}ms", "Perf", new {
                        WindowCount = masterData?.Windows?.Count ?? 0,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                }
                else
                {
                    LoggingService.LogErrorStatic("AtomicSave returned false for master data", null, "Data", new {
                        FilePath = PathSanitizer.SanitizePath(MasterFilePath),
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                    throw new IOException($"Failed to save master data to {PathSanitizer.SanitizePath(MasterFilePath)} — AtomicSave returned false");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed to save master data after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    FilePath = PathSanitizer.SanitizePath(MasterFilePath),
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// 📥 Load complete application state from master.json with recovery
        /// </summary>
        public static MasterData LoadMasterData()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = AtomicFileManager.LoadWithRecovery(
                    MasterFilePath,
                    () => new MasterData()
                );

                // Apply schema migration
                result = MigrationService.MigrateMasterData(result);

                stopwatch.Stop();

                LoggingService.LogInfoStatic($"Master data loaded in {stopwatch.ElapsedMilliseconds}ms", "Perf", new {
                    WindowCount = result.Windows?.Count ?? 0,
                    DurationMs = stopwatch.ElapsedMilliseconds
                });

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoggingService.LogErrorStatic($"Failed to load master data after {stopwatch.ElapsedMilliseconds}ms", ex, "Perf", new {
                    FilePath = PathSanitizer.SanitizePath(MasterFilePath),
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
                return new MasterData();
            }
        }

        #endregion

        #region Notes Management

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

                // Load all notes from master.json to get referenced image paths
                var master = LoadMasterData();
                var referencedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var window in master.Windows)
                {
                    foreach (var note in window.Notes)
                    {
                        foreach (var mediaRef in note.Media)
                        {
                            referencedImages.Add(Path.GetFullPath(mediaRef.FullPath));
                        }
                    }
                }

                LoggingService.LogDebugStatic($"Found {referencedImages.Count} referenced images across {master.Windows?.Count ?? 0} windows", "Data");

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

        #region Recovery Journal

        private static readonly string RecoveryJournalPath = Path.Combine(AppDataFolder, "master.json.recovery");

        /// <summary>
        /// 💾 Write a crash-recovery snapshot atomically.
        /// Called every 2s while text is dirty.
        /// </summary>
        public static void SaveRecoverySnapshot(MasterData masterData)
        {
            try
            {
                AtomicFileManager.AtomicSave(RecoveryJournalPath, masterData, maxBackups: 2);
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to save recovery snapshot", ex, "Data");
            }
        }

        /// <summary>
        /// 🗑️ Clear the recovery snapshot after a clean save.
        /// </summary>
        public static void ClearRecoverySnapshot()
        {
            try
            {
                if (File.Exists(RecoveryJournalPath))
                    File.Delete(RecoveryJournalPath);
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to clear recovery snapshot", ex, "Data");
            }
        }

        /// <summary>
        /// 📥 Load a recovery snapshot if it exists and is fresh enough.
        /// Returns null if no snapshot or snapshot is too old.
        /// </summary>
        public static MasterData? LoadRecoverySnapshot()
        {
            try
            {
                if (!File.Exists(RecoveryJournalPath))
                    return null;

                var fileInfo = new FileInfo(RecoveryJournalPath);
                var age = DateTime.Now - fileInfo.LastWriteTime;
                if (age.TotalHours > AppConstants.RecoveryJournalMaxAgeHours)
                {
                    LoggingService.LogDebugStatic($"Recovery snapshot too old ({age.TotalHours:F1}h), ignoring", "Data");
                    File.Delete(RecoveryJournalPath);
                    return null;
                }

                var result = AtomicFileManager.LoadWithRecovery(
                    RecoveryJournalPath,
                    () => new MasterData()
                );
                LoggingService.LogInfoStatic($"Recovery snapshot loaded ({result.Windows?.Count ?? 0} windows)", "Data");
                return result;
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to load recovery snapshot", ex, "Data");
                return null;
            }
        }

        /// <summary>
        /// 🔄 Attempt silent restore from recovery snapshot.
        /// Called at startup BEFORE any window loads. Merges recovered text
        /// into master.json, then deletes the recovery file.
        /// Returns true if recovery was applied, false otherwise.
        /// </summary>
        public static bool TryRestoreFromRecovery()
        {
            try
            {
                var snapshot = LoadRecoverySnapshot();
                if (snapshot == null)
                    return false;

                var saved = LoadMasterData();
                var merged = MergeRecoveryIntoSaved(saved, snapshot);
                SaveMasterData(merged);
                ClearRecoverySnapshot();

                LoggingService.LogInfoStatic("✅ Recovery restore complete — unsaved text recovered silently", "Data");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("Failed to restore from recovery", ex, "Data");
                return false;
            }
        }

        /// <summary>
        /// Merge recovered windows/notes into saved data.
        /// For each recovered window (matched by ID), updates note text
        /// from the snapshot. New windows in the snapshot are added.
        /// </summary>
        private static MasterData MergeRecoveryIntoSaved(MasterData saved, MasterData recovered)
        {
            if (saved.Windows == null)
                saved.Windows = new List<NoteWindowData>();
            if (recovered.Windows == null)
                return saved;

            int notesRecovered = 0;

            foreach (var recWindow in recovered.Windows)
            {
                var savedWindow = saved.Windows.FirstOrDefault(w => w.Id == recWindow.Id);

                if (savedWindow == null)
                {
                    // Window exists in recovery but not in saved — add it
                    saved.Windows.Add(recWindow);
                    notesRecovered += recWindow.Notes?.Count ?? 0;
                    LoggingService.LogDebugStatic($"Recovery: added lost window '{recWindow.Title}' ({recWindow.Notes?.Count ?? 0} notes)", "Data");
                    continue;
                }

                // Window exists in both — merge notes by title
                if (recWindow.Notes == null || recWindow.Notes.Count == 0)
                    continue;

                if (savedWindow.Notes == null)
                    savedWindow.Notes = new List<SavedNote>();

                foreach (var recNote in recWindow.Notes)
                {
                    // Try by index first — most reliable when two tabs share the same name
                    var recIndex = recWindow.Notes.IndexOf(recNote);
                    SavedNote? savedNote = null;

                    if (recIndex >= 0 && recIndex < savedWindow.Notes.Count)
                    {
                        var candidate = savedWindow.Notes[recIndex];
                        if (candidate.Title == recNote.Title)
                            savedNote = candidate;
                    }

                    // Fallback: title match (handles tab additions/removals between save and crash)
                    if (savedNote == null)
                        savedNote = savedWindow.Notes.FirstOrDefault(
                            n => !string.IsNullOrEmpty(n.Title) && n.Title == recNote.Title);

                    if (savedNote == null)
                    {
                        // Note in recovery but not saved — add it
                        savedWindow.Notes.Add(recNote);
                        notesRecovered++;
                        LoggingService.LogDebugStatic($"Recovery: added lost note '{recNote.Title}'", "Data");
                    }
                    else if (!string.IsNullOrEmpty(recNote.TextContent) && recNote.TextContent != savedNote.TextContent)
                    {
                        // Note exists in both — recovered text is newer
                        savedNote.TextContent = recNote.TextContent;
                        notesRecovered++;
                        LoggingService.LogDebugStatic($"Recovery: restored text for note '{recNote.Title}'", "Data");
                    }
                }

                // Update LastModified to reflect recovery
                savedWindow.LastModified = DateTime.Now;
            }

            if (notesRecovered > 0)
            {
                LoggingService.LogInfoStatic($"Recovery: merged {notesRecovered} note(s) from snapshot into saved data", "Data");
            }

            return saved;
        }

        #endregion

        #endregion
    }
} 