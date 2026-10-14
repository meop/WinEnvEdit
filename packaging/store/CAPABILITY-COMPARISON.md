# WinDirStat / WinEnvEdit elevation comparison

Verified 2026-10-13 against WinDirStat source commit
40d77b693e1e1059aba21412268b3cbaa09cec62 and its published x64 MSIX 2.9.3.0.

| Item | WinDirStat | WinEnvEdit |
|---|---|---|
| Normal process | Ordinary desktop process, normally not elevated | WinUI desktop process at medium integrity |
| User-requested elevation | ShellExecuteEx with the runas verb, relaunching its own EXE | ShellExecuteExW with runas, relaunching its own EXE in headless apply mode |
| Elevated operation | UI/scan/tools continue in elevated instance | Short-lived process writes selected system-environment changes and exits |
| Public package capabilities | runFullTrust only; published MSIX inspection confirms no allowElevation | Candidate now runFullTrust and scoped unvirtualizedResources; allowElevation removed after controlled test |

The elevation mechanism is fundamentally the same. WinEnvEdit confines the
elevated instance to the selected system-variable changes, rather than elevating
the whole interactive session. It already launches normally without admin rights.

## What the capabilities mean

runFullTrust permits packaging/activation of the desktop application at medium
integrity. It is not an administrator token and does not silently bypass UAC.
allowElevation is a separate restricted capability, documented for specific
partner/enterprise elevation scenarios and subject to strict Store review with
advance contact. It does not replace the user's UAC consent.

Microsoft's documentation describes allowElevation broadly, but that description
alone did not establish that our explicit runas implementation needs it. A
controlled test now establishes narrower runtime evidence:

- Registered a separate packagedClassicApp/mediumIL diagnostic with runFullTrust
  and the existing scoped registry exception, deliberately omitting allowElevation.
- The diagnostic uses the actual WinEnvEdit.Core EnvironmentService, unchanged
  from the released application.
- Normal package activation reported a real package identity and isAdministrator=false.
- Its runas save path created a unique system variable; an external HKLM read
  confirmed the write. A second runas call removed it; external cleanup verified.
- Test variable: WINENVEDIT_MSIX_SMOKE_8e5543abea5b43e4b345654d77cdf9ae.
- Test package: Test.WinEnvEdit.FullTrustOnly_1.1.1.0_x64__m5dz9j3qapawm.

This proves that allowElevation is not technically needed for that tested
development-registration path. Both actual-identity candidate architectures were
rebuilt without it and passed MakeAppx/package checks. WinEnvEdit itself still
needs Store-signed installation testing and certification review.

## Store evidence and remaining approval issue

WinDirStat's official Store product is 9PH1GL95P3WF. Its live Store metadata,
retrieved with winget show, explicitly advertises optional administrator access
for accelerated NTFS scans and requires it for recovery. The actual Store-installed
package was inspected: WinDirStatTeam.WinDirStat_2.9.3.0_x64__2386j6qhqh3n2,
Store signature, runFullTrust only, no allowElevation.

The publisher used File > Run Elevated in that Store window, approved UAC and
confirmed the title showed Administrator/Admin. This verifies its own elevation
path in the real Store-installed application, not just the source or GitHub MSIX.
An initial attempt to elevate the executable externally from PowerShell returned
Access denied; that was a different activation path and did not predict the
in-app result. Private certification correspondence remains unavailable.

Removing an unnecessary manifest declaration is not a guarantee of approval.
Microsoft's desktop packaging guidance also states that Store apps requiring
elevation for any functionality are not accepted, while its capability page
describes limited exceptions. The publisher's reported response reinforces that
approval is discretionary. Describe the actual UAC behavior honestly in the
Partner Center certification notes so reviewers can assess this model; do not
conceal system editing. This does not require a new advance-permission email.

WinEnvEdit also still needs user-environment writes to reach the actual
HKCU\Environment key and persist beyond app removal. The scoped
unvirtualizedResources exception is a separate unresolved certification question.
Website, support, descriptions and privacy information can improve the submission,
but do not establish either capability approval or an exception.

## Sources

- [Microsoft capability definitions and approval](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations)
- [Microsoft desktop packaging limitations](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-prepare)
- [WinDirStat Store manifest](https://github.com/windirstat/windirstat/blob/40d77b693e1e1059aba21412268b3cbaa09cec62/setup/store/Package.appxmanifest.template)
- [WinDirStat runas implementation](https://github.com/windirstat/windirstat/blob/40d77b693e1e1059aba21412268b3cbaa09cec62/windirstat/HelpersTasks.cpp)
- [Official WinDirStat Store product](https://apps.microsoft.com/detail/9ph1gl95p3wf)
