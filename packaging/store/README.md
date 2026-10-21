# Microsoft Store publishing

WinEnvEdit 1.1.1 is live in Microsoft Store, verified in Partner Center on
2026-10-20. [Store listing](https://apps.microsoft.com/detail/9PF2J3W4WRGH).
See [submission settings](SUBMISSION.md), [listing text](LISTING.md), and
[runtime test coverage](COMPATIBILITY.md).

## Package updates

`scripts/New-StorePackage.ps1` wraps the existing self-contained published app.
Use PowerShell 7, the Windows SDK MakeAppx.exe, and the reserved product identity:

- Name: meop.WinEnvEdit
- Publisher: CN=4BA5B680-8485-42FF-9312-B49D5A71292B
- Store ID: 9PF2J3W4WRGH
- Publisher display name: meop

    ./scripts/New-StorePackage.ps1 -PublishDirectory PUBLISH_DIR -Architecture x64 -IdentityName meop.WinEnvEdit -Publisher 'CN=4BA5B680-8485-42FF-9312-B49D5A71292B' -MakeAppxPath PATH_TO_MAKEAPPX

Repeat for ARM64. `scripts/Test-StorePackage.ps1` checks package identity,
architecture, resources and desktop targeting. The wrapper copies WinEnvEdit.pri
to resources.pri for packaged MRT resource lookup. Microsoft signs Store packages;
GitHub MSI/ZIP releases are separate distribution artifacts.

The manifest targets Windows.Desktop, minimum build 22000 (Windows 11). Keep only
Desktop enabled in Partner Center and automatic future-device availability off.
Capabilities are runFullTrust and unvirtualizedResources. The latter excludes only
HKCU\Environment from registry write virtualization. The UI runs normally without
admin rights; a requested system save uses a short-lived runas/UAC helper.

The manually dispatched Store candidate workflow creates unsigned packages and
attempts a runtime diagnostic. Its hosted standard-user diagnostic is unresolved;
it does not publish. Release-driven Store submission still needs implementation
and Partner Center / Entra credentials. Scoop, Chocolatey and WinGet automation
are separate.

## Listing-only updates

In Partner Center, choose Start update, edit the English Store listing, replace
or add screenshots and captions, then submit the listing update for review. Keep
the existing packages when no binaries have changed; no app version bump needed.

Use actual app screenshots with sample data and no private environment values.
The current three originals are shared from [docs/images](../../docs/images);
their Store ordering and captions are in [LISTING.md](LISTING.md).
Aim for 4–6 useful views: populated main screen, expanded PATH editing, validation,
and light/dark themes. PNG, at least 1366 x 768, under 50 MB each; captions up to
200 characters. The current Partner Center Desktop upload control advertises 30 files.
Microsoft's documentation still says 10 desktop screenshots per language; use
the portal's limit when uploading. Four to six is sufficient for this app.

References: [screenshot requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images),
[Store update automation](https://learn.microsoft.com/en-us/windows/apps/publish/msstore-dev-cli/github-actions),
[scoped virtualization](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization).
