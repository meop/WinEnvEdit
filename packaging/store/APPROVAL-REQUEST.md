# WinEnvEdit Store capability approval request

Publisher confirmed this request was sent on 2026-10-09 to reportapp@microsoft.com,
Microsoft's documented advance-contact address for allowElevation. On 2026-10-13,
the publisher reported a reply saying elevation approval is unlikely outside
special circumstances and requesting additional website/support/product details.
Approval has not been received; the exact email and case ID have not been recorded.
The text below is the prepared draft; the actual
outgoing email has not been inspected.

Subject: MSIX restricted capability approval — WinEnvEdit, Store ID 9PF2J3W4WRGH

Hello Microsoft Store team,

We have reserved WinEnvEdit's Store product but have not uploaded any packages or
submitted it for certification. Before our first MSIX submission, we are seeking
advance approval for the following restricted capabilities, or guidance on the
required review process for this utility:

- Store ID: 9PF2J3W4WRGH
- Package identity: meop.WinEnvEdit
- Package publisher: CN=4BA5B680-8485-42FF-9312-B49D5A71292B
- Publisher display name: meop
- Package family name: meop.WinEnvEdit_wjhcw1q5ncdmj
- Source: https://github.com/meop/WinEnvEdit
- Target: Windows 11 desktop PCs, x64 and ARM64 only

WinEnvEdit is a free, open-source desktop utility for viewing, editing, importing
and exporting Windows user and system environment variables. We plan to publish
the existing self-contained WinUI 3 / Native AOT application as MSIX.

The manifest requests:

1. runFullTrust: the existing WinUI desktop application runs at medium integrity
   using supported Win32 registry APIs. Normal startup and user-variable editing
   do not request elevation.
2. allowElevation: the user explicitly saves system-variable changes. The app
   invokes the same executable through ShellExecuteExW with the runas verb,
   showing Windows UAC. The elevated process runs a short headless apply mode,
   updates only HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment,
   and exits. UAC is not bypassed. This preserves the utility's core functionality.
3. unvirtualizedResources: user-variable edits must affect the actual
   HKCU\Environment key so external applications receive the changes. These are
   intentional changes to the user's Windows environment and must persist after
   uninstalling the editor. The Windows 11 flexible-virtualization exclusion is
   limited to HKEY_CURRENT_USER\Environment. The manifest does not globally
   disable registry or filesystem virtualization.

The app does not install drivers or services, elevate at ordinary startup,
maintain a privileged background service, or request general filesystem access.
The manifest targets only Windows.Desktop with MinVersion 10.0.22000.0; the
submission will exclude other device families and automatic future-family opt-in.

Current technical validation: x64 and ARM64 packages validate with MakeAppx.
The x64 production app registers and activates successfully with the real package
identity. A packaged diagnostic harness built from the same Core project confirmed
that user-variable create/delete reaches the real HKCU environment, observable
from an external unpackaged process. The same packaged Core service also elevated
through the runas/UAC path, applied and removed a disposable system variable
observable in real HKLM, and completed its temporary-file handoff with default
filesystem virtualization. A denied-UAC test returned the expected failure with
no system-variable write. Its TOML file export/import roundtrip also passed.
This is development testing, not a claim of Store certification. ARM64 runtime,
GUI-flow and signed-deployment results will follow as completed.

Please confirm whether these capabilities can be approved for this Store product,
including the scoped user-environment registry exclusion. If another MSIX manifest
configuration is required, please specify it while preserving user and system
environment editing with UAC consent.

Thank you.

---

Sources for the review process:
[capability declarations](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations),
[scoped virtualization](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization).
