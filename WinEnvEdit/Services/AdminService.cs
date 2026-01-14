using System;
using System.Diagnostics;
using System.Security.Principal;

namespace WinEnvEdit.Services;

/// <summary>
/// Service for checking and requesting administrator privileges.
/// </summary>
public class AdminService : IAdminService {
  public bool IsAdministrator() {
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
  }

  public bool CanRestartAsAdministrator() {
    return !IsAdministrator();
  }

  public void RestartAsAdministrator() {
    if (!CanRestartAsAdministrator()) {
      return;
    }

    var currentProcess = Process.GetCurrentProcess();
    var executablePath = currentProcess.MainModule?.FileName;

    if (executablePath is null) {
      Debug.WriteLine("Could not determine executable path for restart");
      return;
    }

    var startInfo = new ProcessStartInfo {
      FileName = executablePath,
      UseShellExecute = true,
      Verb = "runas" // Triggers UAC prompt
    };

    try {
      Process.Start(startInfo);
      Environment.Exit(0); // Exit current non-elevated process
    }
    catch (Exception ex) {
      Debug.WriteLine($"Failed to restart as admin: {ex.Message}");
    }
  }
}
