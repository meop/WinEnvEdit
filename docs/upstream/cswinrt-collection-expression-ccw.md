# Collection-expression argument to a WinRT IList<T> parameter has no CCW under AOT (works with `new List<T>`)

- **Target repo:** microsoft/CsWinRT
- **Kind:** bug report (or optimizer enhancement) + minimal repro
- **Confidence:** Medium-High
- **Status:** filed as [microsoft/CsWinRT#2475](https://github.com/microsoft/CsWinRT/issues/2475) — **closed as expected.** Upstream
  added an analyzer that warns on these call sites in CsWinRT 2.3, and says the pattern will actually work in
  CsWinRT 3.0. Windows App SDK 2.3.2 still resolves to CsWinRT 2.2 (`WinRT.Runtime.dll` 2.2.0), so we get
  neither the warning nor the fix yet — **keep the workaround.**

## Summary

Passing a **collection expression** (`[x]`) where a WinRT API expects `IList<T>` throws at runtime under AOT:

```
System.InvalidCastException: Failed to create a CCW for object of type
'System.Collections.Generic.List`1[System.String]' for interface with IID '…' (IVector<String>).
```

Passing the **same value via `new List<string> { x }`** works. The CsWinRT AOT optimizer appears to emit the
CCW vtable for the recognizable object-creation expression but not for the collection-expression lowering.

## Environment

Windows 11 25H2 · .NET 10 · Windows App SDK 2.3.x · `PublishAot=true` · `CsWinRTAotOptimizerEnabled=true` · x64.
Originally reported and re-confirmed on Windows App SDK 2.2 (the collection-expression form throws; `new List<string>` works).
Windows App SDK 2.3.2 still brings CsWinRT 2.2, so nothing has changed for us on the 2.3 line either.

## Minimal repro

```csharp
var picker = new FileSavePicker(AppWindow.Id);
// throws under AOT:
picker.FileTypeChoices.Add("TOML", [".toml"]);
// works under AOT:
picker.FileTypeChoices.Add("TOML", new List<string> { ".toml" });
```
`FileTypeChoices` is `IDictionary<string, IList<string>>`; the value crosses the WinRT ABI as `IVector<string>`.

## Workaround in WinEnvEdit

Use the explicit `new List<string> { extension }` form — `WinEnvEdit/Services/DialogService.cs` (`PickSaveFile`).

## Could this be working-as-designed?

The optimizer's vtable discovery is documented as static analysis; it may simply not recognize the
collection-expression lowering as a "type used across the ABI." Could be deemed an optimizer limitation rather
than a bug — but it's a sharp, surprising edge (the two forms are semantically identical) worth either fixing
or calling out in the AOT docs.

## The ask

Recognize collection-expression results in the CCW vtable analysis, or document that collection expressions
should not be passed directly to WinRT `IList<T>`/`IVector<T>` parameters under AOT.
