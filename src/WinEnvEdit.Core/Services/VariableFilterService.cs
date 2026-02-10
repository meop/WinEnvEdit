using WinEnvEdit.Core.Models;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for filtering environment variables based on various criteria.
/// </summary>
public class VariableFilterService : IVariableFilterService {
  /// <summary>
  /// Filters a list of environment variables based on search text, volatile flag, and removed flag.
  /// </summary>
  public List<EnvironmentVariableModel> FilterVariables(
    List<EnvironmentVariableModel> variables,
    string? searchText = null,
    bool showVolatile = false,
    bool includeRemoved = false) {
    var result = new List<EnvironmentVariableModel>();
    var search = searchText?.Trim() ?? string.Empty;
    var hasSearch = search.Length > 0;

    foreach (var variable in variables) {
      // Filter out removed variables unless includeRemoved is true
      if (variable.IsRemoved && !includeRemoved) {
        continue;
      }

      // Filter out volatile variables unless showVolatile is true
      if (variable.IsVolatile && !showVolatile) {
        continue;
      }

      // Filter by search text (name or value contains search)
      if (hasSearch) {
        var nameMatch = variable.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
        var valueMatch = variable.Data.Contains(search, StringComparison.OrdinalIgnoreCase);
        if (!nameMatch && !valueMatch) {
          continue;
        }
      }

      result.Add(variable);
    }

    return result;
  }
}
