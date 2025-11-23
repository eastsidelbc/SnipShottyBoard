using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using SnipShottyBoard.Infrastructure.Logging;
using SnipShottyBoard.Infrastructure.Helpers;

namespace SnipShottyBoard.Core.Managers
{
    /// <summary>
    /// 💾 Atomic file operations with rolling backups and corruption recovery
    /// Provides production-ready data persistence with safety guarantees
    /// ✅ Phase 4D P2.3: Migrated to use LoggingService
    /// </summary>
    public static class AtomicFileManager
    {
        /// <summary>
        /// 💾 Atomically save data with backup and verification
        /// </summary>
        /// <typeparam name="T">Type of data to save</typeparam>
        /// <param name="filePath">Target file path</param>
        /// <param name="data">Data to save</param>
        /// <param name="maxBackups">Maximum number of rolling backups to keep</param>
        /// <returns>True if save succeeded, false otherwise</returns>
        public static bool AtomicSave<T>(string filePath, T data, int maxBackups = 20)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempPath = $"{filePath}.tmp";
                var backupPath = $"{filePath}.bak";
                
                // 1. Serialize to JSON
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(data, jsonOptions);
                
                // 2. Write to temporary file
                File.WriteAllText(tempPath, json);
                
                // 3. Verify the temp file is valid
                if (!VerifyJsonFile<T>(tempPath))
                {
                    File.Delete(tempPath);
                    return false;
                }
                
                // 4. Create rolling backup before atomic replace
                if (File.Exists(filePath))
                {
                    CreateRollingBackup(filePath, maxBackups);
                }
                
                // 5. Atomic replace (single filesystem operation)
                if (File.Exists(filePath))
                {
                    File.Replace(tempPath, filePath, backupPath);
                }
                else
                {
                    File.Move(tempPath, filePath);
                }
                
                // 6. Write verification info
                WriteVerificationInfo(filePath);
                
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic($"AtomicSave failed for {PathSanitizer.SanitizePath(filePath)}", ex, "Data");
                return false;
            }
        }

        /// <summary>
        /// 📥 Load data with automatic corruption recovery
        /// </summary>
        /// <typeparam name="T">Type of data to load</typeparam>
        /// <param name="filePath">File path to load from</param>
        /// <param name="createDefault">Function to create default instance if all recovery fails</param>
        /// <returns>Loaded data or default instance</returns>
        public static T LoadWithRecovery<T>(string filePath, Func<T> createDefault)
        {
            try
            {
                // 1. Try to load primary file
                if (File.Exists(filePath) && VerifyJsonFile<T>(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var result = JsonSerializer.Deserialize<T>(json);
                    if (result != null)
                    {
                        LoggingService.LogDebugStatic($"Loaded {PathSanitizer.SanitizePath(filePath)} successfully", "Data");
                        return result;
                    }
                }

                LoggingService.LogWarningStatic($"Primary file corrupted or missing, attempting recovery: {PathSanitizer.SanitizePath(filePath)}", "Data");

                // 2. Try backup file
                var backupPath = $"{filePath}.bak";
                if (File.Exists(backupPath) && VerifyJsonFile<T>(backupPath))
                {
                    var json = File.ReadAllText(backupPath);
                    var result = JsonSerializer.Deserialize<T>(json);
                    if (result != null)
                    {
                        LoggingService.LogInfoStatic($"Recovered from backup: {PathSanitizer.SanitizePath(backupPath)}", "Data");
                        // Restore the backup as the primary file
                        AtomicSave(filePath, result);
                        return result;
                    }
                }

                // 3. Try rolling backups (newest first)
                var recoveredData = TryRollingBackupRecovery<T>(filePath);
                if (recoveredData != null)
                {
                    LoggingService.LogInfoStatic($"Recovered from rolling backup for {PathSanitizer.SanitizePath(filePath)}", "Data");
                    // Restore the recovered data as the primary file
                    AtomicSave(filePath, recoveredData);
                    return recoveredData;
                }

                // 4. All recovery failed, create default
                LoggingService.LogWarningStatic($"All recovery attempts failed, creating default: {PathSanitizer.SanitizePath(filePath)}", "Data");
                var defaultData = createDefault();
                AtomicSave(filePath, defaultData);
                return defaultData;
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic($"LoadWithRecovery failed for {PathSanitizer.SanitizePath(filePath)}", ex, "Data");
                return createDefault();
            }
        }

        /// <summary>
        /// 🔍 Verify that a JSON file can be deserialized
        /// </summary>
        private static bool VerifyJsonFile<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                
                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return false;
                
                JsonSerializer.Deserialize<T>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 📚 Create timestamped rolling backup
        /// </summary>
        private static void CreateRollingBackup(string filePath, int maxBackups)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                
                if (directory == null) return;

                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var backupFileName = $"{fileName}-{timestamp}{extension}";
                var backupPath = Path.Combine(directory, backupFileName);
                
                File.Copy(filePath, backupPath);
                
                // Clean up old backups
                CleanupOldBackups(directory, fileName, extension, maxBackups);
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("CreateRollingBackup failed", ex, "Data");
            }
        }

        /// <summary>
        /// 🧹 Clean up old backup files, keeping only the most recent ones
        /// </summary>
        private static void CleanupOldBackups(string directory, string fileName, string extension, int maxBackups)
        {
            try
            {
                var backupPattern = $"{fileName}-????????-??????{extension}";
                var backupFiles = Directory.GetFiles(directory, backupPattern)
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .Skip(maxBackups);

                foreach (var oldBackup in backupFiles)
                {
                    File.Delete(oldBackup);
                    LoggingService.LogDebugStatic($"Deleted old backup: {Path.GetFileName(oldBackup)}", "Data");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("CleanupOldBackups failed", ex, "Data");
            }
        }

        /// <summary>
        /// 🔄 Try to recover from rolling backups
        /// </summary>
        private static T? TryRollingBackupRecovery<T>(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                
                if (directory == null) return default(T);

                var backupPattern = $"{fileName}-????????-??????{extension}";
                var backupFiles = Directory.GetFiles(directory, backupPattern)
                    .OrderByDescending(f => File.GetCreationTime(f));

                foreach (var backupFile in backupFiles)
                {
                    if (VerifyJsonFile<T>(backupFile))
                    {
                        var json = File.ReadAllText(backupFile);
                        var result = JsonSerializer.Deserialize<T>(json);
                        if (result != null)
                        {
                            LoggingService.LogInfoStatic($"Recovered from rolling backup: {Path.GetFileName(backupFile)}", "Data");
                            return result;
                        }
                    }
                }

                return default(T);
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("TryRollingBackupRecovery failed", ex, "Data");
                return default(T);
            }
        }

        /// <summary>
        /// ✍️ Write verification information for integrity checking
        /// </summary>
        private static void WriteVerificationInfo(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var verificationPath = $"{filePath}.info";
                
                var verificationData = new
                {
                    FileName = Path.GetFileName(filePath),
                    Size = fileInfo.Length,
                    LastWriteTime = fileInfo.LastWriteTime,
                    CreatedTime = DateTime.Now
                };

                var json = JsonSerializer.Serialize(verificationData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(verificationPath, json);
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("WriteVerificationInfo failed", ex, "Data");
            }
        }

        /// <summary>
        /// 📊 Get backup statistics for a file
        /// </summary>
        public static BackupInfo GetBackupInfo(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                
                if (directory == null) return new BackupInfo();

                var backupPattern = $"{fileName}-????????-??????{extension}";
                var backupFiles = Directory.GetFiles(directory, backupPattern);
                
                var backupInfo = new BackupInfo
                {
                    BackupCount = backupFiles.Length,
                    HasBackupFile = File.Exists($"{filePath}.bak"),
                    HasVerificationFile = File.Exists($"{filePath}.info"),
                    OldestBackup = backupFiles.Length > 0 ? 
                        backupFiles.Min(f => File.GetCreationTime(f)) : (DateTime?)null,
                    NewestBackup = backupFiles.Length > 0 ? 
                        backupFiles.Max(f => File.GetCreationTime(f)) : (DateTime?)null
                };

                return backupInfo;
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorStatic("GetBackupInfo failed", ex, "Data");
                return new BackupInfo();
            }
        }
    }

    /// <summary>
    /// 📊 Information about backup files for a given data file
    /// </summary>
    public class BackupInfo
    {
        public int BackupCount { get; set; }
        public bool HasBackupFile { get; set; }
        public bool HasVerificationFile { get; set; }
        public DateTime? OldestBackup { get; set; }
        public DateTime? NewestBackup { get; set; }
    }
}
