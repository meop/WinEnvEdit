using Microsoft.Win32;

namespace WinEnvEdit.Models;

public class EnvironmentVariable
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public VariableScope Scope { get; set; }
    public RegistryValueKind Kind { get; set; } = RegistryValueKind.String;
    public bool IsVolatile { get; set; }
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public bool HasChanges()
    {
        if (IsNew || IsDeleted)
        {
            return true;
        }

        return Name != OriginalName || Value != OriginalValue;
    }

    public void CommitChanges()
    {
        if (IsDeleted)
        {
            return;
        }

        OriginalName = Name;
        OriginalValue = Value;
        IsNew = false;
    }

    public void RevertChanges()
    {
        Name = OriginalName;
        Value = OriginalValue;
        IsNew = false;
        IsDeleted = false;
    }
}
