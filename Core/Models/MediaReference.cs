using System;
using System.IO;

namespace SnipShottyBoard.Core.Models
{
    /// <summary>
    /// A reference to a media file stored in %AppData%\SnipShottyBoard\images\.
    /// JSON only holds the filename — full paths are resolved at runtime.
    /// </summary>
    public class MediaReference
    {
        /// <summary>
        /// Filename only (e.g. "img_20260425_001234_abc.png").
        /// Full path resolved via DataManager.GetImagesFolder().
        /// </summary>
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        /// When this media item was added to the note.
        /// </summary>
        public DateTime DateAdded { get; set; }

        /// <summary>
        /// Resolves the full path to the media file.
        /// </summary>
        public string FullPath
        {
            get
            {
                var imagesFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SnipShottyBoard", "images");
                return Path.Combine(imagesFolder, Filename);
            }
        }
    }
}
