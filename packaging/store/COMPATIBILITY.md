# WinEnvEdit MSIX compatibility evidence

Updated 2026-10-13. Product type is MSIX/PWA; the application uses MSIX.
Store ID 9PF2J3W4WRGH, identity meop.WinEnvEdit, publisher
CN=4BA5B680-8485-42FF-9312-B49D5A71292B, PFN
meop.WinEnvEdit_wjhcw1q5ncdmj, publisher display name meop.

## Completed checks

| Check | Result | Evidence / limits |
|---|---|---|
| Windows desktop restriction | Passed | Exactly one TargetDeviceFamily: Windows.Desktop; MinVersion 10.0.22000.0. Package check rejects Windows.Universal and unsupported architectures |
| Package generation | Passed | Both actual-identity x64/ARM64 packages pass MakeAppx and repository package checks |
| x64 production app registration / activation | Passed | Real identity registered in development mode; COM app activation opens a responding WinEnvEdit window on Windows 11 Pro x64 build 26300 |
| WinUI packaged resources | Fixed and verified | Initial wrapper exited on activation. Adding resources.pri from the combined WinEnvEdit.pri resolves packaged MRT lookup |
| User environment create / delete | Passed | Diagnostic executable using actual WinEnvEdit.Core runs with verified package identity; writes visible in external HKCU reads, removal verified |
| Scoped virtualization | Passed | Registered manifest excludes only HKEY_CURRENT_USER\Environment; no global registry/filesystem disable |
| System Save / elevated apply | Passed | Same Core service's Save calls ShellExecuteExW/runas and its actual ApplySystemVariablesFromFile. Packaged create/delete visible in real external HKLM reads |
| UAC denial | Passed for Core service | Clearly announced single-prompt retry returned "Elevation was denied by the user"; external HKLM read confirmed no disposable variable was created |
| System save without allowElevation | Passed for Core service | 2026-10-13 separate development-registered package omitting allowElevation created/deleted a real system variable through the actual runas implementation; both candidate architectures rebuilt without the declaration |
| Elevated temporary-file handoff | Passed | System Save completes with default filesystem virtualization; no broad filesystem exemption required |
| Normal launch privilege | Passed for packaged diagnostic | Actual package-activated diagnostic reports isAdministrator=false before user-directed elevation |
| File export / import | Passed for Core service | Actual packaged FileService exports/imports a TOML variable and deletes the disposable file. GUI file-picker interaction remains untested |
| x64 GUI user edits / Save / Undo / Redo | Passed in development package | Actual WinEnvEdit UI created and edited a unique disposable user variable; both saves confirmed outside the app in real HKCU. Undo/Redo observed; variable removed and cleanup verified |
| x64 GUI file pickers / export / import | Passed in development package | Real Save As/Open dialogs exported a local TOML backup and imported a matching fixture with one synthetic addition. Addition appeared as pending UI state and was not written to HKCU; Refresh discarded it |
| Fresh application prebuild / build / unit tests | Passed with build warning | Isolated .NET SDK 10.0.401: Prebuild completed, Debug build succeeded, 180/180 tests passed. MSB3785 SDK-discovery warning remains because SDK is not installed systemwide |

Registry/file tests are integration diagnostics outside the application unit-test
suite. Probe source and results are under V:/code/publishing-work/store-probe.
They exercise the shared services and real Windows package identity; they do not
claim full GUI coverage or Microsoft certification. The temporary test variables
and export file were removed. The source service files used by the probe are
unchanged from v1.1.1.

For continued interactive checks, the real-identity x64 app remains registered
in development mode from the earlier generated staging directory. The new
unsigned candidate omits allowElevation; the earlier development registration
has not been replaced with that candidate.
The diagnostic StoreProbe and FullTrustOnly registrations were removed after
the 2026-10-13 comparison. The obsolete Test.WinEnvEdit registration was also
removed. The remaining real-identity registration is a local development
registration, not a Store download or capability approval. No trusted production
certificate was installed for it and Developer Mode was already enabled.

## Still required

- ARM64 runtime on an actual Windows 11 ARM64 system. Package/PE checks alone
  cannot establish runtime compatibility.
- GUI system editing/UAC and error display. User editing/Save/Undo/Redo and
  import/export file pickers now passed in the actual development-packaged app.
- Signed-install, upgrade and uninstall checks in a clean Windows environment.
  Development registration does not prove signed package deployment behavior.
- Microsoft restricted-capability review and full Store certification. The
  [response to the historical allowElevation request](MICROSOFT-RESPONSE.md)
  is recorded; no case ID was supplied. No package has been uploaded or submitted
  for certification. Prepared listing text is in [LISTING.md](LISTING.md).

Capability review occurs during the ordinary Partner Center submission and
certification process. None of these entries requires advance permission to
upload the current candidate. The original allowElevation inquiry is historical.

## Store device targeting

WinDirStat uses the same desktop-only mechanism in its Store MSIX
[manifest template](https://github.com/windirstat/windirstat/blob/master/setup/store/Package.appxmanifest.template):
its only TargetDeviceFamily is Windows.Desktop. Its
[Store packaging script](https://github.com/windirstat/windirstat/blob/master/setup/store/build-store-msix.ps1)
builds MSIX packages for x86, x64 and ARM64. This verifies the public packaging
configuration; it does not expose their private Partner Center settings.

Package metadata is the installation applicability gate. Windows.Desktop targets
PCs; the minimum build excludes Windows 10. x64/ARM64 package identity fields
select the appropriate executable architecture. Do not add Windows.Universal,
Windows.Xbox, Windows.Holographic or Windows.Team.

On Partner Center's Packages page, retain only Windows 10/11 Desktop availability
and disable automatic future-device-family availability. The minimum version in
the package still restricts installation to Windows 11. The Store's package
availability table should show no eligible package for the other families.

Sources: [TargetDeviceFamily applicability](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-f-targetdevicefamily),
[Store device-family availability](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/upload-app-packages),
[MRT resource discovery](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/mrtcore/mrtcore-overview),
[scoped virtualization](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization).
