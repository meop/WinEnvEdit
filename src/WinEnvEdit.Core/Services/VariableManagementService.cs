using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Types;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for managing environment variable operations (add, update, remove, restore).
/// </summary>
public class VariableManagementService : IVariableManagementService {
  private readonly IVariableCollectionService collectionService;

  public VariableManagementService(IVariableCollectionService collectionService) {
    this.collectionService = collectionService;
  }

  /// <summary>
  /// Adds a new variable, updates existing, or restores deleted variable.
  /// </summary>
  public (IVariableManagementService.AddOrUpdateResult Result, EnvironmentVariableModel? Variable) AddOrUpdateVariable(
    List<EnvironmentVariableModel> variables,
    string name,
    string value,
    RegistryValueKind type,
    VariableScope scope) {
    // Check if variable already exists (not deleted)
    var existingActive = variables.FirstOrDefault(v =>
      !v.IsRemoved &&
      string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));

    if (existingActive != null) {
      // Skip volatile (read-only) variables - can't update them
      if (existingActive.IsVolatile) {
        return (IVariableManagementService.AddOrUpdateResult.NoAction, null);
      }

      // Only update if values are different - avoid no-op
      if (existingActive.Data == value) {
        return (IVariableManagementService.AddOrUpdateResult.NoAction, null);
      }

      // Update existing variable's Data value - preserve original Type
      existingActive.Data = value;
      return (IVariableManagementService.AddOrUpdateResult.Updated, existingActive);
    }

    // Check if there's a deleted variable with same name to restore
    var existingDeleted = variables.FirstOrDefault(v =>
      v.IsRemoved &&
      string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));

    if (existingDeleted != null) {
      // Restore deleted variable by clearing IsRemoved flag
      existingDeleted.IsRemoved = false;

      // Only update Data if different
      if (existingDeleted.Data != value) {
        existingDeleted.Data = value;
      }

      // Preserve original Type
      return (IVariableManagementService.AddOrUpdateResult.Restored, existingDeleted);
    }

    // Create new variable
    var newVariable = new EnvironmentVariableModel {
      Name = name,
      Data = value,
      Scope = scope,
      Type = type,
      IsAdded = true,
      IsRemoved = false,
    };

    // Find sorted insertion position and insert
    var insertIndex = collectionService.FindInsertionIndex(variables, name);
    variables.Insert(insertIndex, newVariable);

    return (IVariableManagementService.AddOrUpdateResult.Added, newVariable);
  }

  /// <summary>
  /// Removes a variable or marks it as removed.
  /// </summary>
  public bool RemoveVariable(List<EnvironmentVariableModel> variables, EnvironmentVariableModel variableToRemove) {
    if (variableToRemove.IsAdded) {
      // Newly added variables: remove from collection (no net change)
      return variables.Remove(variableToRemove);
    }

    // Existing variables: mark as removed but keep in collection for save
    variableToRemove.IsRemoved = true;
    return false;
  }

  /// <summary>
  /// Removes all variables not in the provided set of names.
  /// </summary>
  public int RemoveVariablesNotIn(List<EnvironmentVariableModel> variables, HashSet<string> namesToKeep) {
    var count = 0;
    var toProcess = variables
      .Where(v => !v.IsRemoved && !v.IsVolatile)
      .ToList();

    foreach (var variable in toProcess) {
      if (!namesToKeep.Contains(variable.Name, StringComparer.OrdinalIgnoreCase)) {
        RemoveVariable(variables, variable);
        count++;
      }
    }

    return count;
  }

  /// <summary>
  /// Cleans up after save: removes variables marked as removed and clears IsAdded flags.
  /// </summary>
  public int CleanupAfterSave(List<EnvironmentVariableModel> variables) {
    var removedCount = 0;

    for (var i = variables.Count - 1; i >= 0; i--) {
      if (variables[i].IsRemoved) {
        variables.RemoveAt(i);
        removedCount++;
      }
      else {
        variables[i].IsAdded = false;
      }
    }

    return removedCount;
  }
}
