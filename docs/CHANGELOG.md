# 📝 SnipShottyBoard Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2025-01-01
### 🎨 Tab Drag-and-Drop UX Enhancement
- **Drop Indicator**: Added blue vertical line (3px wide) that shows exactly where tabs will be inserted during drag operations
- **Drag Visual**: Implemented semi-transparent gray ghost tab that follows cursor during drag (changed from blue to gray for better contrast with drop indicator)
- **Edge-like Styling**: Redesigned tabs with Microsoft Edge-inspired appearance
  - Rounded top corners (3px radius)
  - Blue accent underline (2px) on active tabs
  - Medium font weight for selected tabs
  - Smooth hover and pressed state animations
- **Hysteresis System**: Added 5px buffer to prevent indicator flicker when hovering near tab boundaries
- **Improved Drop Detection**: Enhanced FindDropTargetIndex to use tab midpoints for intuitive drop zones
- **Visual Transparency**: Drag visual uses more transparent gray (alpha 140 from 200) to show background content

### 🐛 Fixed
- **Coordinate Transform Bug**: Fixed "Visual is not an ancestor" exception by using MainWindow as common ancestor for both dragCanvas and tab buttons
- **Drop Position Detection**: Fixed drop indicator only appearing at end - now correctly shows between all tabs
- **Build Error**: Removed empty GifFramePlayer.xaml and GifFramePlayer.cs files that were causing XML parse errors
- **Visual Conflict**: Changed drag visual from blue to gray to prevent hiding the blue drop indicator line

### 🔧 Technical
- **Coordinate Transformations**: Implemented proper WPF coordinate transforms using TransformToAncestor(MainWindow)
- **Tag-Based Selection**: Active tab styling now uses Tag="Selected" property to drive XAML triggers (cleaner than code-behind)
- **Theme Resources**: Added AccentBrush (#4A90E2) to both DarkTheme.xaml and LightTheme.xaml
- **Drag Canvas**: Full-window overlay canvas with ZIndex=9999 contains both drag visual and drop indicator
- **Index Calculation**: ReorderTab properly handles forward vs backward movement with insert index adjustment
- **Debug Logging**: Comprehensive OnLogDebug calls throughout drag system (start, move, drop, coordinates, indices)

### 📊 Performance
- Drag visual updates only on mouse move (not continuous)
- Coordinate transforms cached per frame (not per tab)
- Hysteresis reduces unnecessary indicator position updates
- Visual tree changes batched (remove then insert, not swap)

### 📚 Documentation
- **CR.md Section 1.5**: Added comprehensive "Tab Drag-and-Drop System" documentation
  - Drag flow diagrams and code examples
  - Coordinate transformation logic explanation
  - Hysteresis implementation details
  - ReorderTab index calculation (forward/backward cases)
  - Visual tree structure
  - Edge cases and performance considerations

### 🎯 Visual Tree Changes
```
MainWindow
└── Grid
    ├── ScrollViewer (tab strip)
    │   └── StackPanel (tabs)
    └── Canvas (ZIndex=9999, drag overlay)
        ├── Border (gray ghost tab, alpha 140)
        └── Border (blue drop indicator, 3px wide)
```

## [1.2.1] - 2024-12-28
### 🔧 Post-Audit Hardening & Consistency
- **Nullable Warnings Reduction**: Reduced from 274 to 246 nullable warnings by fixing critical Manager and Data layer issues
- **Debug Output Cleanup**: Removed 100+ excessive Console.WriteLine statements from ImageViewerWindow GIF loading
- **Logging Consistency**: Standardized all Data layer logging with consistent "Data:" prefix categories
- **TODO Markers Added**: Added TODO markers for GIF disposal improvements and UI-to-Data separation opportunities
- **Build Stability**: Maintained 0 compilation errors while improving code quality

### 🐛 Fixed
- **MediaSection File I/O**: Added TODO markers for file operations that should move to DataManager
- **ImageViewerWindow Debug Spam**: Cleaned up hundreds of debug statements while preserving essential functionality
- **Manager Events**: Fixed nullable event declarations in SettingsManager and ThemeManager
- **Constants Usage**: Applied AppConstants window dimensions consistently across NoteWindowManager and AppSettings

### 🔧 Technical
- **Architecture Compliance**: Verified no System.Windows dependencies in Data layer
- **Layer Separation**: Documented remaining UI → Data violations for future cleanup
- **Resource Management**: Prepared foundation for ThemeResourceHelper adoption
- **Configuration**: Confirmed MCP server environment variable support

## [1.2.0] - 2024-12-28
### 🏗️ Architecture & Code Quality Improvements
- **Nullable Reference Types**: Enabled nullable reference types across the entire codebase for better type safety
- **Centralized Constants**: Created AppConstants class with all magic numbers and configuration values
- **Enhanced Logging**: Upgraded LoggingService to use Serilog with structured logging, file rotation, and category-based organization  
- **Architecture Compliance**: Removed UI → Data layer violations, moved all file operations to DataManager
- **Theme Resource Validation**: Added ThemeResourceHelper with safe resource access and fallback handling
- **MCP Server Configuration**: Made MCP server project path configurable via SSB_PROJECT_ROOT environment variable

### 🐛 Fixed
- **Cross-Layer Violations**: Removed System.Windows dependency from Data layer (DataManager.cs)
- **Magic Numbers**: Replaced hardcoded values with AppConstants throughout UI components
- **Direct File Access**: UI components now use DataManager instead of direct File.* operations
- **Resource Access**: Added safe theme resource access with graceful degradation

### 🔧 Technical
- Added comprehensive image management methods to DataManager (SaveImageFromClipboard, CopyDroppedImage, DeleteImage, ValidateImageFile)
- Enhanced LoggingService with Serilog backend, daily log rotation, and structured logging with categories
- Created ThemeResourceHelper for safe resource access with type checking and fallbacks
- Updated MediaSection and KeyboardHandler to use DataManager for all file operations
- Replaced magic numbers in MainWindow, MediaSection, and ImageViewerWindow with AppConstants
- Made MCP server configurable with environment variables for flexible development setups

### 📚 Documentation
- **LOGGING.md**: Comprehensive logging guide with examples, monitoring tips, and troubleshooting
- **MCP_SETUP.md**: Complete MCP server setup and configuration guide
- **Enhanced CR.md**: Updated compliance status and architecture validation

## [1.1.0] - 2024-12-23
### 🎉 Added
- **Rich Text Editing**: Converted TextBox to RichTextBox with comprehensive formatting support
- **Keyboard Shortcuts**: Added rich text formatting shortcuts (Ctrl+B, Ctrl+I, Ctrl+U, Ctrl+S, Ctrl+., Ctrl+L, Tab, Shift+Tab)
- **Formatting Features**: Bold, italic, underline, strikethrough, bullet points, numbered lists, and text indentation
- **RTF Storage**: Rich text content is now stored in RTF format for preserved formatting
- **Backward Compatibility**: Existing plain text notes are automatically converted and preserved

### 🐛 Fixed
- **Bullet Points**: Fixed bullet points implementation to actually add bullet characters (•) instead of just indentation
- **Numbered Lists**: Added proper numbered list support with automatic numbering (1., 2., 3., etc.)

### 🔧 Technical
- Replaced TextBox with RichTextBox in TextSection.xaml while maintaining identical styling
- Added RTF content storage in SavedNote.RichTextContent property
- Implemented rich text formatting methods in TextSection.xaml.cs with proper bullet (•) and numbered list (1., 2., 3.) characters
- Enhanced KeyboardHandler.cs with rich text formatting shortcuts including Ctrl+L for numbered lists
- Updated TabManager.cs to handle RTF content loading and saving
- Added rich text formatting event handling in MainWindow.xaml.cs
- Maintained all existing functionality including autosave, placeholder text, and scrolling

## [1.0.6] - 2024-12-23
### 🐛 Fixed
- **GIF Animation**: Fixed GIF animation by using BitmapImage instead of BitmapDecoder frames
- **ImageViewerWindow Close Button**: Fixed close button color to be white instead of themed color

### 🔧 Technical
- Fixed GIF animation by using BitmapImage with proper settings instead of BitmapDecoder frames
- Implemented multiple GIF loading methods with proper fallbacks and comprehensive debugging
- Added comprehensive debugging for GIF animation detection and loading issues

## [1.0.5] - 2024-12-23
### 🐛 Fixed
- **TextSection Scroll**: Added proper mouse wheel scrolling support to text areas

### 🔧 Technical
- Added `PreviewMouseWheel` event handling to `TextSection` for natural scrolling
- Added `System.Windows.Input` using statement to TextSection.xaml.cs

## [1.0.4] - 2024-12-23
### 🐛 Fixed
- **Theme Loading**: Eliminated unnecessary light theme application on startup - now loads saved theme directly

### 🔧 Technical
- Updated `MainWindow` constructor to load settings before theme initialization
- Modified `LoadApplicationData()` to avoid double-loading theme settings

## [1.0.3] - 2024-12-23
### 🐛 Fixed
- **Note Window Memory**: Each note window now loads its own individual data and tabs instead of sharing content

### 🔧 Technical
- Modified `NoteListWindow.OpenNoteWindow()` to pass specific `NoteWindowData` to `MainWindow` constructor
- Updated `MediaSection.ShowFullSizeImage()` to pass full image list for navigation
- Enhanced `ImageViewerWindow` with navigation support and improved GIF loading

## [1.0.2] - 2024-12-23
### 🎉 Added
- **Image Navigation**: Added arrow key navigation (Left/Right) to cycle through images in ImageViewerWindow

### 🔧 Technical
- Added navigation support constructor to `ImageViewerWindow`
- Added `NavigateToPreviousImage()` and `NavigateToNextImage()` methods
- Added Left/Right arrow key handling in keyboard shortcuts
- Updated `MediaSection.ShowFullSizeImage()` to pass full image list and current index

## [1.0.1] - 2024-12-23
### 🎉 Added
- **CHANGELOG.md**: Added comprehensive changelog with semantic versioning
- **Documentation**: Added detailed documentation for all recent fixes and improvements

### 🔧 Technical
- Created CHANGELOG.md following Keep a Changelog format
- Added semantic versioning tracking for all changes

## [1.0.0] - 2024-12-15
### 🎉 Added
- Initial release of SnipShottyBoard
- Multi-tab note taking interface
- Dark/Light theme support
- Image paste and management
- Auto-save functionality
- Multiple note window support
- Settings management system 