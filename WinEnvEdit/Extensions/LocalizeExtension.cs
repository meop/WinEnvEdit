using Microsoft.UI.Xaml.Markup;

using WinEnvEdit.Helpers;

namespace WinEnvEdit.Extensions;

// XAML-side counterpart to Localize.Get: {markup:Localize Key=SomeKey} resolves a resw string at parse time.
// Mirrors GlyphExtension, which is the AOT-proven markup-extension pattern in this app.
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public partial class LocalizeExtension : MarkupExtension {
  public string Key { get; set; } = string.Empty;

  // XAML parser requires an explicit parameterless constructor on MarkupExtension subclasses.
  public LocalizeExtension() {
  }

  protected override object ProvideValue() => Localize.Get(Key);
}
