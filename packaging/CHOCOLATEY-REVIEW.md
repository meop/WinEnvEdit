# Chocolatey 1.1.1 verification follow-up

Investigated 2026-10-13 (publisher local date).

## Result

Package metadata validation passed. Install verification failed on the verifier's
Windows Server 2019 image, build 10.0.17763.0. The package correctly refused the
unsupported OS with `WinEnvEdit requires Windows 11 or later.` The MSI download,
checksum and installation steps were never reached. This result provides no
evidence of an MSI installation failure on supported Windows 11.

- [Package and moderator review](https://community.chocolatey.org/packages/winenvedit/1.1.1)
- [Verifier report and install log](https://gist.github.com/choco-bot/ad5534179c1ce98f96522b0c99a02ff3)
- [Chocolatey's sandbox and exemption explanation](https://blog.chocolatey.org/2023/01/updating-package-services-image-win2019/)

Keep the Windows 11 guard. Re-uploading an identical package or simply rerunning
on the same Server 2019 image cannot resolve the mismatch. Request moderator
review and a verifier exemption, or verification on a supported Windows 11 image.
Approval is the moderators' decision. The publisher authorized the request and
it was posted on October 13, 2026 at 10:45 PM Pacific (October 14 05:45 UTC).
The page confirmed changes were saved and now says "Maintainer responded,
waiting for review/Maintainer update." The exemption is requested, not granted.
No verification rerun or rejection was selected. Normal release-driven package
generation and submission remain configured.

Rechecked October 20, 2026: no moderator reply after the posted exemption request;
status remains Maintainer responded, waiting for review/Maintainer update. The
Windows 11 minimum is also declared in the application project, not just this
package guard. Server 2019 runtime compatibility has not been tested.

Direct exemption precedents verified October 20:

- [Slack 4.52.178](https://community.chocolatey.org/packages/slack/4.52.178):
  Verification Testing Exemption, reason "Requires Windows 11."
- [ElevenClock](https://community.chocolatey.org/packages/elevenclock): verifier
  exemption because the application is intended for Windows 10 and 11.

These support the pending request; they do not grant WinEnvEdit an exemption.

The pending 1.1.1 package was replaced October 20 with the real app icon and
copyright metadata through successful
[Distribution](https://github.com/meop/WinEnvEdit/actions/runs/39537691297).
The package page confirms the iconUrl. Application binaries/version and the
Windows 11 guard are unchanged. Replacing a pending version restarts automated
checks; the earlier exemption request is still part of the review conversation.

The generator also has a fuller Chocolatey-specific description, matching the
improvement requested for PSNWDL's approved package. It explains supported PCs,
editing/backup features, UAC behavior, MSI packaging, and documentation/support.
This description is prepared for the next package upload; the current pending
submission has not been replaced again for this wording-only change. Copyright
and icons were already included in both projects' October 20 uploads.
