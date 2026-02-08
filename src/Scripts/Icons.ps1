#!/usr/bin/env pwsh

<#
.SYNOPSIS
Generate Windows app icon variations from source PNG.

.DESCRIPTION
Creates all required icon sizes from a source PNG for a Windows App SDK app.
Supports creating wide tiles by centering the logo on a transparent canvas.
Also generates an .ico file for window icon with multiple resolutions.
#>

param(
  [Parameter(Mandatory = $true)]
  [string]$SourcePath
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $ScriptDir '..\WinEnvEdit\Assets'
$BackgroundColor = 'transparent'

Write-Host '--- Generating App Icons ---' -ForegroundColor Cyan

# Check if source file exists
if (-not (Test-Path $SourcePath)) {
  Write-Error "Source file not found: $SourcePath"
  exit 1
}

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
  New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
  Write-Host "Created output directory: $OutputDir" -ForegroundColor Yellow
}

# Load System.Drawing assembly
Add-Type -AssemblyName System.Drawing

# Load source image
$sourceImage = [System.Drawing.Image]::FromFile($SourcePath)
Write-Host "Source image: $($sourceImage.Width)x$($sourceImage.Height)" -ForegroundColor Yellow

# Helper function to resize image
function New-ResizedImage {
  param(
    [System.Drawing.Image]$Image,
    [int]$Width,
    [int]$Height
  )

  $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
  $graphics = [System.Drawing.Graphics]::FromImage($bmp)
  $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

  $graphics.DrawImage($Image, 0, 0, $Width, $Height)
  $graphics.Dispose()

  return $bmp
}

# Helper function to create wide image with centered logo
function New-WideImage {
  param(
    [System.Drawing.Image]$Source,
    [int]$Width,
    [int]$Height,
    [string]$BgColor
  )

  $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
  $graphics = [System.Drawing.Graphics]::FromImage($bmp)
  $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

  # Clear with transparent background
  $graphics.Clear([System.Drawing.Color]::Transparent)

  # Calculate centered position
  $logoSize = [Math]::Min($Width, $Height) * 0.6
  $logoX = ($Width - $logoSize) / 2
  $logoY = ($Height - $logoSize) / 2

  # Draw centered logo
  $graphics.DrawImage($Source, $logoX, $logoY, $logoSize, $logoSize)
  $graphics.Dispose()

  return $bmp
}

# Generate variations
Write-Host 'Generating PNG variations...' -ForegroundColor Yellow

$variations = @(
  @{ Name = 'Square44x44Logo.scale-200.png'; Width = 88; Height = 88; Wide = $false },
  @{ Name = 'Square150x150Logo.scale-200.png'; Width = 300; Height = 300; Wide = $false },
  @{ Name = 'Wide310x150Logo.scale-200.png'; Width = 620; Height = 300; Wide = $true },
  @{ Name = 'SplashScreen.scale-200.png'; Width = 1240; Height = 600; Wide = $true },
  @{ Name = 'StoreLogo.png'; Width = 50; Height = 50; Wide = $false },
  @{ Name = 'LockScreenLogo.scale-200.png'; Width = 48; Height = 48; Wide = $false },
  @{ Name = 'Square44x44Logo.targetsize-24_altform-unplated.png'; Width = 24; Height = 24; Wide = $false }
)

foreach ($var in $variations) {
  $outputPath = Join-Path $OutputDir $var.Name

  if ($var.Wide) {
    $resultImage = New-WideImage -Source $sourceImage -Width $var.Width -Height $var.Height -BgColor $BackgroundColor
  } else {
    $resultImage = New-ResizedImage -Image $sourceImage -Width $var.Width -Height $var.Height
  }

  $resultImage.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
  $resultImage.Dispose()

  Write-Host "  Created: $($var.Name) ($($var.Width)x$($var.Height))" -ForegroundColor Gray
}

# Generate ICO file for window icon
Write-Host 'Generating app.ico...' -ForegroundColor Yellow

$icoSizes = @(16, 32, 48, 64, 128, 256)
$imageDataList = @()
$dataOffset = 6 + (16 * $icoSizes.Count)

foreach ($size in $icoSizes) {
  $img = New-ResizedImage -Image $sourceImage -Width $size -Height $size

  $ms = New-Object System.IO.MemoryStream
  $img.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  $img.Dispose()

  $imageData = $ms.ToArray()
  $ms.Dispose()

  # ICO format uses 0 to indicate 256 pixels (since byte max is 255)
  $widthByte = if ($size -ge 256) { [byte]0 } else { [byte]$size }
  $heightByte = if ($size -ge 256) { [byte]0 } else { [byte]$size }

  $imageDataList += @{
    Width = $widthByte
    Height = $heightByte
    Data = $imageData
    Offset = $dataOffset
  }

  $dataOffset += $imageData.Length
}

# Write ICO file
$icoPath = Join-Path $OutputDir 'app.ico'
$fs = [System.IO.File]::Open($icoPath, [System.IO.FileMode]::Create)
$writer = New-Object System.IO.BinaryWriter($fs)

# ICO header
$writer.Write([short]0)  # Reserved
$writer.Write([short]1)  # Type: 1 = icon
$writer.Write([short]$icoSizes.Count)  # Number of images

# Directory entries for each image
foreach ($entry in $imageDataList) {
  $writer.Write([byte]$entry.Width)
  $writer.Write([byte]$entry.Height)
  $writer.Write([byte]0)   # Color palette (0 = no palette)
  $writer.Write([byte]0)   # Reserved (must be 0)
  $writer.Write([short]1)   # Color planes (must be 1)
  $writer.Write([short]32)  # Bits per pixel (must be 32 for PNG)
  $writer.Write([int]$entry.Data.Length)  # Image data size
  $writer.Write([int]$entry.Offset)  # Image data offset
}

# Image data
foreach ($entry in $imageDataList) {
  $writer.Write($entry.Data)
}

$writer.Close()
$fs.Close()

Write-Host "  Created: app.ico ($($icoSizes.Count) resolutions)" -ForegroundColor Gray

# Cleanup
$sourceImage.Dispose()

Write-Host 'Icon generation complete! Created $($variations.Count + 1) files.' -ForegroundColor Green
