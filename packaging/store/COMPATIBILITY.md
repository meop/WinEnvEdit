# MSIX runtime test coverage

What has and has not been exercised on a real MSIX build, as of October 2026.
Store certification does not imply any of the local tests below were performed,
so keep the two apart when judging whether a change is safe.

## Verified

- x64 and ARM64 package structure, identity, executable architecture, resources,
  Windows.Desktop targeting and minimum Windows build 22000.
- Real-identity x64 development-package activation and resource lookup.
- Packaged Core user-variable create/delete, real HKCU visibility and cleanup.
- Packaged Core system-variable create/delete through runas/UAC, denied-UAC
  behavior, temporary-file handoff and real HKLM visibility. No allowElevation
  declaration was needed by the tested implementation.
- Actual x64 development-package GUI user create/edit/save, undo/redo and TOML
  file-picker import/export. Disposable variables and private fixtures removed.
- Application build and 180 unit tests at the time of preparation.

## Remaining coverage

- Actual ARM64 runtime.
- GUI system-save/UAC/error display (shared Core path was tested).
- Clean Store-signed installation, upgrade and uninstall as a standard user.

The hosted CI signed-runtime diagnostic failed because of runner privilege/logon
constraints. It must not be described as passed. Development-package testing is
distinct from normal signed deployment. Diagnostic source is retained under
packaging/store/probe and scripts; failed CI history remains available in Actions.

## TODO: ARM64 launch investigation

Open, needs an interactive session on ARM64 hardware. Evidence gathered
October 24, 2026 on a Windows ARM64 machine (build 26300) over SSH:

| Build | Where | Result |
|---|---|---|
| Shipped 1.1.1 ARM64 (winget MSI, `C:\Program Files\WinEnvEdit`) | ARM64, SSH session | crash `0xC0000602` |
| Current main ARM64 publish, copied over | ARM64, SSH session | crash `0xC0000602` |
| Current main x64 publish | x64, interactive session | runs, ~138 MB working set, exits cleanly |
| `notepad.exe` (control) | ARM64, SSH session | runs |

The crash is a fail-fast inside the Windows App SDK:

```
Faulting module name: Microsoft.UI.Input.dll, version: 10.0.27200.1038
Exception code: 0xc0000602
```

`0xC0000602` is the code `WinEnvEdit.csproj` already comments on, where the
Windows App SDK bootstrapper fail-fasts hunting for an installed runtime. Both
ARM64 attempts fail identically, so this is not a regression since 1.1.1.

**What is not yet known:** whether this is architecture-specific or simply that
WinUI 3 cannot initialize in a non-interactive SSH session. The notepad control
only shows that classic Win32 GUI apps survive there, which WinUI's input stack
may not. No x64 machine was reachable over SSH to complete the comparison, so the
confound is unresolved.

**Next step:** launch the ARM64 build from an interactive desktop session on that
machine and record the outcome. If it runs, the SSH result is an environment
artifact and the remaining ARM64 item is only the GUI checks; if it crashes the
same way, the shipped ARM64 build does not start and that is a release blocker.

Do not record an SSH-session result as a pass or a failure of the application.
