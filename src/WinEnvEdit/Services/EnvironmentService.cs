using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

using Microsoft.Win32;

using WinEnvEdit.Models;

namespace WinEnvEdit.Services;

/// <summary>
/// Service for reading and writing Windows environment variables from/ to registry.
/// </summary>
public partial class EnvironmentService : IEnvironmentService {
  private const string UserEnvironmentKey = @"Environment";
  private const string SystemEnvironmentKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

  public List<EnvironmentVariable> GetVariables() {
    var userVars = GetVariablesFromRegistry(RegistryHive.CurrentUser, UserEnvironmentKey, VariableScope.User);
    var systemVars = GetVariablesFromRegistry(RegistryHive.LocalMachine, SystemEnvironmentKey, VariableScope.System);
    var volatileUserVars = GetVolatileUserVariables();
    var volatileSystemVars = GetVolatileSystemVariables();

    return GetAndSortVariables(userVars.Concat(systemVars), volatileUserVars.Concat(volatileSystemVars));
  }

  public async Task SaveVariables(IEnumerable<EnvironmentVariable> variables) {
    var varsList = variables.ToList();

    if (varsList.Count == 0) {
      return;
    }

    var userVarsList = varsList.Where(v => v.Scope == VariableScope.User).ToList();
    var systemVarsList = varsList.Where(v => v.Scope == VariableScope.System).ToList();

    var scriptPath = Path.Combine(Path.GetTempPath(), $"WinEnvEdit_Save_{Guid.NewGuid()}.ps1");

    try {
      var scriptLines = new List<string> {
        "$ErrorActionPreference = 'Stop'"
      };

      if (userVarsList.Count != 0) {
        scriptLines.Add("Write-Host 'Saving User environment variables...'");
        var userRegistryPath = @"HKCU:\Environment";
        foreach (var variable in userVarsList) {
          AddVariableToScript(scriptLines, userRegistryPath, variable);
        }
      }

      if (systemVarsList.Count != 0) {
        scriptLines.Add("Write-Host 'Saving System environment variables...'");
        var systemRegistryPath = @"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
        foreach (var variable in systemVarsList) {
          AddVariableToScript(scriptLines, systemRegistryPath, variable);
        }
      }

      scriptLines.Add("Write-Host 'Environment variables saved successfully!'");

      var scriptContent = string.Join("\r\n", scriptLines);
      File.WriteAllText(scriptPath, scriptContent);

      var exitCode = systemVarsList.Count != 0
        ? await Task.Run(() => RunScriptElevated(scriptPath))
        : await Task.Run(() => RunScriptHidden(scriptPath));

      if (exitCode != 0) {
        throw new InvalidOperationException("PowerShell script execution failed");
      }
    }
    finally {
      try {
        if (File.Exists(scriptPath)) {
          File.Delete(scriptPath);
        }
      }
      catch {
      }
    }

    await Task.Run(NotifySystemOfChanges);
  }

  private static void AddVariableToScript(List<string> scriptLines, string registryPath, EnvironmentVariable variable) {
    var value = variable.Data?.Replace("'", "''") ?? string.Empty;

    if (variable.IsRemoved) {
      scriptLines.Add($"Remove-ItemProperty -Path '{registryPath}' -Name '{variable.Name}' -ErrorAction SilentlyContinue");
      scriptLines.Add($"Write-Host 'Deleted: {variable.Name}'");
      return;
    }

    // Remove first, then re-create: neither Set-ItemProperty nor New-ItemProperty -Force
    // actually changes the ValueKind of an existing registry value.  A delete + create is
    // the only way to guarantee the type persists through a toggle.
    scriptLines.Add($"Remove-ItemProperty -Path '{registryPath}' -Name '{variable.Name}' -ErrorAction SilentlyContinue");

    if (variable.Type == RegistryValueKind.MultiString) {
      var values = value.Split([';'], StringSplitOptions.RemoveEmptyEntries);
      var quotedValues = string.Join(", ", values.Select(v => $"'{v}'"));
      scriptLines.Add($"New-ItemProperty -Path '{registryPath}' -Name '{variable.Name}' -Value @({quotedValues}) -PropertyType MultiString -Force");
    }
    else if (variable.Type == RegistryValueKind.DWord) {
      scriptLines.Add($"New-ItemProperty -Path '{registryPath}' -Name '{variable.Name}' -Value {value} -PropertyType DWord -Force");
    }
    else if (variable.Type == RegistryValueKind.ExpandString) {
      scriptLines.Add($"New-ItemProperty -Path '{registryPath}' -Name '{variable.Name}' -Value '{value}' -PropertyType ExpandString -Force");
    }
    else {
      scriptLines.Add($"New-ItemProperty -Path '{registryPath}' -Name '{variable.Name}' -Value '{value}' -PropertyType String -Force");
    }

    scriptLines.Add($"Write-Host 'Saved: {variable.Name}'");
  }

  private static int RunScriptHidden(string scriptPath) {
    using var process = Process.Start(new ProcessStartInfo {
      FileName = "powershell.exe",
      Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
      UseShellExecute = false,
      CreateNoWindow = true,
    })!;

    process.WaitForExit();
    return process.ExitCode;
  }

  // Uses ShellExecuteEx rather than Process.Start(Verb="runas") because -WindowStyle Hidden
  // is a race: the shell creates the window before PowerShell processes the flag. ShellExecuteEx
  // with SW_HIDE suppresses it at the shell level. See PATTERNS.md → Elevated Script Launch.
  private static int RunScriptElevated(string scriptPath) {
    var verbPtr = Marshal.StringToHGlobalUni("runas");
    var filePtr = Marshal.StringToHGlobalUni("powershell.exe");
    var argsPtr = Marshal.StringToHGlobalUni($"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"");

    try {
      var info = new ShellExecuteInfo {
        Size = Marshal.SizeOf<ShellExecuteInfo>(),
        Flags = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_NO_UI,
        Verb = verbPtr,
        File = filePtr,
        Parameters = argsPtr,
        Show = SW_HIDE,
      };

      if (!ShellExecuteExW(ref info)) {
        throw new InvalidOperationException("Elevation was denied by the user");
      }

      if (info.Process == IntPtr.Zero) {
        throw new InvalidOperationException("Failed to get process handle for elevated script");
      }

      try {
        WaitForSingleObject(info.Process, INFINITE);
        if (!GetExitCodeProcess(info.Process, out var exitCode)) {
          throw new InvalidOperationException("Failed to get exit code for elevated script");
        }
        return (int)exitCode;
      }
      finally {
        CloseHandle(info.Process);
      }
    }
    finally {
      Marshal.FreeHGlobal(verbPtr);
      Marshal.FreeHGlobal(filePtr);
      Marshal.FreeHGlobal(argsPtr);
    }
  }

  private static List<EnvironmentVariable> GetVariablesFromRegistry(RegistryHive hive, string keyPath, VariableScope scope) {
    var variables = new List<EnvironmentVariable>();

    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
    using var envKey = baseKey.OpenSubKey(keyPath, writable: false);

    if (envKey is null) {
      return variables;
    }

    foreach (var name in envKey.GetValueNames()) {
      var data = envKey.GetValue(name, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
      var type = envKey.GetValueKind(name);
      variables.Add(CreateEnvironmentVariable(name, data?.ToString() ?? string.Empty, scope, type, isVolatile: false));
    }

    return variables;
  }

  private static List<EnvironmentVariable> GetVolatileUserVariables() {
    var variables = new List<EnvironmentVariable>();

    try {
      var currentUserSid = WindowsIdentity.GetCurrent()?.User?.Value;
      if (string.IsNullOrEmpty(currentUserSid)) {
        return variables;
      }

      using var usersKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
      using var volatileKey = usersKey?.OpenSubKey($@"{currentUserSid}\Volatile Environment", writable: false);

      if (volatileKey is null) {
        return variables;
      }

      foreach (var name in volatileKey.GetValueNames()) {
        var data = volatileKey.GetValue(name, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
        var type = volatileKey.GetValueKind(name);
        variables.Add(CreateEnvironmentVariable(name, data?.ToString() ?? string.Empty, VariableScope.User, type, isVolatile: true));
      }
    }
    catch {
    }

    return variables;
  }

  private static List<EnvironmentVariable> GetVolatileSystemVariables() {
    var variables = new List<EnvironmentVariable>();

    try {
      var userRegistryVars = GetVariablesFromRegistry(RegistryHive.CurrentUser, UserEnvironmentKey, VariableScope.User);
      var machineRegistryVars = GetVariablesFromRegistry(RegistryHive.LocalMachine, SystemEnvironmentKey, VariableScope.System);
      var volatileUserVars = GetVolatileUserVariables();

      var processVars = Environment.GetEnvironmentVariables();

      foreach (DictionaryEntry entry in processVars) {
        var name = entry.Key?.ToString();
        if (string.IsNullOrEmpty(name)) {
          continue;
        }

        var data = entry.Value?.ToString() ?? string.Empty;

        var userVarExists = userRegistryVars.Any(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
        var machineVarExists = machineRegistryVars.Any(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
        var volatileUserVarExists = volatileUserVars.Any(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));

        if (!userVarExists && !machineVarExists && !volatileUserVarExists) {
          variables.Add(CreateEnvironmentVariable(name, data, VariableScope.System, RegistryValueKind.String, isVolatile: true));
        }
      }
    }
    catch {
    }

    return variables;
  }

  private static List<EnvironmentVariable> GetAndSortVariables(IEnumerable<EnvironmentVariable> persistentVars, IEnumerable<EnvironmentVariable> volatileVars) {
    var variables = new List<EnvironmentVariable>();
    variables.AddRange(persistentVars);
    variables.AddRange(volatileVars);
    return [.. variables.OrderBy(v => v.Name)];
  }

  private static EnvironmentVariable CreateEnvironmentVariable(string name, string data, VariableScope scope, RegistryValueKind type, bool isVolatile) =>
    new() {
      Name = name,
      Data = data,
      Scope = scope,
      Type = type,
      IsVolatile = isVolatile,
      IsAdded = false,
      IsRemoved = false,
    };

  private static void NotifySystemOfChanges() {
    const int HWND_BROADCAST = 0xffff;
    const uint WM_SETTINGCHANGE = 0x001A;
    const uint SMTO_ABORTIFHUNG = 0x0002;

    SendMessageTimeoutW(
      new IntPtr(HWND_BROADCAST),
      WM_SETTINGCHANGE,
      IntPtr.Zero,
      "Environment",
      SMTO_ABORTIFHUNG,
      5000,
      out _
    );
  }

  #region Win32 Interop

  private const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
  private const uint SEE_MASK_NO_UI = 0x00000400;
  private const int SW_HIDE = 0;
  private const uint INFINITE = uint.MaxValue;

  [StructLayout(LayoutKind.Sequential)]
  private struct ShellExecuteInfo {
    public int Size;
    public uint Flags;
    public IntPtr Window;
    public IntPtr Verb;
    public IntPtr File;
    public IntPtr Parameters;
    public IntPtr Directory;
    public int Show;
    public IntPtr InstApp;
    public IntPtr IdList;
    public IntPtr Class;
    public IntPtr KeyClass;
    public uint HotKey;
    public IntPtr Icon;
    public IntPtr Process;
  }

  [LibraryImport("shell32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool ShellExecuteExW(ref ShellExecuteInfo info);

  [LibraryImport("kernel32.dll", SetLastError = true)]
  private static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

  [LibraryImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool GetExitCodeProcess(IntPtr hProcess, out uint exitCode);

  [LibraryImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool CloseHandle(IntPtr hObject);

  [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
  private static partial IntPtr SendMessageTimeoutW(
    IntPtr hWnd,
    uint Msg,
    IntPtr wParam,
    string lParam,
    uint fuFlags,
    uint uTimeout,
    out IntPtr lpdwResult
  );

  #endregion
}
