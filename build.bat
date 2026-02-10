@echo off
REM Build and test Template DotNet Tool (Windows)

echo Building Template DotNet Tool...
dotnet build --configuration Release
if %errorlevel% neq 0 exit /b %errorlevel%

echo Running validation...
dotnet run --project src/DemaConsulting.TemplateDotNetTool --configuration Release --framework net10.0 --no-build -- --validate
if %errorlevel% neq 0 exit /b %errorlevel%

echo Build and validation completed successfully!
