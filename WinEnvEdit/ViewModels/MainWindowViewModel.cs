using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WinEnvEdit.Models;
using WinEnvEdit.Services;

namespace WinEnvEdit.ViewModels;

public partial class MainWindowViewModel : ObservableObject {
  private readonly IEnvironmentService _environmentService;

  [ObservableProperty]
  private VariableScopeViewModel _systemVariables;

  [ObservableProperty]
  private VariableScopeViewModel _userVariables;

  [ObservableProperty]
  private bool _isAdmin;

  [ObservableProperty]
  private bool _hasPendingChanges;

  [ObservableProperty]
  private bool _showVolatileVariables = true;

  [ObservableProperty]
  private bool _expandAllPaths;

  public MainWindowViewModel() : this(new EnvironmentService()) {
  }

  public MainWindowViewModel(IEnvironmentService environmentService) {
    _environmentService = environmentService;
    _systemVariables = new VariableScopeViewModel(VariableScope.System, _environmentService);
    _userVariables = new VariableScopeViewModel(VariableScope.User, _environmentService);

    // Load initial data
    LoadVariables();
  }

  private void LoadVariables() {
    SystemVariables.LoadFromRegistry();
    UserVariables.LoadFromRegistry();
  }

  [RelayCommand]
  private void Save() {
    // TODO: Implement save logic
  }

  [RelayCommand]
  private void Reset() {
    // TODO: Implement reset logic
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
    // TODO: Implement elevation logic
  }

  [RelayCommand]
  private void ToggleExpandAllPaths() {
    ExpandAllPaths = !ExpandAllPaths;
    // TODO: Apply expand/collapse to all path variables
  }

  [RelayCommand]
  private void ToggleShowVolatileVariables() {
    ShowVolatileVariables = !ShowVolatileVariables;
    // TODO: Apply filter to variable lists
  }

  [RelayCommand]
  private void About() {
    // TODO: Implement about dialog
  }
}
