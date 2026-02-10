#!/usr/bin/env bash
# Build Avalonia Node Editor

set -e  # Exit on error

echo "🔧 Building Avalonia Node Editor..."
dotnet build --configuration Release

echo "✨ Build completed successfully!"
