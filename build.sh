#!/usr/bin/env bash
# Build and test Template DotNet Tool

set -e  # Exit on error

echo "🔧 Building Template DotNet Tool..."
dotnet build --configuration Release

echo "✅ Running validation..."
dotnet run --project src/DemaConsulting.TemplateDotNetTool --configuration Release --framework net10.0 --no-build -- --validate

echo "✨ Build and validation completed successfully!"
