using System;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using WinEnvEdit.Models;

namespace WinEnvEdit.ViewModels;

public partial class VariableViewModel : ObservableObject {
  [ObservableProperty]
  private string _name = string.Empty;

  [ObservableProperty]
  private string _value = string.Empty;

  [ObservableProperty]
  private bool _isLocked;

  [ObservableProperty]
  private bool _isPathList;

  public EnvironmentVariable Model { get; init; }

  public VariableViewModel(EnvironmentVariable model) {
    Model = model;
    Name = model.Name;
    Value = model.Value;
    IsLocked = model.IsVolatile;
    IsPathList = IsPathVariable(model.Name);
  }

  private static bool IsPathVariable(string name) {
    string[] pathVariables = ["PATH", "PATHEXT", "PSMODULEPATH"];
    return pathVariables.Contains(name, StringComparer.OrdinalIgnoreCase);
  }
}
