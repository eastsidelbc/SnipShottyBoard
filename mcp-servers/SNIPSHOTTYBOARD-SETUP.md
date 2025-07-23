# SnipShottyBoard MCP Server Setup Guide

## What is this?
This is a specialized MCP (Model Context Protocol) server designed specifically for developing your SnipShottyBoard WPF sticky notes application. It provides tools tailored for C#/WPF development, including building, testing, code analysis, and project management.

## Features
The SnipShottyBoard MCP server includes these specialized tools:

### 🔧 Development Tools
- **build_wpf_app** - Build your WPF application (Debug/Release)
- **run_wpf_app** - Launch your WPF application  
- **test_wpf_app** - Run unit tests
- **check_dotnet_info** - Check .NET SDK version and info

### 📁 File Management
- **read_file** / **write_file** - File operations
- **list_directory** - Browse project structure
- **find_files** - Search for files by pattern
- **create_backup** - Create project backups

### 🔍 Code Analysis
- **analyze_csharp_code** - Analyze C# files for metrics
- **search_code** - Search across C#, XAML, JSON, and MD files
- **generate_docs** - Generate documentation from code comments

### 📦 Package Management
- **manage_nuget_packages** - List, add, or update NuGet packages

### 💾 Data Management
- **validate_json** - Validate JSON files (settings, note data)
- **format_json** - Format and prettify JSON files

### 📊 Git Operations
- **git_status** - Check git status and recent commits
- **git_commit** - Commit changes with messages

### ⚙️ General
- **run_command** - Execute any shell command

## Setup Instructions

### 1. Install Dependencies
The MCP server uses the existing Node.js setup in the mcp-server directory:
```bash
cd M:\G14C\mcp-server
npm install
```

### 2. Configure Claude Desktop
Copy the contents of `claude_desktop_config_snipshottyboard.json` to your Claude Desktop configuration:

**Windows Path:** `%APPDATA%\Claude\claude_desktop_config.json`

Or merge with your existing config:
```json
{
  "mcpServers": {
    "snipshottyboard": {
      "command": "node",
      "args": ["M:\\G14C\\mcp-server\\snipshottyboard-mcp.js"],
      "env": {
        "NODE_PATH": "M:\\G14C\\mcp-server\\node_modules"
      }
    }
  }
}
```

### 3. Test the Server
You can test the MCP server by running:
```bash
run-snipshottyboard-mcp.bat
```

### 4. Restart Claude Desktop
After updating the configuration, restart Claude Desktop to load the new MCP server.

## Usage Examples

Once set up, you can use commands like:

- "Build my WPF app in Release mode"
- "Run the application" 
- "Search for 'TabManager' in my C# files"
- "Analyze the MainWindow.xaml.cs file"
- "List my NuGet packages"
- "Create a backup called 'pre-refactor'"
- "Check git status"
- "Validate the settings.json file"

## Troubleshooting

### Server Won't Start
- Check that Node.js is installed and in PATH
- Verify the path M:\G14C exists and contains your project
- Make sure @modelcontextprotocol/sdk is installed in node_modules

### Claude Desktop Issues
- Verify the configuration file path and JSON syntax
- Check Claude Desktop logs for connection errors
- Restart Claude Desktop after configuration changes

### Permission Issues
- Ensure the MCP server has read/write access to M:\G14C
- Run Claude Desktop as administrator if needed

## Extending the Server

The server is designed to be extensible. You can add new tools by:

1. Adding a new tool definition in `setupToolHandlers()`
2. Implementing the corresponding method
3. Adding the case in the request handler switch statement

## Project Structure
```
mcp-server/
├── snipshottyboard-mcp.js          # Main MCP server
├── claude_desktop_config_snipshottyboard.json  # Claude config
├── run-snipshottyboard-mcp.bat     # Start script
├── SNIPSHOTTYBOARD-SETUP.md        # This guide
└── node_modules/                   # Dependencies
```

This MCP server is specifically optimized for your SnipShottyBoard project and provides all the tools you need for efficient WPF development!