# Microsoft Store candidate

Submission 1 is now in certification. See the [actual submission record](SUBMISSION.md)
for saved settings, evidence and remaining work. Approval/publication is pending.

Use the normal Partner Center MSIX submission and certification process.
Capability explanations belong in that submission; the current candidate does
not require a separate advance-permission email before upload.

Select MSIX/PWA for this Store product; WinEnvEdit uses MSIX. Current identity:
meop.WinEnvEdit; publisher CN=4BA5B680-8485-42FF-9312-B49D5A71292B; Store ID
9PF2J3W4WRGH. See [completed checks and remaining gates](COMPATIBILITY.md).

New-StorePackage.ps1 wraps the complete self-contained publish directory without
changing the existing MSI/portable release. Use PowerShell 7 on Windows and the
Windows SDK MakeAppx.exe. Supply the exact Identity Name and Publisher from the
reserved Partner Center product. The executable version must match VERSION.

    ./scripts/New-StorePackage.ps1 -PublishDirectory PUBLISH_DIR -Architecture x64 -IdentityName PARTNER_CENTER_NAME -Publisher 'CN=PARTNER_CENTER_PUBLISHER' -MakeAppxPath PATH_TO_MAKEAPPX

Repeat for ARM64 using its publish output. Packages in out/store are unsigned.
Test identities are appropriate only for local packaging checks. Do not submit
those candidates. For sideload testing, sign a candidate with a test certificate
whose subject matches Publisher and trust it only in the isolated test VM.
Microsoft signs an accepted Store MSIX after certification.

The manual Store candidate workflow packages the release matching VERSION after
STORE_IDENTITY_NAME and STORE_PUBLISHER repository variables are configured. It
uploads unsigned x64/ARM64 candidates and does not submit them to the Store.

The candidate requests runFullTrust and unvirtualizedResources. The originally
included allowElevation declaration was removed after the actual Core service
passed packaged system-save/elevation tests without it. See
[the WinDirStat comparison and limits](CAPABILITY-COMPARISON.md).
Windows 11 flexible virtualization excludes only HKCU\Environment so user edits
affect the real environment. Other registry keys and the filesystem retain
default virtualization. Restricted capabilities require Store review.

The manifest targets only Windows.Desktop, minimum 10.0.22000.0 (Windows 11), and
the packager verifies x64/ARM64 executable architecture. Test-StorePackage.ps1
rejects other device families, Windows 10 minimum versions and missing package
resources. On the Partner Center Packages page, also select only Desktop and
disable automatic availability for future device families.

The packager copies the portable build's WinEnvEdit.pri to resources.pri, which
MRT requires for packaged activation. Without it, registration can succeed while
the application exits at launch. The original portable release is unchanged.

Microsoft's strict allowElevation process motivated the original inquiry, but
the current candidate no longer declares that capability. Its documentation also
limits the intended scenarios for unvirtualizedResources and separately restricts
elevated functionality in Store apps. Store eligibility has not been granted.
The publisher sent the [original request](APPROVAL-REQUEST.md) on 2026-10-09 and
provided [Microsoft's response](MICROSOFT-RESPONSE.md) on 2026-10-13. It addresses
the originally requested allowElevation and directs capability review through
certification. It is not a rejection of a WinEnvEdit package submission. Prepared
[listing and certification notes](LISTING.md) describe current behavior. Account
registration alone does not establish capability approval.

Before first publication, test on isolated Windows 11 x64 and ARM64 systems:

- Install and launch the package as a standard user.
- Create a disposable user variable; verify actual HKCU Environment and a newly
  launched external process see it. Edit and remove it, then repeat after upgrade.
- Create a disposable system variable through Save and the UAC prompt; verify
  the actual HKLM system environment. Test denied elevation and retry.
- Confirm the elevated --apply-system handoff can read its temporary input.
- Verify import/export, launch from Start, and simultaneous MSI installation.
- Uninstall; verify intentional environment edits persist and restore test values.

Upload the validated candidate and complete its listing and capability
justifications in Partner Center, then submit for certification. Capability
approval is part of that review, not a prerequisite permission to submit. Wire
automatic publishing to releases after first publication; subsequent submission can
use Microsoft's msstore CLI with Entra credentials and the reserved product ID.
Keep Store credentials in GitHub secrets and identity fields in variables.

References: [virtualization](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization),
[capabilities](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations),
[GitHub automation](https://learn.microsoft.com/en-us/windows/apps/publish/msstore-dev-cli/github-actions).
