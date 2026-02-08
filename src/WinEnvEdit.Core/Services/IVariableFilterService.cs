using System.Collections.Generic;

using WinEnvEdit.Core.Models;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for filtering environment variables based on various criteria.
/// </summary>
public interface IVariableFilterService {
  /// <summary>
  /// Filters a list of environment variables based on search text, volatile flag, and removed flag.
  /// </summary>
  /// <param name="variables">The variables to filter</param>
  /// <param name="searchText">Optional search text to filter by name or value (case-insensitive)</param>
  /// <param name="showVolatile">Whether to include volatile (read-only) variables</param>
  /// <param name="includeRemoved">Whether to include removed variables</param>
  /// <returns>Filtered list of variables</returns>
  public List<EnvironmentVariableModel> FilterVariables(
    List<EnvironmentVariableModel> variables,
    string? searchText = null,
    bool showVolatile = false,
    bool includeRemoved = false);
}
