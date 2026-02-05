using System;
using System.IO;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

using WinEnvEdit.Models;
using WinEnvEdit.Tests.Helpers;
using WinEnvEdit.ViewModels;

namespace WinEnvEdit.Tests.ViewModels;

[TestClass]
public class PathItemViewModelTests {
  private VariableViewModel parentViewModel = null!;

  [TestInitialize]
  public void Setup() {
    var model = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("C:\\Windows;C:\\Program Files")
      .WithType(RegistryValueKind.ExpandString)
      .Build();
    parentViewModel = new VariableViewModel(model, null, null);
  }

  #region Constructor Tests

  [TestMethod]
  public void Constructor_WithValidPath_InitializesCorrectly() {
    // Act
    var pathItem = new PathItemViewModel("C:\\Windows", parentViewModel);

    // Assert
    pathItem.PathValue.Should().Be("C:\\Windows");
    pathItem.Exists.Should().BeTrue("Windows directory should exist on most systems");
  }

  [TestMethod]
  public void Constructor_WithInvalidPath_SetsExistsToFalse() {
    // Act
    var pathItem = new PathItemViewModel("C:\\ThisPathDoesNotExist12345", parentViewModel);

    // Assert
    pathItem.PathValue.Should().Be("C:\\ThisPathDoesNotExist12345");
    pathItem.Exists.Should().BeFalse();
  }

  [TestMethod]
  public void Constructor_WithEmptyPath_SetsExistsToFalse() {
    // Act
    var pathItem = new PathItemViewModel(string.Empty, parentViewModel);

    // Assert
    pathItem.PathValue.Should().Be(string.Empty);
    pathItem.Exists.Should().BeFalse();
  }

  #endregion

  #region PathValue Property Tests

  [TestMethod]
  public void PathValue_SetsValueAndSyncsWithParent() {
    // Arrange
    var pathItem = new PathItemViewModel(string.Empty, parentViewModel);
    parentViewModel.PathItems.Add(pathItem);
    var initialData = parentViewModel.Data;

    // Act
    pathItem.PathValue = "C:\\NewPath";

    // Assert
    pathItem.PathValue.Should().Be("C:\\NewPath");
    parentViewModel.Data.Should().NotBe(initialData, "parent data should be synced");
  }

  [TestMethod]
  public void PathValue_ChangeToSameValue_DoesNotTriggerParentSync() {
    // Arrange
    var pathItem = new PathItemViewModel("C:\\Windows", parentViewModel);
    var initialData = parentViewModel.Data;

    // Act
    pathItem.PathValue = "C:\\Windows";

    // Assert
    parentViewModel.Data.Should().Be(initialData, "data should remain same");
  }

  #endregion

  #region Exists Property Tests

  [TestMethod]
  public void Exists_ValidDirectoryPath_ReturnsTrue() {
    // Arrange - use SystemDrive which should always exist
    var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
    var testPath = Path.Combine(systemDrive, "Windows");

    // Act
    var pathItem = new PathItemViewModel(testPath, parentViewModel);

    // Assert
    pathItem.Exists.Should().BeTrue("Windows directory should exist");
  }

  [TestMethod]
  public void Exists_ValidFilePath_ReturnsTrue() {
    // Arrange
    var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
    var testPath = Path.Combine(systemRoot, "explorer.exe");

    // Act
    var pathItem = new PathItemViewModel(testPath, parentViewModel);

    // Assert
    pathItem.Exists.Should().BeTrue("explorer.exe should exist");
  }

  [TestMethod]
  public void Exists_NonExistentPath_ReturnsFalse() {
    // Act
    var pathItem = new PathItemViewModel("X:\\NonExistent\\Path", parentViewModel);

    // Assert
    pathItem.Exists.Should().BeFalse();
  }

  [TestMethod]
  public void Exists_EmptyPath_ReturnsFalse() {
    // Act
    var pathItem = new PathItemViewModel(string.Empty, parentViewModel);

    // Assert
    pathItem.Exists.Should().BeFalse();
  }

  [TestMethod]
  public void Exists_WhenValidationDisabled_ReturnsTrue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("value")
      .Build();
    var parent = new VariableViewModel(model, null, null);
    parent.EnablePathValidation = false;

    // Act
    var pathItem = new PathItemViewModel("X:\\NonExistent\\Path", parent);

    // Assert
    pathItem.Exists.Should().BeTrue("validation is disabled");
  }

  [TestMethod]
  public void Exists_WithEnvironmentVariable_ExpandsAndChecks() {
    // Arrange - SystemRoot should expand to valid path
    var testPath = "%SystemRoot%";

    // Act
    var pathItem = new PathItemViewModel(testPath, parentViewModel);

    // Assert
    pathItem.Exists.Should().BeTrue("%SystemRoot% should expand to valid path");
  }

  #endregion

  #region IsReadOnly Property Tests

  [TestMethod]
  public void IsReadOnly_ParentNotLocked_ReturnsFalse() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("value")
      .Build();
    var parent = new VariableViewModel(model, null, null);

    // Act
    var pathItem = new PathItemViewModel("C:\\Windows", parent);

    // Assert
    pathItem.IsReadOnly.Should().BeFalse();
  }

  [TestMethod]
  public void IsReadOnly_ParentLocked_ReturnsTrue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("value")
      .WithIsVolatile(true)
      .Build();
    var parent = new VariableViewModel(model, null, null);

    // Act
    var pathItem = new PathItemViewModel("C:\\Windows", parent);

    // Assert
    pathItem.IsReadOnly.Should().BeTrue();
  }

  #endregion

  #region RemoveCommand Tests

  [TestMethod]
  public void RemoveCommand_CallsParentRemovePath() {
    // Arrange
    var pathItem = new PathItemViewModel("C:\\Windows", parentViewModel);
    parentViewModel.PathItems.Add(pathItem);
    var initialCount = parentViewModel.PathItems.Count;

    // Act
    pathItem.RemoveCommand.Execute(null);

    // Assert
    parentViewModel.PathItems.Count.Should().Be(initialCount - 1);
  }

  #endregion
}
