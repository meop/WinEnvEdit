using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Storage.Pickers;

using WinEnvEdit.Extensions;
using WinEnvEdit.Helpers;
using WinEnvEdit.Models;
using WinEnvEdit.Services;

using WinRT.Interop;

namespace WinEnvEdit.ViewModels;

public partial class MainWindowViewModel : ObservableObject {
  private readonly IEnvironmentService environmentService;
  private readonly Window window;
  private readonly IFileService fileService;
  private readonly IStateSnapshotService stateService;
  private readonly IUndoRedoService undoRedoService;
  // Prevents UpdatePendingChangesState from pushing intermediate states to undo stack during restore.
  private bool isRestoringState = false;

  public XamlRoot? XamlRoot => window?.Content?.XamlRoot;

  [ObservableProperty]
  public partial VariableScopeViewModel SystemVariables { get; set; }

  [ObservableProperty]
  public partial VariableScopeViewModel UserVariables { get; set; }

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
  public partial bool HasPendingChanges { get; set; } = false;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(VolatileToggleGlyph))]
  [NotifyPropertyChangedFor(nameof(VolatileToggleTooltip))]
  public partial bool ShowVolatileVariables { get; set; } = false;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(SearchVisibility))]
  public partial bool IsSearchVisible { get; set; } = false;

  public Visibility SearchVisibility => IsSearchVisible ? Visibility.Visible : Visibility.Collapsed;

  [ObservableProperty]
  public partial string SearchText { get; set; } = string.Empty;

  partial void OnSearchTextChanged(string value) {
    SystemVariables.SearchText = value;
    UserVariables.SearchText = value;
  }

  public string VolatileToggleGlyph => ShowVolatileVariables ? Glyph.Hide : Glyph.View;

  public string VolatileToggleTooltip => ShowVolatileVariables ? "Hide Volatile (Ctrl+Shift+V)" : "Show Volatile (Ctrl+Shift+V)";

  public string ExpandPathsIcon => ExpandAllPaths ? Glyph.ChevronUp : Glyph.ChevronDown;

  public string ExpandPathsTooltip => ExpandAllPaths ? "Collapse Paths (Ctrl+Shift+P)" : "Expand Paths (Ctrl+Shift+P)";

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(ExpandPathsIcon))]
  [NotifyPropertyChangedFor(nameof(ExpandPathsTooltip))]
  public partial bool ExpandAllPaths { get; set; } = false;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
  public partial bool CanUndoState { get; set; } = false;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
  public partial bool CanRedoState { get; set; } = false;

  public MainWindowViewModel() : this(new EnvironmentService(), null!, new FileService(), new StateSnapshotService(), new UndoRedoService()) {
  }

  public MainWindowViewModel(IEnvironmentService environmentService, Window window, IFileService fileService, IStateSnapshotService stateService, IUndoRedoService undoRedoService) {
    this.environmentService = environmentService;
    this.window = window;
    this.fileService = fileService;
    this.stateService = stateService;
    this.undoRedoService = undoRedoService;
    SystemVariables = new VariableScopeViewModel(VariableScope.System, environmentService, this);
    UserVariables = new VariableScopeViewModel(VariableScope.User, environmentService, this);

    // Load initial data
    LoadVariables();
  }

  partial void OnShowVolatileVariablesChanged(bool value) {
    SystemVariables.ShowVolatileVariables = value;
    UserVariables.ShowVolatileVariables = value;
    SystemVariables.UpdateFilteredVariables();
    UserVariables.UpdateFilteredVariables();
  }

  partial void OnExpandAllPathsChanged(bool value) {
    foreach (var variable in SystemVariables.Variables.Concat(UserVariables.Variables)) {
      if (variable.IsPathList) {
        variable.IsExpanded = value;
      }
    }
  }

  private IEnumerable<EnvironmentVariable> AllVariables() =>
    SystemVariables.GetAllVariables().Concat(UserVariables.GetAllVariables());

  private async Task<bool> ShowConfirmationDialogAsync(string title, string message, string primaryButtonText = "Okay") {
    if (window.Content.XamlRoot == null) {
      return false;
    }

    var messageText = new TextBlock {
      Text = message,
      TextWrapping = TextWrapping.Wrap,
      VerticalAlignment = VerticalAlignment.Top,
      HorizontalAlignment = HorizontalAlignment.Left,
      TextAlignment = TextAlignment.Left,
    };

    var contentPanel = DialogHelper.CreateDialogPanel([messageText]);
    var dialog = DialogHelper.CreateStandardDialog(window.Content.XamlRoot, title, contentPanel, primaryButtonText, "Cancel");
    var result = await dialog.ShowAsync();
    return result == ContentDialogResult.Primary;
  }

  private void LoadVariables() {
    SystemVariables.LoadFromRegistry();
    UserVariables.LoadFromRegistry();

    var allVariables = AllVariables();
    stateService.CaptureSnapshot(allVariables);
    undoRedoService.Reset(allVariables);
    CanUndoState = false;
    CanRedoState = false;

    HasPendingChanges = false;
  }

  public void UpdatePendingChangesState() {
    var allVariables = AllVariables();
    var isDirty = stateService.IsDirty(allVariables);
    HasPendingChanges = isDirty;

    // Push state to undo history if dirty and not restoring
    if (isDirty && !isRestoringState) {
      undoRedoService.PushState(allVariables);
    }

    // Update undo/redo button states
    CanUndoState = undoRedoService.CanUndo;
    CanRedoState = undoRedoService.CanRedo;
  }

  [RelayCommand]
  private async Task Import() {
    if (HasPendingChanges) {
      if (!await ShowConfirmationDialogAsync("Import", "Discard all unsaved changes first?")) {
        return;
      }
    }

    var hwnd = WindowNative.GetWindowHandle(window);
    var openPicker = new FileOpenPicker();
    InitializeWithWindow.Initialize(openPicker, hwnd);
    openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    openPicker.FileTypeFilter.Add(FileService.FileExtension);

    var file = await openPicker.PickSingleFileAsync();
    if (file == null) {
      return;
    }

    var importedVars = (await fileService.ImportFromFileAsync(file.Path)).ToList();

    // Get imported variable names per scope
    var importedSystemNames = importedVars
      .Where(v => v.Scope == VariableScope.System)
      .Select(v => v.Name)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var importedUserNames = importedVars
      .Where(v => v.Scope == VariableScope.User)
      .Select(v => v.Name)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // Remove variables not in imported file (mark as removed)
    SystemVariables.RemoveVariablesNotIn(importedSystemNames);
    UserVariables.RemoveVariablesNotIn(importedUserNames);

    // Add/update variables from imported file
    foreach (var importedVar in importedVars) {
      if (importedVar.Scope == VariableScope.System) {
        SystemVariables.AddVariable(importedVar.Name, importedVar.Data, importedVar.Type);
      }
      else {
        UserVariables.AddVariable(importedVar.Name, importedVar.Data, importedVar.Type);
      }
    }
  }

  [RelayCommand]
  private async Task ExportAsync() {
    var hwnd = WindowNative.GetWindowHandle(window);
    var savePicker = new FileSavePicker();
    InitializeWithWindow.Initialize(savePicker, hwnd);
    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    savePicker.FileTypeChoices.Add(FileService.FileDescription, [FileService.FileExtension]);
    savePicker.SuggestedFileName = FileService.SuggestedFileName;

    var file = await savePicker.PickSaveFileAsync();
    if (file == null) {
      return;
    }

    var allVars = AllVariables().Where(v => !v.IsRemoved && !v.IsVolatile);
    await fileService.ExportToFileAsync(file.Path, allVars);
  }

  [RelayCommand]
  private async Task Refresh() {
    if (HasPendingChanges) {
      if (!await ShowConfirmationDialogAsync("Refresh", "Discard all unsaved changes first?")) {
        return;
      }
    }
    LoadVariables();
  }

  [RelayCommand(CanExecute = nameof(HasPendingChanges))]
  private async Task Save() {
    var changedVars = stateService.GetChangedVariables(AllVariables()).ToList();
    var hasSystemChanges = changedVars.Any(v => v.Scope == VariableScope.System);
    var hasUserChanges = changedVars.Any(v => v.Scope == VariableScope.User);

    var scope = hasSystemChanges && hasUserChanges
      ? "System and User"
      : hasSystemChanges
        ? "System"
        : "User";

    var message = $"Persist all {scope} changes to the Windows Registry?";

    if (!await ShowConfirmationDialogAsync("Save", message)) {
      return;
    }

    try {
      await environmentService.SaveVariablesAsync(changedVars);

      SystemVariables.CleanupAfterSave();
      UserVariables.CleanupAfterSave();
      stateService.CaptureSnapshot(AllVariables());

      undoRedoService.PushState(AllVariables());
      CanUndoState = undoRedoService.CanUndo;
      CanRedoState = undoRedoService.CanRedo;
      HasPendingChanges = false;
    }
    catch (Exception ex) {
      if (window.Content.XamlRoot == null) {
        return;
      }

      var contentPanel = DialogHelper.CreateDialogPanel([
        new TextBlock {
          Style = Application.Current.Resources["DialogErrorHeaderStyle"] as Style,
          Text = "An error occurred while saving environment variables:",
        },
        new TextBox {
          Style = Application.Current.Resources["DialogErrorDetailStyle"] as Style,
          Text = ex.Message,
        },
      ]);

      var dialog = DialogHelper.CreateStandardDialog(window.Content.XamlRoot, "Save Error", contentPanel, closeButtonText: "Close");
      await dialog.ShowAsync();
    }
  }

  [RelayCommand]
  private void ToggleExpandAllPaths() => ExpandAllPaths = !ExpandAllPaths;

  [RelayCommand]
  private void ToggleShowVolatileVariables() => ShowVolatileVariables = !ShowVolatileVariables;

  [RelayCommand(CanExecute = nameof(CanUndoState))]
  private void Undo() {
    // Perform undo
    var restoredState = undoRedoService.Undo();
    if (restoredState != null) {
      // Restore the previous state
      RestoreState(restoredState);
    }
  }

  [RelayCommand(CanExecute = nameof(CanRedoState))]
  private void Redo() {
    // Perform redo
    var restoredState = undoRedoService.Redo();
    if (restoredState != null) {
      // Restore the next state
      RestoreState(restoredState);
    }
  }

  private void RestoreState(IEnumerable<EnvironmentVariable> restoredVariables) {
    isRestoringState = true;

    try {
      // Preserve expand/collapse state — same pattern as LoadFromRegistry
      var systemExpandMap = SystemVariables.Variables
        .Where(v => v.IsPathList)
        .ToDictionary(v => v.Model.Name, v => v.IsExpanded, StringComparer.OrdinalIgnoreCase);
      var userExpandMap = UserVariables.Variables
        .Where(v => v.IsPathList)
        .ToDictionary(v => v.Model.Name, v => v.IsExpanded, StringComparer.OrdinalIgnoreCase);

      SystemVariables.Variables.Clear();
      UserVariables.Variables.Clear();

      // Rebuild ViewModels from restored models
      foreach (var variable in restoredVariables) {
        if (variable.Scope == VariableScope.System) {
          var viewModel = new VariableViewModel(variable, SystemVariables.RemoveVariable, () => UpdatePendingChangesState(), SystemVariables.UpdateFilteredVariables);
          if (viewModel.IsPathList && systemExpandMap.TryGetValue(variable.Name, out var wasExpanded)) {
            viewModel.IsExpanded = wasExpanded;
          }
          SystemVariables.Variables.Add(viewModel);
        }
        else {
          var viewModel = new VariableViewModel(variable, UserVariables.RemoveVariable, () => UpdatePendingChangesState(), UserVariables.UpdateFilteredVariables);
          if (viewModel.IsPathList && userExpandMap.TryGetValue(variable.Name, out var wasExpanded)) {
            viewModel.IsExpanded = wasExpanded;
          }
          UserVariables.Variables.Add(viewModel);
        }
      }

      // Update filtered views
      SystemVariables.UpdateFilteredVariables();
      UserVariables.UpdateFilteredVariables();

      // Update pending changes state
      UpdatePendingChangesState();
    }
    finally {
      isRestoringState = false;
    }
  }

  [RelayCommand]
  private async Task AboutAsync() {
    var assembly = Assembly.GetExecutingAssembly();
    var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty;
    var description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;
    var version = assembly.GetName().Version?.ToString() ?? string.Empty;

    var productValue = DialogHelper.CreateDialogValue(product);
    productValue.Name = "ProductText";

    var productGrid = DialogHelper.CreateLabelValueGrid("Product", productValue);
    var descriptionGrid = DialogHelper.CreateLabelValueGrid("Description", DialogHelper.CreateDialogValue(description, VerticalAlignment.Top), labelAlignment: VerticalAlignment.Top);
    var versionGrid = DialogHelper.CreateLabelValueGrid("Version", DialogHelper.CreateDialogValue(version));

    var contentPanel = DialogHelper.CreateDialogPanel([
      productGrid,
      descriptionGrid,
      versionGrid,
    ]);

    var contentDialog = DialogHelper.CreateStandardDialog(window.Content.XamlRoot, "About", contentPanel, closeButtonText: "Close");

    contentDialog.Opened += (s, e) => {
      if (contentPanel.FindName("ProductText") is TextBlock prodText) {
        prodText.Focus(FocusState.Programmatic);
      }
    };

    await contentDialog.ShowAsync();
  }
}
