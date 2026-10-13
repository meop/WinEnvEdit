[CmdletBinding()]
param(
  [string]$Version,
  [string]$AssetsDirectory,
  [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$config = Get-Content (Join-Path $repoRoot 'packaging/product.json') -Raw | ConvertFrom-Json
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'out/distribution' }
if (-not $AssetsDirectory) { $AssetsDirectory = Join-Path $OutputDirectory 'downloads' }
New-Item -ItemType Directory -Force -Path $OutputDirectory, $AssetsDirectory | Out-Null

$endpoint = if ($Version) { "repos/$($config.repository)/releases/tags/v$Version" } else { "repos/$($config.repository)/releases/latest" }
$releaseJson = & gh api $endpoint
if ($LASTEXITCODE -ne 0) { throw "Could not read published release: $endpoint" }
$release = $releaseJson | ConvertFrom-Json
$Version = $release.tag_name -replace '^v', ''
if ($release.draft -or $release.prerelease -or $Version -notmatch '^\d+\.\d+\.\d+$') {
  throw 'Distribution packages require a published stable three-part version.'
}

function Get-Asset {
  param([string]$Pattern, [string]$Architecture)
  $name = $Pattern.Replace('{version}', $Version).Replace('{arch}', $Architecture)
  $matches = @($release.assets | Where-Object name -CEQ $name)
  if ($matches.Count -ne 1) { throw "Expected exactly one release asset: $name" }
  $asset = $matches[0]
  $path = Join-Path $AssetsDirectory $name
  if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
    & gh release download $release.tag_name --repo $config.repository --pattern $name --dir $AssetsDirectory
    if ($LASTEXITCODE -ne 0) { throw "Could not download $name" }
  }
  if ((Get-Item -LiteralPath $path).Length -ne $asset.size) { throw "Asset size mismatch: $name" }
  $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
  if ($asset.PSObject.Properties['digest'] -and $asset.digest -and $asset.digest -ne "sha256:$hash") {
    throw "GitHub asset digest mismatch: $name"
  }
  [pscustomobject]@{ Name = $name; Path = $path; Url = $asset.browser_download_url; Hash = $hash }
}

$portable = @{}
$installers = @{}
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($arch in @('x64', 'arm64')) {
  $portable[$arch] = Get-Asset $config.portableAsset $arch
  $archive = [IO.Compression.ZipFile]::OpenRead($portable[$arch].Path)
  try {
    if (-not $archive.GetEntry($config.executable)) { throw "Missing root executable: $($config.executable)" }
  } finally { $archive.Dispose() }
  if ($config.chocolateyMode -eq 'msi') { $installers[$arch] = Get-Asset $config.installerAsset $arch }
}

$baseUrl = 'https://github.com/' + $config.repository + '/releases/download/v$version'
$scoop = [ordered]@{
  version = $Version
  description = $config.description
  homepage = "https://github.com/$($config.repository)"
  license = 'MIT'
  architecture = [ordered]@{
    '64bit' = [ordered]@{ url = $portable.x64.Url; hash = $portable.x64.Hash }
    arm64 = [ordered]@{ url = $portable.arm64.Url; hash = $portable.arm64.Hash }
  }
  shortcuts = ,@($config.executable, $config.product)
  checkver = @{ github = "https://github.com/$($config.repository)" }
  autoupdate = @{ architecture = [ordered]@{
    '64bit' = @{ url = "$baseUrl/$($config.portableAsset.Replace('{version}', '$version').Replace('{arch}', 'x64'))" }
    arm64 = @{ url = "$baseUrl/$($config.portableAsset.Replace('{version}', '$version').Replace('{arch}', 'arm64'))" }
  } }
}
if ($config.minimumWindowsVersion) { $scoop.notes = "Requires Windows $($config.minimumWindowsVersion) or later." }
if ($config.webview2) { $scoop.notes = 'Requires the Microsoft Edge WebView2 Runtime (normally present on Windows 11).' }
$scoopDir = Join-Path $OutputDirectory 'scoop'
New-Item -ItemType Directory -Force -Path $scoopDir | Out-Null
$scoop | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $scoopDir "$($config.id).json") -Encoding utf8NoBOM

$chocoDir = Join-Path $OutputDirectory "chocolatey/$($config.id)"
$toolsDir = Join-Path $chocoDir 'tools'
New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
$dependencies = if ($config.webview2) { '<dependencies><dependency id="webview2-runtime" /></dependencies>' } else { '' }
$description = [Security.SecurityElement]::Escape($config.description)
$nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
  <metadata>
    <id>$($config.id)</id>
    <version>$Version</version>
    <title>$($config.product)</title>
    <authors>Marshall Porter</authors>
    <owners>meop</owners>
    <projectUrl>https://github.com/$($config.repository)</projectUrl>
    <packageSourceUrl>https://github.com/$($config.repository)/tree/HEAD/packaging</packageSourceUrl>
    <licenseUrl>https://github.com/$($config.repository)/blob/HEAD/LICENSE</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>$description</description>
    <summary>$description</summary>
    <tags>$($config.id) desktop windows</tags>
    <releaseNotes>https://github.com/$($config.repository)/releases/tag/v$Version</releaseNotes>
    $dependencies
  </metadata>
  <files><file src="tools/**" target="tools" /></files>
</package>
"@
$nuspec | Set-Content (Join-Path $chocoDir "$($config.id).nuspec") -Encoding utf8NoBOM

$assets = if ($config.chocolateyMode -eq 'msi') { $installers } else { $portable }
$installScript = @'
$ErrorActionPreference = 'Stop'
$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$architecture = [Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITECTURE', 'Machine')
switch ($architecture) {
  'AMD64' { $url = '@X64_URL@'; $checksum = '@X64_HASH@' }
  'ARM64' { $url = '@ARM64_URL@'; $checksum = '@ARM64_HASH@' }
  default { throw "Unsupported Windows architecture: $architecture" }
}
'@
$installScript = $installScript.Replace('@X64_URL@', $assets.x64.Url).Replace('@X64_HASH@', $assets.x64.Hash).Replace('@ARM64_URL@', $assets.arm64.Url).Replace('@ARM64_HASH@', $assets.arm64.Hash)
if ($config.chocolateyMode -eq 'msi') {
  $installScript += @'

if ([Environment]::OSVersion.Version.Build -lt 22000) { throw 'WinEnvEdit requires Windows 11 or later.' }
Install-ChocolateyPackage -PackageName $env:ChocolateyPackageName -FileType 'msi' -SilentArgs '/qn /norestart' -Url $url -Checksum $checksum -ChecksumType 'sha256' -ValidExitCodes @(0, 1641, 3010)
'@
} else {
  $installScript += @'

$appDir = Join-Path $toolsDir 'app'
Install-ChocolateyZipPackage -PackageName $env:ChocolateyPackageName -Url $url -UnzipLocation $appDir -Checksum $checksum -ChecksumType 'sha256'
# Mark the application shim as graphical; package removal removes this portable installation.
New-Item -ItemType File -Path (Join-Path $appDir '@EXE@.gui') -Force | Out-Null
'@
  $installScript = $installScript.Replace('@EXE@', $config.executable)
}
$installScript | Set-Content (Join-Path $toolsDir 'chocolateyinstall.ps1') -Encoding utf8NoBOM
@"
Generated from published release $($release.html_url).
Scoop ZIPs verified for x64 and ARM64 root executables.
SHA256 hashes calculated from downloaded release assets; GitHub digests checked when available.
Native install/uninstall and GUI smoke tests are required before initial public submission.
"@ | Set-Content (Join-Path $OutputDirectory 'VALIDATION.txt') -Encoding utf8NoBOM
Write-Output "Prepared $($config.product) $Version distribution packages in $OutputDirectory"
