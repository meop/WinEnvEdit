using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Win32;

using Windows.ApplicationModel.DataTransfer;

using WinEnvEdit.Extensions;
using WinEnvEdit.Models;
using WinEnvEdit.Validation;

namespace WinEnvEdit.ViewModels;

public partial class VariableViewModel : ObservableObject {
  private readonly Action<VariableViewModel>? deleteCallback;
  private readonly Action? changeCallback;
  private readonly Action<VariableViewModel>? refreshCallback;
  private bool isParsing;
  private bool isSyncingFromPaths;

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

  // For non-path-list variables (String type), tracks if the Data value is a valid path
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasInvalidPath))]
  public partial bool DataPathExists { get; set; } = true;

  public bool HasInvalidPath {
    get {
      if (IsPathList) {
        return PathItems.Any(p => !p.Exists);
      }
      // For non-path-list variables, check if the entire Data is a valid path
      return !DataPathExists;
    }
  }

  /// <summary>
  /// Updates DataPathExists based on current Data value. Called when Data changes for non-path-list variables.
  /// Only validates if Data looks like a filesystem path.
  /// </summary>
  public void UpdateDataPathExists() =>
    DataPathExists = !VariableValidator.LooksLikePath(Data) || VariableValidator.IsValidPath(Data);

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
      // Subscribe BEFORE parsing so items get PropertyChanged subscribed
      PathItems.CollectionChanged += OnPathItemsCollectionChanged;
      ParsePathsFromData();
    }
    else {
      // For non-path-list variables, check if Data is a valid path
      UpdateDataPathExists();
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

  private void UpdateIsLocked() => IsLocked = Model.IsVolatile;

  partial void OnNameChanged(string value) {
    Model.Name = value;
    changeCallback?.Invoke();
  }

  partial void OnDataChanged(string value) {
    var result = VariableValidator.ValidateData(value);
    DataErrorMessage = result.IsValid ? string.Empty : result.ErrorMessage;
    Model.Data = value;
    changeCallback?.Invoke();

    // If Data was changed externally (e.g., collapsed TextBox), sync PathItems
    // Skip if we're currently syncing from paths (to avoid infinite loop)
    if (IsPathList && !isSyncingFromPaths) {
      RefreshPathsFromData();
    }
    else if (!IsPathList) {
      // For non-path-list variables, update path existence check
      UpdateDataPathExists();
    }
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
  private async Task PasteData() {
    var dataPackageView = Clipboard.GetContent();
    if (!dataPackageView.Contains(StandardDataFormats.Text)) {
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
        return;
      }

      var paths = Data.Split(';', StringSplitOptions.RemoveEmptyEntries);
      foreach (var path in paths) {
        PathItems.Add(new PathItemViewModel(path.Trim(), this));
      }

      UpdateAllPathExists();
    }
    finally {
      isParsing = false;
    }
  }

  public void SyncDataFromPaths() {
    isSyncingFromPaths = true;
    try {
      Data = string.Join(";", PathItems.Select(p => p.PathValue));
      Model.Data = Data;
    }
    finally {
      isSyncingFromPaths = false;
    }
  }

  /// <summary>
  /// Refreshes PathItems to match current Data value.
  /// Used during registry refresh when Data is updated externally.
  /// </summary>
  public void RefreshPathsFromData() {
    if (!IsPathList) {
      return;
    }

    var newPaths = string.IsNullOrWhiteSpace(Data)
      ? []
      : Data.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();

    // Update existing items or add/remove as needed
    for (var i = 0; i < newPaths.Count; i++) {
      if (i < PathItems.Count) {
        // Update existing item if value changed
        if (PathItems[i].PathValue != newPaths[i]) {
          PathItems[i].PathValue = newPaths[i];
        }
        else {
          // Value same, but still refresh Exists in case file system changed
          PathItems[i].UpdateExists();
        }
      }
      else {
        // Add new item
        PathItems.Add(new PathItemViewModel(newPaths[i], this));
      }
    }

    // Remove extra items
    while (PathItems.Count > newPaths.Count) {
      PathItems.RemoveAt(PathItems.Count - 1);
    }

    UpdateAllPathExists();
  }

  /// <summary>
  /// Re-evaluates Exists on all path items.
  /// </summary>
  private void UpdateAllPathExists() {
    foreach (var pathItem in PathItems) {
      pathItem.UpdateExists();
    }
  }
}
