using Microsoft.UI.Xaml.Markup;

using WinEnvEdit.Core.Constants;

namespace WinEnvEdit.Extensions;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public partial class GlyphExtension : MarkupExtension {
  public string Name { get; set; } = string.Empty;

  // XAML parser requires an explicit parameterless constructor on MarkupExtension subclasses.
  public GlyphExtension() {
  }

  // Compile-time name -> glyph map (a reflection lookup over Glyph's const fields is not trim/AOT safe).
  protected override object ProvideValue() => Name switch {
    nameof(Glyph.Refresh) => Glyph.Refresh,
    nameof(Glyph.Save) => Glyph.Save,
    nameof(Glyph.Undo) => Glyph.Undo,
    nameof(Glyph.Redo) => Glyph.Redo,
    nameof(Glyph.Export) => Glyph.Export,
    nameof(Glyph.Import) => Glyph.Import,
    nameof(Glyph.Add) => Glyph.Add,
    nameof(Glyph.Remove) => Glyph.Remove,
    nameof(Glyph.GripperBarHorizontal) => Glyph.GripperBarHorizontal,
    nameof(Glyph.AddTo) => Glyph.AddTo,
    nameof(Glyph.RemoveFrom) => Glyph.RemoveFrom,
    nameof(Glyph.Search) => Glyph.Search,
    nameof(Glyph.ChevronUp) => Glyph.ChevronUp,
    nameof(Glyph.ChevronDown) => Glyph.ChevronDown,
    nameof(Glyph.View) => Glyph.View,
    nameof(Glyph.Hide) => Glyph.Hide,
    nameof(Glyph.Help) => Glyph.Help,
    _ => string.Empty,
  };
}
