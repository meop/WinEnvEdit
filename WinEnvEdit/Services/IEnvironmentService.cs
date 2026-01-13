using System.Collections.Generic;

using WinEnvEdit.Models;

namespace WinEnvEdit.Services;

/// <summary>
/// Service for reading and writing Windows environment variables from the registry.
/// </summary>
public interface IEnvironmentService {
  /// <summary>
  /// Gets all user-level environment variables.
  /// </summary>
  List<EnvironmentVariable> GetUserVariables();

  /// <summary>
  /// Gets all system-level environment variables.
  /// </summary>
  List<EnvironmentVariable> GetSystemVariables();

  /// <summary>
  /// Saves a variable to the registry.
  /// </summary>
  void SaveVariable(EnvironmentVariable variable);

  /// <summary>
  /// Deletes a variable from the registry.
  /// </summary>
  void DeleteVariable(EnvironmentVariable variable);

  /// <summary>
  /// Notifies the system that environment variables have changed (broadcasts WM_SETTINGCHANGE).
  /// </summary>
  void NotifySystemOfChanges();
}
