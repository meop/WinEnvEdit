using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Models;
using WinEnvEdit.Tests.Helpers;

namespace WinEnvEdit.Tests.Models;

[TestClass]
public class EnvironmentVariableTests {
  [TestMethod]
  public void Properties_CanBeSet() {
    // Arrange & Act
    var variable = new EnvironmentVariable {
      Name = "TEST_VAR",
      Data = "test_value",
      Type = RegistryValueKind.String,
      IsRemoved = true,
    };

    // Assert
    variable.Name.Should().Be("TEST_VAR");
    variable.Data.Should().Be("test_value");
    variable.Type.Should().Be(RegistryValueKind.String);
    variable.IsRemoved.Should().BeTrue();
  }

  [TestMethod]
  public void IsRemoved_InitializesFalse() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("value")
      .Build();

    // Act & Assert
    variable.IsRemoved.Should().BeFalse();
  }

  [TestMethod]
  public void IsVolatile_WhenSet_ReturnsSetValue() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("value")
      .WithIsVolatile(true)
      .Build();

    // Act & Assert
    variable.IsVolatile.Should().BeTrue();
  }

  [TestMethod]
  public void IsAdded_InitializesFalse() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("value")
      .Build();

    // Act & Assert
    variable.IsAdded.Should().BeFalse();
  }

  [TestMethod]
  public void Type_DefaultsToString() {
    // Arrange
    var variable = new EnvironmentVariable {
      Name = "TEST",
      Data = "value",
    };

    // Act & Assert
    variable.Type.Should().Be(RegistryValueKind.String);
  }

  [TestMethod]
  public void Scope_CanBeUserOrSystem() {
    // Arrange
    var userVar = EnvironmentVariableBuilder.Default()
      .WithName("USER_VAR")
      .WithData("value")
      .WithScope(VariableScope.User)
      .Build();

    var systemVar = EnvironmentVariableBuilder.Default()
      .WithName("SYSTEM_VAR")
      .WithData("value")
      .WithScope(VariableScope.System)
      .Build();

    // Act & Assert
    userVar.Scope.Should().Be(VariableScope.User);
    systemVar.Scope.Should().Be(VariableScope.System);
  }
}

[TestClass]
public class VariableScopeTests {
  [TestMethod]
  public void VariableScope_UserAndSystemAreDifferent() {
    // Assert
    VariableScope.User.Should().NotBe(VariableScope.System);
  }

}
