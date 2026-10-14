[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'This signed-install diagnostic is limited to an isolated GitHub Actions runner.' }
if ($PSEdition -eq 'Core') {
  & "$env:WINDIR/System32/WindowsPowerShell/v1.0/powershell.exe" -NoProfile -File $PSCommandPath
  if ($LASTEXITCODE -ne 0) { throw 'Signed runtime validation failed.' }
  return
}
$repoRoot = Split-Path -Parent $PSScriptRoot
$work = Join-Path $repoRoot 'out/store-runtime'
$inputDir = Join-Path $work 'input'
$name = 'WINENVEDIT_MSIX_SMOKE_' + [Guid]::NewGuid().ToString('N')
$report = Join-Path $work 'report.json'
$identity = 'Test.WinEnvEdit.RuntimeProbe'
$cert = $null
New-Item -ItemType Directory -Force -Path $work | Out-Null
$sdkBin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
$sdk = Get-ChildItem $sdkBin -Filter MakeAppx.exe -Recurse |
  Where-Object FullName -Match '[\\/]x64[\\/]makeappx.exe$' |
  Sort-Object { [version]$_.Directory.Parent.Name } -Descending | Select-Object -First 1
if (-not $sdk) { throw 'Windows SDK packaging tools missing.' }
try {
  dotnet publish (Join-Path $repoRoot 'packaging/store/probe/RuntimeProbe.csproj') -c Release -r win-x64 --self-contained true -o $inputDir --verbosity quiet
  if ($LASTEXITCODE -ne 0) { throw 'Probe build failed.' }
  $assets = Get-ChildItem (Join-Path $repoRoot 'out/store') -Directory -Filter stage-* | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if (-not $assets) { throw 'Build real Store candidates before this diagnostic.' }
  Copy-Item -LiteralPath (Join-Path $assets.FullName 'StoreAssets') -Destination $inputDir -Recurse
  @'
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" IgnorableNamespaces="uap rescap">
  <Identity Name="Test.WinEnvEdit.RuntimeProbe" Publisher="CN=WinEnvEditRuntimeTest" Version="1.0.0.0" ProcessorArchitecture="x64" />
  <Properties><DisplayName>WinEnvEdit runtime diagnostic</DisplayName><PublisherDisplayName>meop</PublisherDisplayName><Logo>StoreAssets/StoreLogo.png</Logo></Properties>
  <Dependencies><TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.22000.0" MaxVersionTested="10.0.26100.0" /></Dependencies>
  <Resources><Resource Language="en-US" /></Resources>
  <Applications><Application Id="Probe" Executable="RuntimeProbe.exe" EntryPoint="Windows.FullTrustApplication"><uap:VisualElements DisplayName="WinEnvEdit runtime diagnostic" Description="Temporary runtime check" BackgroundColor="transparent" Square150x150Logo="StoreAssets/Square150x150Logo.png" Square44x44Logo="StoreAssets/Square44x44Logo.png" /></Application></Applications>
  <Capabilities><rescap:Capability Name="runFullTrust" /></Capabilities>
</Package>
'@ | Set-Content (Join-Path $inputDir 'AppxManifest.xml') -Encoding UTF8
  $package = Join-Path $work 'probe.msix'
  & $sdk.FullName pack /d $inputDir /p $package /o | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'Probe package failed.' }
  $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=WinEnvEditRuntimeTest' -CertStoreLocation Cert:\CurrentUser\My -FriendlyName 'Ephemeral WinEnvEdit CI diagnostic'
  $publicCert = Join-Path $work 'probe.cer'
  Export-Certificate -Cert $cert -FilePath $publicCert | Out-Null
  Import-Certificate -FilePath $publicCert -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
  & (Join-Path $sdk.Directory.FullName 'signtool.exe') sign /fd SHA256 /sha1 $cert.Thumbprint /s My $package | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'Probe signature failed.' }
  Add-AppxPackage -Path $package
  $installed = Get-AppxPackage -Name $identity
  if (-not $installed -or $installed.InstallLocation -notlike '*\WindowsApps\*') { throw 'Probe was not deployed to WindowsApps.' }
  Add-Type -Path (Join-Path $repoRoot 'packaging/store/UnelevatedProcess.cs')
  $probeExe = Join-Path $installed.InstallLocation 'RuntimeProbe.exe'
  $commandLine = "`"$probeExe`" `"$name`" `"$report`""
  [UnelevatedProcess]::Run($probeExe, $commandLine, $work)
  $deadline = (Get-Date).AddSeconds(30)
  while (-not (Test-Path -LiteralPath $report)) {
    if ((Get-Date) -ge $deadline) { throw 'Probe activation timed out.' }
    Start-Sleep -Milliseconds 200
  }
  $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
  if (-not $result.success -or $result.isAdministrator) { throw "Normal-user probe failed: $($result.error)" }
  if ((Get-Item 'HKCU:\Environment').GetValue($name) -ne 'signed-package-value') { throw 'User-environment write was virtualized.' }
  Remove-AppxPackage -Package $installed.PackageFullName
  if ((Get-Item 'HKCU:\Environment').GetValue($name) -ne 'signed-package-value') { throw 'User-environment write did not persist after package removal.' }
  $result | Add-Member -NotePropertyName deployedToWindowsApps -NotePropertyValue $true
  $result | Add-Member -NotePropertyName externalWriteAndPersistenceVerified -NotePropertyValue $true
  $result | ConvertTo-Json | Set-Content -LiteralPath $report
  'Signed runFullTrust-only normal-user environment test passed.'
} finally {
  Get-AppxPackage -Name $identity | Remove-AppxPackage
  Remove-ItemProperty 'HKCU:\Environment' -Name $name -ErrorAction SilentlyContinue
  if ($cert) {
    $trusted = "Cert:\LocalMachine\TrustedPeople\$($cert.Thumbprint)"
    if (Test-Path -LiteralPath $trusted) { Remove-Item -LiteralPath $trusted }
    Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($cert.Thumbprint)" -DeleteKey
  }
}
