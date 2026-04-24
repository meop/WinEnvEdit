using WinEnvEdit.Core.Models;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for reading and writing Windows environment variables from the registry.
/// </summary>
public interface IEnvironmentService {
  /// <summary>
  /// Gets all environment variables (both user and system).
  /// </summary>
  public List<EnvironmentVariableModel> GetVariables();

  /// <summary>
  /// Saves a batch of variables to registry.
  /// Uses PowerShell with runas if system variables are present.
  /// </summary>
  public Task SaveVariables(IEnumerable<EnvironmentVariableModel> variables);
}
