[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$ApplicationId,
  [Parameter(Mandatory)][string]$VariableName,
  [Parameter(Mandatory)][string]$ReportPath
)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'This activation helper is limited to an isolated GitHub Actions runner.' }
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Activation controller must run without administrator privileges.' }
Add-Type -TypeDefinition @'
using System; using System.Runtime.InteropServices;
[ComImport,Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")] class ActivationManager {}
[ComImport,Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IActivationManager { [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)]string app,[MarshalAs(UnmanagedType.LPWStr)]string args,uint options,out uint pid); }
public static class RuntimeActivation { public static void Launch(string app,string args) { uint pid; var hr=((IActivationManager)new ActivationManager()).ActivateApplication(app,args,0,out pid); Marshal.ThrowExceptionForHR(hr); } }
'@
[RuntimeActivation]::Launch($ApplicationId, "$VariableName `"$ReportPath`"")
