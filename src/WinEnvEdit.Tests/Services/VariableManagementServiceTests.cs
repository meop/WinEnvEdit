using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Core.Services;
using WinEnvEdit.Core.Types;
using WinEnvEdit.Tests.Builders;

using Xunit;

namespace WinEnvEdit.Tests.Services;

public class VariableManagementServiceTests {
  private readonly VariableManagementService service;
  private readonly VariableCollectionService collectionService;

  public VariableManagementServiceTests() {
    collectionService = new VariableCollectionService();
    service = new VariableManagementService(collectionService);
  }

  #region AddOrUpdateVariable Tests

  [Fact]
  public void AddOrUpdateVariable_NewVariable_Adds() {
    // Arrange
    var variables = new List<WinEnvEdit.Core.Models.EnvironmentVariableModel>();

    // Act
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "NEW_VAR",
      "value",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.Added);
    variable.Should().NotBeNull();
    variable!.Name.Should().Be("NEW_VAR");
    variable.Data.Should().Be("value");
    variable.IsAdded.Should().BeTrue();
    variable.IsRemoved.Should().BeFalse();
    variables.Should().HaveCount(1);
    variables[0].Should().Be(variable);
  }

  [Fact]
  public void AddOrUpdateVariable_NewVariable_InsertsSorted() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("AAA").Build(),
      EnvironmentVariableBuilder.Default().WithName("CCC").Build(),
    }.ToList();

    // Act
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "BBB",
      "value",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.Added);
    variables.Should().HaveCount(3);
    variables[0].Name.Should().Be("AAA");
    variables[1].Name.Should().Be("BBB");
    variables[2].Name.Should().Be("CCC");
  }

  [Fact]
  public void AddOrUpdateVariable_ExistingActive_UpdatesData() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("old").Build(),
    }.ToList();

    // Act
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "VAR1",
      "new",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.Updated);
    variable.Should().NotBeNull();
    variable!.Data.Should().Be("new");
    variables.Should().HaveCount(1);
  }

  [Fact]
  public void AddOrUpdateVariable_ExistingWithSameData_NoAction() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("same").Build(),
    }.ToList();

    // Act
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "VAR1",
      "same",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.NoAction);
    variable.Should().BeNull();
  }

  [Fact]
  public void AddOrUpdateVariable_ExistingVolatile_NoAction() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("old").WithIsVolatile(true).Build(),
    }.ToList();

    // Act
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "VAR1",
      "new",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.NoAction);
    variable.Should().BeNull();
    variables[0].Data.Should().Be("old");
  }

  [Fact]
  public void AddOrUpdateVariable_DeletedVariable_Restores() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("old").Build(),
    }.ToList();
    variables[0].IsRemoved = true;

    // Act
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "VAR1",
      "new",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.Restored);
    variable.Should().NotBeNull();
    variable!.IsRemoved.Should().BeFalse();
    variable.Data.Should().Be("new");
    variables.Should().HaveCount(1);
  }

  [Fact]
  public void AddOrUpdateVariable_DeletedWithSameData_Restores() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("same").Build(),
    }.ToList();
    variables[0].IsRemoved = true;

    // Act
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "VAR1",
      "same",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.Restored);
    variable.Should().NotBeNull();
    variable!.IsRemoved.Should().BeFalse();
    variable.Data.Should().Be("same");
  }

  [Fact]
  public void AddOrUpdateVariable_CaseInsensitiveMatch() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").WithData("old").Build(),
    }.ToList();

    // Act
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "path",
      "new",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.Updated);
    variable.Should().NotBeNull();
    variable!.Name.Should().Be("PATH"); // Original name preserved
    variable.Data.Should().Be("new");
  }

  [Fact]
  public void AddOrUpdateVariable_PreservesOriginalType() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("old").WithType(RegistryValueKind.ExpandString).Build(),
    }.ToList();

    // Act - Try to update with different type
    var (result, variable) = service.AddOrUpdateVariable(
      variables,
      "VAR1",
      "new",
      RegistryValueKind.String,
      VariableScope.User);

    // Assert
    result.Should().Be(IVariableManagementService.AddOrUpdateResult.Updated);
    variable!.Type.Should().Be(RegistryValueKind.ExpandString); // Original type preserved
  }

  #endregion

  #region RemoveVariable Tests

  [Fact]
  public void RemoveVariable_NewlyAdded_RemovesFromList() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    }.ToList();
    variables[0].IsAdded = true;

    // Act
    var removed = service.RemoveVariable(variables, variables[0]);

    // Assert
    removed.Should().BeTrue();
    variables.Should().BeEmpty();
  }

  [Fact]
  public void RemoveVariable_Existing_MarksAsRemoved() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    }.ToList();
    variables[0].IsAdded = false;

    // Act
    var removed = service.RemoveVariable(variables, variables[0]);

    // Assert
    removed.Should().BeFalse();
    variables.Should().HaveCount(1);
    variables[0].IsRemoved.Should().BeTrue();
  }

  #endregion

  #region RemoveVariablesNotIn Tests

  [Fact]
  public void RemoveVariablesNotIn_RemovesUnlistedVariables() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("KEEP1").Build(),
      EnvironmentVariableBuilder.Default().WithName("REMOVE1").Build(),
      EnvironmentVariableBuilder.Default().WithName("KEEP2").Build(),
      EnvironmentVariableBuilder.Default().WithName("REMOVE2").Build(),
    }.ToList();

    var namesToKeep = new HashSet<string> { "KEEP1", "KEEP2" };

    // Act
    var count = service.RemoveVariablesNotIn(variables, namesToKeep);

    // Assert
    count.Should().Be(2);
    variables[1].IsRemoved.Should().BeTrue(); // REMOVE1 marked
    variables[3].IsRemoved.Should().BeTrue(); // REMOVE2 marked
  }

  [Fact]
  public void RemoveVariablesNotIn_SkipsVolatileVariables() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("KEEP").Build(),
      EnvironmentVariableBuilder.Default().WithName("REMOVE_VOLATILE").WithIsVolatile(true).Build(),
      EnvironmentVariableBuilder.Default().WithName("REMOVE").Build(),
    }.ToList();

    var namesToKeep = new HashSet<string> { "KEEP" };

    // Act
    var count = service.RemoveVariablesNotIn(variables, namesToKeep);

    // Assert
    count.Should().Be(1);
    variables[1].IsRemoved.Should().BeFalse(); // Volatile not marked
    variables[2].IsRemoved.Should().BeTrue(); // Non-volatile marked
  }

  [Fact]
  public void RemoveVariablesNotIn_SkipsAlreadyRemoved() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("KEEP").Build(),
      EnvironmentVariableBuilder.Default().WithName("ALREADY_REMOVED").Build(),
    }.ToList();
    variables[1].IsRemoved = true;

    var namesToKeep = new HashSet<string> { "KEEP" };

    // Act
    var count = service.RemoveVariablesNotIn(variables, namesToKeep);

    // Assert
    count.Should().Be(0); // Already removed, not counted
  }

  [Fact]
  public void RemoveVariablesNotIn_CaseInsensitive() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("PATH").Build(),
      EnvironmentVariableBuilder.Default().WithName("TEMP").Build(),
    }.ToList();

    var namesToKeep = new HashSet<string> { "path" }; // Lowercase

    // Act
    var count = service.RemoveVariablesNotIn(variables, namesToKeep);

    // Assert
    count.Should().Be(1);
    variables[0].IsRemoved.Should().BeFalse(); // PATH kept (case-insensitive match)
    variables[1].IsRemoved.Should().BeTrue(); // TEMP removed
  }

  [Fact]
  public void RemoveVariablesNotIn_EmptyKeepSet_RemovesAll() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    }.ToList();

    var namesToKeep = new HashSet<string>();

    // Act
    var count = service.RemoveVariablesNotIn(variables, namesToKeep);

    // Assert
    count.Should().Be(2);
    variables[0].IsRemoved.Should().BeTrue();
    variables[1].IsRemoved.Should().BeTrue();
  }

  #endregion

  #region CleanupAfterSave Tests

  [Fact]
  public void CleanupAfterSave_RemovesMarkedVariables() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("KEEP").Build(),
      EnvironmentVariableBuilder.Default().WithName("REMOVE").Build(),
    }.ToList();
    variables[1].IsRemoved = true;

    // Act
    var count = service.CleanupAfterSave(variables);

    // Assert
    count.Should().Be(1);
    variables.Should().HaveCount(1);
    variables[0].Name.Should().Be("KEEP");
  }

  [Fact]
  public void CleanupAfterSave_ClearsIsAddedFlags() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    }.ToList();
    variables[0].IsAdded = true;
    variables[1].IsAdded = true;

    // Act
    var count = service.CleanupAfterSave(variables);

    // Assert
    count.Should().Be(0); // No removals
    variables.Should().HaveCount(2);
    variables[0].IsAdded.Should().BeFalse();
    variables[1].IsAdded.Should().BeFalse();
  }

  [Fact]
  public void CleanupAfterSave_BothRemovesAndClearsFlags() {
    // Arrange
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("NEW").Build(),
      EnvironmentVariableBuilder.Default().WithName("DELETED").Build(),
      EnvironmentVariableBuilder.Default().WithName("EXISTING").Build(),
    }.ToList();
    variables[0].IsAdded = true;
    variables[1].IsRemoved = true;
    variables[2].IsAdded = false;

    // Act
    var count = service.CleanupAfterSave(variables);

    // Assert
    count.Should().Be(1); // One removed
    variables.Should().HaveCount(2);
    variables[0].Name.Should().Be("NEW");
    variables[0].IsAdded.Should().BeFalse();
    variables[1].Name.Should().Be("EXISTING");
    variables[1].IsAdded.Should().BeFalse();
  }

  [Fact]
  public void CleanupAfterSave_EmptyList_NoErrors() {
    // Arrange
    var variables = new List<WinEnvEdit.Core.Models.EnvironmentVariableModel>();

    // Act
    var count = service.CleanupAfterSave(variables);

    // Assert
    count.Should().Be(0);
    variables.Should().BeEmpty();
  }

  #endregion
}
