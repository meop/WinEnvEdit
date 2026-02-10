# User Guide

Modern Windows 11 environment editor designed for safety, speed, and ease of use.

## Main Window

The application displays a dual-pane view:
- **Left Pane**: System environment variables (requires elevation to save).
- **Right Pane**: Current User environment variables.

### Key Features

#### 1. Variable Editing
- **Modify**: Edit the name or value of any variable directly in the text box.
- **Add**: Click the **+** button in either the System or User header to add a new variable.
- **Remove**: Click the **X** button on a variable card to mark it for removal.
- **Toggle Type (Ctrl+T)**: Right-click a variable card and select **Toggle Type** to switch between a standard String (`REG_SZ`) and an Expandable String (`REG_EXPAND_SZ`).

#### 2. Path List View
Variables containing multiple paths (semicolon-separated, like `PATH`) can be expanded into a dedicated list view:
- **Expand/Collapse**: Click the chevron button on the variable card.
- **Validation**: Individual paths are checked for existence. Invalid paths are highlighted with a red border.
- **Reordering**: Drag and drop paths within the list to change their order.
- **Add/Remove Rows**: Use the plus and minus buttons within the expanded view to manage individual path entries.

#### 3. Search and Filter
- **Search (Ctrl+F)**: Click the search icon to filter variables in both panes by name or value.
- **Volatile Variables (Ctrl+Shift+V)**: Click the eye icon to show/hide volatile (read-only, session-based) variables.

#### 4. Import and Export
- **Export (Ctrl+E)**: Save your current environment variables to a `.toml` file for backup or sharing.
- **Import (Ctrl+I)**: Load variables from a `.toml` file. This allows you to sync environments between machines.

#### 5. Safety and History
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
