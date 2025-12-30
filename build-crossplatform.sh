#!/bin/bash
# Cross-Platform Build Script for Genie5 (Linux/macOS)
# Builds the Avalonia-based cross-platform UI (Genie.UI)
#
# Usage:
#   ./build-crossplatform.sh                     # Build for current platform
#   ./build-crossplatform.sh --publish           # Create self-contained publish
#   ./build-crossplatform.sh --runtime osx-arm64 # Build for specific runtime
#   ./build-crossplatform.sh --runtime linux-x64 --publish  # Publish for Linux

set -e

PROJECT="src/Genie.UI/Genie.UI.csproj"
OUTPUT_BASE="bin/CrossPlatform"
CONFIGURATION="Release"
RUNTIME=""
PUBLISH=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --runtime|-r)
            RUNTIME="$2"
            shift 2
            ;;
        --publish|-p)
            PUBLISH=true
            shift
            ;;
        --configuration|-c)
            CONFIGURATION="$2"
            shift 2
            ;;
        --help|-h)
            echo "Usage: $0 [options]"
            echo ""
            echo "Options:"
            echo "  --runtime, -r <RID>    Target runtime (win-x64, osx-x64, osx-arm64, linux-x64)"
            echo "  --publish, -p          Create self-contained publish"
            echo "  --configuration, -c    Build configuration (default: Release)"
            echo ""
            echo "Supported runtimes:"
            echo "  win-x64     - Windows 64-bit"
            echo "  win-arm64   - Windows ARM64"
            echo "  osx-x64     - macOS Intel"
            echo "  osx-arm64   - macOS Apple Silicon"
            echo "  linux-x64   - Linux 64-bit"
            echo "  linux-arm64 - Linux ARM64"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Determine output folder
if [ -n "$RUNTIME" ]; then
    OUTPUT_DIR="$OUTPUT_BASE/$RUNTIME"
else
    OUTPUT_DIR="$OUTPUT_BASE/portable"
fi

echo "=== Genie Cross-Platform Build ==="
echo "Project: Genie.UI (Avalonia)"
echo "Configuration: $CONFIGURATION"

if [ "$PUBLISH" = true ]; then
    echo "Mode: Self-contained publish"
    if [ -n "$RUNTIME" ]; then
        echo "Runtime: $RUNTIME"
        dotnet publish "$PROJECT" \
            --configuration "$CONFIGURATION" \
            --output "$OUTPUT_DIR" \
            --runtime "$RUNTIME" \
            --self-contained true
    else
        echo "Runtime: (portable/framework-dependent)"
        dotnet publish "$PROJECT" \
            --configuration "$CONFIGURATION" \
            --output "$OUTPUT_DIR"
    fi
else
    echo "Mode: Build only"
    
    if [ -n "$RUNTIME" ]; then
        dotnet build "$PROJECT" \
            --configuration "$CONFIGURATION" \
            --runtime "$RUNTIME"
        OUTPUT_DIR="src/Genie.UI/bin/$CONFIGURATION/net10.0/$RUNTIME"
    else
        dotnet build "$PROJECT" \
            --configuration "$CONFIGURATION"
        OUTPUT_DIR="src/Genie.UI/bin/$CONFIGURATION/net10.0"
    fi
fi

echo ""
echo "Build successful!"
echo "Output: $OUTPUT_DIR"
echo ""
echo "Supported runtimes for --runtime parameter:"
echo "  win-x64     - Windows 64-bit"
echo "  win-arm64   - Windows ARM64"
echo "  osx-x64     - macOS Intel"
echo "  osx-arm64   - macOS Apple Silicon"
echo "  linux-x64   - Linux 64-bit"
echo "  linux-arm64 - Linux ARM64"

