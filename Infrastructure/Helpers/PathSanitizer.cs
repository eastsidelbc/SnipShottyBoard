using System;
using System.IO;

namespace SnipShottyBoard.Infrastructure.Helpers
{
    /// <summary>
    /// Sanitizes file paths before writing to logs.
    /// Prevents full system paths (C:\Users\Soy\...) from appearing in log files.
    /// Use this any time a file path is included in a log message.
    /// </summary>
    public static class PathSanitizer
    {
        /// <summary>
        /// Alias for Sanitize() — used by DataManager, AtomicFileManager, and MediaSection.
        /// </summary>
        public static string SanitizePath(string? fullPath) => Sanitize(fullPath);

        /// <summary>
        /// Returns just the filename and parent folder from a full path.
        /// Example: C:\Users\Soy\AppData\Roaming\SnipShottyBoard\notes.json
        ///      --> SnipShottyBoard\notes.json
        /// </summary>
        public static string Sanitize(string? fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return "[empty path]";

            try
            {
                string? fileName = Path.GetFileName(fullPath);
                string? parentFolder = Path.GetFileName(Path.GetDirectoryName(fullPath));

                if (string.IsNullOrWhiteSpace(fileName))
                    return "[unknown file]";

                if (string.IsNullOrWhiteSpace(parentFolder))
                    return fileName;

                return $"{parentFolder}\\{fileName}";
            }
            catch (Exception)
            {
                // If path parsing fails for any reason — return a safe placeholder
                return "[invalid path]";
            }
        }

        /// <summary>
        /// Returns just the filename from a full path — no parent folder.
        /// Example: C:\Users\Soy\AppData\Roaming\SnipShottyBoard\notes.json
        ///      --> notes.json
        /// </summary>
        public static string SanitizeToFileName(string? fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return "[empty path]";

            try
            {
                string? fileName = Path.GetFileName(fullPath);
                return string.IsNullOrWhiteSpace(fileName) ? "[unknown file]" : fileName;
            }
            catch (Exception)
            {
                return "[invalid path]";
            }
        }

        /// <summary>
        /// Returns the app data folder name only — strips everything above it.
        /// Example: C:\Users\Soy\AppData\Roaming\SnipShottyBoard\images\img_123.png
        ///      --> images\img_123.png
        /// Useful for image paths which are one level deeper.
        /// </summary>
        public static string SanitizeImagePath(string? fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return "[empty path]";

            try
            {
                // Get the last two segments: subfolder\filename
                string? fileName = Path.GetFileName(fullPath);
                string? subFolder = Path.GetFileName(Path.GetDirectoryName(fullPath));
                string? appFolder = Path.GetFileName(
                    Path.GetDirectoryName(Path.GetDirectoryName(fullPath)));

                if (string.IsNullOrWhiteSpace(fileName))
                    return "[unknown image]";

                if (string.IsNullOrWhiteSpace(subFolder))
                    return fileName;

                // If the subfolder is "images" include one level up for clarity
                if (subFolder.Equals("images", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(appFolder))
                    return $"{subFolder}\\{fileName}";

                return $"{subFolder}\\{fileName}";
            }
            catch (Exception)
            {
                return "[invalid image path]";
            }
        }
    }
}
