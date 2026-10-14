# Distribution packages

Run from the repository root with PowerShell 7 and authenticated GitHub CLI:

    ./scripts/New-DistributionPackages.ps1 -Version RELEASE_VERSION

Use the existing published version; no version bump or new app release is needed.
The generator verifies release asset sizes, available GitHub SHA256 digests and
portable ZIP layout. Output is ignored under out/distribution. Optional
AssetsDirectory and OutputDirectory parameters support a local cache and staging.

Scoop: both projects are published in the shared
[meop bucket](https://github.com/meop/scoop-bucket). Add it with
`scoop bucket add meop https://github.com/meop/scoop-bucket`, then install with
`scoop install meop/winenvedit`. Its independent updater verifies both architectures'
release assets on manual refresh. The app's stable-release pipeline calls its
reusable publisher to update this app immediately after release. The configured
SCOOP_BUCKET_SSH_KEY secret has write access only to the bucket; no personal token
is needed. The distribution workflow can also publish an existing release manually.
The generated manifest here remains a reviewable artifact.
Public Games/Extras inclusion is deferred until established usage or an accepted
exception. Keep the same package names during any migration.

Chocolatey: pack the generated nuspec with choco pack. The workflow packs it and
uploads reviewable artifacts. Publication stays disabled until initial install,
upgrade and uninstall smoke checks pass and a maintainer account is available.
Set repository variable CHOCOLATEY_PUBLISH_ENABLED to true and configure
CHOCOLATEY_API_KEY in the chocolatey environment to enable submission. Configure
environment protection if publication should require review. Chocolatey moderation
remains external. Never put credentials in package files or logs.

The first 1.1.1 submission passed metadata validation but its Windows 11 guard
rejected the verifier's Windows Server 2019 image. See the
[verification follow-up and prepared moderator request](CHOCOLATEY-REVIEW.md).
Keep the OS requirement; an unsupported verifier host needs moderator review.

The reusable distribution workflow runs after successful stable releases, and
workflow_dispatch can package an already published release. Calling it directly
from the release pipeline handles releases created with GitHub's default token,
which do not trigger a second release-event workflow. WinGet remains separate.

Validation does not install packages or change environment variables. Native
smoke tests remain a publication gate; artifact creation is not a public listing.
