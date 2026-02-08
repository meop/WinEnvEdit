# WinEnvEdit

A modern Windows 11 environment variable editor built with WinUI 3 and .NET 10.
Distributed via [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget) for easy installation and updates.

![WinEnvEdit](src/WinEnvEdit/Assets/StoreLogo.png)

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
- **Import/Export**: Backup and restore environment configurations via **TOML**.
- **Modern UI**: Fully responsive design with Mica backdrop and Windows 11 design language.

## Getting Started

### Prerequisites
- **Windows 11 or later**
- **.NET 10 SDK** (for development)

### Build & Run
To build and run the application from the command line:

**Note**: `<Platform>` is auto-detected via `powershell -c 'Write-Output $Env:PROCESSOR_ARCHITECTURE'` (maps `AMD64` → `x64`, `ARM64` → `ARM64`).

```bash
# Format the code
./src/Scripts/Format.ps1

# Build the solution
dotnet build src/WinEnvEdit.slnx -c Debug -p:Platform=<Platform>

# Run the application
bin/<Platform>/Debug/net10.0-windows10.0.26100.0/WinEnvEdit.exe
```

## Building & Releasing

### Version Management

**Single Source of Truth**: The `VERSION` file contains the version number (currently `1.0.0.0`).

**To update version**:
1. Edit `VERSION` file
2. Commit changes
3. Push to `main`
4. GitHub Actions will automatically:
   - Build and create releases using this version
   - Update winget manifest automatically

### GitHub Actions CI/CD

**Workflow**: `.github/workflows/pipeline.yml`

**Automated Process**:
- Triggers on any push to `main` or versioned tag
- Reads `VERSION` file to get version number
- Builds x64 + ARM64 MSI installers
- Creates GitHub Release with both MSIs on version tag
- Updates winget manifest `src/WinEnvEdit.yaml` with new version

### Creating a Release

The release process is fully automated:
1. Update the version number in the **`VERSION`** file
2. Commit and push the change to the `main` branch
3. GitHub Actions will automatically:
   - Detect the version change
   - Build x64 and ARM64 MSI installers
   - Create a new Git Tag (e.g., `v1.0.1`)
   - Create a GitHub Release with the MSIs attached
   - Update the WinGet manifest and commit it back to the repo

### Winget Distribution

**Installation**:
```bash
winget install WinEnvEdit
```

**Updates**:
```bash
winget upgrade WinEnvEdit
```

Winget scans GitHub releases for updates - no app code needed.

## Development

Detailed development guidelines, including standard workflows and MVVM/XAML best practices, are maintained in **[CLAUDE.md](CLAUDE.md)**.

### Testing
Stability is verified by a suite of over 160 unit tests.

```bash
# Run the unit tests
dotnet test src/WinEnvEdit.Tests/WinEnvEdit.Tests.csproj -p:Platform=<Platform> --no-build
```

### Implementation Patterns
High-performance WinUI 3 patterns used in this project are documented in **[PATTERNS.md](PATTERNS.md)**, covering:
- Non-shared Context Menus for ListView items.
- Incremental List Reconciliation.
- Background System Notification broadcasts.
- Elevated Script Launching without window flicker.

## User Guide
For detailed instructions on all features and keyboard shortcuts, see the **[User Guide](USAGE.md)**.

## License
This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Acknowledgments
This project was developed with help from multiple Large Language Models:
- **Anthropic Claude**: Architecture, code generation, and documentation.
- **Google Gemini**: Refactoring, test optimization, and troubleshooting.
- **Z.ai GLM**: Feature implementation and specialized code assistance.

WinEnvEdit was heavily inspired by the original **Rapid Environment Editor**.
