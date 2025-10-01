<# 
  scripts/publish-release.ps1
  Build a versioned Windows Release for SnipShottyBoard and zip it.

  USAGE:
    pwsh scripts/publish-release.ps1             # bump Patch (default)
    pwsh scripts/publish-release.ps1 -Bump Minor # bump Minor
    pwsh scripts/publish-release.ps1 -Version 1.3.0  # set exact version
    pwsh scripts/publish-release.ps1 -SelfContained $true # larger exe, no .NET needed on target

  OUTPUT:
    publish/<version>/win-x64/SnipShottyBoard.exe
    publish/SnipShottyBoard_v<version>_win-x64.zip
#>

[CmdletBinding()]
param(
    [ValidateSet('Major', 'Minor', 'Patch')]
    [string] $Bump = 'Patch',

    [string] $Version,

    [string] $Runtime = 'win-x64',

    [bool] $SelfContained = $false
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "🚀 Publish (versioned) starting..." -ForegroundColor Green

# --- Paths ---
$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$csproj = Join-Path $repoRoot 'SnipShottyBoard.csproj'
$versionFile = Join-Path $repoRoot 'VERSION'
$publishDir = Join-Path $repoRoot 'publish'
$appName = 'SnipShottyBoard'
$tfm = 'net8.0-windows'

Push-Location $repoRoot
try {
    # ---- VERSION resolve ----
    function Parse-Version([string]$v) {
        if ($v -notmatch '^\d+\.\d+\.\d+$') { throw "VERSION must be MAJOR.MINOR.PATCH (got '$v')" }
        $p = $v.Split('.'); [pscustomobject]@{Major = [int]$p[0]; Minor = [int]$p[1]; Patch = [int]$p[2] }
    }

    if ($Version) {
        $newVer = $Version.Trim()
        [void](Parse-Version $newVer)
    }
    else {
        if (-not (Test-Path $versionFile)) { '1.0.0' | Set-Content -NoNewline -Encoding UTF8 $versionFile }
        $cur = (Get-Content $versionFile -Raw).Trim()
        $v = Parse-Version $cur
        switch ($Bump) {
            'Major' { $v = [pscustomobject]@{Major = $v.Major + 1; Minor = 0; Patch = 0 } }
            'Minor' { $v = [pscustomobject]@{Major = $v.Major; Minor = $v.Minor + 1; Patch = 0 } }
            'Patch' { $v = [pscustomobject]@{Major = $v.Major; Minor = $v.Minor; Patch = $v.Patch + 1 } }
        }
        $newVer = "{0}.{1}.{2}" -f $v.Major, $v.Minor, $v.Patch
        $newVer | Set-Content -NoNewline -Encoding UTF8 $versionFile
    }

    Write-Host "🏷️  Version: $newVer" -ForegroundColor Cyan

    # ---- restore ----
    Write-Host "📦 dotnet restore" -ForegroundColor Yellow
    dotnet restore $csproj | Out-Null

    # ---- publish out dir ----
    $outDir = Join-Path $publishDir (Join-Path $newVer $Runtime)  # publish/<ver>/win-x64
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null

    # ---- publish ----
    $sc = $SelfContained ? 'true' : 'false'
    Write-Host "🔨 dotnet publish → $outDir (SelfContained=$SelfContained)" -ForegroundColor Yellow
    dotnet publish $csproj `
        -c Release `
        -r $Runtime `
        -o $outDir `
        /p:PublishSingleFile=true `
        /p:SelfContained=$sc `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishTrimmed=false `
        /p:DebugSymbols=false `
        /p:DebugType=None `
        /p:EnableCompressionInSingleFile=true | Out-Null

    $exePath = Join-Path $outDir "$appName.exe"
    if (-not (Test-Path $exePath)) { throw "Expected exe not found: $exePath" }

    # ---- zip artifact ----
    $zipName = "{0}_v{1}_{2}.zip" -f $appName, $newVer, $Runtime
    $zipPath = Join-Path $publishDir $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Write-Host "🗜️  Creating zip → $zipPath" -ForegroundColor Yellow
    Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zipPath

    # ---- summary ----
    $sizeMB = [Math]::Round((Get-Item $exePath).Length / 1MB, 2)
    Write-Host "`n✅ Publish complete" -ForegroundColor Green
    Write-Host "📍 EXE: $exePath" -ForegroundColor Cyan
    Write-Host "📦 ZIP: $zipPath" -ForegroundColor Cyan
    Write-Host "📊 Size: $sizeMB MB" -ForegroundColor Cyan

    # surface for caller script
    [pscustomobject]@{ Version = $newVer; Zip = $zipPath; OutDir = $outDir } | ConvertTo-Json -Compress
}
catch {
    Write-Host "`n❌ Publish failed: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
