using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Types;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for reading and writing Windows environment variables from/ to registry.
/// </summary>
public partial class EnvironmentService : IEnvironmentService {
  private const string UserEnvironmentKey = @"Environment";
  private const string SystemEnvironmentKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

  public List<EnvironmentVariableModel> GetVariables() {
    var userVars = GetVariablesFromRegistry(RegistryHive.CurrentUser, UserEnvironmentKey, VariableScope.User);
    var systemVars = GetVariablesFromRegistry(RegistryHive.LocalMachine, SystemEnvironmentKey, VariableScope.System);
    var volatileUserVars = GetVolatileUserVariables();
    var volatileSystemVars = GetVolatileSystemVariables();

    var persistent = new List<EnvironmentVariableModel>();
    persistent.AddRange(userVars);
    persistent.AddRange(systemVars);

    var volatileVars = new List<EnvironmentVariableModel>();
    volatileVars.AddRange(volatileUserVars);
    volatileVars.AddRange(volatileSystemVars);

    return GetAndSortVariables(persistent, volatileVars);
  }

  public async Task SaveVariables(IEnumerable<EnvironmentVariableModel> variables) {
    var varsList = variables.ToList();

    if (varsList.Count == 0) {
      return;
    }

    var userVarsList = varsList.Where(v => v.Scope == VariableScope.User).ToList();
    var systemVarsList = varsList.Where(v => v.Scope == VariableScope.System).ToList();

    // HKCU is writable without elevation; HKLM requires admin.
    if (userVarsList.Count != 0) {
      await Task.Run(() => WriteUserVariables(userVarsList));
    }

    if (systemVarsList.Count != 0) {
      await Task.Run(() => SaveSystemVariablesElevated(systemVarsList));
    }

    // Fire-and-forget: other apps reloading shouldn't block the save completing.
    _ = Task.Run(NotifySystemOfChanges);
  }

  private static void WriteUserVariables(List<EnvironmentVariableModel> variables) {
    using var key = Registry.CurrentUser.OpenSubKey(UserEnvironmentKey, writable: true)
      ?? Registry.CurrentUser.CreateSubKey(UserEnvironmentKey);

    foreach (var variable in variables) {
      ApplyVariableToKey(key, variable);
    }
  }

  private static void ApplyVariableToKey(RegistryKey key, EnvironmentVariableModel variable) {
    // Delete first so a ValueKind change (String <-> ExpandString) takes effect.
    key.DeleteValue(variable.Name, throwOnMissingValue: false);

    if (variable.IsRemoved) {
      return;
    }

    var data = variable.Data ?? string.Empty;
    object value = variable.Type switch {
      RegistryValueKind.MultiString => data.Split(';', StringSplitOptions.RemoveEmptyEntries),
      RegistryValueKind.DWord => int.TryParse(data, out var dword) ? dword : 0,
      _ => data,
    };

    key.SetValue(variable.Name, value, variable.Type);
  }

  // Elevation argument: the app relaunches itself elevated with this verb to write HKLM (see Program.Main).
  public const string ElevatedApplyArg = "--apply-system";

  private static void SaveSystemVariablesElevated(List<EnvironmentVariableModel> systemVars) {
    var changesPath = Path.Combine(Path.GetTempPath(), $"WinEnvEdit_Sys_{Guid.NewGuid()}.dat");

    try {
      File.WriteAllText(changesPath, SerializeChanges(systemVars));

      var exePath = Environment.ProcessPath
        ?? throw new InvalidOperationException("Could not resolve the application path for elevation");

      if (RunElevated(exePath, $"{ElevatedApplyArg} \"{changesPath}\"") != 0) {
        throw new InvalidOperationException("Elevated registry update failed");
      }
    }
    finally {
      try {
        if (File.Exists(changesPath)) {
          File.Delete(changesPath);
        }
      }
      catch {
      }
    }
  }

  /// <summary>
  /// Applies serialized system-variable changes by writing HKLM directly. Invoked by the elevated relaunch.
  /// Returns 0 on success, non-zero on failure.
  /// </summary>
  public static int ApplySystemVariablesFromFile(string path) {
    try {
      var changes = DeserializeChanges(File.ReadAllText(path));
      using var key = Registry.LocalMachine.OpenSubKey(SystemEnvironmentKey, writable: true)
        ?? throw new InvalidOperationException("Could not open the system environment key");

      foreach (var variable in changes) {
        ApplyVariableToKey(key, variable);
      }

      return 0;
    }
    catch {
      return 1;
    }
  }

  internal static string SerializeChanges(IEnumerable<EnvironmentVariableModel> variables) {
    static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    return string.Join("\n", variables.Select(v =>
      $"{(v.IsRemoved ? "D" : "S")}\t{Encode(v.Name)}\t{(int)v.Type}\t{Encode(v.Data ?? string.Empty)}"));
  }

  internal static List<EnvironmentVariableModel> DeserializeChanges(string content) {
    static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
    var result = new List<EnvironmentVariableModel>();

    foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
      var parts = line.Split('\t');
      if (parts.Length != 4) {
        continue;
      }

      result.Add(new EnvironmentVariableModel {
        Scope = VariableScope.System,
        IsRemoved = parts[0] == "D",
        Name = Decode(parts[1]),
        Type = (RegistryValueKind)int.Parse(parts[2]),
        Data = Decode(parts[3]),
      });
    }

    return result;
  }

  private static int RunElevated(string fileName, string arguments) {
    var verbPtr = Marshal.StringToHGlobalUni("runas");
    var filePtr = Marshal.StringToHGlobalUni(fileName);
    var argsPtr = Marshal.StringToHGlobalUni(arguments);

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

  private static List<EnvironmentVariableModel> GetVariablesFromRegistry(RegistryHive hive, string keyPath, VariableScope scope) {
    var variables = new List<EnvironmentVariableModel>();

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

  private static List<EnvironmentVariableModel> GetVolatileUserVariables() {
    var variables = new List<EnvironmentVariableModel>();

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

  private static List<EnvironmentVariableModel> GetVolatileSystemVariables() {
    var variables = new List<EnvironmentVariableModel>();

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

  internal static List<EnvironmentVariableModel> GetAndSortVariables(IEnumerable<EnvironmentVariableModel> persistentVars, IEnumerable<EnvironmentVariableModel> volatileVars) {
    var variables = new List<EnvironmentVariableModel>();
    variables.AddRange(persistentVars);
    variables.AddRange(volatileVars);
    return [.. variables.OrderBy(v => v.Name)];
  }

  internal static EnvironmentVariableModel CreateEnvironmentVariable(string name, string data, VariableScope scope, RegistryValueKind type, bool isVolatile) =>
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
      1000,
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
