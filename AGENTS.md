# Agent Quick Reference

Project-specific guidance for agents working on Avalonia Node Editor - a cross-platform
desktop application using Avalonia UI and NodeEditorAvalonia.

## Tech Stack

- C# 12, .NET 10.0, dotnet CLI, NuGet
- Avalonia UI 11.3.11 (cross-platform desktop framework)
- NodeEditorAvalonia 11.3.11 (node editor control)
- CommunityToolkit.Mvvm 8.4.0 (MVVM helpers)

## Key Files

- **`requirements.yaml`** - All requirements for the application
- **`.editorconfig`** - Code style (file-scoped namespaces, 4-space indent, UTF-8+BOM, LF endings)
- **`.cspell.json`, `.markdownlint.json`, `.yamllint.yaml`** - Linting configs
- **`AvaloniaNodeEditor.slnx`** - XML-based solution file

## Architecture

- **MVVM Pattern**: Uses Model-View-ViewModel pattern with Avalonia
- **Observable Properties**: Use `[ObservableProperty]` from CommunityToolkit.Mvvm
- **Data Binding**: Compiled bindings enabled by default for performance

## Code Style

- **XML Docs**: On public members with spaces after `///` in summaries
- **Namespace**: File-scoped namespaces only
- **Using Statements**: Top of file only
- **String Formatting**: Use interpolated strings ($"") for clarity
- **XAML**: Use compiled bindings with `x:DataType` attribute

## Project Structure

- **Views/**: AXAML files and code-behind for UI
- **ViewModels/**: ViewModels with observable properties
- **Models/**: Business logic and data models (to be added as needed)
- **App.axaml**: Application-level styles and resources

## Standard Command-Line Arguments

All DEMA Consulting tools should support:

- `-v`, `--version` - Display version information
- `-?`, `-h`, `--help` - Display help message
- `--silent` - Suppress console output
- `--validate` - Run self-validation
- `--results <file>` - Write validation results to file (TRX or JUnit format)
- `--log <file>` - Write output to log file

## Build and Test

```bash
# Build the project
dotnet build --configuration Release

# Run the application
dotnet run --project src/AvaloniaNodeEditor/AvaloniaNodeEditor.csproj

# Use convenience scripts
./build.sh    # Linux/macOS
build.bat     # Windows
```

## Documentation

- **User Guide**: `docs/guide/guide.md`
- **Requirements**: `requirements.yaml` -> auto-generated docs
- **Build Notes**: Auto-generated via BuildMark
- **Code Quality**: Auto-generated via CodeQL and SonarMark
- **Trace Matrix**: Auto-generated via ReqStream

## CI/CD

- **Quality Checks**: Markdown lint, spell check, YAML lint
- **Build**: Multi-platform (Windows/Linux)
- **CodeQL**: Security scanning
- **Integration Tests**: .NET 8/9/10 on Windows/Linux
- **Documentation**: Auto-generated via Pandoc + Weasyprint

## Common Tasks

```bash
# Format code
dotnet format

# Run all linters
./lint.sh     # Linux/macOS
lint.bat      # Windows

# Pack as NuGet tool
dotnet pack --configuration Release
```
