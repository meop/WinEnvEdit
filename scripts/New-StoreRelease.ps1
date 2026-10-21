[CmdletBinding()]
param(
  [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
  [string]$MakeAppxPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
if ((Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim() -ne $Version) {
  throw 'Check out the source revision matching the stable release VERSION.'
}
if (-not $MakeAppxPath) {
  $sdkBin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
  $tool = Get-ChildItem $sdkBin -Filter MakeAppx.exe -Recurse |
    Where-Object FullName -Match '[\\/]x64[\\/]makeappx.exe$' |
    Sort-Object { [version]$_.Directory.Parent.Name } -Descending |
    Select-Object -First 1
  if (-not $tool) { throw 'Windows SDK MakeAppx.exe was not found.' }
  $MakeAppxPath = $tool.FullName
}
& (Join-Path $PSScriptRoot 'New-DistributionPackages.ps1') -Version $Version
$bundleInput = Join-Path $repoRoot 'out/store-bundle-input'
$uploadDirectory = Join-Path $repoRoot 'out/store-upload'
New-Item -ItemType Directory -Force -Path $bundleInput, $uploadDirectory | Out-Null
if (@(Get-ChildItem $bundleInput -Force).Count -or @(Get-ChildItem $uploadDirectory -Force).Count) {
  throw 'Store bundle input and upload directories must be empty; use a fresh workspace.'
}
foreach ($architecture in @('x64', 'arm64')) {
  $publish = Join-Path $repoRoot "out/store-input/$architecture"
  if (Test-Path -LiteralPath $publish) { throw "Store input already exists: $publish" }
  Expand-Archive (Join-Path $repoRoot "out/distribution/downloads/WinEnvEdit-$architecture.zip") $publish
  & (Join-Path $PSScriptRoot 'New-StorePackage.ps1') -PublishDirectory $publish -Architecture $architecture `
    -IdentityName 'meop.WinEnvEdit' -Publisher 'CN=4BA5B680-8485-42FF-9312-B49D5A71292B' `
    -MakeAppxPath $MakeAppxPath
  Copy-Item (Join-Path $repoRoot "out/store/WinEnvEdit-$Version-$architecture.msix") $bundleInput
}
$bundlePath = Join-Path $uploadDirectory "WinEnvEdit-$Version.msixbundle"
& $MakeAppxPath bundle /d $bundleInput /p $bundlePath /bv "$Version.0" /o
if ($LASTEXITCODE -ne 0) { throw 'MakeAppx bundle failed.' }
Write-Output "Unsigned Store upload bundle: $bundlePath"
