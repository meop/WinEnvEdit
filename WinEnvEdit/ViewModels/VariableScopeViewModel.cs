using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using WinEnvEdit.Models;
using WinEnvEdit.Services;

namespace WinEnvEdit.ViewModels;

public partial class VariableScopeViewModel : ObservableObject {
  private readonly IEnvironmentService _environmentService;

  [ObservableProperty]
  private VariableScope _scope;

  [ObservableProperty]
  private ObservableCollection<VariableViewModel> _variables = [];

  public VariableScopeViewModel(VariableScope scope, IEnvironmentService environmentService) {
    _scope = scope;
    _environmentService = environmentService;
  }

  public void LoadFromRegistry() {
    Variables.Clear();

    var envVars = Scope == VariableScope.System
      ? _environmentService.GetSystemVariables()
      : _environmentService.GetUserVariables();

    foreach (var envVar in envVars.OrderBy(v => v.Name)) {
      Variables.Add(new VariableViewModel(envVar));
    }
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

    Variables.Add(new VariableViewModel(variable));
  }

  public void RemoveVariable(VariableViewModel variable) {
    variable.Model.IsDeleted = true;
    Variables.Remove(variable);
  }
}
