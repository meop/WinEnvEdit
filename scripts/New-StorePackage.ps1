[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$PublishDirectory,
  [Parameter(Mandatory)][ValidateSet('x64', 'arm64')][string]$Architecture,
  [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9.-]{3,50}$')][string]$IdentityName,
  [Parameter(Mandatory)][string]$Publisher,
  [string]$MakeAppxPath,
  [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$version = (Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Store packaging requires a stable three-part VERSION.' }
if ($Publisher -notmatch '^CN=') { throw 'Use the exact publisher distinguished name from Partner Center.' }
$publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
$exePath = Join-Path $publishPath 'WinEnvEdit.exe'
if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) { throw 'PublishDirectory must contain the complete self-contained app.' }
$reader = [IO.BinaryReader]::new([IO.File]::OpenRead($exePath))
try {
  $reader.BaseStream.Position = 0x3c
  $peOffset = $reader.ReadInt32()
  $reader.BaseStream.Position = $peOffset
  if ($reader.ReadUInt32() -ne 0x00004550) { throw 'Invalid executable PE signature.' }
  $machine = $reader.ReadUInt16()
} finally { $reader.Dispose() }
$expectedMachine = if ($Architecture -eq 'x64') { 0x8664 } else { 0xaa64 }
if ($machine -ne $expectedMachine) { throw 'Executable architecture does not match Architecture.' }
$fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).FileVersion
if ($fileVersion -ne "$version.0") { throw "Executable version $fileVersion does not match VERSION $version." }
if (-not $MakeAppxPath) {
  $command = Get-Command MakeAppx.exe -ErrorAction SilentlyContinue
  if ($command) { $MakeAppxPath = $command.Source }
}
if (-not $MakeAppxPath -or -not (Test-Path -LiteralPath $MakeAppxPath -PathType Leaf)) {
  throw 'Pass -MakeAppxPath pointing to Windows SDK MakeAppx.exe.'
}
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'out/store' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$stage = Join-Path $OutputDirectory ('stage-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
Get-ChildItem -LiteralPath $publishPath -Force | Copy-Item -Destination $stage -Recurse -Force
# MRT loads resources.pri when the executable has package identity. The portable
# build names its combined resource index WinEnvEdit.pri instead.
$resourceIndex = Join-Path $stage 'WinEnvEdit.pri'
if (-not (Test-Path -LiteralPath $resourceIndex -PathType Leaf)) { throw 'Missing WinEnvEdit.pri in publish output.' }
Copy-Item -LiteralPath $resourceIndex -Destination (Join-Path $stage 'resources.pri') -Force
$manifest = Get-Content (Join-Path $repoRoot 'packaging/store/AppxManifest.xml.template') -Raw
foreach ($entry in @{ IDENTITY = $IdentityName; PUBLISHER = $Publisher; VERSION = $version; ARCHITECTURE = $Architecture }.GetEnumerator()) {
  $manifest = $manifest.Replace("@$($entry.Key)@", [Security.SecurityElement]::Escape($entry.Value))
}
$manifest | Set-Content (Join-Path $stage 'AppxManifest.xml') -Encoding utf8NoBOM
Add-Type -AssemblyName System.Drawing
$assets = Join-Path $stage 'StoreAssets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null
$sourceImage = [Drawing.Image]::FromFile((Join-Path $repoRoot 'scripts/App.png'))
try {
  foreach ($entry in @{ StoreLogo = 50; Square44x44Logo = 44; Square150x150Logo = 150 }.GetEnumerator()) {
    $bitmap = [Drawing.Bitmap]::new($entry.Value, $entry.Value)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
      $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
      $graphics.DrawImage($sourceImage, 0, 0, $entry.Value, $entry.Value)
      $bitmap.Save((Join-Path $assets "$($entry.Key).png"), [Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
  }
} finally { $sourceImage.Dispose() }
$package = Join-Path $OutputDirectory "WinEnvEdit-$version-$Architecture.msix"
& $MakeAppxPath pack /d $stage /p $package /o
if ($LASTEXITCODE -ne 0) { throw 'MakeAppx validation/packing failed.' }
& (Join-Path $PSScriptRoot 'Test-StorePackage.ps1') -PackagePath $package
Write-Output "Created unsigned candidate: $package. Runtime testing and Store capability approval remain required."
