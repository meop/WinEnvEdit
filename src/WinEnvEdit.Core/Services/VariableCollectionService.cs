using WinEnvEdit.Core.Models;

namespace WinEnvEdit.Core.Services;

public class VariableCollectionService : IVariableCollectionService {
  /// <summary>
  /// Checks if new variables differ from current variables.
  /// Returns true if any changes detected (add, remove, or modification).
  /// Supports both sorted and unsorted inputs using dictionary lookup.
  /// </summary>
  public bool HasChanged(List<EnvironmentVariableModel> current, List<EnvironmentVariableModel> newVars) {
    // Quick count check
    if (current.Count != newVars.Count) {
      return true;
    }

    // Create lookup by name for current variables
    var currentByName = new Dictionary<string, EnvironmentVariableModel>(StringComparer.OrdinalIgnoreCase);
    foreach (var v in current) {
      currentByName[v.Name] = v;
    }

    // Check each new variable
    foreach (var newVar in newVars) {
      if (!currentByName.TryGetValue(newVar.Name, out var currentVar)) {
        return true; // Variable added
      }

      // Compare all relevant fields
      if (currentVar.Data != newVar.Data ||
          currentVar.Type != newVar.Type ||
          currentVar.IsAdded != newVar.IsAdded ||
          currentVar.IsRemoved != newVar.IsRemoved ||
          currentVar.IsVolatile != newVar.IsVolatile) {
        return true;
      }
    }

    return false;
  }

  public List<EnvironmentVariableModel> SortVariables(List<EnvironmentVariableModel> variables) =>
    [.. variables.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)];

  public EnvironmentVariableModel? FindVariable(List<EnvironmentVariableModel> variables, string name) =>
    variables.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));

  public int FindInsertionIndex(List<EnvironmentVariableModel> variables, string name) {
    for (var i = 0; i < variables.Count; i++) {
      if (string.Compare(name, variables[i].Name, StringComparison.OrdinalIgnoreCase) < 0) {
        return i;
      }
    }
    return variables.Count;
  }
}
