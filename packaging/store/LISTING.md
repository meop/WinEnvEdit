# Prepared Store listing — not submitted

Product: WinEnvEdit. Store ID: 9PF2J3W4WRGH. Free, no in-app purchases.
This document is a draft for Partner Center; it does not establish approval.

## Description

View and edit Windows environment variables in a modern desktop interface.
WinEnvEdit shows user, system and session variables, supports editing path lists,
and lets you import and export TOML backups.

User-variable changes can be saved without administrator rights. Saving
system-variable changes asks for your consent through Windows UAC and applies
only the selected changes. The main application normally runs without
administrator privileges.

Environment-variable changes affect other applications. Review edits before
saving and keep a private backup. Existing processes may retain older values
until restarted. Uninstalling the editor does not undo changes you saved.

WinEnvEdit is free and open source. Documentation and volunteer support are
available through the project's GitHub repository. The application works locally
and does not send environment-variable values to the publisher.

Requires a Windows 11 desktop PC. x64 package validation is in progress;
ARM64 runtime validation remains outstanding. Select only architectures verified
for the first submission; do not advertise an untested package as tested.

## Links and properties

- Website: https://meop.github.io/WinEnvEdit/
- Support: https://meop.github.io/WinEnvEdit/support.html
- Privacy policy: https://meop.github.io/WinEnvEdit/privacy.html
- Source and license: https://github.com/meop/WinEnvEdit
- Suggested category: Developer tools (confirm the available Partner Center choice).
- Suggested keywords: environment variables, PATH, Windows, developer tools.
- Device family: Desktop only; disable future-device-family automatic availability.

## Certification explanation

WinEnvEdit is a self-contained WinUI desktop application. runFullTrust enables
its ordinary desktop process to use Windows APIs to read environment variables,
show native UI and file pickers, and save changes selected by the user. It does
not grant administrator rights or bypass UAC. An AppContainer implementation
cannot provide the same direct Windows environment editing behavior.

The main process runs at medium integrity. User saves target HKCU\Environment.
System saves use a short-lived instance of the same executable through the
Windows runas/UAC path. That instance applies the selected system changes under
HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment and exits.
The app does not elevate at normal startup, install a privileged service or
driver, or keep an elevated interactive session. Declining UAC leaves those
system changes unapplied.

These registry writes are the product's explicit environment-editing function.
The application does not need administrator privileges to view variables or
save user variables. Intentional environment changes persist after uninstall.
Import/export uses files selected by the user; the app is not limited solely to
registry writes and its file operations must not be described as such.

The current candidate additionally declares unvirtualizedResources, with a
Windows 11 exclusion limited to HKEY_CURRENT_USER\Environment. User environment
edits must reach the actual Windows environment for other applications to use
them, and remain after the editor is uninstalled. A private per-package copy
would not perform the advertised editing function. The app does not request a
global registry or filesystem virtualization exemption. Other registry locations
and filesystem locations retain the package's default virtualization behavior.

This configuration has passed development-package registry diagnostics and
actual x64 GUI user-save tests. Normally installed, test-signed runtime
validation is still incomplete because the CI runner's administrator/logon
configuration blocked normal-user activation. Do not claim those tests passed.
The capability will be reviewed as part of this submission; no separate
advance approval is requested. Reconcile these notes with the final manifest
if further testing establishes that the extra declaration can be removed.

## Remaining listing materials

- Screenshots of the actual packaged app using synthetic variables only.
- Partner Center age-rating questionnaire and current product properties.
- Final supported architecture selection and signed package runtime results.
- Upload actual-identity package(s), inspect capability prompts, and submit for
  certification after technical validation. No upload or submission has occurred.
