#!/usr/bin/env pwsh

<#
.SYNOPSIS
Format C# code and XAML files in WinEnvEdit projects.

.DESCRIPTION
Runs dotnet format on all projects, then uses xstyler to format XAML files.
Fixes XAML line endings to ensure consistency.
#>

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BaseDir = Join-Path $ScriptDir ".."

Push-Location $BaseDir

Write-Host "Restoring tools and dependencies..." -ForegroundColor Cyan
dotnet tool restore
dotnet restore WinEnvEdit.slnx

Write-Host "Formatting code..." -ForegroundColor Cyan

Write-Host "Running dotnet format..." -ForegroundColor Yellow
dotnet format WinEnvEdit.slnx
if ($LASTEXITCODE -ne 0) {
  Write-Host "dotnet format failed" -ForegroundColor Red
  Pop-Location
  exit 1
}

$projects = Get-ChildItem -Directory | Where-Object { $_.Name -like "WinEnvEdit*" }

foreach ($project in $projects) {
  Write-Host "Processing $($project.Name)..." -ForegroundColor Yellow

  dotnet tool run xstyler --directory $project.Name --recursive --config Settings.XamlStyler
  if ($LASTEXITCODE -ne 0) {
    Write-Host "xstyler failed for $($project.Name)" -ForegroundColor Red
    Pop-Location
    exit 1
  }

  pwsh -NoProfile -File (Join-Path $ScriptDir "Settings.XamlStyler.Fixes.ps1") -Directory $project.Name
  if ($LASTEXITCODE -ne 0) {
    Write-Host "Line ending fix failed for $($project.Name)" -ForegroundColor Red
    Pop-Location
    exit 1
  }
}

Pop-Location

Write-Host "Formatting complete" -ForegroundColor Green
