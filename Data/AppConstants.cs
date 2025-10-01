using System;

namespace SnipShottyBoard.Data
{
    /// <summary>
    /// 🔧 Application constants and configuration values
    /// Centralizes magic numbers and provides clear documentation for configuration values
    /// </summary>
    public static class AppConstants
    {
        #region Auto-Save Configuration
        /// <summary>
        /// Default auto-save interval in seconds
        /// Lower values = more frequent saves = less chance of losing work
        /// </summary>
        public const int DefaultAutoSaveIntervalSeconds = 5;

        /// <summary>
        /// Status bar update interval in seconds
        /// How often status information (time, counts) is refreshed
        /// </summary>
        public const int StatusUpdateIntervalSeconds = 1;
        #endregion

        #region Image Configuration
        /// <summary>
        /// Default thumbnail width in pixels
        /// Used for image thumbnails in note media sections
        /// </summary>
        public const int DefaultThumbnailWidth = 120;

        /// <summary>
        /// Media container width in pixels
        /// Width of the container holding image thumbnails
        /// </summary>
        public const int MediaContainerWidth = 150;

        /// <summary>
        /// Media container minimum height in pixels
        /// Minimum height for media thumbnail containers
        /// </summary>
        public const int MediaContainerMinHeight = 140;

        /// <summary>
        /// Media container thumbnail height in pixels
        /// Height allocated for thumbnail area within container
        /// </summary>
        public const int MediaThumbnailHeight = 120;
        #endregion

        #region Window Configuration
        /// <summary>
        /// Image viewer minimum width in pixels
        /// </summary>
        public const int ImageViewerMinWidth = 400;

        /// <summary>
        /// Image viewer minimum height in pixels
        /// </summary>
        public const int ImageViewerMinHeight = 300;

        /// <summary>
        /// Default image viewer width in pixels
        /// Used when image sizing calculation fails
        /// </summary>
        public const int DefaultImageViewerWidth = 700;

        /// <summary>
        /// Default image viewer height in pixels
        /// Used when image sizing calculation fails
        /// </summary>
        public const int DefaultImageViewerHeight = 500;

        /// <summary>
        /// Screen usage ratio for image viewer sizing
        /// Percentage of screen space to use (90% = 0.9)
        /// </summary>
        public const double ScreenUsageRatio = 0.9;

        /// <summary>
        /// Estimated window chrome height in pixels
        /// Title bar + toolbar + status bar space
        /// </summary>
        public const int WindowChromeHeight = 115; // 35 + 55 + 25

        /// <summary>
        /// Estimated window chrome width in pixels
        /// Side margins and window borders
        /// </summary>
        public const int WindowChromeWidth = 20;
        #endregion

        #region UI Timing
        /// <summary>
        /// Click detection window in milliseconds
        /// Time window for detecting double-clicks on images
        /// </summary>
        public const int ClickDetectionWindowMs = 200;

        /// <summary>
        /// Animation check interval in milliseconds
        /// How often to check GIF animation status during debugging
        /// </summary>
        public const int AnimationCheckIntervalMs = 1000;
        #endregion

        #region Text and Font Configuration
        /// <summary>
        /// Default font size in points
        /// Standard text size for notes and UI elements
        /// </summary>
        public const int DefaultFontSize = 14;

        /// <summary>
        /// Small font size in points
        /// Used for metadata, timestamps, and secondary information
        /// </summary>
        public const int SmallFontSize = 10;
        #endregion

        #region Window Configuration
        /// <summary>
        /// Default window position values
        /// Used when no previous window position is saved or position is invalid
        /// </summary>
        public const double DefaultWindowLeft = 100;
        public const double DefaultWindowTop = 100;
        public const double DefaultWindowWidth = 800;
        public const double DefaultWindowHeight = 600;

        /// <summary>
        /// Minimum valid window size constraints
        /// Prevents windows from becoming too small to be usable
        /// </summary>
        public const double MinWindowWidth = 200;
        public const double MinWindowHeight = 200;
        #endregion

        #region File System
        /// <summary>
        /// File size display unit divisor
        /// Convert bytes to KB (1024 bytes per KB)
        /// </summary>
        public const double BytesToKB = 1024.0;
        #endregion

        #region Tab Configuration
        /// <summary>
        /// Minimum tab button width in pixels
        /// Ensures tab labels remain readable even with many tabs
        /// </summary>
        public const int TabMinWidth = 80;

        /// <summary>
        /// Maximum tab button width in pixels
        /// Prevents tabs from becoming too wide in multi-row layout
        /// </summary>
        public const int TabMaxWidth = 200;

        /// <summary>
        /// Tab strip maximum height in pixels
        /// Limits vertical space used by wrapped tab rows
        /// </summary>
        public const int TabStripMaxHeight = 200;

        /// <summary>
        /// Row grouping tolerance in pixels
        /// Used for detecting which tabs are in the same row (Y-position tolerance)
        /// </summary>
        public const int TabRowGroupingTolerance = 5;

        /// <summary>
        /// Drag & drop hysteresis buffer in pixels
        /// Prevents flickering when mouse hovers near tab boundaries during drag operations
        /// </summary>
        public const double TabDragHysteresisBuffer = 5.0;
        #endregion
    }
}
