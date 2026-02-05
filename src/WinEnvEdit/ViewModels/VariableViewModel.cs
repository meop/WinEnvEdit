using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Win32;

using WinEnvEdit.Extensions;
using WinEnvEdit.Models;
using WinEnvEdit.Validation;

namespace WinEnvEdit.ViewModels;

public partial class VariableViewModel : ObservableObject {
  private readonly Action<VariableViewModel>? deleteCallback;
  private readonly Action? changeCallback;
  private readonly Action<VariableViewModel>? refreshCallback;
  private bool isParsing;

  [ObservableProperty]
  public partial string Name { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Data { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool IsLocked { get; private set; } = false;

  public bool VisualIsLocked => IsLocked;

  [ObservableProperty]
  public partial bool IsPathList { get; set; } = false;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(ExpandTooltip))]
  public partial bool IsExpanded { get; set; } = false;

  public string ExpandTooltip => IsExpanded ? "Collapse" : "Expand";

  public static string IconGlyph => Glyph.Remove;

  [ObservableProperty]
  public partial string DataErrorMessage { get; set; } = string.Empty;

  public bool HasDataError => !string.IsNullOrEmpty(DataErrorMessage);

  [ObservableProperty]
  public partial ObservableCollection<PathItemViewModel> PathItems { get; set; }

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasInvalidPath))]
  public partial bool EnablePathValidation { get; set; } = true;

  public bool HasInvalidPath => EnablePathValidation && PathItems.Any(p => !p.Exists);

  public EnvironmentVariable Model { get; init; }

  public VariableViewModel(EnvironmentVariable model, Action<VariableViewModel>? deleteCallback = null, Action? changeCallback = null, Action<VariableViewModel>? refreshCallback = null) {
    Model = model;
    this.deleteCallback = deleteCallback;
    this.changeCallback = changeCallback;
    this.refreshCallback = refreshCallback;
    Name = model.Name;
    Data = model.Data;
    UpdateIsLocked();
    IsPathList = model.Type == RegistryValueKind.ExpandString;
    PathItems = [];

    if (IsPathList) {
      ParsePathsFromData();
      PathItems.CollectionChanged += OnPathItemsCollectionChanged;
    }
  }

  private void OnPathItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
    if (e.OldItems != null) {
      foreach (PathItemViewModel item in e.OldItems) {
        item.PropertyChanged -= OnPathItemPropertyChanged;
      }
    }

    if (e.NewItems != null) {
      foreach (PathItemViewModel item in e.NewItems) {
        item.PropertyChanged += OnPathItemPropertyChanged;
      }
    }

    // Sync data on any collection change (Add, Remove, Move, Replace) except during parsing
    // This handles drag-drop reorder which uses Remove+Insert internally
    if (!isParsing) {
      SyncDataFromPaths();
      changeCallback?.Invoke();
    }

    OnPropertyChanged(nameof(HasInvalidPath));
  }

  private void OnPathItemPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.PropertyName == nameof(PathItemViewModel.Exists)) {
      OnPropertyChanged(nameof(HasInvalidPath));
    }
  }

  private void UpdateIsLocked() =>
    IsLocked = Model.IsVolatile;

  partial void OnNameChanged(string value) {
    Model.Name = value;
    changeCallback?.Invoke();
  }

  partial void OnDataChanged(string value) {
    var result = VariableValidator.ValidateData(value);
    DataErrorMessage = result.IsValid ? string.Empty : result.ErrorMessage;
    Model.Data = value;
    changeCallback?.Invoke();
  }

  [RelayCommand]
  private void ToggleExpand() {
    IsExpanded = !IsExpanded;
    if (IsExpanded && IsPathList) {
      ParsePathsFromData();
    }
  }

  [RelayCommand]
  private void AddPath() =>
    // Sync and change callback handled by OnPathItemsCollectionChanged
    PathItems.Add(new PathItemViewModel(string.Empty, this));

  [RelayCommand]
  public void RemovePath(PathItemViewModel pathItem) =>
    // Sync and change callback handled by OnPathItemsCollectionChanged
    PathItems.Remove(pathItem);

  [RelayCommand]
  private void Remove() {
    deleteCallback?.Invoke(this);
    changeCallback?.Invoke();
  }

  [RelayCommand]
  private void ToggleType() {
    if (IsLocked) {
      return;
    }

    Model.Type = Model.Type == RegistryValueKind.String
      ? RegistryValueKind.ExpandString
      : RegistryValueKind.String;

    var newIsPathList = Model.Type == RegistryValueKind.ExpandString;

    if (newIsPathList != IsPathList) {
      if (newIsPathList) {
        PathItems.CollectionChanged += OnPathItemsCollectionChanged;
        ParsePathsFromData();
      }
      else {
        PathItems.CollectionChanged -= OnPathItemsCollectionChanged;
        PathItems.Clear();
      }

      IsPathList = newIsPathList;
    }

    changeCallback?.Invoke();
    refreshCallback?.Invoke(this);
  }

  [RelayCommand]
  private void CopyData() {
    var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
    dataPackage.SetText($"{Name}={Data}");
    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
  }

  [RelayCommand]
  private async Task PasteDataAsync() {
    if (IsLocked) {
      return; // Can't paste into locked (volatile) variables
    }

    var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
    if (!dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text)) {
      return;
    }

    var clipboardText = await dataPackageView.GetTextAsync();
    if (string.IsNullOrWhiteSpace(clipboardText)) {
      return;
    }

    // Parse "name=value" format
    var separatorIndex = clipboardText.IndexOf('=');
    if (separatorIndex > 0) {
      // Found "name=value" format - extract value part
      var value = clipboardText[(separatorIndex + 1)..];
      Data = value;
    }
    else {
      // No separator found - paste entire text as value
      Data = clipboardText;
    }
  }

  private void ParsePathsFromData() {
    isParsing = true;
    try {
      PathItems.Clear();
      if (string.IsNullOrWhiteSpace(Data)) {
        UpdatePathValidationState();
        return;
      }

      var paths = Data.Split(';', StringSplitOptions.RemoveEmptyEntries);
      foreach (var path in paths) {
        PathItems.Add(new PathItemViewModel(path.Trim(), this));
      }

      UpdatePathValidationState();
    }
    finally {
      isParsing = false;
    }
  }

  public void SyncDataFromPaths() {
    Data = string.Join(";", PathItems.Select(p => p.PathValue));
    Model.Data = Data;
  }

  private void UpdatePathValidationState() =>
    // Determine if path validation should be enabled based on first path
    EnablePathValidation = PathItems.Count > 0 && LooksLikeValidPath(PathItems[0].PathValue);

  private static bool LooksLikeValidPath(string path) {
    if (string.IsNullOrWhiteSpace(path)) {
      return false;
    }

    var trimmed = path.Trim();

    // Check for drive letter + colon pattern (e.g., "C:\", "D:")
    if (DriveLetterRegex().IsMatch(trimmed)) {
      return true;
    }

    // Check for environment variable macro pattern (e.g., "%SystemRoot%", "%USERPROFILE%\go")
    if (EnvVarMacroRegex().IsMatch(trimmed)) {
      return true;
    }

    return false;
  }

  [GeneratedRegex(@"^[A-Za-z]:")]
  private static partial Regex DriveLetterRegex();
  [GeneratedRegex(@"^%")]
  private static partial Regex EnvVarMacroRegex();
}
