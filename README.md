# WinEnvEdit

Modern Windows 11 environment editor built with WinUI 3 and .NET 10.

Distributed via [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget) for easy installation and updates.

## Inspiration

WinEnvEdit was heavily inspired by **Rapid Environment Editor**, a powerful tool that was widely used until its development slowed around 2018. While RapidEE is still functional, it is a legacy application that struggles with modern high-DPI display scaling and lacks a native Windows 11 aesthetic.

Crucially, Rapid Environment Editor chose a design that requires restarting the entire application as an Administrator to edit system variables. WinEnvEdit improves on this by triggering a standard Windows UAC prompt only during the Save operation. Our design also prioritizes a cleaner, more focused UI over the dense menu systems of legacy editors.

## Why WinUI 3?

Several frameworks were considered to achieve the best balance of performance and native integration:

- **WinUI 3 (Chosen)**: Provides the most authentic Windows 11 look and feel, including Mica backdrops and consistent Light/Dark themes. While the API surface is more modern and "cut-down" compared to WPF, it is the standard for native Windows development.
- **WebView / Blazor Hybrid**: While frameworks like WebView (used by MS Teams) or Blazor Hybrid offer great flexibility, they lack the low-level system integration and lightweight footprint of a native WinUI app.
- **Avalonia**: A modern cross-platform alternative to WPF, but its custom rendering engine made it difficult to achieve a perfectly "native" Windows theme without significant effort. Additionally, implementing efficient scrolling through varied types of list view elements was more complex than in WinUI.
- **MAUI**: Primarily mobile-focused, with desktop features often feeling like secondary additions.
- **Uno Platform**: Powerful for multi-platform apps, but considered overkill for a dedicated Windows utility.
- **Tauri**: Offers a fancy UI but relies on Node.js/NPM which, while functional on ARM64, has a larger dependency footprint than pure .NET.
- **WPF / WinForms / Electron**: Considered legacy or overly resource-heavy for a modern system utility.

## Features

- **Dual-Pane Interface**: Side-by-side management of System and User variables.
- **Incremental Refresh**: Blazing fast UI updates that only refresh changed items using $O(N)$ reconciliation.
- **Path List Management**: Expand semicolon-separated variables (like `PATH`) into a list with drag-and-drop reordering.
- **Validation**: Real-time checking for path existence and variable name validity.
- **Async Registry Operations**: Safe, non-blocking saves with UAC elevation support.
- **Full History**: Unlimited Undo/Redo until changes are saved or refreshed.
- **Modern UI**: Fully responsive design with Mica backdrop and Windows 11 design language.

## Getting Started

### Prerequisites
- **Windows 11**
- **.NET 10 SDK** (for development)

### Build & Run
To build and run the application from the command line:

**Note**: `<Platform>` is auto-detected via `powershell -c 'Write-Output $Env:PROCESSOR_ARCHITECTURE'` (maps `AMD64` → `x64`, `ARM64` → `ARM64`).

```bash
# Run prebuild (format, icons, version sync)
./src/Scripts/Prebuild.ps1

# Build the solution
dotnet build src/WinEnvEdit.slnx -c Debug -p:Platform=<Platform>

# Run the application
bin/<Platform>/Debug/net10.0-windows10.0.26100.0/WinEnvEdit.exe
```

## Building & Releasing

### Version Management

**Single Source of Truth**: The `VERSION` file contains the version number (currently `1.0.0`).

**To update version**:
1. Edit the `VERSION` file.
2. Run **`./src/Scripts/Prebuild.ps1`** to synchronize manifests.
3. Commit and push to `main`.
4. GitHub Actions will automatically build releases and update WinGet.

### GitHub Actions CI/CD

**Automated Workflow**:
- **version**: Detects version changes via Git tags.
- **validate**: Enforces code formatting, version synchronization, and runs unit tests.
- **package**: Builds x64 and ARM64 MSI installers using WiX v6.
- **release**: Creates a GitHub Release and attaches the installers and LICENSE.

### Creating a Release

The release process is fully automated:
1. Update the version number in the **`VERSION`** file.
2. Run **`./src/Scripts/Prebuild.ps1`** locally.
3. Commit and push the change to the `main` branch.
4. GitHub Actions will handle the rest, creating a tag and release.

### Winget Distribution

**Installation**:
```bash
winget install WinEnvEdit
```

**Updates**:
```bash
winget upgrade WinEnvEdit
```

Winget scans GitHub releases for updates - no manual manifest submission required.

## Development

Detailed project development guidelines, architecture rules, and standard workflows are maintained in **[CLAUDE.md](CLAUDE.md)**.

For more general AI agent rules and behavioral guidelines, see **[AGENTS.md](AGENTS.md)**.

### Testing
Stability is verified by a suite of over 180 unit tests.

```bash
# Run the unit tests
dotnet test src/WinEnvEdit.slnx -p:Platform=<Platform>
```

### Implementation Patterns
High-performance WinUI 3 patterns used in this project are documented in **[PATTERNS.md](PATTERNS.md)**.

## Usage
For detailed instructions on all features and keyboard shortcuts, see the **[USAGE.md](USAGE.md)** file.

## License
This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Acknowledgments
This project was developed with help from multiple Large Language Models:
- **Anthropic Claude**: Architecture, code generation, and documentation.
- **Google Gemini**: Refactoring, test optimization, and troubleshooting.
- **Z.ai GLM**: Feature implementation and specialized code assistance.
- **Alibaba Qwen**: Minor edits and refinements.

WinEnvEdit was heavily inspired by the original **Rapid Environment Editor**.
