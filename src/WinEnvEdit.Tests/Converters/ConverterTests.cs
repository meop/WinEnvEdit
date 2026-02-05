using System;

using FluentAssertions;

using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WinEnvEdit.Converters;
using WinEnvEdit.Extensions;

namespace WinEnvEdit.Tests.Converters;

[TestClass]
public class BoolToVisibilityConverterTests {
  private BoolToVisibilityConverter converter = null!;

  [TestInitialize]
  public void Setup() => converter = new BoolToVisibilityConverter();

  [TestMethod]
  public void Convert_True_InvertFalse_ReturnsVisible() {
    converter.Invert = false;
    var result = converter.Convert(true, typeof(Visibility), null!, string.Empty);
    result.Should().Be(Visibility.Visible);
  }

  [TestMethod]
  public void Convert_False_InvertFalse_ReturnsCollapsed() {
    converter.Invert = false;
    var result = converter.Convert(false, typeof(Visibility), null!, string.Empty);
    result.Should().Be(Visibility.Collapsed);
  }

  [TestMethod]
  public void Convert_True_InvertTrue_ReturnsCollapsed() {
    converter.Invert = true;
    var result = converter.Convert(true, typeof(Visibility), null!, string.Empty);
    result.Should().Be(Visibility.Collapsed);
  }

  [TestMethod]
  public void Convert_False_InvertTrue_ReturnsVisible() {
    converter.Invert = true;
    var result = converter.Convert(false, typeof(Visibility), null!, string.Empty);
    result.Should().Be(Visibility.Visible);
  }

  [TestMethod]
  public void Convert_NonBool_ReturnsCollapsed() {
    var result = converter.Convert("not a bool", typeof(Visibility), null!, string.Empty);
    result.Should().Be(Visibility.Collapsed);
  }

  [TestMethod]
  public void ConvertBack_Visible_InvertFalse_ReturnsTrue() {
    converter.Invert = false;
    var result = converter.ConvertBack(Visibility.Visible, typeof(bool), null!, string.Empty);
    result.Should().Be(true);
  }

  [TestMethod]
  public void ConvertBack_Collapsed_InvertFalse_ReturnsFalse() {
    converter.Invert = false;
    var result = converter.ConvertBack(Visibility.Collapsed, typeof(bool), null!, string.Empty);
    result.Should().Be(false);
  }
}

[TestClass]
public class BoolToVisibilityInverseConverterTests {
  [TestMethod]
  public void Constructor_SetsInvertToTrue() {
    var converter = new BoolToVisibilityInverseConverter();
    converter.Invert.Should().BeTrue();
  }

  [TestMethod]
  public void Convert_True_ReturnsCollapsed() {
    var converter = new BoolToVisibilityInverseConverter();
    var result = converter.Convert(true, typeof(Visibility), null!, string.Empty);
    result.Should().Be(Visibility.Collapsed);
  }
}

[TestClass]
public class ExpandChevronConverterTests {
  private ExpandChevronConverter converter = null!;

  [TestInitialize]
  public void Setup() => converter = new ExpandChevronConverter();

  [TestMethod]
  public void Convert_True_ReturnsChevronUp() {
    var result = converter.Convert(true, typeof(string), null!, string.Empty);
    result.Should().Be(Glyph.ChevronUp);
  }

  [TestMethod]
  public void Convert_False_ReturnsChevronDown() {
    var result = converter.Convert(false, typeof(string), null!, string.Empty);
    result.Should().Be(Glyph.ChevronDown);
  }

  [TestMethod]
  public void Convert_NonBool_ReturnsChevronDown() {
    var result = converter.Convert("not a bool", typeof(string), null!, string.Empty);
    result.Should().Be(Glyph.ChevronDown);
  }

  [TestMethod]
  public void ConvertBack_ThrowsNotImplemented() {
    Action act = () => converter.ConvertBack(null!, typeof(bool), null!, string.Empty);
    act.Should().Throw<NotImplementedException>();
  }
}

// PathExistsToBrushConverter and BoolToErrorBrushConverter are not tested here.
// Both converters return SolidColorBrush which is a WinRT type requiring a running
// WinUI application context to construct. The converter logic itself is trivial
// (single conditional returning a brush or Transparent).

[TestClass]
public class PathExistsToBrushConverterTests {
  [TestMethod]
  public void ConvertBack_ThrowsNotImplemented() {
    var converter = new PathExistsToBrushConverter();
    Action act = () => converter.ConvertBack(null!, typeof(bool), null!, string.Empty);
    act.Should().Throw<NotImplementedException>();
  }
}

[TestClass]
public class BoolToErrorBrushConverterTests {
  [TestMethod]
  public void ConvertBack_ThrowsNotImplemented() {
    var converter = new BoolToErrorBrushConverter();
    Action act = () => converter.ConvertBack(null!, typeof(bool), null!, string.Empty);
    act.Should().Throw<NotImplementedException>();
  }
}
