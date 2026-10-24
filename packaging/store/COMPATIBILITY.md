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
