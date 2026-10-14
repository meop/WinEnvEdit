# Microsoft response to the original allowElevation inquiry

Recorded 2026-10-13 from the publisher's pasted email. No package has been
submitted to Microsoft Store. This response is guidance on the original
allowElevation request, not a certification rejection of WinEnvEdit.

Microsoft said restricted capabilities are evaluated during certification.
allowElevation is generally not recommended for Store submissions and usually
will not be approved. The response did not guarantee approval.

The requested justification must explain:

- Why the product needs each requested capability and alternatives explored.
- Which functionality needs it and why less privileged alternatives are insufficient.
- How access and scope follow least privilege.
- Safeguards against misuse, unintended behavior and user harm.
- Assurance that the capability is used only for the stated purposes and paths.

Microsoft requires a valid website URL **or** support contact. Its instruction is
to enter this under Partner Center > Properties > Support info > Use different
details for this app > Support contact info.

Prepared project URLs:

- Product: https://meop.github.io/WinEnvEdit/
- Support: https://meop.github.io/WinEnvEdit/support.html
- Privacy: https://meop.github.io/WinEnvEdit/privacy.html
- Public issue support: https://github.com/meop/WinEnvEdit/issues

These are GitHub-hosted project pages and volunteer issue support, not a paid
support service. No guaranteed response time is offered.

The candidate now omits allowElevation. Certification notes must describe the
capabilities actually in the submitted package, with explicit disclosure of
optional UAC-protected system-variable editing. There is no need to repeat the
obsolete advance inquiry as if allowElevation were still requested.

Microsoft's linked guidance:

- [Restricted-capability approval process](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations#restricted-capability-approval-process)
- [App capability declarations](https://learn.microsoft.com/en-us/windows/uwp/packaging/app-capability-declarations)
