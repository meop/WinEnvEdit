using System.Reflection;

using Microsoft.UI.Xaml.Markup;

namespace WinEnvEdit.Extensions;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public partial class GlyphExtension : MarkupExtension {
  public string Name { get; set; } = string.Empty;

  // XAML parser requires an explicit parameterless constructor on MarkupExtension subclasses.
  public GlyphExtension() {
  }

  protected override object ProvideValue() {
    if (string.IsNullOrEmpty(Name)) {
      return string.Empty;
    }

    return typeof(Glyph).GetField(Name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null) as string ?? string.Empty;
  }
}
