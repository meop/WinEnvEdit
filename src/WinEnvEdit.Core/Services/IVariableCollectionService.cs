using System.Collections.Generic;

using WinEnvEdit.Core.Models;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for managing collections of environment variables with business rules.
/// </summary>
public interface IVariableCollectionService {
  /// <summary>
  /// Determines if the collection has changed compared to a new list.
  /// </summary>
  public bool HasChanged(List<EnvironmentVariableModel> current, List<EnvironmentVariableModel> newVars);

  /// <summary>
  /// Sorts variables alphabetically by name (case-insensitive).
  /// </summary>
  public List<EnvironmentVariableModel> SortVariables(List<EnvironmentVariableModel> variables);

  /// <summary>
  /// Finds a variable by name (case-insensitive).
  /// </summary>
  public EnvironmentVariableModel? FindVariable(List<EnvironmentVariableModel> variables, string name);

  /// <summary>
  /// Finds the insertion index for a new variable to maintain sorted order.
  /// </summary>
  public int FindInsertionIndex(List<EnvironmentVariableModel> variables, string name);
}
