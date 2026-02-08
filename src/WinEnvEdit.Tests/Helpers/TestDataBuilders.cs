using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Types;

namespace WinEnvEdit.Tests.Helpers;

/// <summary>Builder for EnvironmentVariableModel test instances.</summary>
public class EnvironmentVariableBuilder {
  private string name = "TEST_VAR";
  private string data = "test_value";
  private VariableScope scope = VariableScope.User;
  private RegistryValueKind type = RegistryValueKind.String;
  private bool isVolatile = false;
  private bool isAdded = false;
  private bool isRemoved = false;

  public static EnvironmentVariableBuilder Default() => new();

  public EnvironmentVariableBuilder WithName(string name) {
    this.name = name;
    return this;
  }

  public EnvironmentVariableBuilder WithData(string data) {
    this.data = data;
    return this;
  }

  public EnvironmentVariableBuilder WithScope(VariableScope scope) {
    this.scope = scope;
    return this;
  }

  public EnvironmentVariableBuilder WithType(RegistryValueKind type) {
    this.type = type;
    return this;
  }

  public EnvironmentVariableBuilder WithIsVolatile(bool isVolatile) {
    this.isVolatile = isVolatile;
    return this;
  }

  public EnvironmentVariableBuilder WithIsAdded(bool isAdded) {
    this.isAdded = isAdded;
    return this;
  }

  public EnvironmentVariableBuilder WithIsRemoved(bool isRemoved) {
    this.isRemoved = isRemoved;
    return this;
  }

  public EnvironmentVariableModel Build() => new() {
    Name = name,
    Data = data,
    Scope = scope,
    Type = type,
    IsVolatile = isVolatile,
    IsAdded = isAdded,
    IsRemoved = isRemoved,
  };
}
