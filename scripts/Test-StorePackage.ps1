[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackagePath)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
try {
  $entry = $archive.GetEntry('AppxManifest.xml')
  if (-not $entry) { throw 'Missing package manifest.' }
  $reader = [IO.StreamReader]::new($entry.Open())
  try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
  $ns = [Xml.XmlNamespaceManager]::new($manifest.NameTable)
  $ns.AddNamespace('m', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
  $families = @($manifest.SelectNodes('/m:Package/m:Dependencies/m:TargetDeviceFamily', $ns))
  if ($families.Count -ne 1 -or $families[0].GetAttribute('Name') -ne 'Windows.Desktop') {
    throw 'WinEnvEdit must target only Windows.Desktop; Universal, Xbox and other families are unsupported.'
  }
  if ([version]$families[0].GetAttribute('MinVersion') -lt [version]'10.0.22000.0') {
    throw 'WinEnvEdit requires Windows 11 or newer.'
  }
  $identity = $manifest.SelectSingleNode('/m:Package/m:Identity', $ns)
  $architecture = $identity.GetAttribute('ProcessorArchitecture')
  if ($architecture -notin @('x64', 'arm64')) { throw 'Only x64 and ARM64 packages are supported.' }
  $applications = @($manifest.SelectNodes('/m:Package/m:Applications/m:Application', $ns))
  if ($applications.Count -ne 1 -or $applications[0].GetAttribute('Executable') -ne 'WinEnvEdit.exe') {
    throw 'The production package must launch WinEnvEdit.exe.'
  }
  $resources = $archive.GetEntry('resources.pri')
  if (-not $resources -or $resources.Length -eq 0) { throw 'Packaged WinUI activation requires resources.pri.' }
  $exe = $archive.GetEntry('WinEnvEdit.exe')
  if (-not $exe) { throw 'Missing app executable.' }
  $buffer = [IO.MemoryStream]::new()
  $inputStream = $exe.Open()
  try { $inputStream.CopyTo($buffer) } finally { $inputStream.Dispose() }
  $binaryReader = [IO.BinaryReader]::new($buffer)
  try {
    $buffer.Position = 0x3c
    $offset = $binaryReader.ReadInt32()
    $buffer.Position = $offset
    if ($binaryReader.ReadUInt32() -ne 0x00004550) { throw 'Invalid executable PE signature.' }
    $machine = $binaryReader.ReadUInt16()
  } finally { $binaryReader.Dispose() }
  $expectedMachine = if ($architecture -eq 'x64') { 0x8664 } else { 0xaa64 }
  if ($machine -ne $expectedMachine) { throw 'Package/executable architecture mismatch.' }
  [pscustomobject]@{
    IdentityName = $identity.GetAttribute('Name')
    Publisher = $identity.GetAttribute('Publisher')
    Architecture = $architecture
    DeviceFamily = $families[0].GetAttribute('Name')
    MinimumWindowsVersion = $families[0].GetAttribute('MinVersion')
    ResourcesPresent = $true
    RuntimeTested = $false
  }
} finally { $archive.Dispose() }
