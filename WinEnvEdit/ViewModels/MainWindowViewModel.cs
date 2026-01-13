using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinEnvEdit.Models;

namespace WinEnvEdit.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private VariableScopeViewModel _systemVariables = new(VariableScope.System);

    [ObservableProperty]
    private VariableScopeViewModel _userVariables = new(VariableScope.User);

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _hasPendingChanges;

    [ObservableProperty]
    private bool _showVolatileVariables = true;

    [ObservableProperty]
    private bool _expandAllPaths;

    public MainWindowViewModel()
    {
    }

    [RelayCommand]
    private void Save()
    {
    }

    [RelayCommand]
    private void Reset()
    {
    }

    [RelayCommand]
    private void Reload()
    {
    }

    [RelayCommand]
    private void Backup()
    {
    }

    [RelayCommand]
    private void Restore()
    {
    }

    [RelayCommand]
    private void Add()
    {
    }

    [RelayCommand]
    private void Elevate()
    {
    }
}
