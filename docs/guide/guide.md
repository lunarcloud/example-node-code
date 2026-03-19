# Introduction

## Purpose

Avalonia Node Editor is a cross-platform desktop application built with Avalonia UI
and NodifyM.Avalonia that provides a visual node editor interface for creating and
editing node graphs.

## Scope

This user guide covers:

- Installation and build instructions
- Node editor interface overview
- Node types and their properties
- Creating and editing node graphs
- Saving and loading graph files

# Continuous Compliance

This project follows the [Continuous Compliance][continuous-compliance] methodology, which ensures
compliance evidence is generated automatically on every CI run.

## Key Practices

- **Requirements Traceability**: Every requirement is linked to passing tests, and a trace matrix is
  auto-generated on each release
- **Linting Enforcement**: markdownlint, cspell, and yamllint are enforced before any build proceeds
- **Automated Audit Documentation**: Each release ships with generated requirements, justifications,
  trace matrix, and quality reports
- **CodeQL and SonarCloud**: Security and quality analysis runs on every build

# Building

## Prerequisites

- .NET SDK 10.0 or later
- Git

## Build Steps

Clone the repository and build:

```bash
git clone https://github.com/lunarcloud/example-node-code.git
cd example-node-code
dotnet restore
dotnet build --configuration Release
```

Or use the convenience scripts:

```bash
./build.sh    # Linux/macOS
build.bat     # Windows
```

# Usage

## Running the Application

```bash
dotnet run --project src/AvaloniaNodeEditor/AvaloniaNodeEditor.csproj
```

## Node Types

The application includes the following node types:

| Node Type               | Inputs | Outputs | Description                              |
| ----------------------- | ------ | ------- | ---------------------------------------- |
| Number Producer         | None   | Output  | Produces a constant numeric value        |
| Random Number Generator | None   | Output  | Generates a random number in a range     |
| Arithmetic Transform    | A, B   | Result  | Performs arithmetic operations            |
| Pass Filter             | Input  | Output  | Filters signals (Low/High/Mid pass)      |
| Number Reporter         | Input  | None    | Displays the received numeric value      |

## Creating Connections

- Drag from an output connector to an input connector to create a connection
- Each input connector accepts at most one connection
- Output connectors can fan out to multiple inputs
- Alt+Click on a connection to remove it

## Saving and Loading

Use the File ribbon group to save and load graph files in JSON format.

<!-- Link References -->
[continuous-compliance]: https://github.com/demaconsulting/ContinuousCompliance
