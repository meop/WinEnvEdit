using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

  [ObservableProperty]
  private bool _isExpanded;

  [ObservableProperty]
  private ObservableCollection<PathItem> _pathItems = [];

  public EnvironmentVariable Model { get; init; }

  public VariableViewModel(EnvironmentVariable model) {
    Model = model;
    Name = model.Name;
    Value = model.Value;
    IsLocked = model.IsVolatile;
    IsPathList = IsPathVariable(model.Name);

    if (IsPathList) {
      ParsePathsFromValue();
    }
  }

  [RelayCommand]
  private void ToggleExpand() {
    IsExpanded = !IsExpanded;
  }

  [RelayCommand]
  private void AddPath() {
    PathItems.Add(new PathItem { Path = string.Empty });
    SyncValueFromPaths();
  }

  [RelayCommand]
  private void RemovePath(PathItem pathItem) {
    PathItems.Remove(pathItem);
    SyncValueFromPaths();
  }

  private void ParsePathsFromValue() {
    PathItems.Clear();
    if (string.IsNullOrWhiteSpace(Value)) return;

    var paths = Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
    foreach (var path in paths) {
      PathItems.Add(new PathItem { Path = path.Trim() });
    }
  }

  private void SyncValueFromPaths() {
    Value = string.Join(";", PathItems.Select(p => p.Path));
    Model.Value = Value;
  }

  private static bool IsPathVariable(string name) {
    string[] pathVariables = ["PATH", "PATHEXT", "PSMODULEPATH"];
    return pathVariables.Contains(name, StringComparer.OrdinalIgnoreCase);
  }
}
