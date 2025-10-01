# 🔧 SnipShottyBoard MCP Server Setup Guide

This guide explains how to configure and use the Model Context Protocol (MCP) server for SnipShottyBoard development.

## 📋 Overview

The SnipShottyBoard MCP server provides development tools and automation for the WPF application. It includes file operations, build commands, code analysis, and project management features.

## ⚙️ Configuration

### Environment Variables

The MCP server supports configurable project paths using environment variables:

#### Primary Configuration

**`SSB_PROJECT_ROOT`** - Sets the project root directory
```bash
# Windows Command Prompt
set SSB_PROJECT_ROOT=C:\Users\YourName\Desktop\MyProjects\SnipShottyBoard

# Windows PowerShell
$env:SSB_PROJECT_ROOT = "C:\Users\YourName\Desktop\MyProjects\SnipShottyBoard"

# Linux/Mac
export SSB_PROJECT_ROOT="/home/username/projects/SnipShottyBoard"
```

#### Default Behavior

If `SSB_PROJECT_ROOT` is not set, the server defaults to:
```
C:\Users\Jeremy\Desktop\GitHub\SnipShottyBoard
```

### Claude Desktop Configuration

Add to your Claude Desktop configuration (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "snipshottyboard-mcp": {
      "command": "node",
      "args": ["C:\\Path\\To\\Your\\Project\\mcp-servers\\snipshottyboard-mcp.js"],
      "env": {
        "SSB_PROJECT_ROOT": "C:\\Path\\To\\Your\\Project"
      }
    }
  }
}
```

## 🚀 Quick Start

### 1. Prerequisites

- **Node.js 18+** installed
- **MCP SDK** dependencies installed in project
- **.NET 8 SDK** for WPF build operations

### 2. Installation

```bash
# Navigate to your project directory
cd C:\Path\To\Your\SnipShottyBoard

# Install MCP dependencies (if not already installed)
cd mcp-servers
npm install

# Test the server
node snipshottyboard-mcp.js
```

### 3. Environment Setup

**Option A: Global Environment Variable**
```powershell
# Set permanently (requires restart)
[Environment]::SetEnvironmentVariable("SSB_PROJECT_ROOT", "C:\Your\Project\Path", "User")

# Set for current session
$env:SSB_PROJECT_ROOT = "C:\Your\Project\Path"
```

**Option B: Batch File Helper**
Create `run-snipshottyboard-mcp.bat`:
```batch
@echo off
set SSB_PROJECT_ROOT=C:\Your\Project\Path
node snipshottyboard-mcp.js
```

**Option C: PowerShell Script**
Create `run-mcp.ps1`:
```powershell
$env:SSB_PROJECT_ROOT = "C:\Your\Project\Path"
node .\snipshottyboard-mcp.js
```

## 🛠️ Available Tools

The MCP server provides these tools (13/26 implemented):

### ✅ Core File Operations
- `read_file` - Read project files
- `write_file` - Write/modify files
- `list_directory` - Browse project structure
- `find_files` - Search for files by pattern

### ✅ Code Analysis
- `search_code` - Search code patterns
- `analyze_csharp_code` - C# code analysis

### ✅ Build & Run
- `build_wpf_app` - Build the WPF application
- `run_wpf_app` - Launch the application
- `check_dotnet_info` - Verify .NET environment
- `run_command` - Execute shell commands

### ✅ Development Tools
- `validate_json` - Validate JSON files
- `git_status` - Check Git repository status
- `generate_docs` - Create project documentation

### 🎯 Planned Tools (High Priority)
- `format_json` - Format and prettify JSON
- `git_commit` - Automated Git commits
- `backup_data` - Backup project data

## 🔧 Usage Examples

### Basic File Operations

```typescript
// Read a specific file
await use_mcp_tool({
  server_name: "snipshottyboard-mcp",
  tool_name: "read_file",
  arguments: { path: "MainWindow.xaml.cs" }
});

// Write to a file
await use_mcp_tool({
  server_name: "snipshottyboard-mcp", 
  tool_name: "write_file",
  arguments: { 
    path: "newfile.cs",
    content: "// New C# file content"
  }
});
```

### Build Operations

```typescript
// Build the application
await use_mcp_tool({
  server_name: "snipshottyboard-mcp",
  tool_name: "build_wpf_app",
  arguments: { configuration: "Debug" }
});

// Run the application
await use_mcp_tool({
  server_name: "snipshottyboard-mcp",
  tool_name: "run_wpf_app", 
  arguments: {}
});
```

### Code Analysis

```typescript
// Search for patterns
await use_mcp_tool({
  server_name: "snipshottyboard-mcp",
  tool_name: "search_code",
  arguments: { 
    pattern: "System.Windows",
    file_pattern: "*.cs"
  }
});

// Analyze C# code
await use_mcp_tool({
  server_name: "snipshottyboard-mcp",
  tool_name: "analyze_csharp_code",
  arguments: { path: "UI/TabManager.cs" }
});
```

## 📂 Project Structure Assumptions

The MCP server expects this project structure:
```
{SSB_PROJECT_ROOT}/
├── SnipShottyBoard.csproj
├── MainWindow.xaml
├── mcp-servers/
│   ├── snipshottyboard-mcp.js
│   └── package.json
├── Data/
├── UI/
├── Themes/
└── bin/Debug/net8.0-windows/
```

## 🐞 Troubleshooting

### Common Issues

**1. "Project root not found" Error**
```bash
# Check environment variable
echo $SSB_PROJECT_ROOT  # Linux/Mac
echo %SSB_PROJECT_ROOT%  # Windows CMD

# Set the variable correctly
export SSB_PROJECT_ROOT="/correct/path"
```

**2. Node.js Module Errors**
```bash
# Reinstall dependencies
cd mcp-servers
rm -rf node_modules package-lock.json
npm install
```

**3. Permission Errors**
```bash
# Windows: Run as Administrator
# Linux/Mac: Check file permissions
chmod +x snipshottyboard-mcp.js
```

**4. Build Tool Not Found**
```bash
# Verify .NET SDK
dotnet --version

# Should show 8.0.x or later
```

### Debugging

**Enable Debug Output:**
```javascript
// In snipshottyboard-mcp.js, add:
console.log('PROJECT_ROOT:', PROJECT_ROOT);
console.log('Working directory:', process.cwd());
```

**Test Server Manually:**
```bash
cd mcp-servers
node snipshottyboard-mcp.js
# Should show available tools without errors
```

## 🔄 Development Workflow

### Typical Development Session

1. **Set Environment**
   ```bash
   export SSB_PROJECT_ROOT="/path/to/project"
   ```

2. **Start MCP Server** (Claude Desktop handles this)

3. **Use Development Tools**
   - Analyze code patterns
   - Build and test changes
   - Generate documentation
   - Manage Git operations

4. **Test Changes**
   ```bash
   # Build and run
   mcp_tool: build_wpf_app
   mcp_tool: run_wpf_app
   ```

### Best Practices

- **Always set `SSB_PROJECT_ROOT`** for consistent behavior
- **Test server syntax** with `node snipshottyboard-mcp.js` before use
- **Update documentation** when adding new tools
- **Use relative paths** in tool arguments when possible

## 📊 Environment Variables Reference

| Variable | Purpose | Default | Example |
|----------|---------|---------|---------|
| `SSB_PROJECT_ROOT` | Project root directory | `C:\Users\Jeremy\Desktop\GitHub\SnipShottyBoard` | `/home/user/SnipShottyBoard` |
| `NODE_ENV` | Node environment | `development` | `production` |
| `DEBUG` | Debug output level | `false` | `true` |

## 🎯 Advanced Configuration

### Custom Tool Configuration

```javascript
// Add to snipshottyboard-mcp.js
const CUSTOM_CONFIG = {
  buildTimeout: 30000,
  maxFileSize: 1024 * 1024, // 1MB
  allowedExtensions: ['.cs', '.xaml', '.json', '.md']
};
```

### Multi-Project Support

```bash
# Switch between projects
export SSB_PROJECT_ROOT="/path/to/project1"
# ... work on project1 ...

export SSB_PROJECT_ROOT="/path/to/project2" 
# ... work on project2 ...
```

---

## 📞 Support

For MCP server issues:
1. Check environment variables are set correctly
2. Verify Node.js and .NET SDK versions
3. Test server manually with `node snipshottyboard-mcp.js`
4. Check Claude Desktop configuration
5. Review server logs for error details

**Remember**: The MCP server is a development tool - it assumes you have appropriate development environment setup!
