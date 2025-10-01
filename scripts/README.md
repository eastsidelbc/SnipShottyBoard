# SnipShottyBoard - Publishing & Distribution Guide

This folder contains scripts and documentation for building distributable versions of SnipShottyBoard.

## 📦 Building a Release Executable

### Quick Start
```powershell
# From the project root directory:
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

The script will:
1. Restore NuGet packages
2. Clean previous builds  
3. Create a single-file .exe for Windows x64
4. Display the final executable location

### Output Location
Your distributable .exe will be created at:
```
bin\Release\net8.0-windows\win-x64\publish\SnipShottyBoard.exe
```

## 🎨 Custom App Icon

### Icon Requirements
- **File**: `assets/app.ico` 
- **Format**: Windows .ico file (not PNG/JPG)
- **Recommended sizes**: 256x256 (primary), plus 64x64, 48x48, 32x32, 16x16 variants
- **Multi-size**: Include multiple resolutions in a single .ico file for best display

### Converting PNG to ICO
If you only have a PNG file:
1. **Online converters**: Use ICO Convert (iconvert.com) or similar
2. **GIMP**: Export as .ico with multiple sizes
3. **ImageMagick**: `convert icon.png -define icon:auto-resize=256,64,48,32,16 app.ico`

### Setting Your Icon
1. Replace `assets/app.ico` with your custom icon
2. Rebuild using the publish script
3. Your icon will appear in:
   - File Explorer (executable icon)
   - Taskbar when running
   - Window title bar
   - Alt+Tab switcher

## 🚀 Alternative Build Commands

### Windows ARM64 (for Surface devices, etc.)
```powershell
dotnet publish .\SnipShottyBoard.csproj -c Release -r win-arm64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false /p:DebugSymbols=false /p:DebugType=None
```

### Framework-Dependent (smaller file, requires .NET 8 on target)
```powershell
dotnet publish .\SnipShottyBoard.csproj -c Release /p:PublishSingleFile=true /p:DebugSymbols=false /p:DebugType=None
```

### Debug Build (for testing)
```powershell
dotnet publish .\SnipShottyBoard.csproj -c Debug -r win-x64 /p:PublishSingleFile=true
```

## 📋 Distribution Checklist

✅ **Before Distribution:**
- [ ] Test the .exe on a clean Windows machine (without Visual Studio)
- [ ] Verify the custom icon appears correctly
- [ ] Check that all features work (notes, images, themes, etc.)
- [ ] Scan with Windows Defender (to avoid false positives)

✅ **File Properties:**
- Single-file executable (~50-100MB typical size)
- No external dependencies required
- Runs on Windows 10/11 (x64)
- No installation needed - just copy and run

## 🛠️ Troubleshooting

### "Execution Policy" Error
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser
```
Or run with explicit bypass:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

### ".NET SDK not found"
- Install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
- Verify with: `dotnet --version`

### Icon Not Showing
- Ensure `assets/app.ico` exists and is a valid .ico file
- Clear Windows icon cache: `ie4uinit.exe -show`
- Rebuild and test on a different machine

### Large File Size
- Self-contained builds include the .NET runtime (~50MB base)
- For smaller files, use framework-dependent builds (requires .NET 8 on target)

## 🔗 Additional Resources

- [.NET Publishing Documentation](https://docs.microsoft.com/en-us/dotnet/core/deploying/)
- [WPF Deployment Guide](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/deployment/)
- [Windows Icon Guidelines](https://docs.microsoft.com/en-us/windows/apps/design/style/iconography/app-icon-construction)
