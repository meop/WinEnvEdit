using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using WinEnvEdit.Models;

namespace WinEnvEdit.ViewModels;

public partial class VariableViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private bool _isPathList;

    public EnvironmentVariable Model { get; }

    public VariableViewModel(EnvironmentVariable model)
    {
        Model = model;
        Name = model.Name;
        Value = model.Value;
        IsLocked = model.IsVolatile;
        IsPathList = IsPathVariable(model.Name);
    }

    private bool IsPathVariable(string name)
    {
        var pathVariables = new[] { "PATH", "PATHEXT", "PSMODULEPATH" };
        return pathVariables.Contains(name.ToUpperInvariant());
    }
}
