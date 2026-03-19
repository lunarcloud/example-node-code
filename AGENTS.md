# Agent Quick Reference

Project-specific guidance for agents working on Avalonia Node Editor - a cross-platform
desktop application using Avalonia UI and NodifyM.Avalonia.

## Available Specialized Agents

- **requirements** agent - Develops requirements and ensures test coverage linkage
- **technical-writer** agent - Creates accurate documentation following regulatory best practices
- **software-developer** agent - Writes production code following MVVM pattern in literate style
- **test-developer** agent - Creates unit and integration tests following AAA pattern with xUnit
- **code-quality** agent - Enforces linting, static analysis, and security standards
- **code-review** agent - Assists in performing formal file reviews
- **repo-consistency** agent - Ensures downstream repositories remain consistent with template patterns

## Agent Selection Guide

- Fix a bug → call the @software-developer agent with the **request** to fix the bug and the **context** of the
  bug details
- Add a new feature → call the @requirements agent with the **request** to define the feature requirements and the
  **context** of the feature details, then call the @software-developer agent with the **request** to implement the
  feature and the **context** of the requirements, then call the @test-developer agent with the **request** to add
  tests and the **context** of the feature implemented
- Write a test → call the @test-developer agent with the **request** to write the test and the **context** of
  what needs to be tested
- Fix linting or static analysis issues → call the @code-quality agent with the **request** to fix the issues
  and the **context** of the errors encountered
- Update documentation → call the @technical-writer agent with the **request** to update the documentation and
  the **context** of what needs to change
- Add or update requirements → call the @requirements agent with the **request** to add or update requirements
  and the **context** of the feature details
- Ensure test coverage linkage in `requirements.yaml` → call the @requirements agent with the **request** to
  ensure test coverage linkage and the **context** of the current coverage gaps
- Run security scanning or address CodeQL alerts → call the @code-quality agent with the **request** to address
  security scanning or CodeQL alerts and the **context** of the alerts found
- Perform a formal file review → call the @code-review agent with the **request** to perform a formal review and
  the **context** of the review-set name
- Propagate template changes → call the @repo-consistency agent with the **request** to propagate template
  changes and the **context** of the downstream repository

## Tech Stack

- C# 12, .NET 10.0, dotnet CLI, NuGet
- Avalonia UI 11.3.12 (cross-platform desktop framework)
- NodifyM.Avalonia 1.1.9 (node graph editor control — see [`.github/nodify-library.md`][nodify-ref])
- CommunityToolkit.Mvvm 8.4.0 (MVVM helpers)

## Key Files

- **`requirements.yaml`** - All requirements with test linkage (enforced via `dotnet reqstream --enforce`)
- **`.editorconfig`** - Code style (file-scoped namespaces, 4-space indent, UTF-8+BOM, LF endings)
- **`.cspell.yaml`, `.markdownlint-cli2.yaml`, `.yamllint.yaml`** - Linting configs
- **`AvaloniaNodeEditor.slnx`** - XML-based solution file
- **[`.github/nodify-library.md`][nodify-ref]** - NodifyM.Avalonia library reference (controls, bindings, commands, pitfalls)

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
- **Models/**: Business logic and data models
- **App.axaml**: Application-level styles and resources

## Build and Test

```bash
# Build the project
dotnet build --configuration Release

# Run unit tests
dotnet test --configuration Release

# Run the application
dotnet run --project src/AvaloniaNodeEditor/AvaloniaNodeEditor.csproj

# Use convenience scripts
./build.sh    # Linux/macOS
build.bat     # Windows
```

## Documentation

- **User Guide**: `docs/guide/guide.md`
- **Requirements**: `requirements.yaml` → auto-generated docs
- **Build Notes**: Auto-generated via BuildMark
- **Code Quality**: Auto-generated via CodeQL and SonarMark
- **Trace Matrix**: Auto-generated via ReqStream

## Markdown Link Style

- **AI agent markdown files** (`.github/agents/*.agent.md`): Use inline links `[text](url)` so URLs are visible
  in agent context
- **README.md**: Use absolute URLs (shipped in NuGet package)
- **All other markdown files**: Use reference-style links `[text][ref]` with `[ref]: url` at document end

## CI/CD

- **Quality Checks**: Markdown lint, spell check, YAML lint
- **Build**: Multi-platform (Windows/Linux)
- **CodeQL**: Security scanning
- **Unit Tests**: xUnit on Windows/Linux
- **Documentation**: Auto-generated via Pandoc + Weasyprint

## Common Tasks

```bash
# Format code
dotnet format

# Run all linters
./lint.sh     # Linux/macOS
lint.bat      # Windows
```

## Agent Report Files

When agents need to write report files to communicate with each other or the user, follow these guidelines:

- **Naming Convention**: Use the pattern `AGENT_REPORT_xxxx.md` (e.g., `AGENT_REPORT_analysis.md`,
  `AGENT_REPORT_results.md`)
- **Purpose**: These files are for temporary inter-agent communication and should not be committed
- **Exclusions**: Files matching `AGENT_REPORT_*.md` are automatically:
  - Excluded from git (via .gitignore)
  - Excluded from markdown linting
  - Excluded from spell checking

[nodify-ref]: .github/nodify-library.md
