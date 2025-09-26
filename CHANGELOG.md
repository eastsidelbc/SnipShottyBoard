# 📝 SnipShottyBoard Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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