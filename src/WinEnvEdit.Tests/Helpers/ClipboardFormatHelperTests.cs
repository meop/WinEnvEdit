using FluentAssertions;

using WinEnvEdit.Core.Helpers;

using Xunit;

namespace WinEnvEdit.Tests.Helpers;

public class ClipboardFormatHelperTests {
  #region ParseSingleLine Tests

  [Fact]
  public void ParseSingleLine_ValidFormat_ReturnsNameAndValue() {
    // Act
    var result = ClipboardFormatHelper.ParseSingleLine("TEST_VAR=test_value");

    // Assert
    result.Name.Should().Be("TEST_VAR");
    result.Value.Should().Be("test_value");
  }

  [Fact]
  public void ParseSingleLine_WithSpaces_TrimsBoth() {
    // Act
    var result = ClipboardFormatHelper.ParseSingleLine("  TEST_VAR  =  test_value  ");

    // Assert
    result.Name.Should().Be("TEST_VAR");
    result.Value.Should().Be("test_value");
  }

  [Fact]
  public void ParseSingleLine_NoEquals_ReturnsEmptyNameAndFullValue() {
    // Act
    var result = ClipboardFormatHelper.ParseSingleLine("just_value");

    // Assert
    result.Name.Should().BeEmpty();
    result.Value.Should().Be("just_value");
  }

  [Fact]
  public void ParseSingleLine_EqualsAtStart_ReturnsEmptyNameAndFullValue() {
    // Act
    var result = ClipboardFormatHelper.ParseSingleLine("=value");

    // Assert
    result.Name.Should().BeEmpty();
    result.Value.Should().Be("=value");
  }

  [Fact]
  public void ParseSingleLine_EmptyString_ReturnsBothEmpty() {
    // Act
    var result = ClipboardFormatHelper.ParseSingleLine("");

    // Assert
    result.Name.Should().BeEmpty();
    result.Value.Should().BeEmpty();
  }

  [Fact]
  public void ParseSingleLine_WhitespaceOnly_ReturnsBothEmpty() {
    // Act
    var result = ClipboardFormatHelper.ParseSingleLine("   ");

    // Assert
    result.Name.Should().BeEmpty();
    result.Value.Should().BeEmpty();
  }

  [Fact]
  public void ParseSingleLine_ValueContainsEquals_ReturnsCorrectly() {
    // Act
    var result = ClipboardFormatHelper.ParseSingleLine("NAME=value=with=equals");

    // Assert
    result.Name.Should().Be("NAME");
    result.Value.Should().Be("value=with=equals");
  }

  [Fact]
  public void ParseSingleLine_EmptyValue_ReturnsEmptyValue() {
    // Act
    var result = ClipboardFormatHelper.ParseSingleLine("NAME=");

    // Assert
    result.Name.Should().Be("NAME");
    result.Value.Should().BeEmpty();
  }

  #endregion

  #region ParseMultiLine Tests

  [Fact]
  public void ParseMultiLine_ValidLines_ReturnsAll() {
    // Arrange
    var text = "VAR1=value1\r\nVAR2=value2\r\nVAR3=value3";

    // Act
    var result = ClipboardFormatHelper.ParseMultiLine(text);

    // Assert
    result.Should().HaveCount(3);
    result[0].Should().Be(("VAR1", "value1"));
    result[1].Should().Be(("VAR2", "value2"));
    result[2].Should().Be(("VAR3", "value3"));
  }

  [Fact]
  public void ParseMultiLine_MixedNewlines_Handles() {
    // Arrange
    var text = "VAR1=value1\nVAR2=value2\rVAR3=value3";

    // Act
    var result = ClipboardFormatHelper.ParseMultiLine(text);

    // Assert
    result.Should().HaveCount(3);
  }

  [Fact]
  public void ParseMultiLine_SkipsInvalidLines_ReturnsOnlyValid() {
    // Arrange
    var text = "VAR1=value1\r\ninvalid_line\r\nVAR2=value2\r\n=no_name\r\nVAR3=value3";

    // Act
    var result = ClipboardFormatHelper.ParseMultiLine(text);

    // Assert
    result.Should().HaveCount(3);
    result[0].Should().Be(("VAR1", "value1"));
    result[1].Should().Be(("VAR2", "value2"));
    result[2].Should().Be(("VAR3", "value3"));
  }

  [Fact]
  public void ParseMultiLine_EmptyString_ReturnsEmpty() {
    // Act
    var result = ClipboardFormatHelper.ParseMultiLine("");

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void ParseMultiLine_TrimsNamesAndValues() {
    // Arrange
    var text = "  VAR1  =  value1  \r\n  VAR2  =  value2  ";

    // Act
    var result = ClipboardFormatHelper.ParseMultiLine(text);

    // Assert
    result.Should().HaveCount(2);
    result[0].Should().Be(("VAR1", "value1"));
    result[1].Should().Be(("VAR2", "value2"));
  }

  [Fact]
  public void ParseMultiLine_SkipsEmptyNameLines() {
    // Arrange
    var text = "VAR1=value1\r\n  =value2\r\nVAR3=value3";

    // Act
    var result = ClipboardFormatHelper.ParseMultiLine(text);

    // Assert
    result.Should().HaveCount(2);
    result[0].Should().Be(("VAR1", "value1"));
    result[1].Should().Be(("VAR3", "value3"));
  }

  #endregion
}
