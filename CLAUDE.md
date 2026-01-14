# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WinEnvEdit is a Windows 11+ environment variable editor built with WinUI 3 and .NET 10. It's a packaged Windows application using MSIX deployment.

**Minimum Requirements:** Windows 11 RTM (10.0.22000.0) or higher

## Build and Run Commands

**IMPORTANT**: All build commands MUST specify a platform (x64 or ARM64). The project does not support AnyCPU builds due to MSIX packaging requirements. Use the platform matching your current system architecture.

### Build (Debug)
```bash
dotnet build WinEnvEdit/WinEnvEdit.csproj -c Debug -p:Platform=<PLATFORM>
```
Replace `<PLATFORM>` with `x64` or `ARM64` based on your system.

### Run (Debug)
```bash
dotnet run --project WinEnvEdit/WinEnvEdit.csproj -p:Platform=<PLATFORM>
```

### Publish (Release, trimmed)
```bash
dotnet publish WinEnvEdit/WinEnvEdit.csproj -c Release -p:Platform=<PLATFORM>
```

### Build for both platforms
```bash
dotnet build WinEnvEdit/WinEnvEdit.csproj -p:Platform=x64 && dotnet build WinEnvEdit/WinEnvEdit.csproj -p:Platform=ARM64
```

## Technical Architecture

### Framework Stack
- **Target Framework**: .NET 10 (net10.0-windows10.0.26100.0)
- **Minimum Windows Version**: Windows 11 RTM (10.0.22000.0)
- **UI Framework**: WinUI 3 via Windows App SDK 1.8
- **Packaging**: MSIX with EnableMsixTooling

### Build Configuration
- **Nullable Reference Types**: Enabled
- **Warnings as Errors**: Enabled for Release builds on x64 and ARM64
- **AOT Compilation**: Disabled (PublishAot=False)
- **Trimming**: Enabled for Release builds, disabled for Debug
- **ReadyToRun**: Disabled
- **WebView2**: Explicitly excluded (not used by the app)
- **Platform Target**: Must be ARM64 or x64 (AnyCPU not supported for MSIX packaging)

### Platform Support
- x64, ARM64 (x86 removed - Windows 11+ doesn't require it)
- Runtime identifiers: win-x64, win-arm64
- Development machine is ARM64

### Application Structure
- `App.xaml.cs`: Application entry point, launches MainWindow
- `MainWindow.xaml(.cs)`: Main UI window with Mica backdrop material
- `app.manifest`: Declares Windows 10 compatibility and PerMonitorV2 DPI awareness

### Key Dependencies
- Microsoft.WindowsAppSDK (1.8.251106002)
- Microsoft.Windows.SDK.BuildTools (10.0.26100.7175)

## Development Notes

### Project File Location
The solution uses a .slnx file (new Visual Studio solution format), but you can build/run directly via the .csproj at `WinEnvEdit/WinEnvEdit.csproj`.

### DPI Awareness
The app uses PerMonitorV2 DPI awareness, ensuring proper scaling on multi-monitor setups with different DPI settings.

### WinUI 3 XAML Best Practices

**CRITICAL GOTCHAS** - These issues cause cryptic XAML compiler failures:

#### DataTemplates with x:Bind and x:DataType
1. **MUST use ResourceDictionary with partial class code-behind**
   - DataTemplates using `x:Bind` with `x:DataType` CANNOT be placed directly in `Application.Resources`
   - They MUST be in a separate ResourceDictionary file with a matching partial class code-behind file
   - Example structure:
     ```
     Resources/VariableTemplates.xaml       # Contains DataTemplates
     Resources/VariableTemplates.xaml.cs    # Partial class: public partial class VariableTemplates : ResourceDictionary
     ```
   - This is required for the XAML compiler (XamlCompiler.exe) to generate proper binding code

2. **Merge the ResourceDictionary, not reference in Application.Resources**
   ```xaml
   <!-- In App.xaml -->
   <Application.Resources>
     <ResourceDictionary>
       <ResourceDictionary.MergedDictionaries>
         <ResourceDictionary Source="Resources/VariableTemplates.xaml" />
       </ResourceDictionary.MergedDictionaries>
     </ResourceDictionary>
   </Application.Resources>
   ```

#### x:Bind with Converters at Window Level
3. **NEVER use x:Bind with converters when binding to Window properties**
   - Window is NOT a FrameworkElement, so `SetConverterLookupRoot` fails with compile error
   - Error: `cannot convert from 'YourApp.MainWindow' to 'Microsoft.UI.Xaml.FrameworkElement'`
   - **Solution**: Create computed properties in ViewModel that return the desired type directly
   - Example: Instead of `{x:Bind IsAdmin, Converter={StaticResource BoolToVisibilityInverseConverter}}`
     Add `public Visibility ElevateButtonVisibility => IsAdmin ? Visibility.Collapsed : Visibility.Visible;`
     Then use `{x:Bind ViewModel.ElevateButtonVisibility, Mode=OneWay}`

#### x:Bind Best Practices
4. **Prefer x:Bind over Binding for performance** - compiled bindings are faster
5. **Bind directly to Window properties when available**
   - If Window has a ViewModel property, use `{x:Bind ViewModel.Property}`
   - This is simpler than using DataContext patterns
6. **Mode specifications**:
   - `x:Bind` defaults to `Mode=OneTime` (not OneWay like traditional Binding)
   - Use `Mode=OneWay` for properties that change (observable properties)
   - Commands default to OneTime which is fine (ICommand instances don't change)
7. **Always specify x:DataType when using x:Bind in DataTemplates**
   ```xaml
   <DataTemplate x:Key="MyTemplate" x:DataType="vm:MyViewModel">
     <TextBlock Text="{x:Bind PropertyName}" />
   </DataTemplate>
   ```

#### DataTemplateSelector Pattern
8. **Selector class MUST be marked partial** for C#/WinRT compatibility
   ```csharp
   public partial class MyTemplateSelector : DataTemplateSelector {
     protected override DataTemplate SelectTemplateCore(object item) { ... }
   }
   ```

### XAML UI
The MainWindow uses WinUI 3's MicaBackdrop for a modern Windows 11-style appearance.

## Coding Standards

This project follows strict coding standards enforced by `.editorconfig`. All code contributions must adhere to these rules:

### C# Using Directives (Import) Order
**CRITICAL**: All using statements MUST be ordered in this exact sequence:
1. **System namespaces** (e.g., `using System;`, `using System.Collections.Generic;`)
2. **Third-party packages** (e.g., `using Microsoft.UI.Xaml;`, `using CommunityToolkit.Mvvm;`)
3. **Project namespaces** (e.g., `using WinEnvEdit.Models;`, `using WinEnvEdit.ViewModels;`)

**Separate each group with a blank line.**

Example:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

using WinEnvEdit.Models;
using WinEnvEdit.Services;
using WinEnvEdit.ViewModels;
```

### Naming Conventions
- **Private fields**: Prefix with `_` (e.g., `_myField`)
- **Interfaces**: Prefix with `I` (e.g., `IEnvironmentService`)
- **Async methods**: Suffix with `Async` (e.g., `LoadDataAsync`)
- **Type names**: PascalCase (e.g., `EnvironmentVariable`)
- **Method names**: PascalCase (e.g., `SaveChanges`)
- **Local variables**: camelCase (e.g., `variableName`)

### Code Style
- **Line endings**: Always use LF (Unix-style), never CRLF (enforced by .editorconfig and .gitattributes)
- **Namespaces**: Use file-scoped namespaces (`namespace WinEnvEdit;` instead of block syntax)
- **Braces**: Always use braces for control flow statements, even single-line
- **var keyword**: Use `var` when type is apparent from initialization (IDE suggestion, enforced by .editorconfig)
  - Good: `var viewModel = new MainWindowViewModel();`
  - Good: `var path = GetPath();`
  - Avoid: `var count = Calculate();` (use explicit type if not obvious)
- **this qualifier**: Do NOT use `this.` unless required for disambiguation (enforced by .editorconfig)
- **Accessibility modifiers**: Always specify (public, private, etc.) (enforced by .editorconfig)
- **Brace style**: K&R style - opening braces on same line (enforced by .editorconfig)

### MVVM Architecture
This application uses the MVVM pattern with CommunityToolkit.Mvvm:
- **Models**: In `Models/` folder - pure data classes, no UI logic
- **ViewModels**: In `ViewModels/` folder - use `[ObservableProperty]` and `[RelayCommand]` attributes
- **Views**: XAML files in `Views/` folder (or root for MainWindow)
- **Services**: In `Services/` folder - interfaces prefixed with `I`

### File Organization
```
WinEnvEdit/
├── Models/          # Data models (EnvironmentVariable, PathItem, etc.)
├── ViewModels/      # ViewModels with [ObservableProperty] attributes
├── Views/           # XAML user controls and windows
├── Services/        # Service interfaces and implementations
├── Converters/      # Value converters for XAML bindings
├── App.xaml(.cs)    # Application entry point
└── MainWindow.xaml(.cs)  # Main window
```

### Documentation
- Public APIs must have XML documentation comments (`///`)
- Complex logic should have inline comments explaining "why", not "what"
- No period at end of validation error messages

### Design Reference
See `DESIGN.md` for comprehensive UI/UX specifications and architecture details.
