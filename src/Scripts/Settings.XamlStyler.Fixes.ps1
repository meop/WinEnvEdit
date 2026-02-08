#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Fixes XAML line endings after XamlStyler formatting.

.DESCRIPTION
  XamlStyler sometimes introduces CRLF line endings or inconsistent formatting.
  This script ensures all XAML files use LF line endings and UTF-8 with BOM.

.PARAMETER Directory
  The directory to search for XAML files.
#>

param(
  [Parameter(Mandatory = $true)]
  [string]$Directory
)

$utf8Bom = New-Object System.Text.UTF8Encoding($true)

Get-ChildItem -Path $Directory -Recurse -Filter *.xaml |
  Where-Object { $_.FullName -notlike '*\bin\*' -and $_.FullName -notlike '*\obj\*' } |
  ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName)
    $fixed = $content.Replace("`r`n", "`n")
    if ($fixed -ne $content) {
      [System.IO.File]::WriteAllText($_.FullName, $fixed, $utf8Bom)
    }
  }
