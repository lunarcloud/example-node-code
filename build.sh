#!/usr/bin/env bash
# Build and test Avalonia Node Editor

set -e  # Exit on error

echo "🔧 Building Avalonia Node Editor..."
dotnet build --configuration Release

echo "🧪 Running unit tests..."
dotnet test --configuration Release

echo "✨ Build and tests completed successfully!"
