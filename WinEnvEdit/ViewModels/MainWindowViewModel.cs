using System.Linq;

using Microsoft.UI.Xaml;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WinEnvEdit.Models;
using WinEnvEdit.Services;

namespace WinEnvEdit.ViewModels;

public partial class MainWindowViewModel : ObservableObject {
  private readonly IEnvironmentService _environmentService;
  private readonly IAdminService _adminService;

  [ObservableProperty]
  private VariableScopeViewModel _systemVariables;

  [ObservableProperty]
  private VariableScopeViewModel _userVariables;

  [ObservableProperty]
  private bool _isAdmin;

  public bool IsNotAdmin => !IsAdmin;

  public Visibility ElevateButtonVisibility => IsAdmin ? Visibility.Collapsed : Visibility.Visible;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
  [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
  private bool _hasPendingChanges;

  [ObservableProperty]
  private bool _showVolatileVariables;

  [ObservableProperty]
  private bool _expandAllPaths;

  public MainWindowViewModel() : this(new EnvironmentService(), new AdminService()) {
  }

  public MainWindowViewModel(IEnvironmentService environmentService, IAdminService adminService) {
    _environmentService = environmentService;
    _adminService = adminService;
    _systemVariables = new VariableScopeViewModel(VariableScope.System, _environmentService, this);
    _userVariables = new VariableScopeViewModel(VariableScope.User, _environmentService, this);

    // Detect if running as administrator
    _isAdmin = _adminService.IsAdministrator();

    // Load initial data
    LoadVariables();
  }

  partial void OnIsAdminChanged(bool value) {
    OnPropertyChanged(nameof(IsNotAdmin));
    OnPropertyChanged(nameof(ElevateButtonVisibility));
  }

  partial void OnShowVolatileVariablesChanged(bool value) {
    SystemVariables.ShowVolatileVariables = value;
    UserVariables.ShowVolatileVariables = value;
  }

  partial void OnExpandAllPathsChanged(bool value) {
    // Apply expand/collapse to all path variables
    foreach (var variable in SystemVariables.Variables.Concat(UserVariables.Variables)) {
      if (variable.IsPathList) {
        variable.IsExpanded = value;
      }
    }
  }

  private void LoadVariables() {
    SystemVariables.LoadFromRegistry();
    UserVariables.LoadFromRegistry();
    HasPendingChanges = false;
  }

  public void UpdatePendingChangesState() {
    HasPendingChanges = SystemVariables.HasPendingChanges() || UserVariables.HasPendingChanges();
  }

  [RelayCommand(CanExecute = nameof(CanSave))]
  private void Save() {
    // TODO: Implement save logic
  }

  private bool CanSave() {
    return HasPendingChanges;
  }

  [RelayCommand(CanExecute = nameof(CanReset))]
  private void Reset() {
    // TODO: Implement reset logic
  }

  private bool CanReset() {
    return HasPendingChanges;
  }

  [RelayCommand]
  private void Reload() {
    LoadVariables();
  }

  [RelayCommand]
  private void Backup() {
    // TODO: Implement backup logic
  }

  [RelayCommand]
  private void Restore() {
    // TODO: Implement restore logic
  }

  [RelayCommand]
  private void Add() {
    // TODO: Implement add variable dialog
  }

  [RelayCommand]
  private void Elevate() {
    _adminService.RestartAsAdministrator();
  }

  [RelayCommand]
  private void ToggleExpandAllPaths() {
    ExpandAllPaths = !ExpandAllPaths;
  }

  [RelayCommand]
  private void ToggleShowVolatileVariables() {
    ShowVolatileVariables = !ShowVolatileVariables;
  }

  [RelayCommand]
  private void About() {
    // TODO: Implement about dialog
  }
}
