[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$PackagePath,
  [Parameter(Mandatory)][string]$ReportPath,
  [Parameter(Mandatory)][ValidatePattern('^WINENVEDIT_MSIX_SMOKE_[a-f0-9]{32}$')][string]$VariableName
)
$ErrorActionPreference = 'Stop'
# Start-Process -Credential creates the new user's environment, so the parent's
# GITHUB_ACTIONS variable is not inherited. The parent enforces that CI guard.
if ($env:USERNAME -notmatch '^WEEProbe_[a-f0-9]{8}$' -or (Split-Path -Parent $PackagePath) -notlike "$env:PUBLIC\WinEnvEditRuntime_*") {
  throw 'This controller requires the isolated parent-created test user and directory.'
}
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Runtime controller must be a standard user.' }
$identity = 'Test.WinEnvEdit.RuntimeProbe'
try {
  Add-AppxPackage -Path $PackagePath
  $installed = Get-AppxPackage -Name $identity
  if (-not $installed -or $installed.InstallLocation -notlike '*\WindowsApps\*') { throw 'Probe was not deployed to WindowsApps.' }
  $probeExe = Join-Path $installed.InstallLocation 'RuntimeProbe.exe'
  $probe = Start-Process -FilePath $probeExe -ArgumentList "$VariableName `"$ReportPath`"" -WindowStyle Hidden -PassThru
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
