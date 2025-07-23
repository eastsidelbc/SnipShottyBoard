# SnipShottyBoard MCP Server - Features Roadmap

**Last Updated:** July 16, 2025  
**Project:** SnipShottyBoard WPF Sticky Notes Application  
**MCP Server Version:** 1.0.1  

---

## 📊 **CURRENT STATUS**

**✅ COMPLETED: 13 of 26 features (50%)**

### **Core Features (Working)**
- ✅ `read_file` - Read contents of files in the project
- ✅ `write_file` - Write content to files in the project  
- ✅ `list_directory` - List contents of directories
- ✅ `find_files` - Find files matching patterns (FIXED - supports wildcards like *.cs)
- ✅ `search_code` - Search for code patterns across the project
- ✅ `analyze_csharp_code` - Analyze C# code for issues and patterns
- ✅ `build_wpf_app` - Build the WPF application (FIXED - detects running apps)
- ✅ `run_wpf_app` - Run the WPF application (FIXED - detects if already running)
- ✅ `check_dotnet_info` - Check .NET SDK info (FIXED - avoids problematic commands)
- ✅ `run_command` - Execute shell commands

### **Recently Added (Session July 16, 2025)**
- ✅ `validate_json` - Validate JSON files (note data, settings, etc.)
- ✅ `git_status` - Check git status and recent commits
- ✅ `generate_docs` - Create API documentation from C# XML comments

---

## 🎯 **HIGH PRIORITY - Next to Add**

### **JSON & Data Management**
- 🔲 `format_json` - Pretty-format JSON files for debugging
- 🔲 `backup_data` - Backup your notes and settings to timestamped folders

### **Git Operations** 
- 🔲 `git_commit` - Commit your development changes
- 🔲 `git_init` - Initialize git repo if not already done

### **Project Documentation**
- 🔲 `create_changelog` - Generate changelog from git commits  
- 🔲 `analyze_project_structure` - Overview of your architecture

**Recommended Next 3:** `format_json`, `git_commit`, `backup_data`

---

## 🔧 **MEDIUM PRIORITY - Development Tools**

### **Testing & Quality**
- 🔲 `test_wpf_app` - Run unit tests (when you add them)
- 🔲 `analyze_performance` - Check for potential performance issues
- 🔲 `check_code_quality` - Analyze code complexity and maintainability

### **Package Management**
- 🔲 `manage_nuget_packages` - List/add/update packages like WPF-UI, Serilog
- 🔲 `update_packages` - Update all packages to latest versions
- 🔲 `audit_packages` - Check for security vulnerabilities

### **Image & Media Tools** 
- 🔲 `optimize_images` - Compress images in your project
- 🔲 `analyze_media_usage` - Check which images are being used

---

## 📚 **LOW PRIORITY - Nice to Have**

### **Deployment**
- 🔲 `create_installer` - Build installer packages
- 🔲 `publish_app` - Create release builds  
- 🔲 `package_for_distribution` - Zip releases with all dependencies

### **Roadmap Management**
- 🔲 `update_roadmap` - Add completed features to roadmap
- 🔲 `track_todos` - Find and organize TODO comments
- 🔲 `generate_feature_list` - Extract features from roadmap

### **Specialized**
- 🔲 `monitor_app_logs` - Watch Serilog output files
- 🔲 `cleanup_temp_files` - Clean bin/obj and temp directories
- 🔲 `analyze_wpf_resources` - Check XAML resources and themes

---

## 🚀 **IMPLEMENTATION STRATEGY**

### **Phase 1: Core Productivity (HIGH PRIORITY)**
Focus on essential development workflow tools that provide immediate value:

1. **JSON Management** - `format_json`, `backup_data`
2. **Git Workflow** - `git_commit`, `git_init` 
3. **Documentation** - `create_changelog`, `analyze_project_structure`

### **Phase 2: Development Quality (MEDIUM PRIORITY)**
Add tools for code quality and project maintenance:

1. **Testing Tools** - `test_wpf_app`, `check_code_quality`
2. **Package Management** - `manage_nuget_packages`, `update_packages`
3. **Performance** - `analyze_performance`, `optimize_images`

### **Phase 3: Advanced Features (LOW PRIORITY)**
Polish and specialized features for mature development:

1. **Deployment** - `create_installer`, `publish_app`
2. **Project Management** - `update_roadmap`, `track_todos`
3. **Maintenance** - `cleanup_temp_files`, `monitor_app_logs`

---

## 🔧 **TECHNICAL NOTES**

### **Fixed Issues**
- ✅ **File Pattern Search** - `*.cs` wildcard patterns now work correctly
- ✅ **.NET CLI Issues** - Uses `--version`, `--list-sdks`, `--list-runtimes` instead of problematic `--info`
- ✅ **App Running Detection** - Build/run tools detect if SnipShottyBoard.exe is already running

### **Architecture** 
- **Pattern:** Each feature requires 3 code additions:
  1. Tool definition in `tools` array
  2. Case statement in request handler  
  3. Method implementation in class

### **File Location**
- **MCP Server:** `mcp-servers/snipshottyboard-mcp.js`
- **This Document:** `FEATURES_TO_ADD.md` (project root)

---

## 📝 **CHANGE LOG**

### **July 16, 2025**
- ✅ Added `validate_json` - Validate JSON files
- ✅ Added `git_status` - Check git status and recent commits  
- ✅ Added `generate_docs` - Generate API documentation
- ✅ Fixed `find_files` wildcard pattern bug
- ✅ Created this features roadmap document

### **Previous Sessions**
- ✅ Fixed .NET CLI issues
- ✅ Fixed file pattern search 
- ✅ Implemented core file operations
- ✅ Added C# code analysis tools
- ✅ Added build and run functionality

---

## 🎯 **QUICK REFERENCE**

**To add next feature:**
1. Add tool definition to `tools` array
2. Add case to `switch` statement  
3. Add method implementation before `async run()`
4. Test with `node snipshottyboard-mcp.js`
5. Restart Claude Desktop
6. Update this document

**Current working features:** 13/26 (50% complete)  
**Next milestone:** Complete HIGH PRIORITY features (6 remaining)  
**Project focus:** WPF sticky notes application development tools