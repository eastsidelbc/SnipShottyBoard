# SnipShottyBoard - A Modern Tabbed Sticky Notes Application

**SnipShottyBoard** is a sophisticated desktop sticky notes application built with WPF (.NET 8) that combines the simplicity of traditional sticky notes with modern productivity features. Here's what makes it special:

## Core Functionality

- **📝 Multi-tab Note Taking**: Create, organize, and manage multiple notes in a tabbed interface
- **🖼️ Image & Screenshot Support**: Paste images directly into notes (Ctrl+V) with thumbnail previews
- **💾 Auto-Save**: Automatically saves your work every 5 seconds with robust data persistence
- **🎨 Modern UI**: Clean, professional interface with light/dark theme support
- **🪟 Multiple Windows**: Support for multiple note windows for better organization

## Key Features

### 1. Smart Tab Management
- Create new tabs with the + button
- Drag & drop to reorder tabs
- Right-click context menus for rename, duplicate, delete
- Tab scrolling for many tabs

### 2. Rich Content Support
- Text notes with word count tracking
- Image paste support with full-screen previews
- Drag & drop image import
- Image timestamps and organization

### 3. Professional Architecture
- Clean, modular codebase with manager classes
- Comprehensive settings system
- Custom dialog system replacing standard message boxes
- Status bar with live information (tab count, word count, save status, clock)

### 4. User Experience
- Custom window chrome with rounded corners and drop shadows
- Keyboard shortcuts (Ctrl+T for new tab, Ctrl+W to close, etc.)
- Help system with comprehensive shortcuts guide
- Settings window for customization

## Technical Highlights

- **Framework**: WPF with .NET 8 on Windows
- **UI Library**: WPF-UI for modern controls
- **Data Storage**: JSON-based persistence in AppData
- **Architecture**: Event-driven with manager classes for maintainability
- **Theming**: Dynamic light/dark theme switching

## Use Cases

- **Quick Note Taking**: Jot down ideas, reminders, or thoughts
- **Meeting Notes**: Combine text and screenshots in organized tabs
- **Project Organization**: Keep related information in separate tabs
- **Clipboard Management**: Paste and organize images and text snippets
- **Desktop Productivity**: Always-accessible notes that stay organized

## Getting Started

### Prerequisites
- Windows 10/11
- .NET 8 Runtime

### Installation
1. Download the latest release
2. Extract the files
3. Run `SnipShottyBoard.exe`

### Keyboard Shortcuts
- `Ctrl+T`: New tab
- `Ctrl+W`: Close current tab
- `Ctrl+Tab`: Next tab
- `Ctrl+Shift+Tab`: Previous tab
- `F2`: Rename current tab
- `Ctrl+V`: Paste image or text
- `Ctrl+S`: Manual save

## Development

This is essentially a **professional-grade sticky notes application** that goes beyond basic note-taking to provide a comprehensive digital workspace for capturing, organizing, and managing both text and visual information. It's designed for users who want a lightweight but powerful tool for daily productivity tasks.

### Project Structure
- `MainWindow.xaml/cs`: Main application window
- `NoteTab.xaml/cs`: Individual note tab implementation
- `UI/`: User interface components (TextSection, MediaSection, etc.)
- `Data/`: Data models and persistence logic
- `Themes/`: Light and dark theme definitions

### Building from Source
```bash
git clone <repository-url>
cd SnipShottyBoard
dotnet build
dotnet run
```

## License

[Add your license information here]

## Contributing

[Add contribution guidelines here]