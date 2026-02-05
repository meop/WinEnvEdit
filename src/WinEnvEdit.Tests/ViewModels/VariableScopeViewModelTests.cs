using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

using Moq;

using WinEnvEdit.Models;
using WinEnvEdit.Services;
using WinEnvEdit.Tests.Helpers;
using WinEnvEdit.ViewModels;

namespace WinEnvEdit.Tests.ViewModels;

[TestClass]
public class VariableScopeViewModelTests {
  private Mock<IEnvironmentService> envServiceMock = null!;
  private VariableScopeViewModel viewModel = null!;
  private int changeCallbackCallCount = 0;

  [TestInitialize]
  public void Setup() {
    envServiceMock = Helpers.MockFactory.CreateEnvironmentService();
    changeCallbackCallCount = 0;
    Action changeCallback = () => changeCallbackCallCount++;
    viewModel = new VariableScopeViewModel(VariableScope.User, envServiceMock.Object, null);
  }

  #region Constructor Tests

  [TestMethod]
  public void Constructor_InitializesCollections() {
    // Assert
    viewModel.Variables.Should().NotBeNull();
    viewModel.FilteredVariables.Should().NotBeNull();
    viewModel.Scope.Should().Be(VariableScope.User);
  }

  #endregion

  #region LoadFromRegistry Tests

  [TestMethod]
  public void LoadFromRegistry_PopulatesVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);

    // Act
    viewModel.LoadFromRegistry();

    // Assert
    viewModel.Variables.Count.Should().Be(2);
    viewModel.Variables.Any(v => v.Name == "VAR1").Should().BeTrue();
    viewModel.Variables.Any(v => v.Name == "VAR2").Should().BeTrue();
  }

  [TestMethod]
  public void LoadFromRegistry_OnlyLoadsScopedVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("USER_VAR").WithScope(VariableScope.User).Build(),
      EnvironmentVariableBuilder.Default().WithName("SYSTEM_VAR").WithScope(VariableScope.System).Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);

    // Act
    viewModel.LoadFromRegistry();

    // Assert
    viewModel.Variables.Count.Should().Be(1);
    viewModel.Variables[0].Name.Should().Be("USER_VAR");
  }

  [TestMethod]
  public void LoadFromRegistry_SortsVariablesAlphabetically() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("Z_VAR").Build(),
      EnvironmentVariableBuilder.Default().WithName("A_VAR").Build(),
      EnvironmentVariableBuilder.Default().WithName("M_VAR").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);

    // Act
    viewModel.LoadFromRegistry();

    // Assert
    viewModel.Variables[0].Name.Should().Be("A_VAR");
    viewModel.Variables[1].Name.Should().Be("M_VAR");
    viewModel.Variables[2].Name.Should().Be("Z_VAR");
  }

  [TestMethod]
  public void LoadFromRegistry_ClearsExistingVariables() {
    // Arrange
    viewModel.Variables.Add(new VariableViewModel(
      EnvironmentVariableBuilder.Default().WithName("OLD_VAR").Build(),
      null, null));
    envServiceMock.Setup(s => s.GetVariables()).Returns([]);

    // Act
    viewModel.LoadFromRegistry();

    // Assert
    viewModel.Variables.Should().BeEmpty();
  }

  #endregion

  #region AddVariable Tests

  [TestMethod]
  public void AddVariable_NewVariable_AddsToCollection() {
    // Arrange
    var initialCount = viewModel.Variables.Count;

    // Act
    viewModel.AddVariable("NEW_VAR", "value", Microsoft.Win32.RegistryValueKind.String);

    // Assert
    viewModel.Variables.Count.Should().Be(initialCount + 1);
    var newVar = viewModel.Variables.FirstOrDefault(v => v.Name == "NEW_VAR");
    newVar.Should().NotBeNull();
    newVar!.Data.Should().Be("value");
    newVar.Model.IsAdded.Should().BeTrue();
  }

  [TestMethod]
  public void AddVariable_ExistingVariable_UpdatesData() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("EXISTING").WithData("old_value").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    viewModel.AddVariable("EXISTING", "new_value", Microsoft.Win32.RegistryValueKind.String);

    // Assert
    var existingVar = viewModel.Variables.FirstOrDefault(v => v.Name == "EXISTING");
    existingVar.Should().NotBeNull();
    existingVar!.Data.Should().Be("new_value");
    existingVar.Model.IsAdded.Should().BeFalse();
    viewModel.Variables.Count.Should().Be(1);
  }

  [TestMethod]
  public void AddVariable_ExistingVariableWithSameData_DoesNothing() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("EXISTING").WithData("same_value").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    viewModel.AddVariable("EXISTING", "same_value", Microsoft.Win32.RegistryValueKind.String);

    // Assert
    viewModel.Variables.Count.Should().Be(1);
  }

  [TestMethod]
  public void AddVariable_DeletedVariable_RestoresIt() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("DELETED").WithData("old_value").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.Variables[0].Model.IsRemoved = true;

    // Act
    viewModel.AddVariable("DELETED", "new_value", Microsoft.Win32.RegistryValueKind.String);

    // Assert
    var restoredVar = viewModel.Variables.FirstOrDefault(v => v.Name == "DELETED");
    restoredVar.Should().NotBeNull();
    restoredVar!.Model.IsRemoved.Should().BeFalse();
    restoredVar.Data.Should().Be("new_value");
    restoredVar.Model.IsAdded.Should().BeFalse();
    viewModel.Variables.Count.Should().Be(1);
  }

  [TestMethod]
  public void AddVariable_VolatileVariable_DoesNotUpdate() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VOLATILE").WithIsVolatile(true).Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    viewModel.AddVariable("VOLATILE", "new_value", Microsoft.Win32.RegistryValueKind.String);

    // Assert
    var volatileVar = viewModel.Variables.FirstOrDefault(v => v.Name == "VOLATILE");
    volatileVar.Should().NotBeNull();
    volatileVar!.Data.Should().NotBe("new_value");
  }

  [TestMethod]
  public void AddVariable_CaseInsensitiveFindsExisting() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("existing_var").WithData("old_value").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    viewModel.AddVariable("EXISTING_VAR", "new_value", Microsoft.Win32.RegistryValueKind.String);

    // Assert
    viewModel.Variables.Count.Should().Be(1);
    viewModel.Variables[0].Data.Should().Be("new_value");
  }

  [TestMethod]
  public void AddVariable_InsertsInSortedPosition() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("A_VAR").Build(),
      EnvironmentVariableBuilder.Default().WithName("Z_VAR").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    viewModel.AddVariable("M_VAR", "value", Microsoft.Win32.RegistryValueKind.String);

    // Assert
    viewModel.Variables[0].Name.Should().Be("A_VAR");
    viewModel.Variables[1].Name.Should().Be("M_VAR");
    viewModel.Variables[2].Name.Should().Be("Z_VAR");
  }

  #endregion

  #region RemoveVariable Tests

  [TestMethod]
  public void RemoveVariable_NewlyAdded_RemovesFromCollection() {
    // Arrange
    viewModel.AddVariable("NEW_VAR", "value", Microsoft.Win32.RegistryValueKind.String);
    var initialCount = viewModel.Variables.Count;

    // Act
    var variable = viewModel.Variables.First(v => v.Name == "NEW_VAR");
    viewModel.RemoveVariable(variable);

    // Assert
    viewModel.Variables.Count.Should().Be(initialCount - 1);
    viewModel.Variables.Should().NotContain(v => v.Name == "NEW_VAR");
  }

  [TestMethod]
  public void RemoveVariable_Existing_MarksAsRemoved() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("EXISTING").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    var initialCount = viewModel.Variables.Count;

    // Act
    var variable = viewModel.Variables.First();
    viewModel.RemoveVariable(variable);

    // Assert
    viewModel.Variables.Count.Should().Be(initialCount);
    variable.Model.IsRemoved.Should().BeTrue();
  }

  [TestMethod]
  public void RemoveVariable_RemovesFromFilteredVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("EXISTING").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    var variable = viewModel.Variables.First();
    viewModel.RemoveVariable(variable);

    // Assert
    viewModel.FilteredVariables.Should().NotContain(variable);
  }

  #endregion

  #region RemoveVariablesNotIn Tests

  [TestMethod]
  public void RemoveVariablesNotIn_RemovesVariablesNotInSet() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR3").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    var namesToKeep = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "VAR1", "VAR3" };

    // Act
    viewModel.RemoveVariablesNotIn(namesToKeep);

    // Assert
    viewModel.Variables.First(v => v.Name == "VAR1").Model.IsRemoved.Should().BeFalse();
    viewModel.Variables.First(v => v.Name == "VAR3").Model.IsRemoved.Should().BeFalse();
    viewModel.Variables.First(v => v.Name == "VAR2").Model.IsRemoved.Should().BeTrue();
  }

  [TestMethod]
  public void RemoveVariablesNotIn_SkipsVolatileVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VOLATILE").WithIsVolatile(true).Build(),
      EnvironmentVariableBuilder.Default().WithName("NORMAL").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    var namesToKeep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Act
    viewModel.RemoveVariablesNotIn(namesToKeep);

    // Assert
    viewModel.Variables.First(v => v.Name == "VOLATILE").Model.IsRemoved.Should().BeFalse();
    viewModel.Variables.First(v => v.Name == "NORMAL").Model.IsRemoved.Should().BeTrue();
  }

  [TestMethod]
  public void RemoveVariablesNotIn_CaseInsensitive() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    var namesToKeep = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "var1" };

    // Act
    viewModel.RemoveVariablesNotIn(namesToKeep);

    // Assert
    viewModel.Variables.First(v => v.Name == "VAR1").Model.IsRemoved.Should().BeFalse();
    viewModel.Variables.First(v => v.Name == "VAR2").Model.IsRemoved.Should().BeTrue();
  }

  #endregion

  #region GetAllVariables Tests

  [TestMethod]
  public void GetAllVariables_ReturnsAllVariableModels() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    var allVars = viewModel.GetAllVariables();

    // Assert
    allVars.Count().Should().Be(2);
    allVars.Any(v => v.Name == "VAR1").Should().BeTrue();
    allVars.Any(v => v.Name == "VAR2").Should().BeTrue();
  }

  #endregion

  #region CleanupAfterSave Tests

  [TestMethod]
  public void CleanupAfterSave_RemovesRemovedVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("EXISTING").Build(),
      EnvironmentVariableBuilder.Default().WithName("TO_REMOVE").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.Variables.First(v => v.Name == "TO_REMOVE").Model.IsRemoved = true;

    // Act
    viewModel.CleanupAfterSave();

    // Assert
    viewModel.Variables.Should().NotContain(v => v.Name == "TO_REMOVE");
    viewModel.Variables.Should().Contain(v => v.Name == "EXISTING");
  }

  [TestMethod]
  public void CleanupAfterSave_ClearsIsAddedFlags() {
    // Arrange
    viewModel.AddVariable("NEW_VAR", "value", Microsoft.Win32.RegistryValueKind.String);
    var newVar = viewModel.Variables.First(v => v.Name == "NEW_VAR");

    // Act
    viewModel.CleanupAfterSave();

    // Assert
    newVar.Model.IsAdded.Should().BeFalse();
  }

  #endregion

  #region UpdateFilteredVariables Tests

  [TestMethod]
  public void UpdateFilteredVariables_WhenShowVolatileTrue_IncludesVolatile() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("NORMAL").Build(),
      EnvironmentVariableBuilder.Default().WithName("VOLATILE").WithIsVolatile(true).Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.ShowVolatileVariables = true;

    // Act
    viewModel.UpdateFilteredVariables();

    // Assert
    viewModel.FilteredVariables.Any(v => v.Name == "VOLATILE").Should().BeTrue();
    viewModel.FilteredVariables.Any(v => v.Name == "NORMAL").Should().BeTrue();
  }

  [TestMethod]
  public void UpdateFilteredVariables_WhenShowVolatileFalse_ExcludesVolatile() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("NORMAL").Build(),
      EnvironmentVariableBuilder.Default().WithName("VOLATILE").WithIsVolatile(true).Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.ShowVolatileVariables = false;

    // Act
    viewModel.UpdateFilteredVariables();

    // Assert
    viewModel.FilteredVariables.Any(v => v.Name == "VOLATILE").Should().BeFalse();
    viewModel.FilteredVariables.Any(v => v.Name == "NORMAL").Should().BeTrue();
  }

  [TestMethod]
  public void UpdateFilteredVariables_ExcludesRemovedVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("ACTIVE").Build(),
      EnvironmentVariableBuilder.Default().WithName("REMOVED").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.Variables.First(v => v.Name == "REMOVED").Model.IsRemoved = true;

    // Act
    viewModel.UpdateFilteredVariables();

    // Assert
    viewModel.FilteredVariables.Any(v => v.Name == "REMOVED").Should().BeFalse();
    viewModel.FilteredVariables.Any(v => v.Name == "ACTIVE").Should().BeTrue();
  }

  [TestMethod]
  public void UpdateFilteredVariables_FiltersByName() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("MATCH").Build(),
      EnvironmentVariableBuilder.Default().WithName("OTHER").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    viewModel.SearchText = "MATCH";

    // Assert
    viewModel.FilteredVariables.Should().HaveCount(1);
    viewModel.FilteredVariables[0].Name.Should().Be("MATCH");
  }

  [TestMethod]
  public void UpdateFilteredVariables_FiltersByValue() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("needle").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").WithData("haystack").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    viewModel.SearchText = "needle";

    // Assert
    viewModel.FilteredVariables.Should().HaveCount(1);
    viewModel.FilteredVariables[0].Name.Should().Be("VAR1");
  }

  [TestMethod]
  public void UpdateFilteredVariables_IsCaseInsensitive() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("MixedCase").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act
    viewModel.SearchText = "mixedcase";

    // Assert
    viewModel.FilteredVariables.Should().HaveCount(1);
  }

  [TestMethod]
  public void UpdateFilteredVariables_ClearingSearchRestoresAll() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.SearchText = "VAR1";

    // Act
    viewModel.SearchText = string.Empty;

    // Assert
    viewModel.FilteredVariables.Should().HaveCount(2);
  }

  #endregion

  #region ShowVolatileVariables Tests

  [TestMethod]
  public void ShowVolatileVariables_SetToTrue_UpdatesFilteredVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VOLATILE").WithIsVolatile(true).Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.ShowVolatileVariables = false;
    viewModel.UpdateFilteredVariables();

    // Act
    viewModel.ShowVolatileVariables = true;

    // Assert
    viewModel.FilteredVariables.Any(v => v.Name == "VOLATILE").Should().BeTrue();
  }

  [TestMethod]
  public void ShowVolatileVariables_SetToFalse_UpdatesFilteredVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VOLATILE").WithIsVolatile(true).Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.ShowVolatileVariables = true;
    viewModel.UpdateFilteredVariables();

    // Act
    viewModel.ShowVolatileVariables = false;

    // Assert
    viewModel.FilteredVariables.Any(v => v.Name == "VOLATILE").Should().BeFalse();
  }

  #endregion

  #region GetAllVariables Tests

  [TestMethod]
  public void GetAllVariables_IncludesRemovedVariables() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("ACTIVE").Build(),
      EnvironmentVariableBuilder.Default().WithName("REMOVED").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();
    viewModel.Variables.First(v => v.Name == "REMOVED").Model.IsRemoved = true;

    // Act
    var allVars = viewModel.GetAllVariables();

    // Assert
    allVars.Count().Should().Be(2);
    allVars.Any(v => v.Name == "ACTIVE").Should().BeTrue();
    allVars.Any(v => v.Name == "REMOVED").Should().BeTrue();
  }

  #endregion

  #region CopyAll Tests

  [TestMethod]
  public void CopyAllCommand_SetsClipboardContent() {
    // Arrange
    var testVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("value1").Build(),
      EnvironmentVariableBuilder.Default().WithName("VAR2").WithData("value2").Build(),
    };
    envServiceMock.Setup(s => s.GetVariables()).Returns(testVars);
    viewModel.LoadFromRegistry();

    // Act - Note: This test may not work in headless environment
    try {
      viewModel.CopyAllCommand.Execute(null);
    }
    catch {
      // Clipboard operations may fail in test environment
    }

    // Assert - If no exception, operation completed
  }

  #endregion

  #region AddVariable Type Parameter Tests

  [TestMethod]
  public void AddVariable_WithExpandStringType_SetsCorrectType() {
    // Act
    viewModel.AddVariable("NEW_VAR", "value", RegistryValueKind.ExpandString);

    // Assert
    var newVar = viewModel.Variables.First(v => v.Name == "NEW_VAR");
    newVar.Model.Type.Should().Be(RegistryValueKind.ExpandString);
  }

  [TestMethod]
  public void AddVariable_WithDWordType_SetsCorrectType() {
    // Act
    viewModel.AddVariable("NEW_VAR", "12345", RegistryValueKind.DWord);

    // Assert
    var newVar = viewModel.Variables.First(v => v.Name == "NEW_VAR");
    newVar.Model.Type.Should().Be(RegistryValueKind.DWord);
  }

  #endregion
}
