using Microsoft.Win32;

namespace WinEnvEdit.Core.Types;

/// <summary>
/// Base class for all variable change operations.
/// </summary>
public abstract record VariableDelta(VariableScope Scope, string Name);

/// <summary>
/// Represents a variable that was added.
/// </summary>
public record VariableAdded(
  VariableScope Scope,
  string Name,
  string Data,
  RegistryValueKind Type,
  bool IsVolatile,
  bool IsAdded,
  bool IsRemoved
) : VariableDelta(Scope, Name);

/// <summary>
/// Represents a variable that was removed.
/// </summary>
public record VariableRemoved(
  VariableScope Scope,
  string Name,
  string Data,
  RegistryValueKind Type,
  bool IsVolatile,
  bool IsAdded,
  bool IsRemoved
) : VariableDelta(Scope, Name);

/// <summary>
/// Represents a variable whose data or type was modified.
/// </summary>
public record VariableModified(
  VariableScope Scope,
  string Name,
  string OldData,
  string NewData,
  RegistryValueKind OldType,
  RegistryValueKind NewType
) : VariableDelta(Scope, Name);

/// <summary>
/// Represents a collection of changes between two states.
/// </summary>
public record StateDelta(List<VariableDelta> Changes) {
  /// <summary>
  /// Returns true if this delta contains no changes.
  /// </summary>
  public bool IsEmpty => Changes.Count == 0;
}
