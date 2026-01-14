namespace WinEnvEdit.Services;

/// <summary>
/// Service for checking and requesting administrator privileges.
/// </summary>
public interface IAdminService {
  /// <summary>
  /// Checks if the current process is running with administrator privileges.
  /// </summary>
  bool IsAdministrator();

  /// <summary>
  /// Checks if the application can be restarted with elevated privileges.
  /// </summary>
  bool CanRestartAsAdministrator();

  /// <summary>
  /// Restarts the application with elevated privileges (triggers UAC prompt).
  /// </summary>
  void RestartAsAdministrator();
}
