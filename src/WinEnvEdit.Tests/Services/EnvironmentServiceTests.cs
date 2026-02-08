using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Models;
using WinEnvEdit.Services;

namespace WinEnvEdit.Tests.Services;

[TestClass]
public class EnvironmentServiceTests {
  #region GetAndSortVariables Tests

  [TestMethod]
  public void GetAndSortVariables_CombinesAndSortsCorrectly() {
    // Arrange
    var persistent = new List<EnvironmentVariable> {
      new() { Name = "Z_VAR", Data = "z", Scope = VariableScope.System },
      new() { Name = "A_VAR", Data = "a", Scope = VariableScope.User }
    };
    var volatileVars = new List<EnvironmentVariable> {
      new() { Name = "M_VAR", Data = "m", Scope = VariableScope.User, IsVolatile = true }
    };

    // Act
    var result = EnvironmentService.GetAndSortVariables(persistent, volatileVars);

    // Assert
    result.Count.Should().Be(3);
    result[0].Name.Should().Be("A_VAR");
    result[1].Name.Should().Be("M_VAR");
    result[2].Name.Should().Be("Z_VAR");
  }

  #endregion

  #region CreateEnvironmentVariable Tests

  [TestMethod]
  public void CreateEnvironmentVariable_SetsInitialStateCorrectly() {
    // Act
    var variable = EnvironmentService.CreateEnvironmentVariable("NAME", "DATA", VariableScope.User, RegistryValueKind.String, isVolatile: true);

    // Assert
    variable.Name.Should().Be("NAME");
    variable.Data.Should().Be("DATA");
    variable.Scope.Should().Be(VariableScope.User);
    variable.Type.Should().Be(RegistryValueKind.String);
    variable.IsVolatile.Should().BeTrue();
    variable.IsAdded.Should().BeFalse();
    variable.IsRemoved.Should().BeFalse();
  }

  #endregion
}
