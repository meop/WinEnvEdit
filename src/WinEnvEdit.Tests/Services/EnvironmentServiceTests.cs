using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Models;
using WinEnvEdit.Services;

namespace WinEnvEdit.Tests.Services;

[TestClass]
public class EnvironmentServiceTests {
  private EnvironmentService service = null!;

  [TestInitialize]
  public void Setup() {
    service = new EnvironmentService();
  }

  #region GetVariables Tests

  [TestMethod]
  public void GetVariables_ReturnsVariables() {
    // Act
    var variables = service.GetVariables();

    // Assert
    variables.Should().NotBeEmpty("Windows should have environment variables");
  }

  [TestMethod]
  public void GetVariables_ContainsBothSystemAndUserScope() {
    // Act
    var variables = service.GetVariables().ToList();

    // Assert
    variables.Should().Contain(v => v.Scope == VariableScope.System);
    variables.Should().Contain(v => v.Scope == VariableScope.User);
  }

  [TestMethod]
  public void GetVariables_VariablesHaveRequiredProperties() {
    // Act
    var variables = service.GetVariables().ToList();

    // Assert
    foreach (var variable in variables) {
      variable.Name.Should().NotBeNullOrEmpty();
      variable.Scope.Should().BeOneOf(VariableScope.System, VariableScope.User);
      variable.Type.Should().BeOneOf(RegistryValueKind.String, RegistryValueKind.ExpandString, RegistryValueKind.DWord, RegistryValueKind.MultiString);
    }
  }

  #endregion

  #region PowerShell Script Generation Tests

  // Note: AddVariableToScript is private and takes complex parameters (List<string>, registryPath)
  // The actual script generation is internal implementation and tested implicitly via SaveVariables
  // Cannot directly unit test private method without refactoring for testability

  #endregion
}
