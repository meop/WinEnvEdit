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
  public partial VariableScopeViewModel SystemVariables { get; set; }

  [ObservableProperty]
  public partial VariableScopeViewModel UserVariables { get; set; }

  [ObservableProperty]
  public partial bool IsAdmin { get; set; }

  public bool IsNotAdmin => !IsAdmin;

  public Visibility PermissionsButtonVisibility => IsAdmin ? Visibility.Collapsed : Visibility.Visible;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
  [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
  public partial bool HasPendingChanges { get; set; }

  [ObservableProperty]
  public partial bool ShowVolatileVariables { get; set; }

  [ObservableProperty]
  public partial bool ExpandAllPaths { get; set; }

  public MainWindowViewModel() : this(new EnvironmentService(), new AdminService()) {
  }

  public MainWindowViewModel(IEnvironmentService environmentService, IAdminService adminService) {
    _environmentService = environmentService;
    _adminService = adminService;
    SystemVariables = new VariableScopeViewModel(VariableScope.System, _environmentService, this);
    UserVariables = new VariableScopeViewModel(VariableScope.User, _environmentService, this);

    // Detect if running as administrator
    IsAdmin = _adminService.IsAdministrator();

    // Load initial data
    LoadVariables();
  }

  partial void OnIsAdminChanged(bool value) {
    OnPropertyChanged(nameof(IsNotAdmin));
    OnPropertyChanged(nameof(PermissionsButtonVisibility));
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

  public void UpdatePendingChangesState() => HasPendingChanges = SystemVariables.HasPendingChanges() || UserVariables.HasPendingChanges();

  private bool CanUndo() {
    return HasPendingChanges;
  }

  private bool CanSave() {
    return HasPendingChanges;
  }

  [RelayCommand]
  private void Import() {
    // TODO: Implement import logic
  }

  [RelayCommand]
  private void Export() {
    // TODO: Implement export logic
  }

  [RelayCommand]
  private void Refresh() {
    LoadVariables();
  }

  [RelayCommand(CanExecute = nameof(CanUndo))]
  private void Undo() {
    // TODO: Implement reset logic
  }

  [RelayCommand(CanExecute = nameof(CanSave))]
  private void Save() {
    // TODO: Implement save logic
  }

  [RelayCommand]
  private void Add() {
    // TODO: Implement add variable dialog
  }

  [RelayCommand]
  private void Permissions() {
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
