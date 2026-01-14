using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WinEnvEdit.Models;

namespace WinEnvEdit.ViewModels;

public partial class VariableViewModel : ObservableObject {
  private readonly Action<VariableViewModel>? _deleteCallback;
  private readonly Action? _changeCallback;

  [ObservableProperty]
  public partial string Name { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Value { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool IsLocked { get; set; }

  public bool VisualIsLocked => IsLocked;

  [ObservableProperty]
  public partial bool IsPathList { get; set; }

  [ObservableProperty]
  public partial bool IsExpanded { get; set; }

  [ObservableProperty]
  public partial ObservableCollection<PathItem> PathItems { get; set; } = [];

  public EnvironmentVariable Model { get; init; }

  public VariableViewModel(EnvironmentVariable model, Action<VariableViewModel>? deleteCallback = null, Action? changeCallback = null) {
    Model = model;
    _deleteCallback = deleteCallback;
    _changeCallback = changeCallback;
    Name = model.Name;
    Value = model.Value;
    IsLocked = model.IsVolatile;
    IsPathList = IsPathVariable(model.Name);

    if (IsPathList) {
      ParsePathsFromValue();
    }
  }

  partial void OnNameChanged(string value) {
    Model.Name = value;
    _changeCallback?.Invoke();
  }

  partial void OnValueChanged(string value) {
    Model.Value = value;
    _changeCallback?.Invoke();
  }

  [RelayCommand]
  private void ToggleExpand() {
    IsExpanded = !IsExpanded;
  }

  [RelayCommand]
  private void AddPath() {
    PathItems.Add(new PathItem { PathValue = string.Empty });
    SyncValueFromPaths();
  }

  [RelayCommand]
  private void RemovePath(PathItem pathItem) {
    PathItems.Remove(pathItem);
    SyncValueFromPaths();
  }

  [RelayCommand]
  private void Delete() {
    _deleteCallback?.Invoke(this);
  }

  private void ParsePathsFromValue() {
    PathItems.Clear();
    if (string.IsNullOrWhiteSpace(Value)) {
      return;
    }

    var paths = Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
    foreach (var path in paths) {
      PathItems.Add(new PathItem { PathValue = path.Trim() });
    }
  }

  private void SyncValueFromPaths() {
    Value = string.Join(";", PathItems.Select(p => p.PathValue));
    Model.Value = Value;
  }

  private static bool IsPathVariable(string name) {
    string[] pathVariables = ["PATH", "PATHEXT", "PSMODULEPATH"];
    return pathVariables.Contains(name, StringComparer.OrdinalIgnoreCase);
  }
}
