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

## Usage

The application displays a dual-pane view:
- **Left Pane**: System environment variables (requires elevation to save).
- **Right Pane**: Current User environment variables.

### Variable Editing

- **Modify**: Edit the name or value of any variable directly in the text box.
- **Add**: Click the **+** button in either the System or User header to add a new variable.
- **Remove**: Click the **X** button on a variable card to mark it for removal.
- **Toggle Type (Ctrl+T)**: Right-click a variable card and select **Toggle Type** to switch between a standard String (`REG_SZ`) and an Expandable String (`REG_EXPAND_SZ`).

### Path List View

Variables containing multiple paths (semicolon-separated, like `PATH`) can be expanded into a dedicated list view:
- **Expand/Collapse**: Click the chevron button on the variable card.
- **Validation**: Individual paths are checked for existence. Invalid paths are highlighted with a red border.
- **Reordering**: Drag and drop paths within the list to change their order.
- **Add/Remove Rows**: Use the plus and minus buttons within the expanded view to manage individual path entries.

### Search and Filter

- **Search (Ctrl+F)**: Click the search icon to filter variables in both panes by name or value.
- **Volatile Variables (Ctrl+Shift+V)**: Click the eye icon to show/hide volatile (read-only, session-based) variables.

### Import and Export

- **Export (Ctrl+E)**: Save your current environment variables to a `.toml` file for backup or sharing.
- **Import (Ctrl+I)**: Load variables from a `.toml` file. This allows you to sync environments between machines.

### Safety and History

- **Undo/Redo (Ctrl+Z / Ctrl+Y)**: Full history of changes is maintained until you save or refresh.
- **Pending Changes**: The **Save** button is only enabled when there are changes to apply.
- **Elevation**: Saving System variables will trigger a standard Windows UAC prompt.

## Keyboard Shortcuts

| Shortcut       | Action                     |
| -------------- | -------------------------- |
| `F5`           | Refresh from Registry      |
| `Ctrl+S`       | Save Changes               |
| `Ctrl+Z`       | Undo                       |
| `Ctrl+Y`       | Redo                       |
| `Ctrl+F`       | Search                     |
| `Ctrl+T`       | Toggle Type                |
| `Ctrl+E`       | Export to File             |
| `Ctrl+I`       | Import from File           |
| `Ctrl+Shift+P` | Toggle All Path Views      |
| `Ctrl+Shift+V` | Toggle Volatile Variables  |
| `Ctrl+C`       | Copy Variable (Name=Value) |
| `Ctrl+V`       | Paste Variable Value       |

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

## License

This project is licensed under the MIT License - see [LICENSE.txt](LICENSE.txt).

## Acknowledgments

This project was developed with help from multiple Large Language Models:
- **Anthropic Claude**: Architecture, code generation, and documentation.
- **Google Gemini**: Refactoring, test optimization, and troubleshooting.
- **Z.ai GLM**: Feature implementation and specialized code assistance.
- **Alibaba Qwen**: Minor edits and refinements.
