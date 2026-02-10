#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Runs all prebuild steps for a full build.

.DESCRIPTION
  Formats code, generates icons, and synchronizes versions.
  Use this before a full build to ensure all generated files are up to date.

.PARAMETER Force
  Force regeneration of all assets even if they appear up to date.
#>

param (
  [switch]$Force
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Get-Item $scriptDir).Parent.Parent.FullName
$srcDir = Join-Path $rootDir 'src'
$baseDir = Join-Path $scriptDir '..'

Write-Host 'Prebuild started.' -ForegroundColor Cyan

function Format-Code {
  Write-Host 'Formatting code started.' -ForegroundColor Yellow

  Push-Location $baseDir

  dotnet format WinEnvEdit.slnx
  if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host 'Error: dotnet format failed' -ForegroundColor Red
    exit 1
  }

  dotnet tool restore

  $projects = Get-ChildItem -Directory | Where-Object { $_.Name -like 'WinEnvEdit*' }

  foreach ($project in $projects) {
    Write-Host "Styling: $($project.Name)" -ForegroundColor Gray

    dotnet tool run xstyler --directory $project.Name --recursive --config Settings.XamlStyler
    if ($LASTEXITCODE -ne 0) {
      Pop-Location
      Write-Host "Error: XAML styler run failed for $($project.Name)" -ForegroundColor Red
      exit 1
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    Get-ChildItem -Path $project.Name -Recurse -Filter *.xaml |
      Where-Object { $_.FullName -notlike '*\bin\*' -and $_.FullName -notlike '*\obj\*' } |
      ForEach-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        $fixed = $content.Replace("`r`n", "`n")
        if ($fixed -ne $content) {
          [System.IO.File]::WriteAllText($_.FullName, $fixed, $utf8NoBom)
        }
      }
  }

  Pop-Location
  Write-Host 'Formatting code completed.' -ForegroundColor Yellow
}

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

function New-WideImage {
  param(
    [System.Drawing.Image]$Source,
    [int]$Width,
    [int]$Height
  )

  $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
  $graphics = [System.Drawing.Graphics]::FromImage($bmp)
  $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

  $graphics.Clear([System.Drawing.Color]::Transparent)

  $logoSize = [Math]::Min($Width, $Height) * 0.6
  $logoX = ($Width - $logoSize) / 2
  $logoY = ($Height - $logoSize) / 2

  $graphics.DrawImage($Source, $logoX, $logoY, $logoSize, $logoSize)
  $graphics.Dispose()

  return $bmp
}

function New-Assets {
  param(
    [switch]$Force
  )

  Write-Host 'Generating assets started.' -ForegroundColor Yellow

  $sourcePath = Join-Path $scriptDir 'App.png'
  $outputDir = Join-Path $srcDir 'WinEnvEdit\Assets'

  if (-not (Test-Path $sourcePath)) {
    Write-Host "Error: Source file not found at $sourcePath" -ForegroundColor Red
    exit 1
  }

  if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
  }

  $sourceTime = (Get-Item $sourcePath).LastWriteTime
  $sourceImage = $null

  # 1. Process App.ico
  $icoPath = Join-Path $outputDir 'App.ico'
  $relIcoPath = $icoPath.Replace("$srcDir\", "")
  $needsIco = $Force -or -not (Test-Path $icoPath) -or ($sourceTime -gt (Get-Item $icoPath).LastWriteTime)

  if ($needsIco) {
    Add-Type -AssemblyName System.Drawing
    $sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
    Write-Host "Generating: $relIcoPath" -ForegroundColor Gray

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

      $widthByte = if ($size -ge 256) { [byte]0 } else { [byte]$size }
      $heightByte = if ($size -ge 256) { [byte]0 } else { [byte]$size }

      $imageDataList += @{
        Width  = $widthByte
        Height = $heightByte
        Data   = $imageData
        Offset = $dataOffset
      }

      $dataOffset += $imageData.Length
    }

    $fs = [System.IO.File]::Open($icoPath, [System.IO.FileMode]::Create)
    $writer = New-Object System.IO.BinaryWriter($fs)

    $writer.Write([int16]0)
    $writer.Write([int16]1)
    $writer.Write([int16]$icoSizes.Count)

    foreach ($entry in $imageDataList) {
      $writer.Write([byte]$entry.Width)
      $writer.Write([byte]$entry.Height)
      $writer.Write([byte]0)
      $writer.Write([byte]0)
      $writer.Write([int16]1)
      $writer.Write([int16]32)
      $writer.Write([int]$entry.Data.Length)
      $writer.Write([int]$entry.Offset)
    }

    foreach ($entry in $imageDataList) {
      $writer.Write($entry.Data)
    }

    $writer.Close()
    $fs.Close()
  } else {
    Write-Host "Up to date: $relIcoPath" -ForegroundColor Gray
  }

  # 2. Process PNG assets
  $pngVariations = @(
    @{ Name = 'Square44x44Logo.scale-200.png'; Width = 88; Height = 88; Wide = $false },
    @{ Name = 'Square150x150Logo.scale-200.png'; Width = 300; Height = 300; Wide = $false },
    @{ Name = 'Wide310x150Logo.scale-200.png'; Width = 620; Height = 300; Wide = $true },
    @{ Name = 'SplashScreen.scale-200.png'; Width = 1240; Height = 600; Wide = $true },
    @{ Name = 'StoreLogo.png'; Width = 50; Height = 50; Wide = $false },
    @{ Name = 'LockScreenLogo.scale-200.png'; Width = 48; Height = 48; Wide = $false },
    @{ Name = 'Square44x44Logo.targetsize-24_altform-unplated.png'; Width = 24; Height = 24; Wide = $false }
  )

  foreach ($var in $pngVariations) {
    $outputPath = Join-Path $outputDir $var.Name
    $relOutputPath = $outputPath.Replace("$srcDir\", "")
    $needsPng = $Force -or -not (Test-Path $outputPath) -or ($sourceTime -gt (Get-Item $outputPath).LastWriteTime)

    if ($needsPng) {
      if ($null -eq $sourceImage) {
        Add-Type -AssemblyName System.Drawing
        $sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
      }
      Write-Host "Generating: $relOutputPath" -ForegroundColor Gray

      if ($var.Wide) {
        $resultImage = New-WideImage -Source $sourceImage -Width $var.Width -Height $var.Height
      } else {
        $resultImage = New-ResizedImage -Image $sourceImage -Width $var.Width -Height $var.Height
      }

      $resultImage.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
      $resultImage.Dispose()
    } else {
      Write-Host "Up to date: $relOutputPath" -ForegroundColor Gray
    }
  }

  if ($sourceImage) {
    $sourceImage.Dispose()
  }

  Write-Host 'Generating assets completed.' -ForegroundColor Yellow
}

function Sync-XmlManifest($path, $regex, $value) {
  if (-not (Test-Path $path)) {
    Write-Host "Error: File not found at $path" -ForegroundColor Red
    exit 1
  }

  $relPath = $path.Replace("$srcDir\", "")
  Write-Host "Processing: $relPath" -ForegroundColor Gray

  $content = Get-Content $path -Raw
  $replacement = '${1}' + $value + '${2}'
  $content = $content -replace $regex, $replacement
  $content = $content.TrimEnd() + "`n"

  $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

function Sync-Versions {
  $version = (Get-Content (Join-Path $rootDir 'VERSION')).Trim()
  Write-Host "Version: $version" -ForegroundColor Gray
  Write-Host 'Synchronizing versions started.' -ForegroundColor Yellow

  $winVersion = if ($version.Split('.').Count -eq 3) { "$version.0" } else { $version }

  $versionProps = Join-Path $srcDir 'Directory.Packages.props'
  $versionPropsContent = Get-Content $versionProps -Raw
  if ($versionPropsContent -match '<PackageVersion\s+Include="Microsoft\.WindowsAppSDK"\s+Version="([^"]+)"') {
    $appSdkVersion = $matches[1]
    $appSdkParts = $appSdkVersion.Split('.')
    $appSdkMajor = [int]$appSdkParts[0]
    $appSdkMinor = [int]$appSdkParts[1]
  } else {
    Write-Host "Error: Could not find Windows App SDK version in $versionProps" -ForegroundColor Red
    exit 1
  }

  Sync-XmlManifest `
    -path (Join-Path $srcDir 'WinEnvEdit\App.manifest') `
    -regex '(<assemblyIdentity\s+[^>]*version=")[0-9.]*(")' `
    -value $winVersion

  Sync-XmlManifest `
    -path (Join-Path $srcDir 'WinEnvEdit\Package.appxmanifest') `
    -regex '(<Identity\s+[^>]*Version=")[0-9.]*(")' `
    -value $winVersion

  $yamlFile = Join-Path $srcDir 'WinEnvEdit.yaml'
  if (Test-Path $yamlFile) {
    $relYamlPath = $yamlFile.Replace("$srcDir\", "")
    Write-Host "Processing: $relYamlPath" -ForegroundColor Gray
    $content = Get-Content $yamlFile -Raw
    $content = $content -replace '(PackageVersion:\s+)[^\s\r\n]*', ('${1}' + $version)
    $content = $content -replace '(releases\/download\/v)[0-9.]*(\/WinEnvEdit)', ('${1}' + $version + '${2}')
    $content = $content -replace '(- PackageIdentifier:\s+Microsoft\.WindowsAppRuntime\.)[^\s\r\n]*', ('${1}' + "$appSdkMajor.$appSdkMinor")
    $content = $content.TrimEnd() + "`n"

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($yamlFile, $content, $utf8NoBom)
  }

  Write-Host 'Synchronizing versions completed.' -ForegroundColor Yellow
}

Format-Code
New-Assets -Force:$Force
Sync-Versions

Write-Host 'Prebuild completed.' -ForegroundColor Green
