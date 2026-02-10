@echo off
REM Build Avalonia Node Editor (Windows)

echo Building Avalonia Node Editor...
dotnet build --configuration Release
if %errorlevel% neq 0 exit /b %errorlevel%

echo Build completed successfully!
