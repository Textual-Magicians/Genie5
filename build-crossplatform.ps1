# Cross-Platform Build Script for Genie5
# Builds the Avalonia-based cross-platform UI (Genie.UI)
#
# Usage:
#   .\build-crossplatform.ps1                    # Build for current platform
#   .\build-crossplatform.ps1 -Publish           # Create self-contained publish
#   .\build-crossplatform.ps1 -Runtime win-x64   # Build for specific runtime
#   .\build-crossplatform.ps1 -Runtime osx-arm64 -Publish  # Publish for macOS ARM

param(
    [string]$Runtime = "",           # Target runtime: win-x64, osx-x64, osx-arm64, linux-x64
    [switch]$Publish = $false,       # Create self-contained publish
    [string]$Configuration = "Release"
)

$Project = "src\Genie.UI\Genie.UI.csproj"
$OutputBase = "bin\CrossPlatform"

# Determine output folder name
if ($Runtime) {
    $OutputDir = "$OutputBase\$Runtime"
} else {
    $OutputDir = "$OutputBase\portable"
}

Write-Host "=== Genie Cross-Platform Build ===" -ForegroundColor Cyan
Write-Host "Project: Genie.UI (Avalonia)" -ForegroundColor White
Write-Host "Configuration: $Configuration" -ForegroundColor White

if ($Publish) {
    Write-Host "Mode: Self-contained publish" -ForegroundColor White
    if ($Runtime) {
        Write-Host "Runtime: $Runtime" -ForegroundColor White
    } else {
        Write-Host "Runtime: (portable/framework-dependent)" -ForegroundColor White
    }
    
    $publishArgs = @(
        "publish", $Project,
        "--configuration", $Configuration,
        "--output", $OutputDir
    )
    
    if ($Runtime) {
        $publishArgs += "--runtime"
        $publishArgs += $Runtime
        $publishArgs += "--self-contained"
        $publishArgs += "true"
    }
    
    Write-Host ""
    Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
    & dotnet @publishArgs
} else {
    Write-Host "Mode: Build only" -ForegroundColor White
    
    $buildArgs = @(
        "build", $Project,
        "--configuration", $Configuration
    )
    
    if ($Runtime) {
        $buildArgs += "--runtime"
        $buildArgs += $Runtime
        $OutputDir = "src\Genie.UI\bin\$Configuration\net10.0\$Runtime"
    } else {
        $OutputDir = "src\Genie.UI\bin\$Configuration\net10.0"
    }
    
    Write-Host ""
    Write-Host "Running: dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
    & dotnet @buildArgs
}

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Build FAILED!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build successful!" -ForegroundColor Green
Write-Host "Output: $OutputDir" -ForegroundColor Yellow

# Show available runtimes hint
Write-Host ""
Write-Host "Supported runtimes for -Runtime parameter:" -ForegroundColor Cyan
Write-Host "  win-x64     - Windows 64-bit" -ForegroundColor Gray
Write-Host "  win-arm64   - Windows ARM64" -ForegroundColor Gray
Write-Host "  osx-x64     - macOS Intel" -ForegroundColor Gray
Write-Host "  osx-arm64   - macOS Apple Silicon" -ForegroundColor Gray
Write-Host "  linux-x64   - Linux 64-bit" -ForegroundColor Gray
Write-Host "  linux-arm64 - Linux ARM64" -ForegroundColor Gray

