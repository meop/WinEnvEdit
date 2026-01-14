using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using WinEnvEdit.Models;
using WinEnvEdit.Services;

namespace WinEnvEdit.ViewModels;

public partial class VariableScopeViewModel : ObservableObject {
  private readonly IEnvironmentService _environmentService;
  private readonly MainWindowViewModel? _parentViewModel;

  [ObservableProperty]
  public partial VariableScope Scope { get; set; }

  [ObservableProperty]
  public partial ObservableCollection<VariableViewModel> Variables { get; set; } = [];

  [ObservableProperty]
  public partial bool ShowVolatileVariables { get; set; } = true;

  [ObservableProperty]
  public partial ObservableCollection<VariableViewModel> FilteredVariables { get; set; } = [];

  public VariableScopeViewModel(VariableScope scope, IEnvironmentService environmentService, MainWindowViewModel? parentViewModel = null) {
    Scope = scope;
    _environmentService = environmentService;
    _parentViewModel = parentViewModel;
  }

  partial void OnShowVolatileVariablesChanged(bool value) {
    UpdateFilteredVariables();
  }

  partial void OnVariablesChanged(ObservableCollection<VariableViewModel> value) {
    UpdateFilteredVariables();
  }

  private void UpdateFilteredVariables() {
    FilteredVariables.Clear();
    foreach (var variable in Variables.Where(v => ShowVolatileVariables || !v.IsLocked)) {
      FilteredVariables.Add(variable);
    }
  }

  public void LoadFromRegistry() {
    Variables.Clear();

    var envVars = Scope == VariableScope.System
      ? _environmentService.GetSystemVariables()
      : _environmentService.GetUserVariables();

    foreach (var envVar in envVars.OrderBy(v => v.Name)) {
      Variables.Add(new VariableViewModel(envVar, RemoveVariable, () => _parentViewModel?.UpdatePendingChangesState()));
    }

    UpdateFilteredVariables();
  }

  public void AddVariable(string name, string value, bool isPathList = false) {
    var variable = new EnvironmentVariable {
      Name = name,
      Value = value,
      OriginalName = string.Empty,
      OriginalValue = string.Empty,
      Scope = Scope,
      IsNew = true,
      IsDeleted = false
    };

    Variables.Add(new VariableViewModel(variable, RemoveVariable, () => _parentViewModel?.UpdatePendingChangesState()));
    UpdateFilteredVariables();
  }

  public void RemoveVariable(VariableViewModel variable) {
    variable.Model.IsDeleted = true;
    Variables.Remove(variable);
    UpdateFilteredVariables();
    _parentViewModel?.UpdatePendingChangesState();
  }

  public bool HasPendingChanges() {
    return Variables.Any(v => v.Model.HasChanges());
  }
}
