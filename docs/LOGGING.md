# 🐛 SnipShottyBoard Logging Guide

This document explains the logging system in SnipShottyBoard and how to use it for debugging and monitoring.

## 📋 Overview

SnipShottyBoard uses a centralized logging system built on **Serilog** with structured logging, automatic file rotation, and category-based organization. All logging goes through the `LoggingService` class which provides both file and debug output.

## 📂 Log File Locations

**Primary Log File:**
```
%APPDATA%\SnipShottyBoard\logs\snipshottyboard-YYYY-MM-DD.log
```

**Example Paths:**
- Windows: `C:\Users\{Username}\AppData\Roaming\SnipShottyBoard\logs\snipshottyboard-2024-12-28.log`

**Log Rotation:**
- Daily rotation (new file each day)
- Retains 7 days of logs automatically
- Automatic cleanup of old log files

## 🎯 Log Categories

The system uses categories to organize log entries:

| Category | Purpose | Example Usage |
|----------|---------|---------------|
| **UI** | User interface operations | Window initialization, user interactions |
| **Manager** | Business logic operations | Tab management, theme switching |
| **Data** | File I/O and persistence | Note saving, image management |
| **General** | Uncategorized operations | Default category |

## 📊 Log Levels

| Level | Icon | Usage | Example |
|-------|------|-------|---------|
| **Debug** | 🐛 | Detailed development info | `loggingService.LogDebug("Tab created", "UI")` |
| **Info** | ℹ️ | General information | `loggingService.LogInfo("App started", "General")` |
| **Warning** | ⚠️ | Potential issues | `loggingService.LogWarning("Resource not found", "UI")` |
| **Error** | ❌ | Exceptions and errors | `loggingService.LogError("Save failed", ex, "Data")` |

## 🔧 How to Use Logging

### Basic Usage

```csharp
// Initialize logging service (usually in MainWindow)
private readonly LoggingService loggingService = new LoggingService();

// Debug messages
loggingService.LogDebug("Starting operation", "UI");

// Information
loggingService.LogInfo("User action completed", "Manager");

// Warnings
loggingService.LogWarning("Resource fallback used", "UI");

// Errors with exception
try 
{
    // Some operation
}
catch (Exception ex)
{
    loggingService.LogError("Operation failed", ex, "Data");
}
```

### Category Guidelines

**UI Category:**
- Window lifecycle events
- User interactions
- Theme changes
- Control updates

**Manager Category:**
- Tab operations
- Settings changes
- Business logic flow
- Event handling

**Data Category:**
- File operations
- JSON serialization
- Database operations
- Image management

## 📝 Log Format

**Debug Output Format:**
```
[14:30:45.123] [DBG] UI: Tab created successfully
[14:30:45.456] [ERR] Data: Failed to save file - Access denied
```

**File Output Format:**
```
[2024-12-28 14:30:45.123] [DBG] UI: Tab created successfully
[2024-12-28 14:30:45.456] [ERR] Data: Failed to save file - Access denied
System.UnauthorizedAccessException: Access to the path is denied.
   at System.IO.File.WriteAllText(String path, String contents)
```

## 🔍 Monitoring Logs

### Real-time Monitoring

**PowerShell (Windows):**
```powershell
Get-Content "$env:APPDATA\SnipShottyBoard\logs\snipshottyboard-$(Get-Date -Format 'yyyy-MM-dd').log" -Wait -Tail 10
```

**Command Prompt:**
```cmd
tail -f "%APPDATA%\SnipShottyBoard\logs\snipshottyboard-*.log"
```

### Visual Studio Debug Output

When running in development, all logs also appear in the Visual Studio Debug Output window with the same formatting.

## 🔧 Configuration

### Log Level Configuration

The logging system is configured in `LoggingService.cs`:

```csharp
.MinimumLevel.Debug()  // Change to Information, Warning, or Error as needed
```

### File Retention

```csharp
.WriteTo.File(
    LogFilePath,
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 7  // Keep 7 days of logs
)
```

## 🚨 Common Log Patterns

### Application Startup
```
[DBG] UI: Starting MainWindow initialization
[DBG] UI: All managers initialized
[DBG] UI: Timers started
[DBG] UI: Event handlers wired up
[INF] General: Application startup complete
```

### Error Patterns
```
[ERR] Data: Error saving notes: System.IO.IOException: The process cannot access the file
[WRN] UI: Theme resource 'MissingBrush' not found, using fallback
[ERR] Manager: Tab creation failed: System.ArgumentNullException: Value cannot be null
```

### Performance Monitoring
```
[DBG] Manager: Tab switch completed in 45ms
[DBG] Data: Auto-save completed in 120ms
[WRN] UI: Image loading took 2.3s - consider optimization
```

## 🐞 Debugging Tips

### Finding Issues
1. **Look for ERROR level entries** - these indicate actual problems
2. **Check WARNING entries** - these may indicate degraded functionality
3. **Use categories to filter** - focus on relevant subsystems
4. **Monitor timing** - DEBUG entries often include performance info

### Common Issues
- **File access errors**: Check permissions on `%APPDATA%\SnipShottyBoard`
- **Theme resource warnings**: Indicates missing or invalid theme resources
- **Image loading failures**: Check image file corruption or unsupported formats
- **JSON serialization errors**: May indicate data corruption

### Log File Not Created
If log files aren't being created:
1. Check `%APPDATA%\SnipShottyBoard\logs` folder exists
2. Verify write permissions to AppData folder
3. Look for fallback debug output in Visual Studio
4. Check if antivirus is blocking file creation

## 🔄 Log Rotation Details

- **Daily Files**: New log file created each day at midnight
- **Filename Pattern**: `snipshottyboard-YYYY-MM-DD.log`
- **Automatic Cleanup**: Files older than 7 days are automatically deleted
- **Size Limits**: No per-file size limits (daily rotation handles this)

## 🎯 Integration with Other Systems

### MCP Server Integration
The MCP development server can access logs for debugging:
```javascript
// In snipshottyboard-mcp.js
const logPath = path.join(process.env.APPDATA, 'SnipShottyBoard', 'logs');
```

### Exception Handling
All manager classes should use the logging service:
```csharp
catch (Exception ex)
{
    loggingService.LogError("Operation description", ex, "CategoryName");
    // Handle gracefully without crashing
}
```

---

## 📞 Support

For logging-related issues:
1. Check this documentation first
2. Examine recent log entries for clues
3. Enable Debug level logging for detailed output
4. Check Windows Event Viewer for system-level issues

**Remember**: Logs are your best friend for understanding application behavior and diagnosing issues!
