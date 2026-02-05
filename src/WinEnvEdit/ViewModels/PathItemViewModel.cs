using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinEnvEdit.ViewModels;

public partial class PathItemViewModel(string pathValue, VariableViewModel parentViewModel) : ObservableObject {
  private readonly VariableViewModel parent = parentViewModel;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(Exists))]
  public partial string PathValue { get; set; } = pathValue;

  public bool Exists => !ShouldValidate || IsValidPath(PathValue);

  public bool IsReadOnly => parent.VisualIsLocked;

  public bool ShouldValidate => parent.EnablePathValidation;

  partial void OnPathValueChanged(string value) {
    parent.SyncDataFromPaths();
  }

  [RelayCommand]
  private void Remove() => parent.RemovePath(this);

  private static bool IsValidPath(string path) {
    if (string.IsNullOrWhiteSpace(path)) {
      return false;
    }

    try {
      // Environment variables in path (e.g. %SystemRoot%) need expansion to check existence
      var expanded = System.Environment.ExpandEnvironmentVariables(path);
      return Directory.Exists(expanded) || File.Exists(expanded);
    }
    catch {
      return false;
    }
  }
}
