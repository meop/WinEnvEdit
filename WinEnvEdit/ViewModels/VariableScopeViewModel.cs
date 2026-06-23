using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;

using WinEnvEdit.Core.Helpers;
using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Core.Types;
using WinEnvEdit.Core.Validators;
using WinEnvEdit.Helpers;
using WinEnvEdit.Services;

namespace WinEnvEdit.ViewModels;

public partial class VariableScopeViewModel(IEnvironmentService environmentService, IClipboardService clipboardService, MainWindowViewModel? parentViewModel = null) : ObservableObject {
  private const int DialogLabelWidth = 80;

  [ObservableProperty]
  public partial VariableScope Scope { get; set; }

  [ObservableProperty]
  public partial ObservableCollection<VariableViewModel> Variables { get; set; }

  [ObservableProperty]
  public partial bool ShowVolatileVariables { get; set; } = false;

  [ObservableProperty]
  public partial ObservableCollection<VariableViewModel> FilteredVariables { get; set; }

  [ObservableProperty]
  public partial string SearchText { get; set; } = string.Empty;

  public VariableScopeViewModel(VariableScope scope, IEnvironmentService environmentService, IClipboardService clipboardService, MainWindowViewModel? parentViewModel = null)
      : this(environmentService, clipboardService, parentViewModel) {
    Scope = scope;
    Variables = [];
    FilteredVariables = [];
  }

  partial void OnSearchTextChanged(string value) => UpdateFilteredVariables();

  partial void OnShowVolatileVariablesChanged(bool value) => UpdateFilteredVariables();

  partial void OnVariablesChanged(ObservableCollection<VariableViewModel> value) => UpdateFilteredVariables();

  public void UpdateFilteredVariables(VariableViewModel? changedVariable = null) {
    if (FilteredVariables is null || Variables is null) {
      return;
    }

    var shouldShowVolatile = ShowVolatileVariables;
    var search = SearchText?.Trim() ?? string.Empty;
    var hasSearch = search.Length > 0;

    var targetList = new List<VariableViewModel>();
    foreach (var variable in Variables) {
      // Filter out removed variables
      if (variable.Model.IsRemoved) {
        continue;
      }

      // Filter out volatile variables unless ShowVolatileVariables is true
      if (!variable.Model.IsVolatile || shouldShowVolatile) {
        if (hasSearch) {
          var nameMatch = variable.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
          var valueMatch = variable.Data.Contains(search, StringComparison.OrdinalIgnoreCase);
          if (!nameMatch && !valueMatch) {
            continue;
          }
        }
        targetList.Add(variable);
      }
    }

    // Fast path: If it's a bulk update (no specific variable changed) and the lists are very different,
    // just use Clear and Add. This is significantly faster for initial load and search.
    if (changedVariable == null) {
      if (FilteredVariables.Count == 0 || !FilteredVariables.SequenceEqual(targetList)) {
        FilteredVariables.Clear();
        foreach (var v in targetList) {
          FilteredVariables.Add(v);
        }
      }
      return;
    }

    // Incremental update for targeted refreshes (e.g. Toggle Type)
    var targetSet = new HashSet<VariableViewModel>(targetList);

    // 1. Remove items no longer present
    for (var i = FilteredVariables.Count - 1; i >= 0; i--) {
      if (!targetSet.Contains(FilteredVariables[i])) {
        FilteredVariables.RemoveAt(i);
      }
    }

    // 2. Add or Move items to match targetList
    for (var i = 0; i < targetList.Count; i++) {
      var targetVar = targetList[i];
      if (i < FilteredVariables.Count) {
        if (FilteredVariables[i] == targetVar) {
          // Already at the right position — leave the container alone. The single visibility-gated template
          // updates itself from the VM's property changes (e.g. a type toggle flips ShowEditable/ShowPathList),
          // so there is no template to "refresh"; replacing the item with itself would only regenerate the
          // card container, flashing a placeholder over the whole card (label included).
          continue;
        }

        var existingIndex = -1;
        // Search forward from current position for small moves
        for (var j = i + 1; j < FilteredVariables.Count; j++) {
          if (FilteredVariables[j] == targetVar) {
            existingIndex = j;
            break;
          }
        }

        if (existingIndex >= 0) {
          FilteredVariables.Move(existingIndex, i);
        }
        else {
          FilteredVariables.Insert(i, targetVar);
        }
      }
      else {
        FilteredVariables.Add(targetVar);
      }
    }
  }

  public void RefreshVariable(VariableViewModel variable) => UpdateFilteredVariables(variable);

  public void LoadFromRegistry() {
    var allVars = environmentService.GetVariables();
    var newVars = allVars.Where(v => v.Scope == Scope).OrderBy(v => v.Name).ToList();
    RestoreFromVariables(newVars);
  }

  /// <summary>
  /// Restores Variables collection from a list of EnvironmentVariableModel models.
  /// Uses O(N) reconciliation to minimize UI updates (only adds/removes/updates what changed).
  /// Used by Refresh, Import, Undo, and Redo to avoid scrollbar bounce.
  /// </summary>
  public void RestoreFromVariables(List<EnvironmentVariableModel> newVars) {
    // Fast path: if nothing changed, skip update entirely to avoid scroll bounce
    if (!HasVariablesChanged(newVars)) {
      return;
    }

    // Reconcile in place against the (name-sorted) target instead of clearing and rebuilding. Reusing the
    // existing VM instances keeps unchanged rows — and their nested code-hosted path lists — from being torn
    // down and re-rendered (the flicker on undo/redo/refresh), and lets UpdateFilteredVariables' SequenceEqual
    // fast-path skip the outer list rebuild too. A variable is reused even across a String <-> ExpandString
    // type flip (UpdateFrom transitions it in place, preserving expand state); only a genuinely new variable
    // (or the rare volatile-identity change) is built fresh.
    var existingByName = Variables.ToDictionary(v => v.Model.Name, v => v, StringComparer.OrdinalIgnoreCase);
    var keepNames = new HashSet<string>(newVars.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

    for (var i = Variables.Count - 1; i >= 0; i--) {
      if (!keepNames.Contains(Variables[i].Model.Name)) {
        Variables.RemoveAt(i);
      }
    }

    for (var i = 0; i < newVars.Count; i++) {
      var newVar = newVars[i];

      if (existingByName.TryGetValue(newVar.Name, out var existing)
          && existing.Model.IsVolatile == newVar.IsVolatile) {
        var currentIndex = Variables.IndexOf(existing);
        if (currentIndex != i) {
          Variables.Move(currentIndex, i);
        }
        existing.UpdateFrom(newVar);
      }
      else {
        // Genuinely new variable (or the rare volatile-identity change): build a fresh VM.
        if (existing is not null) {
          Variables.Remove(existing);
        }

        var viewModel = new VariableViewModel(newVar, clipboardService, RemoveVariable, () => parentViewModel?.UpdatePendingChangesState(), UpdateFilteredVariables);
        Variables.Insert(i, viewModel);
      }
    }

    UpdateFilteredVariables();
  }

  /// <summary>
  /// Checks if new variables differ from current Variables collection.
  /// Returns true if any changes detected (add, remove, or modification).
  /// Supports both sorted (Refresh) and unsorted (Import/Undo/Redo) inputs.
  /// </summary>
  private bool HasVariablesChanged(List<EnvironmentVariableModel> newVars) {
    // Quick count check
    if (Variables.Count != newVars.Count) {
      return true;
    }

    // Create lookup by name for current variables
    var currentByName = Variables.ToDictionary(
      v => v.Model.Name,
      v => v.Model,
      StringComparer.OrdinalIgnoreCase
    );

    // Check each new variable
    foreach (var newVar in newVars) {
      if (!currentByName.TryGetValue(newVar.Name, out var current)) {
        return true; // Variable added
      }

      // Compare all relevant fields
      if (current.Data != newVar.Data ||
          current.Type != newVar.Type ||
          current.IsAdded != newVar.IsAdded ||
          current.IsRemoved != newVar.IsRemoved ||
          current.IsVolatile != newVar.IsVolatile) {
        return true;
      }
    }

    return false;
  }

  public void AddVariable(string name, string value, RegistryValueKind type) {
    // Check if variable already exists (not deleted)
    var existingActiveViewModel = Variables.FirstOrDefault(v =>
      !v.Model.IsRemoved &&
      string.Equals(v.Model.Name, name, StringComparison.OrdinalIgnoreCase));

    if (existingActiveViewModel != null) {
      // Skip volatile (read-only) variables - can't update them
      if (existingActiveViewModel.Model.IsVolatile) {
        return;
      }

      // Only update if values are different - avoid triggering change notification for no-op
      if (existingActiveViewModel.Data == value) {
        // Data matches, preserve original type to avoid false dirty state
        return;
      }

      // Update existing variable's Data value - preserve original Type
      // (Paste doesn't include type info, so we keep existing type)
      existingActiveViewModel.Data = value;
      return;
    }

    // Check if there's a deleted variable with same name to restore
    var existingDeletedViewModel = Variables.FirstOrDefault(v =>
      v.Model.IsRemoved &&
      string.Equals(v.Model.Name, name, StringComparison.OrdinalIgnoreCase));

    if (existingDeletedViewModel != null) {
      // Restore deleted variable in-place by clearing IsRemoved flag
      // This preserves the original Model object and change history
      existingDeletedViewModel.Model.IsRemoved = false;

      // Only update Data if different
      if (existingDeletedViewModel.Data != value) {
        existingDeletedViewModel.Data = value;
      }

      // IMPORTANT: Preserve original Type to match snapshot
      // Paste doesn't include type info, so we keep the existing type
      // to avoid false dirty state when the only difference is type

      UpdateFilteredVariables();
      parentViewModel?.UpdatePendingChangesState();
      return;
    }

    // Create new variable
    var variable = new EnvironmentVariableModel {
      Name = name,
      Data = value,
      Scope = Scope,
      Type = type,
      IsAdded = true,
      IsRemoved = false,
    };

    var newViewModel = new VariableViewModel(variable, clipboardService, RemoveVariable, () => parentViewModel?.UpdatePendingChangesState(), UpdateFilteredVariables);

    // Find sorted insertion position
    var insertIndex = 0;
    for (var i = 0; i < Variables.Count; i++) {
      if (string.Compare(name, Variables[i].Name, StringComparison.OrdinalIgnoreCase) < 0) {
        insertIndex = i;
        break;
      }
      insertIndex = i + 1;
    }

    Variables.Insert(insertIndex, newViewModel);
    UpdateFilteredVariables();
    parentViewModel?.UpdatePendingChangesState();
  }

  public void RemoveVariable(VariableViewModel variable) {
    if (variable.Model.IsAdded) {
      // Newly added variables can just be removed from the collection (no net change)
      Variables.Remove(variable);
    }
    else {
      // Existing variables: mark as removed but keep in collection for save operation
      variable.Model.IsRemoved = true;
    }

    UpdateFilteredVariables();
    parentViewModel?.UpdatePendingChangesState();
  }

  /// <summary>
  /// Removes variables whose names are not in the provided set.
  /// Used during import to remove variables not present in the imported file.
  /// Skips volatile (read-only) variables.
  /// </summary>
  public void RemoveVariablesNotIn(HashSet<string> namesToKeep) {
    foreach (var variable in Variables.Where(v => !v.Model.IsRemoved && !v.Model.IsVolatile).ToList()) {
      if (!namesToKeep.Contains(variable.Name)) {
        RemoveVariable(variable);
      }
    }
  }

  /// <summary>Rebuilds the bound list in sorted order, resyncing the ListView visual after a drop.</summary>
  public void ResyncFilteredVariables() {
    var ordered = FilteredVariables.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
    FilteredVariables.Clear();
    foreach (var variable in ordered) {
      FilteredVariables.Add(variable);
    }
  }

  /// <summary>
  /// Gets all variable models for this scope.
  /// </summary>
  public IEnumerable<EnvironmentVariableModel> GetAllVariables() => Variables.Select(v => v.Model);

  /// <summary>
  /// Removes variables marked as removed and clears IsAdded flags after a successful save.
  /// </summary>
  public void CleanupAfterSave() {
    foreach (var variable in Variables.ToList()) {
      if (variable.Model.IsRemoved) {
        Variables.Remove(variable);
      }
      else {
        variable.Model.IsAdded = false;
      }
    }
    UpdateFilteredVariables();
  }

  [RelayCommand]
  private async Task Add() {
    if (parentViewModel?.XamlRoot is null) {
      return;
    }

    var nameTextBox = DialogHelper.CreateDialogTextBox();
    var valueTextBox = DialogHelper.CreateDialogTextBox();

    var stringRadio = new RadioButton {
      Content = "String (REG_SZ)",
      IsChecked = true,
      Margin = new Thickness(0, 0, 12, 0),
    };

    var expandStringRadio = new RadioButton {
      Content = "Expandable String (REG_EXPAND_SZ)",
      IsChecked = false,
    };

    var typePanel = new StackPanel {
      Orientation = Orientation.Horizontal,
      Children = {
        stringRadio,
        expandStringRadio,
      },
    };

    var nameGrid = DialogHelper.CreateLabelValueGrid(Localize.Get("NameLabel"), nameTextBox, labelWidth: DialogLabelWidth);
    var dataGrid = DialogHelper.CreateLabelValueGrid(Localize.Get("DataLabel"), valueTextBox, labelWidth: DialogLabelWidth);
    var typeGrid = DialogHelper.CreateLabelValueGrid(Localize.Get("TypeLabel"), typePanel, labelWidth: DialogLabelWidth);

    var errorPanel = new StackPanel {
      Margin = new Thickness(0, 4, 0, 0),
      Spacing = 4,
    };

    var contentPanel = DialogHelper.CreateDialogPanel([
      nameGrid,
      dataGrid,
      typeGrid,
      errorPanel,
    ]);

    var dialog = DialogHelper.CreateStandardDialog(parentViewModel.XamlRoot, Localize.Get("AddDialogTitle"), contentPanel, Localize.Get("OkayButton"), Localize.Get("CancelButton"));
    dialog.IsPrimaryButtonEnabled = false;

    var errorBrush = Application.Current.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush;
    var normalBrush = nameTextBox.BorderBrush;
    var standardThickness = (Thickness)Application.Current.Resources["StandardBorderThickness"];

    // Default margin from style (0,4,0,0) - no need to adjust if border thickness is constant

    void ValidateInput() {
      var name = nameTextBox.Text;
      var data = valueTextBox.Text;

      var nameErrors = VariableValidator.ValidateNameAllErrors(name);
      var dataErrors = VariableValidator.ValidateDataAllErrors(data);

      var nameValid = nameErrors.Count == 0;
      var dataValid = dataErrors.Count == 0;

      nameTextBox.BorderBrush = nameValid ? normalBrush : errorBrush;
      nameTextBox.BorderThickness = standardThickness;

      valueTextBox.BorderBrush = dataValid ? normalBrush : errorBrush;
      valueTextBox.BorderThickness = standardThickness;

      errorPanel.Children.Clear();

      var errorStyle = Application.Current.Resources["ErrorMessageStyle"] as Style;

      foreach (var error in nameErrors) {
        errorPanel.Children.Add(new TextBlock {
          Style = errorStyle,
          Text = $"{Localize.Get("NameLabel")} {error}",
        });
      }

      foreach (var error in dataErrors) {
        errorPanel.Children.Add(new TextBlock {
          Style = errorStyle,
          Text = $"{Localize.Get("DataLabel")} {error}",
        });
      }

      dialog.IsPrimaryButtonEnabled = nameValid && dataValid;
    }

    nameTextBox.TextChanged += (s, e) => ValidateInput();
    valueTextBox.TextChanged += (s, e) => ValidateInput();

    nameTextBox.Loaded += (s, e) => nameTextBox.Focus(FocusState.Programmatic);
    dialog.Opened += (s, e) => ValidateInput();

    var result = await dialog.ShowAsync();

    if (result == ContentDialogResult.Primary) {
      var name = nameTextBox.Text.Trim();
      var data = valueTextBox.Text;

      var type = expandStringRadio.IsChecked == true
        ? RegistryValueKind.ExpandString
        : RegistryValueKind.String;

      AddVariable(name, data, type);
    }
  }

  [RelayCommand]
  private void CopyAll() {
    var lines = FilteredVariables.Where(v => !v.Model.IsVolatile).Select(v => $"{v.Name}={v.Data}");
    var text = string.Join(Environment.NewLine, lines);
    clipboardService.SetText(text);
  }

  [RelayCommand]
  private async Task PasteAll() {
    var text = await clipboardService.GetText();
    if (string.IsNullOrWhiteSpace(text)) {
      return;
    }

    var parsed = ClipboardFormatHelper.ParseMultiLine(text);

    void ApplyAll() {
      foreach (var (name, value) in parsed) {
        AddVariable(name, value, RegistryValueKind.String);
      }
    }

    if (parentViewModel is not null) {
      parentViewModel.RunBatch(ApplyAll);
    }
    else {
      ApplyAll();
    }
  }
}
