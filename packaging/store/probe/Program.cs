using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32;
using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Core.Types;

if (args.Length != 2 || !args[0].StartsWith("WINENVEDIT_MSIX_SMOKE_", StringComparison.Ordinal)) {
  return 2;
}
var name = args[0];
var output = args[1];
try {
  var isAdministrator = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
  if (isAdministrator) {
    throw new InvalidOperationException("Probe must run without administrator privileges.");
  }
  uint size = 0;
  _ = PackageIdentity.GetCurrentPackageFullName(ref size, null);
  var identity = new char[size];
  var result = PackageIdentity.GetCurrentPackageFullName(ref size, identity);
  if (result != 0 || !new string(identity).StartsWith("Test.WinEnvEdit.RuntimeProbe_", StringComparison.Ordinal)) {
    throw new InvalidOperationException("Probe must have package identity.");
  }
  await new EnvironmentService().SaveVariables([new EnvironmentVariableModel {
    Name = name,
    Data = "signed-package-value",
    Scope = VariableScope.User,
    Type = RegistryValueKind.String,
  }]);
  File.WriteAllText(output, JsonSerializer.Serialize(new {
    success = true, isAdministrator, packageIdentity = new string(identity).TrimEnd('\0'), name,
  }));
  return 0;
} catch (Exception ex) {
  File.WriteAllText(output, JsonSerializer.Serialize(new { success = false, error = ex.Message, name }));
  return 1;
}
internal static class PackageIdentity {
  [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
  internal static extern int GetCurrentPackageFullName(ref uint size, [Out] char[]? packageFullName);
}
