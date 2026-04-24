#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Detects the host CPU architecture and prints the platform identifier.

.DESCRIPTION
  Reads PROCESSOR_ARCHITECTURE from the registry and maps it to the
  platform identifier used by dotnet build and other project tooling.
  Output: "ARM64" or "x64" (printed to stdout).
#>

$arch = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment').PROCESSOR_ARCHITECTURE

switch ($arch) {
  'AMD64' { Write-Output 'x64' }
  default { Write-Output $arch }
}
