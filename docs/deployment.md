# Deployment Patterns

---

## Elevated Script Launch

Launch a PowerShell script with UAC elevation without a visible terminal window.

**Problem:** `Process.Start` with `Verb = "runas"` and `-WindowStyle Hidden` is a race condition — the shell shows the window before PowerShell processes the flag.

**Solution:** Use `ShellExecuteExW` via P/Invoke. Its `uShow` field (`SW_HIDE = 0`) is applied at the shell level before any child window is created.

```csharp
[StructLayout(LayoutKind.Sequential)]
private struct ShellExecuteInfo {
  public int Size;       // Marshal.SizeOf<ShellExecuteInfo>()
  public uint Flags;     // SEE_MASK_NOSHOWUI suppresses error dialogs
  public IntPtr Window;
  public IntPtr Verb;    // "runas" — triggers UAC elevation
  public IntPtr File;    // "powershell.exe"
  public IntPtr Parameters;
  public IntPtr Directory;
  public int Show;       // SW_HIDE = 0 — the key difference from Process.Start
  // ... remaining fields ...
  public IntPtr Process; // Populated on return — wait on this handle
}
```

String fields (`Verb`, `File`, `Parameters`) are `LPCWSTR` pointers — marshal manually with `Marshal.StringToHGlobalUni` / `FreeHGlobal` in a try/finally. `LibraryImport` cannot marshal strings inside structs.

After the call: `WaitForSingleObject(info.Process, INFINITE)` → `GetExitCodeProcess` → `CloseHandle`.

**Notes:**
- UAC prompt still appears (unavoidable — that's Windows enforcing elevation)
- The PowerShell terminal window does not appear
- Non-elevated path (user-only vars) uses `Process.Start` with `CreateNoWindow = true` — no P/Invoke needed
- `SEE_MASK_NOSHOWUI` suppresses the shell's error dialog if `ShellExecuteExW` fails

---

## Unpackaged Framework-Dependent Deployment

Configure WinUI 3 apps for a small MSI that relies on external runtimes (~10 MB instead of ~100 MB self-contained).

### Project Properties

```xml
<!-- .NET Configuration -->
<SelfContained>false</SelfContained>
<PublishTrimmed>false</PublishTrimmed>
<PublishAot>false</PublishAot>

<!-- Windows App SDK Configuration -->
<WindowsPackageType>None</WindowsPackageType>
<WindowsAppSDKSelfContained>false</WindowsAppSDKSelfContained>
<WindowsAppSDKBootstrapInitialize>true</WindowsAppSDKBootstrapInitialize>
<WindowsAppSDKDeploymentManagerInitialize>false</WindowsAppSDKDeploymentManagerInitialize>

<!-- Critical: Required for .pri file generation in publish -->
<EnableMsixTooling>true</EnableMsixTooling>
```

### Publish Command

```powershell
dotnet publish -c Release -p:Platform=x64
# Note: NO -r flag (see below)
```

### Why Each Property

| Property | Value | Reason |
|----------|-------|--------|
| `SelfContained` | `false` | Framework-dependent — requires .NET 10 installed |
| `PublishTrimmed` | `false` | Trimming fails with WinUI dependencies |
| `WindowsPackageType` | `None` | Unpackaged app (not MSIX) |
| `WindowsAppSDKSelfContained` | `false` | Requires Windows App SDK 1.8 installed |
| `WindowsAppSDKBootstrapInitialize` | `true` | Auto-initializes Bootstrap for framework-dependent apps |
| `WindowsAppSDKDeploymentManagerInitialize` | `false` | DeploymentManager requires package identity (we're unpackaged) |
| **`EnableMsixTooling`** | **`true`** | **CRITICAL:** Makes `dotnet publish` copy `.pri` files to output |

### Why EnableMsixTooling=true?

Without this, `dotnet publish` omits the Package Resource Index (`.pri`) file, causing:
```
Exception code: 0xc000027b
Faulting module: Microsoft.UI.Xaml.dll
```
The `.pri` file contains compiled XAML (`.xbf`) and resource metadata — WinUI can't initialize without it. Reference: [WindowsAppSDK #3451](https://github.com/microsoft/WindowsAppSDK/issues/3451)

### Why No -r Flag?

Do NOT use `dotnet publish -r win-x64` for framework-dependent apps. The `-r` flag creates a hybrid deployment mode that breaks Windows App SDK bootstrap initialization with "package identity" errors.

- **Without `-r`:** Clean `bin/.../publish/` output, pure managed assemblies — works correctly
- **With `-r win-x64`:** Nested output with runtime-specific natives — breaks bootstrap with `0xc000027b`

Use `-r` only for self-contained deployments or cross-platform targets.

### Why MSI Instead of MSIX?

| Format | Signing Required? | Unsigned experience |
|--------|------------------|---------------------|
| **MSI** | No | SmartScreen warning, but installs normally |
| **MSIX** | Yes (production) | Requires Developer Mode if unsigned |

Unsigned MSIX requires end-users to enable Developer Mode — not viable for general distribution. Code signing costs $100–400/year. MSI + WinGet is the practical choice for unsigned open-source distribution; WinGet installs provide inherent trust that reduces SmartScreen warnings.

### Why No Trimming?

Trimming requires `SelfContained=true` and fails with WinUI 3:
```
error IL2104: Assembly 'Microsoft.Web.WebView2.Core' produced trim warnings
error IL2104: Assembly 'WinRT.Runtime' produced trim warnings
error NETSDK1144: Optimizing assemblies for size failed
```
WinUI dependencies use reflection and dynamic code generation and aren't marked trim-safe. Accept the ~10 MB framework-dependent output as the minimum viable size.

References: [WindowsAppSDK #2478](https://github.com/microsoft/WindowsAppSDK/issues/2478), [.NET Trimming docs](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)

### WinGet Dependencies

```yaml
Dependencies:
  PackageDependencies:
    - PackageIdentifier: Microsoft.DotNet.DesktopRuntime.10
    - PackageIdentifier: Microsoft.WindowsAppRuntime.1.8
```
