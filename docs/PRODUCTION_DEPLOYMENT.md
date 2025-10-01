# 🚀 SnipShottyBoard Production Deployment Guide

## Overview

SnipShottyBoard is now production-ready with enterprise-grade logging, atomic data persistence, and comprehensive error handling. This guide covers deployment, monitoring, and maintenance.

## ✅ Production Readiness Features

### 🔒 **Data Safety**
- **Atomic saves** with `File.Replace()` operations
- **Rolling backups** (20 most recent with timestamps)
- **Corruption recovery** from backup chains
- **Verification files** for integrity checking
- **Graceful degradation** on data errors

### 📊 **Comprehensive Logging**
- **Structured logging** with Serilog to rolling files
- **Application lifecycle** events (startup, shutdown, crashes)
- **System information** logging (OS, runtime, memory)
- **Categorized logs**: UI, Manager, Data, Lifecycle, System
- **Debug tools** accessible via developer menu

### 💪 **Error Handling**
- **Global exception handlers** for UI and background threads
- **Graceful crash recovery** with user-friendly dialogs
- **Automatic data saving** before critical errors
- **Error reporting** with log file access

## 🏗️ Deployment Options

### Option 1: Framework-Dependent (Recommended for most users)
```bash
# Requires .NET 8 Runtime on target machine
dotnet publish -c Release -r win-x64 --no-self-contained

# Produces smaller deployment (~50MB)
# User must install .NET 8 Runtime first
```

### Option 2: Self-Contained (Recommended for distribution)
```bash
# Includes .NET 8 Runtime in deployment
dotnet publish -c Release -r win-x64 --self-contained true

# Produces larger deployment (~150MB)
# Runs on any Windows 10/11 machine
```

### Option 3: Single File (Recommended for portability)
```bash
# Creates one executable file
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# ~150MB single .exe file
# Extracts temporarily at runtime
```

## 📁 Data Locations

### User Data Directory
```
%APPDATA%\SnipShottyBoard\
├── notes.json                    # Main notes data
├── notes.json.bak                # Immediate backup
├── notes-YYYYMMDD-HHMMSS.json    # Rolling backups (20 kept)
├── notes.json.info               # Verification file
├── appdata.json                  # Application state
├── settings.json                 # User preferences
├── images\                       # User-uploaded images
└── logs\                         # Application logs
    ├── snipshottyboard-YYYYMMDD.log
    └── older log files...
```

### Log Files
- **Retention**: 7 days of rolling logs
- **Format**: JSON-structured with categories
- **Access**: Developer menu → "Open Logs Folder"

## 🔧 Configuration

### Environment Variables
```bash
# Override MCP server project root (for development)
set SSB_PROJECT_ROOT=C:\Custom\Path\To\Project
```

### Registry Keys (Optional)
```
HKEY_CURRENT_USER\Software\SnipShottyBoard\
├── Theme (Light|Dark)
├── AlwaysOnTop (true|false)
└── AutoSaveInterval (seconds)
```

## 📊 Monitoring & Maintenance

### Health Checks
1. **Startup Time**: Should be < 2 seconds
2. **Tab Switching**: Should be < 100ms response
3. **Auto-save**: Should complete < 500ms
4. **Memory Usage**: Should stay < 100MB with 20 tabs

### Log Monitoring
```bash
# Watch for critical patterns
findstr /i "error\|fatal\|exception" %APPDATA%\SnipShottyBoard\logs\*.log

# Check backup health
dir %APPDATA%\SnipShottyBoard\notes-*.json
```

### Backup Information
Access via **Developer menu → Backup Information**:
- Backup count and timestamps
- Verification file status
- Recovery readiness

## 🚨 Troubleshooting

### Data Recovery
If notes.json is corrupted:
1. App automatically restores from `.bak` file
2. If that fails, restores from newest timestamped backup
3. If all fail, creates new empty notes file
4. Original corruption is logged for analysis

### Log Analysis
```bash
# View recent errors
type %APPDATA%\SnipShottyBoard\logs\snipshottyboard-*.log | findstr "ERROR"

# Check startup issues
type %APPDATA%\SnipShottyBoard\logs\snipshottyboard-*.log | findstr "Lifecycle"

# Monitor memory usage
type %APPDATA%\SnipShottyBoard\logs\snipshottyboard-*.log | findstr "Working Set"
```

### Common Issues

#### "Notes not loading"
1. Check `%APPDATA%\SnipShottyBoard\logs\` for corruption errors
2. Verify backup files exist
3. Run **Developer menu → Force Save All**

#### "High memory usage"
1. Check for large images in notes
2. Review logs for memory warnings
3. Restart application to reset state

#### "Crash on startup"
1. Check logs for initialization errors
2. Verify .NET 8 runtime is installed
3. Try running as administrator

## 🔐 Security Considerations

### Data Protection
- Notes stored in user's `%APPDATA%` (protected by Windows user permissions)
- Images copied to app data folder (isolated from original locations)
- No network communication (offline-only application)
- No external dependencies or telemetry

### Access Control
- Application runs with user privileges only
- No elevation required
- Respects Windows file system permissions
- No registry modifications (beyond user preferences)

## 📈 Performance Optimization

### Startup Optimization
- Lazy-load images on tab activation
- Defer non-critical initialization
- Use background threads for file operations

### Memory Management
- Dispose image resources on tab close
- Limit image cache size
- Garbage collect on low memory

### Storage Optimization
- Compress old backup files (optional)
- Clean up orphaned images periodically
- Limit backup retention to 20 files

## 🏢 Enterprise Deployment

### Group Policy (Optional)
```
Administrative Templates\SnipShottyBoard\
├── Default Theme
├── Auto-save Interval
├── Maximum Backup Files
└── Log Retention Days
```

### Network Installation
```bash
# Deploy to network share
xcopy /s "SnipShottyBoard" "\\server\share\apps\SnipShottyBoard\"

# Create shortcuts
mklink "\\server\share\shortcuts\SnipShottyBoard.lnk" "\\server\share\apps\SnipShottyBoard\SnipShottyBoard.exe"
```

### Silent Installation Script
```batch
@echo off
echo Installing SnipShottyBoard...
xcopy /s /y "SnipShottyBoard" "%ProgramFiles%\SnipShottyBoard\"
echo Creating shortcuts...
mklink "%USERPROFILE%\Desktop\SnipShottyBoard.lnk" "%ProgramFiles%\SnipShottyBoard\SnipShottyBoard.exe"
echo Installation complete!
```

## 📞 Support Information

### Version Information
- **Current Version**: 1.2.1
- **.NET Target**: 8.0
- **Platform**: Windows 10/11 x64
- **Dependencies**: WPF-UI 4.0.3, Serilog

### Log Submission
For support requests, include:
1. Latest log file from `%APPDATA%\SnipShottyBoard\logs\`
2. System information (available in startup logs)
3. Reproduction steps
4. Expected vs actual behavior

### Developer Menu Access
**🔧 Developer** button in title bar provides:
- **📂 Open Logs Folder** - View current logs
- **📊 Backup Information** - Check data safety status
- **🗂️ Open Data Folder** - Access all app data
- **🔄 Force Save All** - Manual save trigger

---

*This deployment guide ensures SnipShottyBoard runs reliably in production environments with comprehensive monitoring and maintenance capabilities.*
