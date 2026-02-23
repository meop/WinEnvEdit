# WinEnvEdit

Modern Windows 11 environment editor built with WinUI 3 and .NET 10.

## Installation

```bash
winget install meop.WinEnvEdit
```

```bash
winget upgrade meop.WinEnvEdit
```

## Features

- **Dual-Pane Interface**: Side-by-side management of System and User variables.
- **Incremental Refresh**: Blazing fast UI updates that only refresh changed items using $O(N)$ reconciliation.
- **Path List Management**: Expand semicolon-separated variables (like `PATH`) into a list with drag-and-drop reordering.
- **Validation**: Real-time checking for path existence and variable name validity.
- **Async Registry Operations**: Safe, non-blocking saves with UAC elevation support.
- **Full History**: Unlimited Undo/Redo until changes are saved or refreshed.
- **Modern UI**: Fully responsive design with Mica backdrop and Windows 11 design language.

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

## Development

### Prerequisites

- **Microsoft Windows 11**
- **Microsoft Windows App SDK 1.8**
- **Microsoft .NET SDK 10.0**

### Build & Run

`<Platform>` is `ARM64` or `x64` — run `./src/Scripts/Platform.ps1` to detect.

```bash
./src/Scripts/Prebuild.ps1
dotnet build src/WinEnvEdit.slnx -c Debug -p:Platform=<Platform>
bin/<Platform>/Debug/net10.0-windows10.0.26100.0/WinEnvEdit.exe
```

### Testing

```bash
dotnet test src/WinEnvEdit.slnx -p:Platform=<Platform>
```

### Releasing

Bump the `VERSION` file, run `./src/Scripts/Prebuild.ps1`, commit and push to `main`. GitHub Actions handles the rest.

**Pipeline** (`.github/workflows/pipeline.yaml`):

| Job          | Runs on        | Condition                                             |
| ------------ | -------------- | ----------------------------------------------------- |
| **version**  | all pushes/PRs | Detects if `VERSION` has a new tag                    |
| **validate** | all pushes/PRs | Checks formatting, version sync, builds, tests        |
| **publish**  | main only      | Builds x64 and ARM64 MSI installers                   |
| **release**  | main only      | Creates GitHub Release with installers and LICENSE    |
| **package**  | main only      | Submits WinGet update PR via wingetcreate             |

### Implementation Patterns

High-performance WinUI 3 patterns used in this project are documented in **[PATTERNS.md](PATTERNS.md)**.

## Usage

For detailed instructions on all features and keyboard shortcuts, see **[USAGE.md](USAGE.md)**.

## License

This project is licensed under the MIT License - see [LICENSE.txt](LICENSE.txt).

## Acknowledgments

This project was developed with help from multiple Large Language Models:
- **Anthropic Claude**: Architecture, code generation, and documentation.
- **Google Gemini**: Refactoring, test optimization, and troubleshooting.
- **Z.ai GLM**: Feature implementation and specialized code assistance.
- **Alibaba Qwen**: Minor edits and refinements.
