using System;
using System.IO;
using SnipShottyBoard.Data;

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

        // ── Per-image customization (Schema v3) ──────────────────────

        /// <summary>
        /// User-defined label displayed beneath the thumbnail.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Thumbnail container width in pixels.
        /// Values: Big(150), Medium(100), Small(60). Default = Big.
        /// </summary>
        public int ThumbnailSize { get; set; } = AppConstants.ThumbnailSizeDefault;

        /// <summary>
        /// When true the thumbnail shows a placeholder instead of the image.
        /// </summary>
        public bool IsHidden { get; set; } = false;

        /// <summary>
        /// Show/hide the label row beneath the image.
        /// </summary>
        public bool ShowLabel { get; set; } = false;

        /// <summary>
        /// Show/hide the date portion of the timestamp row.
        /// </summary>
        public bool ShowDate { get; set; } = true;

        /// <summary>
        /// Show/hide the time portion of the timestamp row.
        /// </summary>
        public bool ShowTime { get; set; } = true;

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
