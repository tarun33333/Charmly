<#
.SYNOPSIS
    Builds and packages Charmly into a self-contained release ZIP ready for GitHub Releases.
#>

[CmdletBinding()]
param(
    [string]$Version = "v0.9.1",
    [string]$Architecture = "win-x64"
)

$ErrorActionPreference = "Stop"

# Ensure .NET 9 is in session PATH
$DotnetLocal = "$env:LOCALAPPDATA\Microsoft\dotnet"
if (Test-Path "$DotnetLocal\dotnet.exe") {
    $env:PATH = "$DotnetLocal;$env:PATH"
}
if (Test-Path "D:\Hangly\.dotnet") { $env:DOTNET_CLI_HOME = "D:\Hangly\.dotnet" }
if (Test-Path "D:\Hangly\.appdata") { $env:APPDATA = "D:\Hangly\.appdata" }
if (Test-Path "D:\Hangly\.nuget\packages") { $env:NUGET_PACKAGES = "D:\Hangly\.nuget\packages" }

$WorkspaceRoot = Split-Path -Parent $PSScriptRoot
Set-Location $WorkspaceRoot

$PublishDir = Join-Path $WorkspaceRoot "publish\$Architecture"
$ZipName = "Charmly-$Version-Windows-x64.zip"
$ZipPath = Join-Path $WorkspaceRoot "publish\$ZipName"

Write-Host "[*] Publishing self-contained Charmly ($Architecture, Release)..." -ForegroundColor Cyan
& dotnet publish "src\Hangly.App\Hangly.App.csproj" -c Release -r $Architecture --self-contained true -o $PublishDir

# Clean up any legacy app binaries so the package contains Charmly.exe
Remove-Item (Join-Path $PublishDir "Hangly.exe") -Force -ErrorAction SilentlyContinue

Write-Host "[*] Creating Release ZIP package..." -ForegroundColor Cyan
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}
[System.IO.Compression.ZipFile]::CreateFromDirectory($PublishDir, $ZipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

$SizeMB = [math]::Round((Get-Item $ZipPath).Length / 1MB, 2)
Write-Host "[+] Release ZIP package created: $ZipPath ($SizeMB MB)" -ForegroundColor Green

Write-Host "[*] Building standalone Setup Wizard (CharmlySetup.exe)..." -ForegroundColor Cyan
$InstallerPublishDir = Join-Path $WorkspaceRoot "publish\installer"
& dotnet publish "src\Charmly.Installer\Charmly.Installer.csproj" -c Release -r $Architecture --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $InstallerPublishDir

$FinalSetupExe = Join-Path $WorkspaceRoot "publish\CharmlySetup.exe"
Copy-Item (Join-Path $InstallerPublishDir "CharmlySetup.exe") $FinalSetupExe -Force
$SetupSizeMB = [math]::Round((Get-Item $FinalSetupExe).Length / 1MB, 2)

Write-Host "[+] Release artifacts ready for GitHub Releases:" -ForegroundColor Green
Write-Host "    1. Standalone Installer: $FinalSetupExe ($SetupSizeMB MB)" -ForegroundColor Yellow
Write-Host "    2. Portable ZIP Package: $ZipPath ($SizeMB MB)" -ForegroundColor Yellow
Write-Host "You can upload both files to your GitHub Release at https://github.com/tarun33333/Charmly/releases/new" -ForegroundColor Cyan
