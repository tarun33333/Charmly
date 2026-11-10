<#
.SYNOPSIS
    Launcher and build helper for Charmly by Tarun.
.DESCRIPTION
    Builds and runs the full WinUI 3 + Win2D Charmly application using .NET 9.
#>

[CmdletBinding()]
param(
    [switch]$BuildOnly,
    [switch]$RunTests,
    [string]$Configuration = "Release"
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

$WorkspaceRoot = $PSScriptRoot
Set-Location $WorkspaceRoot

if ($RunTests) {
    Write-Host "[*] Running Charmly Core unit tests..." -ForegroundColor Cyan
    & dotnet test "tests\Hangly.Core.Tests\Hangly.Core.Tests.csproj" -c $Configuration --no-restore
    exit $LASTEXITCODE
}

Write-Host "[*] Building Charmly ($Configuration, x64)..." -ForegroundColor Cyan
& dotnet build "src\Hangly.App\Hangly.App.csproj" -c $Configuration -r win-x64 -p:Platform=x64 --no-restore

if (-not $BuildOnly) {
    $ExePath = Join-Path $WorkspaceRoot "src\Hangly.App\bin\x64\$Configuration\net9.0-windows10.0.22621.0\win-x64\Charmly.exe"
    if (-not (Test-Path $ExePath)) {
        $ExePath = Join-Path $WorkspaceRoot "src\Hangly.App\bin\x64\$Configuration\net9.0-windows10.0.22621.0\win-x64\Hangly.exe"
    }
    if (Test-Path $ExePath) {
        Write-Host "[+] Launching Charmly Desktop Charms & Library..." -ForegroundColor Green
        Write-Host "Look at the top of your screen or click the tray icon for Customize & Library!" -ForegroundColor Yellow
        $res = ([wmiclass]"win32_process").Create($ExePath)
        if ($res.ReturnValue -ne 0) {
            Start-Process -FilePath $ExePath
        }
    } else {
        Write-Error "Executable not found at: $ExePath"
    }
}
