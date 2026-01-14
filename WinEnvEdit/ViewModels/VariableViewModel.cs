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
  private string _name = string.Empty;

  [ObservableProperty]
  private string _value = string.Empty;

  [ObservableProperty]
  private bool _isLocked;

  public bool VisualIsLocked => IsLocked;

  [ObservableProperty]
  private bool _isPathList;

  [ObservableProperty]
  private bool _isExpanded;

  [ObservableProperty]
  private ObservableCollection<PathItem> _pathItems = [];

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
    if (string.IsNullOrWhiteSpace(Value)) return;

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
