using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Tests.Helpers;

using Xunit;

namespace WinEnvEdit.Tests.Services;

public class VariableCollectionServiceTests {
  private readonly VariableCollectionService service;

  public VariableCollectionServiceTests() {
    service = new VariableCollectionService();
  }

  #region HasChanged Tests

  [Fact]
  public void HasChanged_SameLists_ReturnsFalse() {
    // Arrange
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("value1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").WithData("value2").Build(),
    };

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("value1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").WithData("value2").Build(),
    };

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void HasChanged_DifferentCounts_ReturnsTrue() {
    // Arrange
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    };

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void HasChanged_DifferentData_ReturnsTrue() {
    // Arrange
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("value1").Build(),
    };

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("value2").Build(),
    };

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void HasChanged_DifferentType_ReturnsTrue() {
    // Arrange
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithType(RegistryValueKind.String).Build(),
    };

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithType(RegistryValueKind.ExpandString).Build(),
    };

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void HasChanged_DifferentIsAdded_ReturnsTrue() {
    // Arrange
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };
    list1[0].IsAdded = false;

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };
    list2[0].IsAdded = true;

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void HasChanged_DifferentIsRemoved_ReturnsTrue() {
    // Arrange
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };
    list1[0].IsRemoved = false;

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };
    list2[0].IsRemoved = true;

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void HasChanged_DifferentIsVolatile_ReturnsTrue() {
    // Arrange
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithIsVolatile(false).Build(),
    };

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithIsVolatile(true).Build(),
    };

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void HasChanged_UnsortedLists_HandlesCorrectly() {
    // Arrange - different order but same content
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("value1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").WithData("value2").Build(),
    };

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR2").WithData("value2").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("value1").Build(),
    };

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void HasChanged_DifferentNames_ReturnsTrue() {
    // Arrange
    var list1 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };

    var list2 = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    };

    // Act
    var result = service.HasChanged(list1, list2);

    // Assert
    result.Should().BeTrue();
  }

  #endregion

  #region SortVariables Tests

  [Fact]
  public void SortVariables_UnsortedList_ReturnsSorted() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("ZEBRA").Build(),
      EnvironmentVariableBuilder.Default().WithName("APPLE").Build(),
      EnvironmentVariableBuilder.Default().WithName("BANANA").Build(),
    };

    // Act
    var result = service.SortVariables(variables);

    // Assert
    result.Should().HaveCount(3);
    result[0].Name.Should().Be("APPLE");
    result[1].Name.Should().Be("BANANA");
    result[2].Name.Should().Be("ZEBRA");
  }

  [Fact]
  public void SortVariables_CaseInsensitive_SortsCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("zebra").Build(),
      EnvironmentVariableBuilder.Default().WithName("APPLE").Build(),
      EnvironmentVariableBuilder.Default().WithName("BaNaNa").Build(),
    };

    // Act
    var result = service.SortVariables(variables);

    // Assert
    result.Should().HaveCount(3);
    result[0].Name.Should().Be("APPLE");
    result[1].Name.Should().Be("BaNaNa");
    result[2].Name.Should().Be("zebra");
  }

  #endregion

  #region FindVariable Tests

  [Fact]
  public void FindVariable_ExistingVariable_ReturnsVariable() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    };

    // Act
    var result = service.FindVariable(variables, "VAR2");

    // Assert
    result.Should().NotBeNull();
    result!.Name.Should().Be("VAR2");
  }

  [Fact]
  public void FindVariable_CaseInsensitive_ReturnsVariable() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };

    // Act
    var result = service.FindVariable(variables, "var1");

    // Assert
    result.Should().NotBeNull();
    result!.Name.Should().Be("VAR1");
  }

  [Fact]
  public void FindVariable_NotFound_ReturnsNull() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };

    // Act
    var result = service.FindVariable(variables, "VAR2");

    // Assert
    result.Should().BeNull();
  }

  #endregion

  #region FindInsertionIndex Tests

  [Fact]
  public void FindInsertionIndex_BeforeFirst_ReturnsZero() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("BANANA").Build(),
      EnvironmentVariableBuilder.Default().WithName("CHERRY").Build(),
    };

    // Act
    var result = service.FindInsertionIndex(variables, "APPLE");

    // Assert
    result.Should().Be(0);
  }

  [Fact]
  public void FindInsertionIndex_BetweenItems_ReturnsCorrectIndex() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("APPLE").Build(),
      EnvironmentVariableBuilder.Default().WithName("CHERRY").Build(),
    };

    // Act
    var result = service.FindInsertionIndex(variables, "BANANA");

    // Assert
    result.Should().Be(1);
  }

  [Fact]
  public void FindInsertionIndex_AfterLast_ReturnsCount() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("APPLE").Build(),
      EnvironmentVariableBuilder.Default().WithName("BANANA").Build(),
    };

    // Act
    var result = service.FindInsertionIndex(variables, "ZEBRA");

    // Assert
    result.Should().Be(2);
  }

  [Fact]
  public void FindInsertionIndex_EmptyList_ReturnsZero() {
    // Arrange
    var variables = new List<EnvironmentVariableModel>();

    // Act
    var result = service.FindInsertionIndex(variables, "APPLE");

    // Assert
    result.Should().Be(0);
  }

  [Fact]
  public void FindInsertionIndex_CaseInsensitive_WorksCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("apple").Build(),
      EnvironmentVariableBuilder.Default().WithName("CHERRY").Build(),
    };

    // Act
    var result = service.FindInsertionIndex(variables, "BANANA");

    // Assert
    result.Should().Be(1);
  }

  #endregion
}
