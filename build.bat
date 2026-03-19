@echo off
REM Build and test Avalonia Node Editor (Windows)

echo Building Avalonia Node Editor...
dotnet build --configuration Release
if %errorlevel% neq 0 exit /b %errorlevel%

echo Running unit tests...
dotnet test --configuration Release
if %errorlevel% neq 0 exit /b %errorlevel%

echo Build and tests completed successfully!
