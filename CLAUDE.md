# CLAUDE.md

Guidance for Claude Code when working with this repository.

**See also**: [AGENTS.md](AGENTS.md) for git safety rules and commit guidelines.

---

## Quick Reference

**Standard Workflow** (Claude detects host platform and targets it):
```bash
# Format code first
dotnet format src/WinEnvEdit/WinEnvEdit.csproj --no-restore

# Build solution (main + test projects)
dotnet build -c Debug -p:Platform=<Platform>

# Run unit tests
dotnet test src/WinEnvEdit.Tests/WinEnvEdit.Tests.csproj -p:Platform=<Platform> --no-build

# Run the application
bin/<Platform>/Debug/net10.0-windows10.0.26100.0/WinEnvEdit.exe
```

**Release Build**:
```bash
# Build Release (self-contained, bundles .NET runtime)
dotnet build -c Release -p:Platform=<Platform>

# Run Release directly
bin/<Platform>/Release/net10.0-windows10.0.26100.0/win-<Platform>/WinEnvEdit.exe
```

**Note**: `<Platform>` is auto-detected via `powershell -c 'Write-Output $Env:PROCESSOR_ARCHITECTURE'`
(single quotes required — Git Bash swallows `$Env:` if double-quoted). Maps `AMD64` → `x64`, `ARM64` → `ARM64`.

**Key Rules**:
- **Format code first**: `dotnet format src/WinEnvEdit/WinEnvEdit.csproj --no-restore`
- **Build solution**: Includes both main and test projects
- **Run tests after build**: Verify code quality and catch regressions
- 2-space indentation, LF line endings (enforced by `.editorconfig`)
- Using statements: System → Third-party → Project (with blank lines)
- Private fields: `_fieldName`, Interfaces: `IInterfaceName`, Async: `MethodAsync()`
- No inline initializers with `[ObservableProperty]` - initialize in constructor
- Never use `x:Bind` in ResourceDictionary files - use `{Binding}` instead
- **Tests are part of every change**: Every fix or feature must include an assessment of test impact. Create, update, or remove tests as needed — do not wait to be asked. If something genuinely can't be unit tested (OS APIs, clipboard, no seam), note why and move on.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Build & Run Commands](#build--run-commands)
3. [Code Formatting & Style](#code-formatting--style)
4. [Development Workflow](#development-workflow)
5. [Architecture & Technical Details](#architecture--technical-details)
6. [MVVM & Coding Standards](#mvvm--coding-standards)
7. [XAML Best Practices](#xaml-best-practices)
8. [WinUI Implementation Patterns](#winui-implementation-patterns)
9. [Testing](#testing)
10. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Project Overview

WinEnvEdit is a Windows 11+ environment variable editor built with WinUI 3 and .NET 10. The solution contains two projects:

**Main Project** (`WinEnvEdit`):
- Unpackaged application that runs as a standard executable
- Contains application code: Models, ViewModels, Services, UI

**Test Project** (`WinEnvEdit.Tests`):
- Unit test suite with 47+ tests
- Uses MSTest framework with FluentAssertions
- Mirrors main project structure for easy navigation

**Project Requirements**:
- **Minimum OS**: Windows 11 RTM (10.0.22000.0) or higher
- **Target Framework**: .NET 10
- **UI Framework**: WinUI 3 (Windows App SDK 1.8)
- **Deployment**: Unpackaged (runs as .exe directly, no MSIX required)
- **Testing**: Integrated MSTest with optional coverage analysis

### Environment Setup

Ensure you have:
- **Windows 11 or later** installed
- **.NET 10 SDK** - verify with `dotnet --version`
- **Visual Studio Code** or **Visual Studio 2022** (optional, but recommended for XAML editing)

### Project Structure

```
src/
├── WinEnvEdit/                      # Main application project
│   ├── Models/                      # Data models (EnvironmentVariable, etc.)
│   ├── ViewModels/                  # ViewModels with [ObservableProperty]
│   ├── Views/                       # XAML resources and templates
│   ├── Services/                    # Business logic (EnvironmentService, etc.)
│   ├── Validation/                  # Input validation (VariableValidator)
│   ├── Converters/                  # XAML value converters
│   ├── Resources/                   # ResourceDictionaries (VariableTemplates.xaml)
│   ├── App.xaml(.cs)                # Application entry point
│   ├── MainWindow.xaml(.cs)         # Main UI window
│   └── WinEnvEdit.csproj            # Main project file
│
└── WinEnvEdit.Tests/                # Unit test project
    ├── Services/                    # Tests for business logic
    ├── ViewModels/                  # Tests for UI state management
    ├── Validation/                  # Tests for input validation
    ├── Converters/                  # Tests for value converters
    ├── Helpers/                     # Test utilities (builders, mocks)
    ├── AssemblyInfo.cs              # Test assembly configuration
    └── WinEnvEdit.Tests.csproj      # Test project file
```

The test project (`WinEnvEdit.Tests`) mirrors the structure of the main project for easy navigation.

---

## Build & Run Commands

### Standard Workflow

**Claude auto-detects `<Platform>`** (see Quick Reference note above).

**Build the solution** (compiles both main and test projects):
```bash
dotnet build -c Debug -p:Platform=<Platform>
```

**Format code before building** (required for code quality):
```bash
dotnet format src/WinEnvEdit/WinEnvEdit.csproj --no-restore
```

**Run unit tests after building**:
```bash
dotnet test src/WinEnvEdit.Tests/WinEnvEdit.Tests.csproj -p:Platform=<Platform> --no-build
```

### Debug Configuration

**Purpose**: Fast builds for development. Requires .NET 10 SDK installed on the system.

```bash
# Format code first
dotnet format src/WinEnvEdit/WinEnvEdit.csproj --no-restore

# Build Debug (main + test projects)
dotnet build -c Debug -p:Platform=<Platform>

# Run tests
dotnet test src/WinEnvEdit.Tests/WinEnvEdit.Tests.csproj -p:Platform=<Platform> --no-build

# Run the application directly
bin/<Platform>/Debug/net10.0-windows10.0.26100.0/WinEnvEdit.exe
```

**Debug build characteristics**:
- **Not self-contained**: Requires .NET 10 SDK on system
- **Fast build times**: ~5-8 seconds (both projects)
- **Size**: ~139MB (includes Windows App SDK files, no .NET runtime)
- **Platforms**: ARM64 and x64 supported
- **Tests**: Included in solution build

### Release Configuration

**Purpose**: Self-contained deployment for distribution. Bundles .NET runtime for users without .NET SDK.

```bash
# Format code first
dotnet format src/WinEnvEdit/WinEnvEdit.csproj --no-restore

# Build Release (main + test projects)
dotnet build -c Release -p:Platform=<Platform>

# Run tests
dotnet test src/WinEnvEdit.Tests/WinEnvEdit.Tests.csproj -p:Platform=<Platform> --no-build

# Run the application directly
bin/<Platform>/Release/net10.0-windows10.0.26100.0/win-<Platform>/WinEnvEdit.exe
```

**Release build characteristics**:
- **Self-contained .NET runtime**: Includes .NET 10 runtime DLLs
- **Windows App SDK**: Bundled with the build
- **Size**: ~215MB (includes .NET runtime + Windows App SDK)
- **Platforms**: ARM64 and x64 supported
- **Tests**: Can be run against release build

### Building in Visual Studio

When using Visual Studio, the solution contains two projects:
1. **WinEnvEdit** - Main application project (WinExe)
2. **WinEnvEdit.Tests** - Unit test project (WinExe, for MSTest AppContainer infrastructure)

**Workflow**:
1. Select the **"WinEnvEdit (Unpackaged)"** launch profile to run the main app
2. Visual Studio will auto-detect your platform and build appropriately
3. **Build solution** (Ctrl+Shift+B): Compiles both main and test projects
4. **Run tests** via Test Explorer: Right-click test class → Run Tests
5. **Debug mode** (F5): Runs the application with debugger attached
6. **Release builds**: Self-contained with .NET runtime bundled

**Note**: Format code with `dotnet format src/WinEnvEdit/WinEnvEdit.csproj --no-restore` before committing.

---

## Code Formatting & Style

### Automatic Formatting

Claude should automatically format code after modifications:

**C# files**:
```bash
dotnet format src/WinEnvEdit/WinEnvEdit.csproj --no-restore
```

**XAML files**: Follow `.editorconfig` rules (2-space indentation, LF line endings). Ensure consistent attribute alignment matching existing patterns.

### C# Using Directives (Imports)

**Order matters**. Use this exact sequence with blank lines between groups:

```csharp
// 1. System namespaces
using System;
using System.Collections.Generic;
using System.Linq;

// 2. Third-party packages
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

// 3. Project namespaces
using WinEnvEdit.Models;
using WinEnvEdit.Services;
using WinEnvEdit.ViewModels;
```

### Code Style

- **Line endings**: LF (Unix-style), never CRLF
- **Indentation**: 2 spaces (enforced by `.editorconfig`)
- **Namespaces**: File-scoped (`namespace WinEnvEdit;`)
- **Braces**: K&R style - opening brace on same line, always use braces even for single-line statements
- **var keyword**: Use when type is obvious from initialization
  - ✓ `var viewModel = new MainWindowViewModel();`
  - ✓ `var path = GetPath();`
  - ✗ `var count = Calculate();` (use explicit type if unclear)
- **this qualifier**: Do NOT use unless needed for disambiguation
- **Accessibility modifiers**: Always explicit (public, private, etc.)
- **Trailing commas**: Use in multi-line initializers
  ```csharp
  var items = new List<string> {
    "first",
    "second",
    "third",  // Trailing comma
  };
  ```

### Naming Conventions

| Type            | Convention          | Example                      |
| --------------- | ------------------ | ---------------------------- |
| Private fields  | camelCase (no prefix) | `fieldName`                  |
| Properties      | PascalCase          | `WindowSize`                 |
| Methods         | PascalCase          | `GetPath()`, `SaveChanges()` |
| Async methods   | `Async` suffix       | `LoadDataAsync()`            |
| Interfaces      | `I` prefix          | `IEnvironmentService`        |
| Local variables | camelCase           | `isDirty`, `itemCount`       |
| Classes/Types   | PascalCase           | `EnvironmentVariable`        |

---

## Development Workflow

### Common Tasks

#### Add a New Value Converter

1. Create file in `Converters/` folder (e.g., `MyConverter.cs`)
2. Implement `IValueConverter` interface
3. Register in `App.xaml` **before** MergedDictionaries:
   ```xaml
   <converters:MyConverter x:Key="MyConverter"/>
   ```
4. Use in XAML: `{Binding Property, Converter={StaticResource MyConverter}}`

#### Add a New Command

1. Add `[RelayCommand]` method to ViewModel (from CommunityToolkit.Mvvm)
   ```csharp
   [RelayCommand]
   private void MyCommand() { /* logic */ }
   ```
2. Bind in XAML: `Command="{x:Bind ViewModel.MyCommandCommand}"`
3. Run `dotnet format` to ensure proper formatting

#### Add a New Observable Property

1. Use `[ObservableProperty]` attribute:
   ```csharp
   [ObservableProperty]
   public partial bool IsExpanded { get; set; }
   ```
2. **Initialize in constructor**, NOT inline:
   ```csharp
   public MyViewModel() {
     IsExpanded = false;
   }
   ```
3. Bind in XAML: `{x:Bind ViewModel.IsExpanded, Mode=TwoWay}`

#### Add a New Environment Variable Type

1. Create model in `Models/` (inherit from `EnvironmentVariable`)
2. Create or update ViewModel in `ViewModels/`
3. Create DataTemplate in `Resources/VariableTemplates.xaml`
4. Update TemplateSelector in `Resources/VariableTemplates.xaml.cs`
5. Register template in App.xaml if needed

### Design Patterns Used

**MVVM with CommunityToolkit.Mvvm**:
- ViewModels inherit `ObservableObject`
- Properties use `[ObservableProperty]` for automatic change notifications
- Commands use `[RelayCommand]` for automatic ICommand implementation

**Converters**:
- Custom converters implement `IValueConverter`
- Registered in `App.xaml` and used via `StaticResource`
- Always use `{Binding}` (not `x:Bind`) in ResourceDictionary templates

**Collections**:
- Observable collections for dynamic lists
- Items use CardBackgroundFillColorDefaultBrush for consistent styling

---

## Architecture & Technical Details

### Framework Stack

- **Target Framework**: .NET 10 (net10.0-windows10.0.26100.0)
- **Minimum Windows Version**: Windows 11 RTM (10.0.22000.0)
- **UI Framework**: WinUI 3 via Windows App SDK 1.8
- **Packaging**: Unpackaged (WindowsPackageType=None, runs as standard .exe)
- **Build System**: MSBuild (.csproj format)

### Build Configuration

#### Debug Configuration
- **Self-contained**: No (requires .NET SDK)
- **Trimming**: Disabled
- **AOT**: Disabled
- **Warnings as Errors**: Enabled
- **Purpose**: Fast development builds

#### Release Configuration
- **Self-contained**: Yes (bundles .NET runtime)
- **Trimming**: Disabled (WinUI 3 assemblies aren't trim-safe)
- **AOT**: Disabled
- **Warnings as Errors**: Enabled
- **Purpose**: Distribution-ready builds

#### Common Settings
- **Nullable Reference Types**: Enabled
- **ReadyToRun**: Disabled
- **WebView2**: Explicitly excluded (not used)
- **Windows App SDK**: Self-contained (WindowsAppSDKSelfContained=true)
- **Platform Target**: Must be ARM64 or x64 (x86 not supported on Windows 11+)

### Platform Support

- **ARM64** and **x64** supported (x86 removed — Windows 11+ doesn't require it)
- **Runtime identifiers**: win-arm64, win-x64

### Key Dependencies

| Package                            | Purpose                             |
| ---------------------------------- | ----------------------------------- |
| `Microsoft.WindowsAppSDK` (1.8.x)  | WinUI 3 framework                   |
| `Microsoft.Windows.SDK.BuildTools` | Windows SDK tools                   |
| `CommunityToolkit.Mvvm`            | MVVM patterns and source generation |

### Visual Design

- **Mica Backdrop**: Modern Windows 11 visual style
- **PerMonitorV2 DPI Awareness**: Proper scaling on multi-monitor setups with different DPI
- **CardBackgroundFillColorDefaultBrush**: Consistent card styling for variables
- **Theme Dictionaries**: Separate Light/Dark mode resources

---

## MVVM & Coding Standards

### MVVM Architecture

This application uses **MVVM with CommunityToolkit.Mvvm**:

- **Models** (`Models/`): Pure data classes, no UI logic
  - Example: `EnvironmentVariable`, `VariableScope`

- **ViewModels** (`ViewModels/`): Business logic and state management
  - Inherit from `ObservableObject`
  - Use `[ObservableProperty]` for properties
  - Use `[RelayCommand]` for commands
  - Example: `MainWindowViewModel`, `VariableScopeViewModel`

- **Views** (XAML files): UI presentation only
  - `MainWindow.xaml`: Main application window
  - `Resources/VariableTemplates.xaml`: Reusable card templates

- **Services** (`Services/`): Domain logic and external interactions
  - Interfaces prefixed with `I` (e.g., `IEnvironmentService`)
  - Examples: `EnvironmentService` (registry access), `AdminService` (elevation)

### CommunityToolkit.Mvvm Rules

#### ObservableProperty Pattern

❌ **WRONG** - Inline initializers can cause NullReferenceException:
```csharp
[ObservableProperty]
public partial ObservableCollection<Item> Items { get; set; } = [];
```

✅ **CORRECT** - Initialize in constructor:
```csharp
[ObservableProperty]
public partial ObservableCollection<Item> Items { get; set; }

public MyViewModel() {
  Items = [];
}
```

**Why**: The source generator creates property change handlers that fire during initialization. If dependent properties aren't initialized yet, you'll get a NullReferenceException.

#### RelayCommand Pattern

```csharp
[RelayCommand]
private void MyCommand() { /* logic */ }

[RelayCommand(CanExecute = nameof(CanExecute))]
private void ConditionalCommand() { /* logic */ }

private bool CanExecute() => someCondition;
```

XAML usage:
```xaml
<!-- Simple command -->
<Button Command="{x:Bind ViewModel.MyCommandCommand}"/>

<!-- Conditional command -->
<Button Command="{x:Bind ViewModel.ConditionalCommandCommand}"
        IsEnabled="{x:Bind ViewModel.CanExecute, Mode=OneWay}"/>
```

### Documentation

- **Public APIs**: Include XML documentation (`///`)
  ```csharp
  /// <summary>Gets or sets the environment variables collection.</summary>
  public ObservableCollection<EnvironmentVariable> Variables { get; set; }
  ```
- **Complex logic**: Add inline comments explaining "why", not "what"
- **Error messages**: No period at end (consistency)

---

## XAML Best Practices

### Critical Rules

#### Rule 1: Binding Location Matters

✅ **Use `x:Bind` in Window/Page XAML files** (like MainWindow.xaml):
```xaml
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}"/>
```

❌ **NEVER use `x:Bind` in ResourceDictionary files** (like VariableTemplates.xaml):
```xaml
<!-- Wrong - won't work reliably -->
<TextBlock Text="{x:Bind Item.Name}"/>

<!-- Correct - use traditional Binding -->
<TextBlock Text="{Binding Name}"/>
```

#### Rule 2: DataTemplate + ResourceDictionary Structure

ResourceDictionaries using DataTemplates **must have a code-behind**:

```
Resources/VariableTemplates.xaml       # Contains DataTemplates
Resources/VariableTemplates.xaml.cs    # Partial class code-behind
```

In `VariableTemplates.xaml.cs`:
```csharp
namespace WinEnvEdit.Resources;

public partial class VariableTemplates : ResourceDictionary {
  public VariableTemplates() {
    InitializeComponent();
  }
}
```

In `App.xaml`, merge the dictionary:
```xaml
<ResourceDictionary.MergedDictionaries>
  <ResourceDictionary Source="Resources/VariableTemplates.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

#### Rule 3: Resource Declaration Order

**Converters MUST be declared BEFORE MergedDictionaries**:

```xaml
<Application.Resources>
  <ResourceDictionary>
    <!-- 1. Converters FIRST -->
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
    <converters:ExpandChevronConverter x:Key="ExpandChevronConverter"/>

    <!-- 2. THEN merged dictionaries that use them -->
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="Resources/VariableTemplates.xaml"/>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

#### Rule 4: Converter at Window Level

❌ **NEVER use `x:Bind` with converters on Window properties**:
```xaml
<!-- Fails - Window is not a FrameworkElement -->
<Button Visibility="{x:Bind IsAdmin, Converter={StaticResource BoolToVisibilityInverseConverter}}"/>
```

✅ **Create computed properties in ViewModel instead**:
```csharp
public Visibility PermissionsButtonVisibility => IsAdmin ? Visibility.Collapsed : Visibility.Visible;
```

```xaml
<Button Visibility="{x:Bind ViewModel.PermissionsButtonVisibility, Mode=OneWay}"/>
```

#### Rule 5: Binding Modes

| Binding                     | Default Mode | Usage                                 |
| --------------------------- | ------------ | ------------------------------------- |
| `{x:Bind}`                  | OneTime      | Static values, computed properties    |
| `{x:Bind ..., Mode=OneWay}` | OneWay       | Observable properties (read-only UI)  |
| `{x:Bind ..., Mode=TwoWay}` | TwoWay       | Editable controls (TextBox, CheckBox) |
| `{Binding}`                 | OneWay       | Default for ResourceDictionary        |

#### Rule 6: DataTemplateSelector

Selector classes **must be marked `partial`** for C#/WinRT compatibility:

```csharp
public partial class VariableTemplateSelector : DataTemplateSelector {
  protected override DataTemplate SelectTemplateCore(object item) {
    if (item is EnvironmentVariable var) {
      return var.IsPathList ? PathListTemplate : EditableTemplate;
    }
    return ReadOnlyTemplate;
  }

  public DataTemplate ReadOnlyTemplate { get; set; }
  public DataTemplate EditableTemplate { get; set; }
  public DataTemplate PathListTemplate { get; set; }
}
```

#### Rule 7: Prefer StaticResources Over Inline Styles

**Always prefer defining styles as StaticResources in App.xaml rather than using inline properties.**

❌ **WRONG** - Inline properties repeated across elements:
```xaml
<Button Background="Transparent"
        BorderThickness="0"
        Padding="4"
        FontSize="14"/>
<Button Background="Transparent"
        BorderThickness="0"
        Padding="4"
        FontSize="14"/>
```

✅ **CORRECT** - Define once as StaticResource:
```xaml
<!-- In App.xaml -->
<Style x:Key="VariableIconButton" TargetType="Button">
  <Setter Property="Background" Value="Transparent"/>
  <Setter Property="BorderThickness" Value="0"/>
  <Setter Property="Padding" Value="4"/>
  <Setter Property="FontSize" Value="14"/>
</Style>

<!-- Usage -->
<Button Style="{StaticResource VariableIconButton}"/>
<Button Style="{StaticResource VariableIconButton}"/>
```

**Benefits**:
- Single source of truth for styling
- Easier to maintain and update
- Reduces XAML duplication
- Ensures visual consistency across the app

**When to consolidate**:
- When the same set of properties appears 2+ times
- When styling follows a consistent pattern (headers, cards, buttons)
- For reusable MenuFlyouts or other resources

### Formatting Rules

- **Indentation**: 2 spaces (enforced by `.editorconfig`)
- **Line endings**: LF only (enforced by `.gitattributes`)
- **Attribute alignment**: Multi-line attributes should align with the opening tag
  ```xaml
  <Button Grid.Column="1"
          Content="Add"
          Command="{x:Bind ViewModel.AddCommand}"
          Background="Transparent"/>
  ```

---

## WinUI Implementation Patterns

For detailed WinUI 3 patterns and implementations, see **[PATTERNS.md](PATTERNS.md)**.

**Quick Rules** (full details in [PATTERNS.md](PATTERNS.md)):
- **DataTemplateSelector** - Must use `partial` class
- **Centered Dialog Title** - Use TitleTemplate
- **TextBox Context Menu** - Use TextCommandBarFlyout, not custom MenuFlyout
- **Drag and Drop** - Set CanReorderItems, AllowDrop, CanDragItems
- **Elevated Script Launch** - Use ShellExecuteEx with SW_HIDE, not Process.Start with Verb="runas"
- **Non-Shared Context Menu** - Define flyouts in Styles, not as shared Resources
- **Incremental Refresh** - Use O(N) reconciliation for ObservableCollections
- **Async System Notifications** - Run WM_SETTINGCHANGE on background threads
- **CommandBar Stability** - Enforce explicit heights to prevent layout jumps

**Common Gotchas**:

**ContextFlyout on Whitespace** - Place on Grid with `Background="Transparent"`:
```xaml
<Grid Background="Transparent">
  <Grid.ContextFlyout>
    <MenuFlyout>
      <MenuFlyoutItem Text="Copy All" Command="{x:Bind ViewModel.CopyAllCommand}"/>
    </MenuFlyout>
  </Grid.ContextFlyout>
</Grid>
```

**ContentDialog Focus** - Use `Opened` event to focus content programmatically:
```csharp
dialog.Opened += (s, e) => {
  if (dialog.Content is TextBlock textBlock) {
    textBlock.Focus(FocusState.Programmatic);
  }
};
```

---

## Troubleshooting

### Build Fails with "Platform not specified"

**Cause**: Missing or wrong platform specification in build command.

**Solution**: Always use `-p:Platform=<Platform>`. Claude auto-detects (see Quick Reference). Manual builds must specify explicitly.

### App Won't Run - Missing .NET Runtime

**Problem**: Debug build fails with ".NET runtime not found" error.

**Solution**: Debug builds require .NET 10 SDK installed. Either:
1. Install .NET 10 SDK: `winget install Microsoft.DotNet.SDK.10`
2. Or build a self-contained Release version: `dotnet build -c Release`

### App Won't Run - Windows App SDK Error

**Problem**: App fails with "Windows App SDK initialization error".

**Solution**: The app bundles the Windows App SDK files. If you still see this error, ensure you're running on Windows 11 or later. The Windows App SDK 1.8 requires Windows 11 (10.0.22000.0) minimum.

### XAML Compiler Error (Cryptic Message)

**Common causes**:
1. DataTemplate with `x:Bind` in ResourceDictionary without code-behind → Use `{Binding}` instead
2. Converter referenced before it's declared → Move converter declaration before MergedDictionaries
3. TemplateSelector not marked `partial` → Add `partial` keyword to class

### Runtime: NullReferenceException During Initialization

**Cause**: Inline initializer on `[ObservableProperty]` property.

**Solution**: Initialize in constructor instead. See MVVM section Rule "ObservableProperty Pattern".

### Formatting Fails or Doesn't Match `.editorconfig`

**Solution**: Run `dotnet format src/WinEnvEdit/WinEnvEdit.csproj --no-restore`. Claude runs this automatically after C# changes.

### XAML Attributes Not Aligned Consistently

**Solution**: Manually reformat using Read/Write tools, matching existing indentation patterns. XAML formatting is not automated.

---

## Testing

### Running Tests

**Run all tests**:
```bash
dotnet test src/WinEnvEdit.Tests/WinEnvEdit.Tests.csproj -p:Platform=x64
```

**Run specific test class**:
```bash
dotnet test --filter "FullyQualifiedName~StateSnapshotServiceTests"
```

**Run with detailed output**:
```bash
dotnet test -p:Platform=x64 --logger "console;verbosity=detailed"
```

### Test Structure

Tests mirror production code structure:
```
Tests/
├── Services/           # Business logic tests
├── ViewModels/         # UI state management tests
├── Validation/         # Input validation tests
├── Converters/         # Value converter tests
└── Helpers/            # Test utilities (TestDataBuilders, MockFactory)
```

### Test Patterns

**Naming**: `{MethodName}_{Scenario}_{ExpectedResult}`
- Example: `IsDirty_VariableRemoved_ReturnsTrue`

**Structure**: Arrange-Act-Assert (AAA)
- Use `EnvironmentVariableBuilder` for test data
- Use `MockFactory` for service mocks
- Use FluentAssertions for readable assertions

**Example**:
```csharp
[TestMethod]
public void IsDirty_VariableRemoved_ReturnsTrue() {
  // Arrange
  var variable = EnvironmentVariableBuilder.Default()
    .WithName("TEST")
    .Build();
  _service.CaptureSnapshot(new[] { variable });
  variable.IsRemoved = true;

  // Act
  var result = _service.IsDirty(new[] { variable });

  // Assert
  result.Should().BeTrue("removed variable exists in snapshot");
}
```

### Coverage Goals

- **Services**: >90% coverage (critical business logic)
- **ViewModels**: >80% coverage (orchestration logic)
- **Validation**: 100% coverage (all rules)
- **Converters**: >70% coverage (simple transformations)

### Adding New Tests

1. Create test class in corresponding folder (e.g., `Services/NewServiceTests.cs`)
2. Use `[TestClass]` attribute
3. Follow naming conventions
4. Use TestDataBuilders for setup
5. Run `dotnet test` to verify

---

## Summary

This document covers the essentials for working with WinEnvEdit. Key takeaways:

- ✓ App runs as a standard .exe (no MSIX deployment needed)
- ✓ Debug builds are fast but require .NET SDK
- ✓ Release builds are self-contained (bundle .NET runtime)
- ✓ Auto-format C# after changes
- ✓ Use 2-space indentation everywhere
- ✓ Initialize observable properties in constructors
- ✓ Use `{Binding}` in ResourceDictionaries, `x:Bind` in Window XAML
- ✓ Declare converters before MergedDictionaries
- ✓ Platform detection is automatic
- ✓ When in doubt, match existing code patterns

For questions or edge cases not covered here, refer to the codebase itself - existing code serves as the authoritative reference for style and patterns.
