using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.Win32;

using WinEnvEdit.Models;

namespace WinEnvEdit.Services;

/// <summary>
/// Service for reading and writing Windows environment variables from the registry.
/// </summary>
public class EnvironmentService : IEnvironmentService {
  private const string UserEnvironmentKey = @"Environment";
  private const string SystemEnvironmentKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

  public List<EnvironmentVariable> GetUserVariables() {
    return GetVariablesFromRegistry(RegistryHive.CurrentUser, UserEnvironmentKey, VariableScope.User);
  }

  public List<EnvironmentVariable> GetSystemVariables() {
    return GetVariablesFromRegistry(RegistryHive.LocalMachine, SystemEnvironmentKey, VariableScope.System);
  }

  public void SaveVariable(EnvironmentVariable variable) {
    var hive = variable.Scope == VariableScope.User ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;
    var keyPath = variable.Scope == VariableScope.User ? UserEnvironmentKey : SystemEnvironmentKey;

    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
    using var envKey = baseKey.OpenSubKey(keyPath, writable: true);

    if (envKey is null) {
      throw new InvalidOperationException($"Unable to open registry key: {keyPath}");
    }

    // If name changed, delete old variable
    if (variable.OriginalName != variable.Name && !string.IsNullOrEmpty(variable.OriginalName)) {
      envKey.DeleteValue(variable.OriginalName, throwOnMissingValue: false);
    }

    // Set the new/updated variable
    envKey.SetValue(variable.Name, variable.Value, variable.Kind);

    NotifySystemOfChanges();
  }

  public void DeleteVariable(EnvironmentVariable variable) {
    var hive = variable.Scope == VariableScope.User ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;
    var keyPath = variable.Scope == VariableScope.User ? UserEnvironmentKey : SystemEnvironmentKey;

    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
    using var envKey = baseKey.OpenSubKey(keyPath, writable: true);

    if (envKey is null) {
      throw new InvalidOperationException($"Unable to open registry key: {keyPath}");
    }

    envKey.DeleteValue(variable.Name, throwOnMissingValue: false);

    NotifySystemOfChanges();
  }

  public void NotifySystemOfChanges() {
    // Broadcast WM_SETTINGCHANGE to notify all applications
    const int HWND_BROADCAST = 0xffff;
    const int WM_SETTINGCHANGE = 0x001A;

    SendMessageTimeout(
      new IntPtr(HWND_BROADCAST),
      WM_SETTINGCHANGE,
      IntPtr.Zero,
      "Environment",
      SMTO_ABORTIFHUNG,
      5000,
      out _
    );
  }

  private List<EnvironmentVariable> GetVariablesFromRegistry(RegistryHive hive, string keyPath, VariableScope scope) {
    var variables = new List<EnvironmentVariable>();

    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
    using var envKey = baseKey.OpenSubKey(keyPath, writable: false);

    if (envKey is null) {
      return variables;
    }

    foreach (var name in envKey.GetValueNames()) {
      var value = envKey.GetValue(name, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
      var kind = envKey.GetValueKind(name);

      var variable = new EnvironmentVariable {
        Name = name,
        Value = value?.ToString() ?? string.Empty,
        OriginalName = name,
        OriginalValue = value?.ToString() ?? string.Empty,
        Scope = scope,
        Kind = kind,
        IsVolatile = false,
        IsNew = false,
        IsDeleted = false
      };

      variables.Add(variable);
    }

    return variables;
  }

  #region Win32 Interop

  [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern IntPtr SendMessageTimeout(
    IntPtr hWnd,
    int Msg,
    IntPtr wParam,
    string lParam,
    int fuFlags,
    int uTimeout,
    out IntPtr lpdwResult
  );

  private const int SMTO_ABORTIFHUNG = 0x0002;

  #endregion
}
