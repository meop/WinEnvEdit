#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Packages WinEnvEdit into MSI installers using WiX v6.

.DESCRIPTION
  1. Restores dotnet tools.
  2. Synchronizes version from VERSION file to App.manifest and Package.appxmanifest.
  3. Publishes binaries for specified platform.
  4. Builds MSI installer via .wixproj.

.PARAMETER Platform
  The platform to build for (x64 or ARM64).
#>

param (
  [Parameter(Mandatory = $true)]
  [ValidateSet('ARM64', 'x64')]
  [string]$Platform
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootDir = (Get-Item $scriptDir).Parent.Parent.FullName
$srcDir = Join-Path $rootDir 'src'
$projectFile = Join-Path $srcDir 'WinEnvEdit\WinEnvEdit.csproj'
$wixProj = Join-Path $srcDir 'WinEnvEdit.Installer\WinEnvEdit.Installer.wixproj'
$versionFile = Join-Path $rootDir 'VERSION'

Write-Host "--- Packaging WinEnvEdit for $Platform ---" -ForegroundColor Cyan

# 1. Restore tools
Write-Host 'Restoring dotnet tools...' -ForegroundColor Yellow
dotnet tool restore

# 2. Get Version
$version = (Get-Content $versionFile).Trim()
$winVersion = if ($version.Split('.').Count -eq 3) { "$version.0" } else { $version }
Write-Host "Version: $version ($winVersion)" -ForegroundColor Yellow

# 3. Sync Manifests
Write-Host 'Syncing manifest versions...' -ForegroundColor Yellow
$appManifest = Join-Path $srcDir 'WinEnvEdit\App.manifest'
$pkgManifest = Join-Path $srcDir 'WinEnvEdit\Package.appxmanifest'

# Helper to update XML version and ensure exactly one trailing newline
function Update-ManifestVersion($path, $regex, $value) {
  $content = Get-Content $path -Raw
  $content = $content -replace $regex, "`${1}$value`""
  # Trim all trailing whitespace and add exactly one newline
  $content = $content.TrimEnd() + "`r`n"
  [System.IO.File]::WriteAllText($path, $content)
}

Update-ManifestVersion -path $appManifest -regex '(<assemblyIdentity[^>]+version=")[0-9.]*"' -value $winVersion
Update-ManifestVersion -path $pkgManifest -regex '(<Identity[^>]+Version=")[0-9.]*"' -value $winVersion

# 4. Publish binaries
Write-Host "Publishing binaries for $Platform..." -ForegroundColor Yellow
$rid = if ($Platform -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }
dotnet publish $projectFile -c Release -p:Platform=$Platform -r $rid

# 5. Build MSI
Write-Host 'Building MSI installer via .wixproj...' -ForegroundColor Yellow
dotnet build $wixProj -c Release -p:Platform=$Platform

$outputMsi = Join-Path $rootDir "src\WinEnvEdit.Installer\bin\$Platform\Release\WinEnvEdit-$Platform.msi"
if (Test-Path $outputMsi) {
  Write-Host "Successfully built: $outputMsi" -ForegroundColor Green
} else {
  Write-Error "MSI file not found at $outputMsi"
}
