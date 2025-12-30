# Build and Run Script for Genie Cross-Platform UI (Avalonia)
# Simple script for Windows development - builds and copies to Run folder

$ErrorActionPreference = "Stop"

$BuildConfig = "Release"
$TargetFramework = "net10.0"
$Project = "src\Genie.UI\Genie.UI.csproj"
$SourceDir = "src\Genie.UI\bin\$BuildConfig\$TargetFramework"
$RunDir = "bin\Avalonia"

Write-Host "Building Genie (Cross-Platform UI)..." -ForegroundColor Cyan
dotnet build $Project --configuration $BuildConfig

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Create Run directory if it doesn't exist
if (!(Test-Path $RunDir)) {
    New-Item -ItemType Directory -Path $RunDir | Out-Null
    Write-Host "Created $RunDir directory" -ForegroundColor Yellow
}

# Copy all files to Run folder
Write-Host "Copying files to $RunDir..." -ForegroundColor Cyan
Copy-Item -Path "$SourceDir\*" -Destination $RunDir -Recurse -Force

Write-Host "Files copied to $RunDir" -ForegroundColor Green
Write-Host ""
Write-Host "To run Genie, use:" -ForegroundColor White
Write-Host "  .\bin\Avalonia\Genie.exe" -ForegroundColor Yellow
Write-Host ""

# Check for -Run parameter (non-interactive) or prompt
if ($args -contains "-Run" -or $args -contains "-r") {
    Write-Host "Starting Genie..." -ForegroundColor Cyan
    Start-Process "$RunDir\Genie.exe"
} else {
    # Ask if user wants to run
    $run = Read-Host "Run Genie now? (y/n)"
    if ($run -eq "y" -or $run -eq "Y") {
        Write-Host "Starting Genie..." -ForegroundColor Cyan
        Start-Process "$RunDir\Genie.exe"
    }
}

