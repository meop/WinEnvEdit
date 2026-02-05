using Microsoft.Win32;

namespace WinEnvEdit.Models;

public class EnvironmentVariable {
  public string Name { get; set; } = string.Empty;
  public string Data { get; set; } = string.Empty;
  public VariableScope Scope { get; init; }
  public RegistryValueKind Type { get; set; } = RegistryValueKind.String;
  public bool IsVolatile { get; init; }
  public bool IsAdded { get; set; }
  public bool IsRemoved { get; set; }
}
