[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$PackagePath,
  [Parameter(Mandatory)][string]$ReportPath,
  [Parameter(Mandatory)][ValidatePattern('^WINENVEDIT_MSIX_SMOKE_[a-f0-9]{32}$')][string]$VariableName
)
$ErrorActionPreference = 'Stop'
# The parent enforces the CI guard; scheduled tasks do not inherit job variables.
if ((Split-Path -Parent $PackagePath) -notlike "$env:PUBLIC\WinEnvEditRuntime_*") { throw 'This controller requires the isolated parent-created directory.' }
$identity = 'Test.WinEnvEdit.RuntimeProbe'
try {
  $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
  if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Runtime controller must run without administrator privileges.' }
  Add-AppxPackage -Path $PackagePath
  $installed = Get-AppxPackage -Name $identity
  if (-not $installed -or $installed.InstallLocation -notlike '*\WindowsApps\*') { throw 'Probe was not deployed to WindowsApps.' }
  Add-Type -TypeDefinition @'
using System; using System.Runtime.InteropServices;
[ComImport,Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")] class ActivationManager {}
[ComImport,Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IActivationManager { [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)]string app,[MarshalAs(UnmanagedType.LPWStr)]string args,uint options,out uint pid); }
public static class RuntimeActivation { public static uint Launch(string app,string args) { uint pid; var hr=((IActivationManager)new ActivationManager()).ActivateApplication(app,args,0,out pid); Marshal.ThrowExceptionForHR(hr); return pid; } }
'@
  $probeId = [RuntimeActivation]::Launch($installed.PackageFamilyName + '!Probe', "$VariableName `"$ReportPath`"")
  $probe = [Diagnostics.Process]::GetProcessById($probeId)
  if (-not $probe.WaitForExit(30000)) {
    $probe.Kill()
    throw 'Installed probe timed out.'
  }
  if (-not (Test-Path -LiteralPath $ReportPath)) { throw "Probe produced no report; exit code $($probe.ExitCode)." }
  $result = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
  if (-not $result.success -or $result.isAdministrator) { throw "Normal-user probe failed: $($result.error)" }
  if ((Get-Item 'HKCU:\Environment').GetValue($VariableName) -ne 'signed-package-value') { throw 'User-environment write was virtualized.' }
  Remove-AppxPackage -Package $installed.PackageFullName
  if ((Get-Item 'HKCU:\Environment').GetValue($VariableName) -ne 'signed-package-value') { throw 'User-environment write did not persist after package removal.' }
  $result | Add-Member -NotePropertyName deployedToWindowsApps -NotePropertyValue $true
  $result | Add-Member -NotePropertyName externalWriteAndPersistenceVerified -NotePropertyValue $true
  $result | Add-Member -NotePropertyName standardUserController -NotePropertyValue $true
  $result | Add-Member -NotePropertyName operatingSystem -NotePropertyValue ([Environment]::OSVersion.VersionString)
  $result | ConvertTo-Json | Set-Content -LiteralPath $ReportPath
  'Signed runFullTrust-only standard-user environment test passed.'
} catch {
  [pscustomobject]@{ success = $false; error = $_.Exception.Message } | ConvertTo-Json | Set-Content -LiteralPath $ReportPath
  throw
} finally {
  Get-AppxPackage -Name $identity | Remove-AppxPackage
  Remove-ItemProperty 'HKCU:\Environment' -Name $VariableName -ErrorAction SilentlyContinue
}
