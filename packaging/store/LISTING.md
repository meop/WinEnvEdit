# Microsoft Store listing

Product: WinEnvEdit. Store ID: 9PF2J3W4WRGH. Free, no in-app purchases.
Product configuration and package publishing are in [README.md](README.md).

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

Requires a Windows 11 desktop PC.

## Screenshots

Shared originals are in [docs/images](../../docs/images), also used by the README
and GitHub Pages gallery. Display in this order:

1. `environment-and-path.png`: View system and user environment variables side by side, with individual PATH entries expanded for editing.
2. `search.png`: Search variable names and values across both system and user scopes.
3. `add-variable.png`: Add standard or expandable variables with input validation before saving.

All three publisher-provided PNGs are 2538 x 1430 pixels.

Requirements: PNG, at least 1366 x 768, under 50 MB each, captions up to 200
characters. Microsoft's
[documentation](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images)
says 10 desktop screenshots per language while the Partner Center upload control
advertises 30; use the portal's limit. Four to six views are enough for this app:
populated main screen, expanded PATH editing, validation, and light/dark themes.
Use real screenshots with sample data and no private environment values.

## Listing-only updates

Text and screenshot changes need no new package or version bump. In Partner
Center choose **Start update**, edit the English listing, replace or add
screenshots and captions, keep the existing packages, and submit for review.

Automation does not touch listing content — the Store workflow only submits
packages, so edits here stay manual. Finish any listing update before a package
submission, since the workflow refuses to submit while a draft is pending.

## Links and properties

- Website: https://meop.github.io/WinEnvEdit/
- Support: https://meop.github.io/WinEnvEdit/support.html
- Privacy policy: https://meop.github.io/WinEnvEdit/privacy.html
- Source and license: https://github.com/meop/WinEnvEdit
- Saved categories: Utilities + tools; secondary Developer tools.
- Potential keywords: environment variables, PATH, Windows, developer tools.
  Keywords were not entered for the first submission.
- Device family: Desktop only; disable future-device-family automatic availability.
