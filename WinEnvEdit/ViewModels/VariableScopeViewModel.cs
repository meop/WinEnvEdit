using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WinEnvEdit.Models;

namespace WinEnvEdit.ViewModels;

public partial class VariableScopeViewModel : ObservableObject
{
    [ObservableProperty]
    private VariableScope _scope;

    [ObservableProperty]
    private ObservableCollection<VariableViewModel> _variables = new();

    public VariableScopeViewModel(VariableScope scope)
    {
        Scope = scope;
    }
}
