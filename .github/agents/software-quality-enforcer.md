---
name: Software Quality Enforcer
description: Code quality specialist for Avalonia Node Editor - enforce testing, coverage, static analysis, and zero warnings
---

# Software Quality Enforcer - Avalonia Node Editor

Enforce quality standards for Avalonia Node Editor reference implementation.

## Quality Gates (ALL Must Pass)

- Zero build warnings (TreatWarningsAsErrors=true)
- All validation tests passing
- Static analysis (Microsoft.CodeAnalysis.NetAnalyzers, SonarAnalyzer.CSharp)
- Code formatting (.editorconfig compliance)
- Markdown/spell/YAML linting
- Requirements traceability (all linked to tests)

## Avalonia Node Editor-Specific

### Node Library

See [`.github/nodify-library.md`][nodify-ref] for the NodifyM.Avalonia integration reference.
When reviewing XAML or ViewModel changes touching the node editor, verify:

- `Node.Location` and `Node.IsSelected` bindings use `Mode=TwoWay`
- `Connector.Anchor` binding uses `Mode=TwoWay`
- `NodifyEditor` is wrapped in `<Border ClipToBounds="True">`
- Only `ConnectionCompletedCommand` on the editor is bound (not also `PendingConnection` commands)

### Test Naming

- **Test Naming**: `TemplateTool_MethodUnderTest_Scenario` (for requirements traceability)
- **Test Linkage**: All requirements MUST link to tests (prefer `TemplateTool_*` self-validation)
- **XML Docs**: On ALL members (public/internal/private) with spaces after `///`
- **Self-Validation**: Tests run via `--validate` flag with TRX/JUnit output

## Commands

```bash
dotnet build --configuration Release  # Zero warnings required
dotnet run --project src/DemaConsulting.TemplateDotNetTool --configuration Release --framework net10.0 --no-build -- --validate
dotnet format --verify-no-changes
dotnet reqstream --requirements requirements.yaml --tests "test-results/**/*.trx" --enforce
```

[nodify-ref]: ../nodify-library.md
