# Avalonia Node Editor

A cross-platform node editor application built with Avalonia UI and NodeEditorAvalonia.

## Features

- **Cross-platform**: Runs on Windows, Linux, and macOS
- **Node Editor**: Visual node-based editing using NodeEditorAvalonia
- **Modern UI**: Built with Avalonia 11.3.11 and Fluent theme
- **.NET 10**: Built on the latest .NET 10.0 framework

## Building

### Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download)

### Build Instructions

```bash
# Clone the repository
git clone https://github.com/lunarcloud/example-node-code.git
cd example-node-code

# Restore dependencies
dotnet restore AvaloniaNodeEditor.slnx

# Build the project
dotnet build AvaloniaNodeEditor.slnx

# Run the application
dotnet run --project src/AvaloniaNodeEditor/AvaloniaNodeEditor.csproj
```

## Project Structure

- `AvaloniaNodeEditor.slnx` - XML-based solution file (slnx format)
- `src/AvaloniaNodeEditor/` - Main application project
  - Uses Avalonia MVVM pattern
  - Integrates NodeEditorAvalonia for node editing
  - Supports cross-platform desktop deployment

## Dependencies

- **Avalonia** (11.3.11) - Cross-platform UI framework
- **NodeEditorAvalonia** (11.3.11) - Node editor control
- **NodeEditorAvalonia.Mvvm** (11.3.11) - MVVM support for node editor
- **CommunityToolkit.Mvvm** (8.4.0) - MVVM helpers

## License

This project is based on the template from [TemplateDotNetTool](https://github.com/demaconsulting/TemplateDotNetTool/pull/1).
