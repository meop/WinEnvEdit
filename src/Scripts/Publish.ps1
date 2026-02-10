#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Builds the MSI installer using WiX v6.

.DESCRIPTION
  Generates installer resources, publishes binaries for the specified platform,
  and builds the MSI installer via the .wixproj project.

.PARAMETER Platform
  The platform to build for (x64 or ARM64).

.PARAMETER Force
  Force regeneration of installer resources even if they appear up to date.
#>

param (
  [Parameter(Mandatory = $true)]
  [ValidateSet('ARM64', 'x64')]
  [string]$Platform,

  [switch]$Force
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootDir = (Get-Item $scriptDir).Parent.Parent.FullName
$srcDir = Join-Path $rootDir 'src'
$projectFile = Join-Path $srcDir 'WinEnvEdit\WinEnvEdit.csproj'
$wixProj = Join-Path $srcDir 'WinEnvEdit.Installer\WinEnvEdit.Installer.wixproj'
$installerDir = Join-Path $srcDir 'WinEnvEdit.Installer'

Write-Host 'Publishing started.' -ForegroundColor Cyan

function New-LicenseRtf {
  $licenseRtfPath = Join-Path $installerDir 'Assets\LICENSE.rtf'
  $relPath = $licenseRtfPath.Replace("$srcDir\", "")
  Write-Host "Generating: $relPath" -ForegroundColor Gray

  $licenseText = Get-Content (Join-Path $rootDir 'LICENSE.txt') -Raw
  $licenseText = $licenseText -replace "`r`n", "`n"
  $paragraphs = $licenseText -split "`n`n+"
  $licenseRtf = ($paragraphs | ForEach-Object {
      $_.Trim() -replace "`n", " "
    }) -join "\par`n"

  $rtfContent = "{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}}`n"
  $rtfContent += "{\*\generator Riched20 10.0.22621}\viewkind4\uc1`n"
  $rtfContent += "\pard\sa200\sl276\slmult1\f0\fs20\lang9`n"
  $rtfContent += $licenseRtf
  $rtfContent += "}`n"

  $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($licenseRtfPath, $rtfContent, $utf8NoBom)
}

function New-BannerBitmap {
  param(
    [System.Drawing.Image]$SourceImage
  )

  $bannerPath = Join-Path $installerDir 'Assets\Banner.bmp'
  $relPath = $bannerPath.Replace("$srcDir\", "")
  Write-Host "Generating: $relPath" -ForegroundColor Gray

  $bannerWidth = 493
  $bannerHeight = 58

  $bannerBmp = New-Object System.Drawing.Bitmap($bannerWidth, $bannerHeight)
  $bannerGraphics = [System.Drawing.Graphics]::FromImage($bannerBmp)
  $bannerGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $bannerGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

  $bannerGradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point($bannerWidth, 0)),
    [System.Drawing.Color]::FromArgb(255, 255, 140, 0),
    [System.Drawing.Color]::FromArgb(255, 100, 150, 255)
  )

  $bannerBlend = New-Object System.Drawing.Drawing2D.ColorBlend(4)
  $bannerBlend.Colors = @(
    [System.Drawing.Color]::FromArgb(255, 255, 140, 0),
    [System.Drawing.Color]::FromArgb(255, 255, 100, 150),
    [System.Drawing.Color]::FromArgb(255, 150, 100, 255),
    [System.Drawing.Color]::FromArgb(255, 100, 150, 255)
  )
  $bannerBlend.Positions = @(0.0, 0.33, 0.66, 1.0)
  $bannerGradient.InterpolationColors = $bannerBlend

  $bannerGraphics.FillRectangle($bannerGradient, 0, 0, $bannerWidth, $bannerHeight)

  $bannerLogoSize = 48
  $bannerLogoX = $bannerWidth - $bannerLogoSize - 5
  $bannerLogoY = ($bannerHeight - $bannerLogoSize) / 2

  $bannerGraphics.DrawImage($SourceImage, $bannerLogoX, $bannerLogoY, $bannerLogoSize, $bannerLogoSize)

  $bannerGraphics.Dispose()
  $bannerGradient.Dispose()

  $bannerBmp.Save($bannerPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
  $bannerBmp.Dispose()
}

function New-DialogBitmap {
  param(
    [System.Drawing.Image]$SourceImage
  )

  $dialogPath = Join-Path $installerDir 'Assets\Dialog.bmp'
  $relPath = $dialogPath.Replace("$srcDir\", "")
  Write-Host "Generating: $relPath" -ForegroundColor Gray

  $dialogWidth = 493
  $dialogHeight = 312

  $dialogBmp = New-Object System.Drawing.Bitmap($dialogWidth, $dialogHeight)
  $dialogGraphics = [System.Drawing.Graphics]::FromImage($dialogBmp)
  $dialogGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $dialogGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

  $dialogGradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point($dialogWidth, $dialogHeight)),
    [System.Drawing.Color]::FromArgb(255, 255, 140, 0),
    [System.Drawing.Color]::FromArgb(255, 100, 150, 255)
  )

  $dialogBlend = New-Object System.Drawing.Drawing2D.ColorBlend(4)
  $dialogBlend.Colors = @(
    [System.Drawing.Color]::FromArgb(255, 255, 140, 0),
    [System.Drawing.Color]::FromArgb(255, 255, 100, 150),
    [System.Drawing.Color]::FromArgb(255, 150, 100, 255),
    [System.Drawing.Color]::FromArgb(255, 100, 150, 255)
  )
  $dialogBlend.Positions = @(0.0, 0.33, 0.66, 1.0)
  $dialogGradient.InterpolationColors = $dialogBlend

  $dialogGraphics.FillRectangle($dialogGradient, 0, 0, $dialogWidth, $dialogHeight)

  $dialogLogoSize = 120
  $dialogLogoX = 30
  $dialogLogoY = ($dialogHeight - $dialogLogoSize) / 2

  $dialogGraphics.DrawImage($SourceImage, $dialogLogoX, $dialogLogoY, $dialogLogoSize, $dialogLogoSize)

  $dialogGraphics.Dispose()
  $dialogGradient.Dispose()

  $dialogBmp.Save($dialogPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
  $dialogBmp.Dispose()
}

function New-Assets {
  param(
    [switch]$Force
  )

  Write-Host 'Generating assets started.' -ForegroundColor Yellow

  $sourcePath = Join-Path $scriptDir 'App.png'

  if (-not (Test-Path $sourcePath)) {
    Write-Host "Error: Source file not found at $sourcePath" -ForegroundColor Red
    exit 1
  }

  $assetsDir = Join-Path $installerDir 'Assets'
  if (-not (Test-Path $assetsDir)) {
    New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
  }

  $licenseSource = Join-Path $rootDir 'LICENSE.txt'
  $licenseRtfPath = Join-Path $installerDir 'Assets\LICENSE.rtf'
  $bannerPath = Join-Path $installerDir 'Assets\Banner.bmp'
  $dialogPath = Join-Path $installerDir 'Assets\Dialog.bmp'
  $sourceTime = (Get-Item $sourcePath).LastWriteTime
  $licenseSourceTime = (Get-Item $licenseSource).LastWriteTime

  # Check LICENSE.rtf
  $needsLicense = $Force -or -not (Test-Path $licenseRtfPath) -or ($licenseSourceTime -gt (Get-Item $licenseRtfPath).LastWriteTime)

  if ($needsLicense) {
    New-LicenseRtf
  } else {
    $relPath = $licenseRtfPath.Replace("$srcDir\", "")
    Write-Host "Up to date: $relPath" -ForegroundColor Gray
  }

  # Check bitmaps
  Add-Type -AssemblyName System.Drawing
  $sourceImage = [System.Drawing.Image]::FromFile($sourcePath)

  $bitmapAssets = @(
    @{ Path = $bannerPath; Name = 'Banner.bmp' }
    @{ Path = $dialogPath; Name = 'Dialog.bmp' }
  )

  foreach ($asset in $bitmapAssets) {
    $needsGen = $Force -or -not (Test-Path $asset.Path) -or ($sourceTime -gt (Get-Item $asset.Path).LastWriteTime)
    $relPath = $asset.Path.Replace("$srcDir\", "")

    if ($needsGen) {
      if ($asset.Name -eq 'Banner.bmp') {
        New-BannerBitmap -SourceImage $sourceImage
      } elseif ($asset.Name -eq 'Dialog.bmp') {
        New-DialogBitmap -SourceImage $sourceImage
      }
    } else {
      Write-Host "Up to date: $relPath" -ForegroundColor Gray
    }
  }

  $sourceImage.Dispose()

  Write-Host 'Generating assets completed.' -ForegroundColor Yellow
}

New-Assets -Force:$Force

Write-Host "Platform: $Platform" -ForegroundColor Gray
Write-Host 'Binary publishing started.' -ForegroundColor Yellow
dotnet publish $projectFile -c Release -p:Platform=$Platform
Write-Host 'Binary publishing completed.' -ForegroundColor Yellow

Write-Host 'MSI building started.' -ForegroundColor Yellow
dotnet build $wixProj -c Release -p:Platform=$Platform
Write-Host 'MSI building completed.' -ForegroundColor Yellow

$outputMsi = Join-Path $rootDir "src\WinEnvEdit.Installer\bin\$Platform\Release\WinEnvEdit-$Platform.msi"
if (-not (Test-Path $outputMsi)) {
  Write-Host "Error: MSI file not found at $outputMsi" -ForegroundColor Red
  exit 1
}

Write-Host 'Publishing completed.' -ForegroundColor Green
