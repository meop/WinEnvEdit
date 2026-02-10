using FluentAssertions;

using WinEnvEdit.Core.Services;
using WinEnvEdit.Tests.Builders;

using Xunit;

namespace WinEnvEdit.Tests.Services;

public class VariableFilterServiceTests {
  private readonly VariableFilterService service;

  public VariableFilterServiceTests() {
    service = new VariableFilterService();
  }

  #region Basic Filtering Tests

  [Fact]
  public void FilterVariables_NoFilters_ReturnsAll() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR3").Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, showVolatile: true, includeRemoved: true);

    // Assert
    result.Should().HaveCount(3);
  }

  [Fact]
  public void FilterVariables_EmptyList_ReturnsEmpty() {
    // Arrange
    var variables = new List<WinEnvEdit.Core.Models.EnvironmentVariableModel>();

    // Act
    var result = service.FilterVariables(variables);

    // Assert
    result.Should().BeEmpty();
  }

  #endregion

  #region Removed Filter Tests

  [Fact]
  public void FilterVariables_ExcludesRemovedByDefault() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    }.ToList();
    variables[1].IsRemoved = true;

    // Act
    var result = service.FilterVariables(variables);

    // Assert
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("VAR1");
  }

  [Fact]
  public void FilterVariables_IncludeRemoved_ReturnsAll() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    }.ToList();
    variables[1].IsRemoved = true;

    // Act
    var result = service.FilterVariables(variables, includeRemoved: true);

    // Assert
    result.Should().HaveCount(2);
  }

  #endregion

  #region Volatile Filter Tests

  [Fact]
  public void FilterVariables_ExcludesVolatileByDefault() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithIsVolatile(false).Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").WithIsVolatile(true).Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables);

    // Assert
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("VAR1");
  }

  [Fact]
  public void FilterVariables_ShowVolatile_ReturnsAll() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithIsVolatile(false).Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").WithIsVolatile(true).Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, showVolatile: true);

    // Assert
    result.Should().HaveCount(2);
  }

  #endregion

  #region Search Filter Tests

  [Fact]
  public void FilterVariables_SearchByName_ReturnsMatches() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("C:\\Windows").Build(),
      EnvironmentVariableBuilder.Default().WithName("TEMP").WithData("C:\\Temp").Build(),
      EnvironmentVariableBuilder.Default().WithName("HOME").WithData("C:\\Users").Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, searchText: "PATH");

    // Assert
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("PATH");
  }

  [Fact]
  public void FilterVariables_SearchByValue_ReturnsMatches() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("C:\\Windows").Build(),
      EnvironmentVariableBuilder.Default().WithName("TEMP").WithData("C:\\Temp").Build(),
      EnvironmentVariableBuilder.Default().WithName("HOME").WithData("C:\\Windows\\System32").Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, searchText: "Windows");

    // Assert
    result.Should().HaveCount(2);
    result[0].Name.Should().Be("PATH");
    result[1].Name.Should().Be("HOME");
  }

  [Fact]
  public void FilterVariables_SearchCaseInsensitive() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("C:\\Windows").Build(),
      EnvironmentVariableBuilder.Default().WithName("temp").WithData("C:\\Temp").Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, searchText: "path");

    // Assert
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("PATH");
  }

  [Fact]
  public void FilterVariables_SearchNoMatches_ReturnsEmpty() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("C:\\Windows").Build(),
      EnvironmentVariableBuilder.Default().WithName("TEMP").WithData("C:\\Temp").Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, searchText: "NOTFOUND");

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void FilterVariables_SearchEmptyString_ReturnsAll() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, searchText: "");

    // Assert
    result.Should().HaveCount(2);
  }

  [Fact]
  public void FilterVariables_SearchWhitespace_ReturnsAll() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, searchText: "   ");

    // Assert
    result.Should().HaveCount(2);
  }

  [Fact]
  public void FilterVariables_SearchTrimsWhitespace() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("C:\\Windows").Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, searchText: "  PATH  ");

    // Assert
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("PATH");
  }

  #endregion

  #region Combined Filter Tests

  [Fact]
  public void FilterVariables_CombinedFilters_AppliesAll() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("C:\\Windows").WithIsVolatile(false).Build(),
      EnvironmentVariableBuilder.Default().WithName("TEMP").WithData("C:\\Temp").WithIsVolatile(true).Build(),
      EnvironmentVariableBuilder.Default().WithName("HOME").WithData("C:\\Users").WithIsVolatile(false).Build(),
    }.ToList();
    variables[2].IsRemoved = true;

    // Act - search for "Path" without volatile or removed
    var result = service.FilterVariables(variables, searchText: "Path");

    // Assert
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("PATH");
  }

  [Fact]
  public void FilterVariables_SearchAndShowVolatile() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("C:\\Windows").WithIsVolatile(false).Build(),
      EnvironmentVariableBuilder.Default().WithName("PATHEXT").WithData(".exe").WithIsVolatile(true).Build(),
      EnvironmentVariableBuilder.Default().WithName("TEMP").WithData("C:\\Temp").WithIsVolatile(false).Build(),
    }.ToList();

    // Act
    var result = service.FilterVariables(variables, searchText: "PATH", showVolatile: true);

    // Assert
    result.Should().HaveCount(2);
    result[0].Name.Should().Be("PATH");
    result[1].Name.Should().Be("PATHEXT");
  }

  [Fact]
  public void FilterVariables_AllFiltersEnabled_ReturnsAll() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("C:\\Windows").WithIsVolatile(false).Build(),
      EnvironmentVariableBuilder.Default().WithName("TEMP").WithData("C:\\Temp").WithIsVolatile(true).Build(),
      EnvironmentVariableBuilder.Default().WithName("HOME").WithData("C:\\Users").WithIsVolatile(false).Build(),
    }.ToList();
    variables[2].IsRemoved = true;

    // Act
    var result = service.FilterVariables(variables, showVolatile: true, includeRemoved: true);

    // Assert
    result.Should().HaveCount(3);
  }

  #endregion
}
