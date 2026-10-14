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
Approval is the moderators' decision; no exemption has been requested or granted
yet. Normal release-driven package generation and submission remain configured.

## Prepared moderator comment (not sent)

Please review an OS-related Package Verifier exemption for winenvedit 1.1.1,
or run verification on a supported Windows 11 image if available.

The verifier report at
https://gist.github.com/choco-bot/ad5534179c1ce98f96522b0c99a02ff3
identifies Windows Server 2019, build 10.0.17763.0. Installation stops at the
intentional Windows 11 minimum-build guard (22000), before download or MSI
execution, with "WinEnvEdit requires Windows 11 or later."

WinEnvEdit is a Windows 11 desktop application. Removing this guard would offer
installation on an unsupported OS. Package metadata validation has passed.
The same generated 1.1.1 Chocolatey package was tested on Windows 11 x64 on
October 13, 2026 with Chocolatey 2.7.4: silent MSI installation, application
launch, forced same-version reinstall, and Chocolatey's MSI auto-uninstall all
passed. ARM64 runtime and upgrades between different app versions are not
claimed as tested.

Release assets and checksums are unchanged. The package uses /qn /norestart
and accepts MSI exit codes 0, 1641 and 3010. Please advise if any additional
evidence is required for the OS exemption.
