# WinEnvEdit - WinUI Design Document

## Overview

WinEnvEdit is a Windows 11+ environment variable editor that provides a modern, user-friendly alternative to the built-in Windows Environment Variables dialog. This document outlines the design for recreating the Avalonia prototype using WinUI 3.

## Application Purpose

- View, create, edit, and delete Windows environment variables
- Support both **System** (machine-level, requires admin) and **User** (profile-level) variables
- Handle **Path-like variables** (e.g., PATH) with individual path entry editing
- Backup and restore environment variables to/from YAML files
- Provide clear visual feedback for validation and pending changes

---

## UI Layout

### Window Structure

```
+---------------------------------------------------------------------+
|  WinEnvEdit [- Administrator]                            [-][o][x] |
+---------------------------------------------------------------------+
|  [Toolbar]                                                          |
|  +----------------------------------------------------------------+ |
|  | [Reload][Backup][Restore][Reset][Save][Add] [Elevate]  [Expand]| |
|  |                                             (if needed) [Filter]| |
|  |                                                         [About] | |
|  +----------------------------------------------------------------+ |
+----------------------------+----------------------------------------+
|       SYSTEM               |           USER                         |
+----------------------------+----------------------------------------+
|                            |                                        |
|  +----------------------+  |  +----------------------------------+  |
|  | VARIABLE_NAME     [x]|  |  | VARIABLE_NAME                 [x]|  |
|  | +------------------+ |  |  | +------------------------------+ |  |
|  | | value            | |  |  | | value                        | |  |
|  | +------------------+ |  |  | +------------------------------+ |  |
|  +----------------------+  |  +----------------------------------+  |
|                            |                                        |
|  +----------------------+  |  +----------------------------------+  |
|  | PATH          [v][x]|  |  | PATH                      [v][x]|  |
|  | +------------------+ |  |  | +------------------------------+ |  |
|  | | C:\bin        [x]| |  |  | | C:\Users\me\.local\bin   [x]| |  |
|  | | C:\tools      [x]| |  |  | | C:\custom\path           [x]| |  |
|  | | [+ Add Path]     | |  |  | | [+ Add Path]                 | |  |
|  | +------------------+ |  |  | +------------------------------+ |  |
|  +----------------------+  |  +----------------------------------+  |
|                            |                                        |
|         (scrollable)       |              (scrollable)              |
+----------------------------+----------------------------------------+
```

### Layout Specifications

1. **CommandBar (Top)** - WinUI CommandBar with AppBarButtons
   - Left-aligned primary commands: Reload, Backup, Restore, Reset, Save, Add Variable
   - Elevate to Admin button (visible only when not running as admin)
   - Right-aligned secondary commands: Expand All toggle, Show Volatile toggle, About

2. **Content Area** - Two-column Grid layout
   - Left column: System Variables with "System" header
   - Right column: User Variables with "User" header
   - Each column contains a scrollable ListView of variable cards

3. **Variable Cards** - Three visual templates based on variable type:
   - **Read-Only Card**: For locked variables (volatile or system without admin)
   - **Editable Card**: For regular string variables
   - **Path-List Card**: For expandable path-like variables (PATH, PATHEXT, etc.)

---

## Variable Card Designs

### Regular Variable Card (Editable)

```
+----------------------------------------------------+
| VARIABLE_NAME                                  [x] |
| +------------------------------------------------+ |
| | variable value here                            | |
| +------------------------------------------------+ |
+----------------------------------------------------+
```

- Variable name: Bold, left-aligned
- Delete button: Right-aligned, visible on hover or always
- Value TextBox: Full width, editable

### Read-Only Variable Card

```
+----------------------------------------------------+
| VARIABLE_NAME (locked)                             |
| +------------------------------------------------+ |
| | variable value (read-only, grayed out)         | |
| +------------------------------------------------+ |
+----------------------------------------------------+
```

- Variable name: Bold, grayed out
- No delete button
- Value displayed in read-only style (different background)

### Path-Like Variable Card (Collapsed)

```
+----------------------------------------------------+
| PATH                                       [v] [x] |
| +------------------------------------------------+ |
| | C:\bin;C:\tools;C:\Windows\System32            | |
| +------------------------------------------------+ |
+----------------------------------------------------+
```

### Path-Like Variable Card (Expanded)

```
+----------------------------------------------------+
| PATH                                       [^] [x] |
| +------------------------------------------------+ |
| | [::] C:\bin                                [x] | |  <- exists (normal)
| | [::] C:\missing\path                       [x] | |  <- missing (red text)
| | [::] C:\tools                              [x] | |  <- exists (normal)
| | [+ Add Path]                                   | |
| +------------------------------------------------+ |
+----------------------------------------------------+
```

- Drag handle [::] for reordering
- Alternating row backgrounds for visual clarity
- Red text/highlight for paths that don't exist on disk
- Add Path button at bottom of list

---

## Toolbar Commands

| Icon | Command | Description | Keyboard Shortcut |
|------|---------|-------------|-------------------|
| Reload | Reload | Reload variables from registry, discard changes | Ctrl+R |
| Export | Backup | Export all variables to YAML file | Ctrl+B |
| Import | Restore | Import variables from YAML backup | Ctrl+O |
| Undo | Reset | Discard pending changes | Ctrl+Z |
| Save | Save | Save all pending changes to registry | Ctrl+S |
| Add | Add | Add new variable (shows dialog) | Ctrl+N |
| Shield | Elevate | Restart as Administrator | - |
| Expand | Expand All | Toggle expand/collapse all path-like variables | Ctrl+E |
| Eye | Volatile | Toggle visibility of volatile variables | Ctrl+H |
| Help | About | Show about dialog | F1 |

---

## Context Menu (Right-Click on Variable)

| Option | Description |
|--------|-------------|
| Copy Name | Copy variable name to clipboard |
| Copy Value | Copy variable value to clipboard |
| Convert to Path-List | Convert string variable to path-like |
| Convert to String | Convert path-like variable to string |

---

## Dialogs

### Add Variable Dialog

```
+-----------------------------------------+
| Add Environment Variable                |
+-----------------------------------------+
|                                         |
| Scope:    ( ) User   ( ) System         |
|                                         |
| Type:     ( ) String ( ) Path-List      |
|                                         |
| Name:     [________________________]    |
|                                         |
| Value:    [________________________]    |
|                                         |
|           [Cancel]         [Add]        |
+-----------------------------------------+
```

### Confirmation Dialogs

- **Unsaved Changes**: "You have unsaved changes. Discard them?"
- **Delete Variable**: "Delete variable '{name}'?"
- **Reload Confirmation**: "Reload will discard all pending changes. Continue?"

---

## Data Architecture

### Models

```
EnvironmentVariable
  - Name: string
  - Value: string
  - OriginalName: string
  - OriginalValue: string
  - Scope: VariableScope (System | User)
  - Kind: RegistryValueKind (String | ExpandString)
  - IsVolatile: bool
  - IsNew: bool
  - IsDeleted: bool
  - HasChanges(): bool
  - CommitChanges(): void
  - RevertChanges(): void

PathListEnvironmentVariable : EnvironmentVariable
  - PathItems: ObservableCollection<PathItem>
  - IsExpanded: bool
  - SyncValueFromPaths(): void

PathItem
  - Path: string
  - Exists: bool (computed from file system)
  - Parent: PathListEnvironmentVariable
```

### ViewModels

```
MainWindowViewModel
  - SystemVariables: VariableScopeViewModel
  - UserVariables: VariableScopeViewModel
  - IsAdmin: bool
  - HasPendingChanges: bool
  - ShowVolatileVariables: bool
  - ExpandAllPaths: bool
  - Commands: Save, Reset, Reload, Backup, Restore, Add, Elevate
  - Services: IEnvironmentService, IDialogService, IAdminService

VariableScopeViewModel
  - Scope: VariableScope
  - Variables: ObservableCollection<VariableViewModel>
  - FilteredVariables: ICollectionView (filtered, sorted)
  - AddVariable(name, value, type): void
  - RemoveVariable(variable): void
  - LoadFromRegistry(): void

VariableViewModel
  - Model: EnvironmentVariable
  - IsLocked: bool (computed)
  - IsPathList: bool (computed)
  - Commands: Delete, Copy, ConvertType, ToggleExpand
  - PathItems: ObservableCollection<PathItemViewModel> (if path-list)
  - AddPath(), RemovePath(item): void
```

### Services

```
IEnvironmentService
  - GetUserVariables(): List<EnvironmentVariable>
  - GetSystemVariables(): List<EnvironmentVariable>
  - SaveVariable(variable): void
  - DeleteVariable(variable): void
  - NotifySystemOfChanges(): void  // WM_SETTINGCHANGE

IAdminService
  - IsRunningAsAdmin(): bool
  - RestartAsAdmin(): void

IDialogService
  - ShowConfirmation(title, message): Task<bool>
  - ShowMessage(title, message): Task
  - ShowSaveFileDialog(filter): Task<string?>
  - ShowOpenFileDialog(filter): Task<string?>

IBackupService
  - ExportToYaml(path, variables): Task
  - ImportFromYaml(path): Task<List<EnvironmentVariable>>
```

---

## Validation Rules

1. **Variable Name**
   - Cannot be empty
   - Cannot contain `=` character
   - Must be unique within scope

2. **Variable Value**
   - Maximum length: 32,767 characters (Windows limit)
   - Show warning when approaching limit

3. **Path Items**
   - Visual indicator (red) when path doesn't exist on disk
   - Warning only, not blocking

---

## Theming

WinUI 3 handles light/dark themes automatically via system settings. Use:

- `ThemeResource` for all colors and brushes
- Mica backdrop for modern Windows 11 appearance (already configured)
- Standard WinUI controls which adapt to theme automatically

### Custom Theme Resources Needed

| Resource | Light | Dark | Usage |
|----------|-------|------|-------|
| PathMissingForeground | #D32F2F | #EF5350 | Missing path text |
| AlternatingRowBackground | #F5F5F5 | #2D2D2D | Path list rows |
| LockedVariableOpacity | 0.6 | 0.6 | Grayed out locked vars |

---

## Keyboard Navigation

- **Tab**: Move between controls
- **Arrow Keys**: Navigate variable list
- **Enter**: Commit edit in TextBox
- **Delete**: Delete selected variable (with confirmation)
- **Escape**: Cancel current edit / close dialog

---

## Accessibility

- All interactive elements must have AutomationProperties.Name
- Support high contrast mode
- Keyboard-navigable throughout
- Screen reader friendly variable announcements

---

## Known Issues from Prototype to Address

1. **Delete button alignment** - Ensure consistent alignment in path list items
2. **Scroll behavior** - ListView scrolling when path-list expands should be smooth
3. **Auto-scroll on Add** - Adding path item should not cause unexpected scroll jumps
4. **Vertical spacing** - Consistent spacing between read-only and editable cards

---

## Implementation Phases

### Phase 1: Core Structure
- [ ] Project structure (MVVM folders, services)
- [ ] Main window layout with two-column grid
- [ ] CommandBar with placeholder commands
- [ ] Basic variable card template (editable string)

### Phase 2: Data Layer
- [ ] Environment variable models
- [ ] Registry read/write service
- [ ] ViewModel implementation with change tracking

### Phase 3: Variable Display
- [ ] Read-only variable template
- [ ] Path-list variable template (collapsed)
- [ ] Path-list variable template (expanded)
- [ ] Template selector logic

### Phase 4: Editing Features
- [ ] Add variable dialog
- [ ] Delete variable with confirmation
- [ ] Path item add/remove
- [ ] Drag-and-drop reordering for path items

### Phase 5: Advanced Features
- [ ] Backup/Restore (YAML)
- [ ] Admin elevation
- [ ] Volatile variable filtering
- [ ] Context menus

### Phase 6: Polish
- [ ] Keyboard shortcuts
- [ ] Validation indicators
- [ ] Path existence checking
- [ ] Error handling and user feedback
