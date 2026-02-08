using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using WinEnvEdit.Extensions;
using WinEnvEdit.Helpers;
using WinEnvEdit.Models;
using WinEnvEdit.Services;

namespace WinEnvEdit.ViewModels;

public partial class MainWindowViewModel : ObservableObject {
  private readonly IEnvironmentService environmentService;
  private readonly Window window;
  private readonly IFileService fileService;
  private readonly IStateSnapshotService stateService;
  private readonly IUndoRedoService undoRedoService;
  private readonly IClipboardService clipboardService;
  private readonly IDialogService dialogService;
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

  public MainWindowViewModel() : this(new EnvironmentService(), null!, new FileService(), new StateSnapshotService(), new UndoRedoService(), new ClipboardService(), new DialogService(null!)) {
  }

  public MainWindowViewModel(IEnvironmentService environmentService, Window window, IFileService fileService, IStateSnapshotService stateService, IUndoRedoService undoRedoService, IClipboardService clipboardService, IDialogService dialogService) {
    this.environmentService = environmentService;
    this.window = window;
    this.fileService = fileService;
    this.stateService = stateService;
    this.undoRedoService = undoRedoService;
    this.clipboardService = clipboardService;
    this.dialogService = dialogService;

    SystemVariables = new VariableScopeViewModel(VariableScope.System, environmentService, clipboardService, dialogService, this);
    UserVariables = new VariableScopeViewModel(VariableScope.User, environmentService, clipboardService, dialogService, this);

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

  private void LoadVariables() {
    // LoadFromRegistry now uses incremental updates - only changes what's different
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
      if (!await dialogService.ShowConfirmation("Import", "This will discard all unsaved changes")) {
        return;
      }
    }

    var filePath = await dialogService.PickOpenFile(FileService.FileExtension);
    if (filePath == null) {
      return;
    }

    var importedVars = (await fileService.ImportFromFile(filePath)).ToList();

    // Preserve existing volatile variables (they're not in the file)
    var systemVolatile = SystemVariables.Variables
      .Where(v => v.Model.IsVolatile)
      .Select(v => v.Model);
    var userVolatile = UserVariables.Variables
      .Where(v => v.Model.IsVolatile)
      .Select(v => v.Model);

    // Combine imported vars with volatile vars, then sort by name
    var systemImported = new List<EnvironmentVariable>();
    systemImported.AddRange(importedVars.Where(v => v.Scope == VariableScope.System));
    systemImported.AddRange(systemVolatile);
    systemImported = [.. systemImported.OrderBy(v => v.Name)];

    var userImported = new List<EnvironmentVariable>();
    userImported.AddRange(importedVars.Where(v => v.Scope == VariableScope.User));
    userImported.AddRange(userVolatile);
    userImported = [.. userImported.OrderBy(v => v.Name)];

    // Use unified restoration for minimal UI updates
    SystemVariables.RestoreFromVariables(systemImported);
    UserVariables.RestoreFromVariables(userImported);

    // Update state after import
    stateService.CaptureSnapshot(AllVariables());
    undoRedoService.Reset(AllVariables());
    CanUndoState = false;
    CanRedoState = false;
    HasPendingChanges = false;
  }

  [RelayCommand]
  private async Task Export() {
    var filePath = await dialogService.PickSaveFile(FileService.FileDescription, FileService.FileExtension, FileService.SuggestedFileName);
    if (filePath == null) {
      return;
    }

    var allVars = AllVariables().Where(v => !v.IsRemoved && !v.IsVolatile);
    await fileService.ExportToFile(filePath, allVars);
  }

  [RelayCommand]
  private async Task Refresh() {
    if (HasPendingChanges) {
      if (!await dialogService.ShowConfirmation("Refresh", "This will discard all unsaved changes")) {
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

    if (!await dialogService.ShowConfirmation("Save", $"This will persist all {scope} changes to the Windows Registry")) {
      return;
    }

    try {
      await environmentService.SaveVariables(changedVars);

      SystemVariables.CleanupAfterSave();
      UserVariables.CleanupAfterSave();
      stateService.CaptureSnapshot(AllVariables());

      undoRedoService.PushState(AllVariables());
      CanUndoState = undoRedoService.CanUndo;
      CanRedoState = undoRedoService.CanRedo;
      HasPendingChanges = false;
    }
    catch (Exception ex) {
      await dialogService.ShowError("Save Error", "An error occurred while saving environment variables:", ex.Message);
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
      var restoredList = restoredVariables.ToList();
      var systemRestoredVars = restoredList.Where(v => v.Scope == VariableScope.System).ToList();
      var userRestoredVars = restoredList.Where(v => v.Scope == VariableScope.User).ToList();

      // Check if each pane has changes and only update those that do
      var systemChanged = HasScopeChanged(SystemVariables, systemRestoredVars);
      var userChanged = HasScopeChanged(UserVariables, userRestoredVars);

      if (systemChanged) {
        RestoreScopeVariables(SystemVariables, systemRestoredVars);
      }

      if (userChanged) {
        RestoreScopeVariables(UserVariables, userRestoredVars);
      }

      // Update pending changes state
      UpdatePendingChangesState();
    }
    finally {
      isRestoringState = false;
    }
  }

  private bool HasScopeChanged(VariableScopeViewModel scopeViewModel, List<EnvironmentVariable> restoredVariables) {
    var currentVariables = scopeViewModel.Variables.Select(v => v.Model).ToList();

    if (currentVariables.Count != restoredVariables.Count) {
      return true;
    }

    // Compare variables in order (both should be sorted by name)
    for (var i = 0; i < currentVariables.Count; i++) {
      if (!AreVariablesEqual(currentVariables[i], restoredVariables[i])) {
        return true;
      }
    }

    return false;
  }

  private static bool AreVariablesEqual(EnvironmentVariable a, EnvironmentVariable b) =>
    string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) &&
    a.Data == b.Data &&
    a.Type == b.Type &&
    a.IsAdded == b.IsAdded &&
    a.IsRemoved == b.IsRemoved &&
    a.IsVolatile == b.IsVolatile;

  private void RestoreScopeVariables(VariableScopeViewModel scopeViewModel, List<EnvironmentVariable> restoredVariables) {
    // Use unified restoration for minimal UI updates
    scopeViewModel.RestoreFromVariables(restoredVariables);
  }

  [RelayCommand]
  private async Task About() {
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
