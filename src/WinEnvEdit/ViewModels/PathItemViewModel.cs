using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WinEnvEdit.Validation;

namespace WinEnvEdit.ViewModels;

public partial class PathItemViewModel : ObservableObject {
  private readonly VariableViewModel parent;
  private bool isInitializing = true;

  public PathItemViewModel(string pathValue, VariableViewModel parentViewModel) {
    parent = parentViewModel;
    PathValue = pathValue;
    Exists = !VariableValidator.LooksLikePath(pathValue) || VariableValidator.IsValidPath(pathValue);
    isInitializing = false;
  }

  [ObservableProperty]
  public partial string PathValue { get; set; }

  [ObservableProperty]
  public partial bool Exists { get; set; }

  public bool IsReadOnly => parent.VisualIsLocked;

  partial void OnPathValueChanged(string value) {
    if (isInitializing) {
      return;
    }
    parent.SyncDataFromPaths();
    UpdateExists();
  }

  /// <summary>
  /// Updates the Exists property based on current PathValue.
  /// Validation applies if the value looks like a filesystem path.
  /// </summary>
  public void UpdateExists() => Exists = !VariableValidator.LooksLikePath(PathValue) || VariableValidator.IsValidPath(PathValue);

  [RelayCommand]
  private void Remove() => parent.RemovePath(this);
}
