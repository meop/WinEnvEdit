using FluentAssertions;

using WinEnvEdit.Core.Constants;

using Xunit;

namespace WinEnvEdit.Tests.Extensions;

public class GlyphTests {
  [Fact]
  public void Glyph_ConstantsAreDefined() {
    // Assert - Verify that key glyph constants exist and are non-empty
    Glyph.Refresh.Should().NotBeNullOrEmpty();
    Glyph.Save.Should().NotBeNullOrEmpty();
    Glyph.Undo.Should().NotBeNullOrEmpty();
    Glyph.Redo.Should().NotBeNullOrEmpty();
    Glyph.Add.Should().NotBeNullOrEmpty();
    Glyph.Remove.Should().NotBeNullOrEmpty();
    Glyph.View.Should().NotBeNullOrEmpty();
    Glyph.Hide.Should().NotBeNullOrEmpty();
    Glyph.ChevronDown.Should().NotBeNullOrEmpty();
    Glyph.ChevronUp.Should().NotBeNullOrEmpty();
    Glyph.Import.Should().NotBeNullOrEmpty();
    Glyph.Export.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void Glyph_Constants_AreNotEqual() {
    // Assert - Different glyphs should have different values
    Glyph.ChevronDown.Should().NotBe(Glyph.ChevronUp);
    Glyph.View.Should().NotBe(Glyph.Hide);
    Glyph.Add.Should().NotBe(Glyph.Remove);
    Glyph.Save.Should().NotBe(Glyph.Refresh);
  }
}
