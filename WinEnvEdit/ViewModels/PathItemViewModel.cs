using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WinEnvEdit.Core.Validators;

using WinRT;

namespace WinEnvEdit.ViewModels;

[GeneratedBindableCustomProperty]
public partial class PathItemViewModel : ObservableObject {
  private readonly VariableViewModel parent;
  private readonly bool isInitializing = true;

  public PathItemViewModel(string pathValue, VariableViewModel parentViewModel) {
    parent = parentViewModel;
    PathValue = pathValue;
    Exists = !VariableValidator.LooksLikePath(pathValue) || VariableValidator.IsValidPath(pathValue);
    RemoveCommand = new RelayCommand(() => parent.RemovePath(this));
    isInitializing = false;
  }

  [ObservableProperty]
  public partial string PathValue { get; set; }

  [ObservableProperty]
  public partial bool Exists { get; set; }

  public bool IsReadOnly => parent.VisualIsLocked;

  public IRelayCommand RemoveCommand { get; }

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
}
