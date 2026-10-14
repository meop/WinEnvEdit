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
$testUser = $null
$child = $null
$standardWork = $null
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
  $username = 'WEEProbe_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
  $password = ConvertTo-SecureString ('aA!9' + [Guid]::NewGuid().ToString('N')) -AsPlainText -Force
  $testUser = New-LocalUser -Name $username -Password $password -AccountNeverExpires -PasswordNeverExpires
  Add-LocalGroupMember -SID 'S-1-5-32-545' -Member $testUser.Name
  $credential = New-Object Management.Automation.PSCredential("$env:COMPUTERNAME\$username", $password)
  $standardWork = Join-Path $env:PUBLIC ('WinEnvEditRuntime_' + [Guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Path $standardWork | Out-Null
  $acl = Get-Acl -LiteralPath $standardWork
  $rule = New-Object Security.AccessControl.FileSystemAccessRule($testUser.SID, 'Modify', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
  $acl.AddAccessRule($rule)
  Set-Acl -LiteralPath $standardWork -AclObject $acl
  Copy-Item -LiteralPath $package -Destination (Join-Path $standardWork 'probe.msix')
  $controller = Join-Path $standardWork 'Invoke-StoreRuntimeAsUser.ps1'
  Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Invoke-StoreRuntimeAsUser.ps1') -Destination $controller
  $standardReport = Join-Path $standardWork 'report.json'
  $arguments = "-NoProfile -File `"$controller`" -PackagePath `"$standardWork\probe.msix`" -ReportPath `"$standardReport`" -VariableName $name"
  $child = Start-Process -FilePath "$env:WINDIR/System32/WindowsPowerShell/v1.0/powershell.exe" -ArgumentList $arguments -Credential $credential -LoadUserProfile -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'controller-output.txt') -RedirectStandardError (Join-Path $work 'controller-error.txt')
  if (-not $child.WaitForExit(90000)) { throw 'Standard-user controller timed out.' }
  if (Test-Path -LiteralPath $standardReport) { Copy-Item -LiteralPath $standardReport -Destination $report -Force }
  if ($child.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) {
    Get-Content -LiteralPath (Join-Path $work 'controller-error.txt')
    throw 'Standard-user runtime controller failed.'
  }
  $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
  if (-not $result.success -or -not $result.standardUserController -or -not $result.externalWriteAndPersistenceVerified -or $result.isAdministrator) { throw "Standard-user probe failed: $($result.error)" }
  'Signed runFullTrust-only standard-user environment test passed.'
} finally {
  if ($child -and -not $child.HasExited) { $child.Kill(); $child.WaitForExit() }
  if ($standardWork -and (Test-Path -LiteralPath (Join-Path $standardWork 'report.json'))) { Copy-Item -LiteralPath (Join-Path $standardWork 'report.json') -Destination $report -Force }
  if ($testUser) {
    Get-AppxPackage -User $testUser.SID.Value -Name $identity | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue
    Remove-LocalUser -SID $testUser.SID
  }
  if ($cert) {
    $trusted = "Cert:\LocalMachine\TrustedPeople\$($cert.Thumbprint)"
    if (Test-Path -LiteralPath $trusted) { Remove-Item -LiteralPath $trusted }
    Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($cert.Thumbprint)" -DeleteKey
  }
}
