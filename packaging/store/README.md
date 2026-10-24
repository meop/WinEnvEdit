# Microsoft Store publishing

WinEnvEdit is published at [apps.microsoft.com](https://apps.microsoft.com/detail/9PF2J3W4WRGH).
Stable releases submit Store updates automatically.

- [SETUP.md](SETUP.md) — one-time credential and Partner Center setup, including
  how an Individual publisher with a personal Microsoft account reaches an API
  that assumes an organization
- [LISTING.md](LISTING.md) — listing text, screenshots and their requirements
- [COMPATIBILITY.md](COMPATIBILITY.md) — MSIX runtime test coverage

## How an update reaches the Store

The stable-release pipeline calls
[`.github/workflows/store.yaml`](../../.github/workflows/store.yaml) after
publishing the GitHub release. It verifies and wraps the released x64 and ARM64
portable binaries, validates their desktop-only manifests, and produces one
unsigned MSIX bundle. Microsoft signs the package after accepting the submission.

Submission requires `STORE_PUBLISH_ENABLED` to be `true` and the four
`microsoft-store` environment secrets to be present. Preparation works without
credentials. No application version bump is needed to configure any of this.

Package updates keep the existing listing, screenshots and pricing. The workflow
does not synchronize listing text or website changes into the Store.

## Running it by hand

**Actions > Microsoft Store > Run workflow**, on `main`, with the released
version:

| Operation | Effect |
| --- | --- |
| `prepare` | Builds the bundle artifact only. The default. |
| `check` | Also proves authenticated product access. Creates no submission. |
| `submit` | Submits the bundle for certification. |

The workflow refuses to submit while Partner Center has a pending submission,
including a listing-only draft or an update in certification — finish that first.
It never deletes another submission. Use source whose `VERSION` matches the
released assets, and do not resubmit an already published version.

Do not edit API-created drafts in Partner Center while automation is working on
them; Microsoft
[documents that restriction](https://learn.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services).

Microsoft certifies every update. A successful submission command is not approval
or public availability — check Partner Center for status.

## Product configuration

Set in the original submission and carried forward by package updates:

- Identity `meop.WinEnvEdit`, publisher `CN=4BA5B680-8485-42FF-9312-B49D5A71292B`,
  publisher display name `meop`, Store ID `9PF2J3W4WRGH`
- `Windows.Desktop` only, minimum build 22000 (Windows 11), x64 and ARM64.
  Mobile, Xbox, Team, Mixed Reality and automatic future-device availability are
  off; keep them off
- Capabilities `runFullTrust` and `unvirtualizedResources`, no `allowElevation`.
  The second excludes only `HKCU\Environment` from registry write virtualization,
  via [scoped virtualization](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization)
- Free and public, all offered markets. Utilities + tools, secondary Developer
  tools. Age rating Everyone / 3+
- [Website](https://meop.github.io/WinEnvEdit/),
  [support](https://meop.github.io/WinEnvEdit/support.html),
  [privacy](https://meop.github.io/WinEnvEdit/privacy.html)

The UI runs without administrator rights; a requested system save uses a
short-lived runas/UAC helper. Preserve these manifest restrictions in any change.

## Building a package locally

`scripts/New-StorePackage.ps1` wraps an existing self-contained published app.
Needs PowerShell 7 and the Windows SDK `MakeAppx.exe`:

```powershell
./scripts/New-StorePackage.ps1 -PublishDirectory PUBLISH_DIR -Architecture x64 -IdentityName meop.WinEnvEdit -Publisher 'CN=4BA5B680-8485-42FF-9312-B49D5A71292B' -MakeAppxPath PATH_TO_MAKEAPPX
```

Repeat for ARM64. `scripts/Test-StorePackage.ps1` checks identity, architecture,
resources and desktop targeting. The wrapper copies `WinEnvEdit.pri` to
`resources.pri` for packaged MRT lookup. Microsoft signs Store packages; the
GitHub MSI and ZIP releases are separate distribution artifacts, as are Scoop,
Chocolatey and WinGet.

The separate, manually dispatched Store candidate workflow is a runtime
diagnostic only. Its hosted standard-user check is unresolved and it does not
publish; see [COMPATIBILITY.md](COMPATIBILITY.md).
