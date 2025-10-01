# SnipShottyBoard - Windows Release Publishing Script
# Builds a single-file .exe for distribution

Write-Host "🚀 Starting SnipShottyBoard Release Build..." -ForegroundColor Green
Write-Host ""

# Navigate to project root (script should be run from project root)
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot

try {
    # Step 1: Restore NuGet packages
    Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE"
    }
    
    # Step 2: Clean previous builds
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path "bin\Release") {
        Remove-Item "bin\Release" -Recurse -Force
    }
    
    # Step 3: Publish single-file executable for Windows x64
    Write-Host "🔨 Publishing single-file Release build for Windows x64..." -ForegroundColor Yellow
    dotnet publish .\SnipShottyBoard.csproj `
        -c Release `
        -r win-x64 `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishTrimmed=false `
        /p:DebugSymbols=false `
        /p:DebugType=None
    
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
    
    # Step 4: Check if the executable was created
    $exePath = "bin\Release\net8.0-windows\win-x64\publish\SnipShottyBoard.exe"
    if (Test-Path $exePath) {
        $fullPath = (Get-Item $exePath).FullName
        $fileSize = [Math]::Round((Get-Item $exePath).Length / 1MB, 2)
        
        Write-Host ""
        Write-Host "✅ Build completed successfully!" -ForegroundColor Green
        Write-Host "📍 Executable location: $fullPath" -ForegroundColor Cyan
        Write-Host "📊 File size: $fileSize MB" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "🎯 Ready to distribute! You can now:" -ForegroundColor Green
        Write-Host "   • Double-click the .exe to run" -ForegroundColor White
        Write-Host "   • Copy to any Windows 10/11 machine" -ForegroundColor White
        Write-Host "   • No .NET installation required on target machine" -ForegroundColor White
    }
    else {
        throw "❌ Expected executable not found at: $exePath"
    }
}
catch {
    Write-Host ""
    Write-Host "❌ Build failed: $_" -ForegroundColor Red
    Write-Host "💡 Try running 'dotnet --version' to check your .NET installation" -ForegroundColor Yellow
    exit 1
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "🏁 Build script completed!" -ForegroundColor Green
