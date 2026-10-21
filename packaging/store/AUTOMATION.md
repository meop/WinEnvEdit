# Microsoft Store release automation

The stable-release pipeline calls `.github/workflows/store.yaml` after publishing
the GitHub release. It verifies and wraps the released x64 and ARM64 portable
binaries, validates their desktop-only manifests, and creates one unsigned MSIX
bundle. Microsoft signs the package after accepting the Store submission.

Automatic submission is disabled until credentials are configured and the
repository variable `STORE_PUBLISH_ENABLED` is set to `true`. Preparation works
without Store credentials. No application version bump is needed to configure it.

## One-time account setup

Follow [Microsoft's setup instructions](https://learn.microsoft.com/en-us/windows/apps/publish/msstore-dev-cli/github-actions):

1. In Partner Center, open Account settings and associate an existing Microsoft
   Entra tenant, or create a tenant there if none exists.
2. Register an Entra application for GitHub publishing. Under Partner Center's
   Account settings > User management > Microsoft Entra applications, add that
   application with the Manager role required by the submission API.
3. Obtain the tenant ID, application/client ID and Partner Center seller ID.
   Create an application client secret; record its expiry for rotation.
4. In [WinEnvEdit's GitHub environments](https://github.com/meop/WinEnvEdit/settings/environments),
   open **microsoft-store** and add these **environment secrets**:

   | Secret | Value |
   |---|---|
   | `AZURE_AD_TENANT_ID` | Associated tenant ID |
   | `AZURE_AD_APPLICATION_CLIENT_ID` | Publishing application's client ID |
   | `AZURE_AD_APPLICATION_SECRET` | Client secret **value**, not its identifier |
   | `SELLER_ID` | Partner Center seller ID |

   Enter secrets directly into GitHub; do not paste them into chat or commit them.

## Check access and enable

1. Open Actions > Microsoft Store > Run workflow on `main`. Enter the existing
   release version (`1.1.1` currently) and choose operation **check**. This builds
   the bundle and verifies authenticated product access without creating a submission.
2. Once access succeeds, set repository Actions variable `STORE_PUBLISH_ENABLED`
   to `true`. Future stable releases from the normal pipeline then submit
   automatically. No polling schedule or separate manually created release is needed.
3. Microsoft still certifies every update. A successful submission command is
   not evidence of approval or public availability; check Partner Center status.

The workflow refuses to submit if Partner Center already has a pending submission,
including a listing-only draft or update in certification. Finish that update first.
It does not delete another submission. If a release is blocked, run the workflow
again with **submit** after resolving the existing submission. Use source whose
VERSION matches the released assets; do not resubmit the already published version.

Manual operation **prepare** only builds an artifact; it is the default.
The `microsoft-store` environment only permits deployments from `main`.

Package updates retain the existing Store listing, screenshots and pricing using
Microsoft's CLI. This workflow does not automatically synchronize Markdown/HTML
captions or website changes into the Store. Listing-only edits remain in Partner
Center. Do not edit API-created drafts in Partner Center while automation is
working on them; [Microsoft documents that restriction](https://learn.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services).

## Validation and limitations

The release packager has been exercised against both real 1.1.1 release ZIPs,
including MakeAppx validation and creation of the two-architecture bundle.
Authenticated upload/certification is untested until the credentials are configured.
Manifest validation does not replace the remaining runtime checks in
[COMPATIBILITY.md](COMPATIBILITY.md). The older Store candidate workflow remains
a separate manual diagnostic; its hosted runtime limitation is not treated as a
passing test by this workflow.
