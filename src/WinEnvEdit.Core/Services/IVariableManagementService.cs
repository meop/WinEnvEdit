using System.Collections.Generic;

using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Types;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for managing environment variable operations (add, update, remove, restore).
/// </summary>
public interface IVariableManagementService {
  /// <summary>
  /// Result of an AddOrUpdate operation.
  /// </summary>
  public enum AddOrUpdateResult {
    /// <summary>No action taken (volatile variable or data unchanged)</summary>
    NoAction,
    /// <summary>Updated existing active variable</summary>
    Updated,
    /// <summary>Restored a deleted variable</summary>
    Restored,
    /// <summary>Added new variable</summary>
    Added,
  }

  /// <summary>
  /// Adds a new variable, updates existing, or restores deleted variable.
  /// Handles case-insensitive matching and volatile variable skipping.
  /// </summary>
  /// <param name="variables">The current list of variables</param>
  /// <param name="name">Variable name</param>
  /// <param name="value">Variable value</param>
  /// <param name="type">Variable type</param>
  /// <param name="scope">Variable scope</param>
  /// <returns>Result of the operation and the affected variable</returns>
  public (AddOrUpdateResult Result, EnvironmentVariableModel? Variable) AddOrUpdateVariable(
    List<EnvironmentVariableModel> variables,
    string name,
    string value,
    RegistryValueKind type,
    VariableScope scope);

  /// <summary>
  /// Removes a variable or marks it as removed.
  /// Newly added variables are removed from list, existing ones are marked.
  /// </summary>
  /// <param name="variables">The current list of variables</param>
  /// <param name="variableToRemove">The variable to remove</param>
  /// <returns>True if variable was removed from list, false if marked as removed</returns>
  public bool RemoveVariable(List<EnvironmentVariableModel> variables, EnvironmentVariableModel variableToRemove);

  /// <summary>
  /// Removes all variables not in the provided set of names.
  /// Used during import. Skips volatile variables.
  /// </summary>
  /// <param name="variables">The current list of variables</param>
  /// <param name="namesToKeep">Names of variables to keep</param>
  /// <returns>Number of variables removed or marked</returns>
  public int RemoveVariablesNotIn(List<EnvironmentVariableModel> variables, HashSet<string> namesToKeep);

  /// <summary>
  /// Cleans up after save: removes variables marked as removed and clears IsAdded flags.
  /// </summary>
  /// <param name="variables">The current list of variables</param>
  /// <returns>Number of variables removed from list</returns>
  public int CleanupAfterSave(List<EnvironmentVariableModel> variables);
}
