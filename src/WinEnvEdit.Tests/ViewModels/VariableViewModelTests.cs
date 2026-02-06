using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Tests.Helpers;
using WinEnvEdit.ViewModels;

namespace WinEnvEdit.Tests.ViewModels;

[TestClass]
public class VariableViewModelTests {
  private int changeCallbackCallCount = 0;
  private VariableViewModel? deletedVariable = null;

  [TestInitialize]
  public void Setup() {
    changeCallbackCallCount = 0;
    deletedVariable = null;
  }

  #region Constructor Tests

  [TestMethod]
  public void Constructor_WithValidModel_InitializesCorrectly() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithName("TEST_VAR")
      .WithData("test_value")
      .WithType(RegistryValueKind.String)
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.Name.Should().Be("TEST_VAR");
    viewModel.Data.Should().Be("test_value");
    viewModel.Model.Should().Be(model);
    viewModel.IsLocked.Should().BeFalse();
    viewModel.DataErrorMessage.Should().BeEmpty();
  }

  [TestMethod]
  public void Constructor_WithVolatileModel_SetsIsLockedTrue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithIsVolatile(true)
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.IsLocked.Should().BeTrue();
  }

  [TestMethod]
  public void Constructor_WithPathVariable_SetsIsPathListTrue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .AsPathVariable()
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.IsPathList.Should().BeTrue();
    viewModel.PathItems.Should().NotBeNull();
    viewModel.PathItems.Count.Should().BeGreaterThan(0);
  }

  [TestMethod]
  public void Constructor_WithExpandString_SetsIsPathListTrue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithType(RegistryValueKind.ExpandString)
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.IsPathList.Should().BeTrue();
  }

  #endregion

  #region Name Property Tests

  [TestMethod]
  public void Name_SetToValidValue_UpdatesModel() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.Name = "NEW_NAME";

    // Assert
    viewModel.Name.Should().Be("NEW_NAME");
    model.Name.Should().Be("NEW_NAME");
  }

  [TestMethod]
  public void Name_SetToInvalidValue_UpdatesModelWithInvalidValue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.Name = "INVALID=NAME";

    // Assert
    model.Name.Should().Be("INVALID=NAME");
  }

  [TestMethod]
  public void Name_ChangeTriggersCallback_WhenProvided() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, () => changeCallbackCallCount++);
    changeCallbackCallCount = 0;

    // Act
    viewModel.Name = "NEW_NAME";

    // Assert
    changeCallbackCallCount.Should().Be(1);
  }

  #endregion

  #region Data Property Tests

  [TestMethod]
  public void Data_SetToValidValue_UpdatesModelAndClearsError() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.Data = "new_data";

    // Assert
    viewModel.Data.Should().Be("new_data");
    model.Data.Should().Be("new_data");
    viewModel.DataErrorMessage.Should().BeEmpty();
  }

  [TestMethod]
  public void Data_SetToInvalidValue_SetsError() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.Data = "invalid\0data";

    // Assert
    viewModel.DataErrorMessage.Should().Be("cannot contain null characters");
  }

  [TestMethod]
  public void Data_ChangeTriggersCallback_WhenProvided() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, () => changeCallbackCallCount++);
    changeCallbackCallCount = 0;

    // Act
    viewModel.Data = "new_data";

    // Assert
    changeCallbackCallCount.Should().Be(1);
  }

  #endregion

  #region HasDataError Tests

  [TestMethod]
  public void HasDataError_WhenDataValid_ReturnsFalse() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.Data = "valid data";

    // Assert
    viewModel.HasDataError.Should().BeFalse();
  }

  [TestMethod]
  public void HasDataError_WhenDataInvalid_ReturnsTrue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.Data = "invalid\0data";

    // Assert
    viewModel.HasDataError.Should().BeTrue();
  }

  #endregion

  #region IsExpanded/ExpandTooltip Tests

  [TestMethod]
  public void ExpandTooltip_WhenExpanded_ReturnsCollapse() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);
    viewModel.IsExpanded = true;

    // Act
    var tooltip = viewModel.ExpandTooltip;

    // Assert
    tooltip.Should().Be("Collapse");
  }

  [TestMethod]
  public void ExpandTooltip_WhenNotExpanded_ReturnsExpand() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);
    viewModel.IsExpanded = false;

    // Act
    var tooltip = viewModel.ExpandTooltip;

    // Assert
    tooltip.Should().Be("Expand");
  }

  #endregion

  #region Path List Handling Tests

  [TestMethod]
  public void Constructor_WithPathData_ParsesPathsCorrectly() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithName("PATH")
      .WithData("C:\\Windows;C:\\Program Files;D:\\Tools")
      .WithType(RegistryValueKind.ExpandString)
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.PathItems.Count.Should().Be(3);
    viewModel.PathItems[0].PathValue.Should().Be("C:\\Windows");
    viewModel.PathItems[1].PathValue.Should().Be("C:\\Program Files");
    viewModel.PathItems[2].PathValue.Should().Be("D:\\Tools");
  }

  [TestMethod]
  public void Constructor_WithEmptyPathData_CreatesEmptyPathItems() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithName("PATH")
      .WithData("")
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.PathItems.Count.Should().Be(0);
  }

  [TestMethod]
  public void Constructor_WithWhitespacePathData_CreatesEmptyPathItems() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithName("PATH")
      .WithData("   ")
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.PathItems.Count.Should().Be(0);
  }

  [TestMethod]
  public void SyncDataFromPaths_JoinsPathsWithSemicolons() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .AsPathVariable()
      .Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.SyncDataFromPaths();

    // Assert
    var paths = viewModel.Data.Split(';').Select(p => p.Trim()).ToList();
    paths.Should().Contain(p => p == "C:\\Windows\\System32");
    paths.Should().Contain(p => p == "C:\\Windows");
  }

  #endregion

  #region Path Items Collection Tests

  [TestMethod]
  public void PathItems_Add_SyncsToData() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .AsPathVariable()
      .Build();
    var viewModel = new VariableViewModel(model, null, null);
    var initialData = viewModel.Data;

    // Act
    viewModel.PathItems.Add(new PathItemViewModel("E:\\NewPath", viewModel));

    // Assert
    viewModel.Data.Should().NotBe(initialData);
    viewModel.Data.Should().Contain("E:\\NewPath");
  }

  [TestMethod]
  public void PathItems_Remove_SyncsToData() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .AsPathVariable()
      .Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    var itemToRemove = viewModel.PathItems.First();
    viewModel.PathItems.Remove(itemToRemove);

    // Assert
    viewModel.Data.Should().NotContain(itemToRemove.PathValue);
  }

  [TestMethod]
  public void PathItems_Move_SyncsToData() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .AsPathVariable()
      .Build();
    var viewModel = new VariableViewModel(model, null, null);
    var firstItem = viewModel.PathItems.First();
    var lastItem = viewModel.PathItems.Last();

    // Act - simulate move by remove + add
    viewModel.PathItems.Remove(firstItem);
    viewModel.PathItems.Add(firstItem);

    // Assert
    viewModel.Data.Should().EndWith(firstItem.PathValue);
  }

  #endregion

  #region HasInvalidPath Tests

  [TestMethod]
  public void HasInvalidPath_WithInvalidPaths_ReturnsTrue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .AsPathVariable()
      .WithData("X:\\NonExistent\\Path")
      .Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    var hasInvalid = viewModel.HasInvalidPath;

    // Assert
    hasInvalid.Should().BeTrue();
  }

  [TestMethod]
  public void HasInvalidPath_PathList_WithNonPathFirstEntry_AndInvalidSecondEntry_ReturnsTrue() {
    // Arrange - "aa" is not a path, but "X:\fake" is and doesn't exist
    var model = EnvironmentVariableBuilder.Default()
      .WithName("PATH")
      .WithData("aa;X:\\fake")
      .WithType(RegistryValueKind.ExpandString)
      .Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act & Assert
    viewModel.HasInvalidPath.Should().BeTrue("second entry looks like a filesystem path and doesn't exist");
  }

  [TestMethod]
  public void HasInvalidPath_PathList_WithNonPathEntries_ReturnsFalse() {
    // Arrange - Neither entry looks like a path
    var model = EnvironmentVariableBuilder.Default()
      .WithName("PATH")
      .WithData("aa;bb")
      .WithType(RegistryValueKind.ExpandString)
      .Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act & Assert
    viewModel.HasInvalidPath.Should().BeFalse("no entries look like filesystem paths");
  }

  [TestMethod]
  public void HasInvalidPath_NonPathList_WithNonPathValue_ReturnsFalse() {
    // Arrange - "some_text" is not a path-like value
    var model = EnvironmentVariableBuilder.Default()
      .WithName("MY_VAR")
      .WithData("some_text")
      .WithType(RegistryValueKind.String)
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.HasInvalidPath.Should().BeFalse("value doesn't look like a filesystem path");
  }

  #endregion

  #region Non-PathList Path Validation Tests

  [TestMethod]
  public void DataPathExists_NonPathList_WithValidPath_ReturnsTrue() {
    // Arrange
    var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
    var model = EnvironmentVariableBuilder.Default()
      .WithName("MY_PATH")
      .WithData(systemRoot)
      .WithType(RegistryValueKind.String) // Not ExpandString, so not a path list
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.DataPathExists.Should().BeTrue();
    viewModel.HasInvalidPath.Should().BeFalse();
  }

  [TestMethod]
  public void DataPathExists_NonPathList_WithInvalidPath_ReturnsFalse() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithName("MY_PATH")
      .WithData("X:\\NonExistent\\Path\\That\\Does\\Not\\Exist")
      .WithType(RegistryValueKind.String) // Not ExpandString, so not a path list
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.DataPathExists.Should().BeFalse();
    viewModel.HasInvalidPath.Should().BeTrue();
  }

  [TestMethod]
  public void DataPathExists_NonPathList_WhenDataChanges_Updates() {
    // Arrange
    var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
    var model = EnvironmentVariableBuilder.Default()
      .WithName("MY_PATH")
      .WithData(systemRoot)
      .WithType(RegistryValueKind.String)
      .Build();
    var viewModel = new VariableViewModel(model, null, null);
    viewModel.DataPathExists.Should().BeTrue();

    // Act
    viewModel.Data = "X:\\NonExistent\\Path";

    // Assert
    viewModel.DataPathExists.Should().BeFalse();
    viewModel.HasInvalidPath.Should().BeTrue();
  }

  [TestMethod]
  public void DataPathExists_NonPathList_WithEnvironmentVariable_ExpandsAndChecks() {
    // Arrange - %SystemRoot% should expand to C:\Windows
    var model = EnvironmentVariableBuilder.Default()
      .WithName("MY_PATH")
      .WithData("%SystemRoot%")
      .WithType(RegistryValueKind.String)
      .Build();

    // Act
    var viewModel = new VariableViewModel(model, null, null);

    // Assert
    viewModel.DataPathExists.Should().BeTrue("environment variable should be expanded and path checked");
  }

  #endregion

  #region ToggleExpandCommand Tests

  [TestMethod]
  public void ToggleExpandCommand_TogglesIsExpanded() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null);
    var initialState = viewModel.IsExpanded;

    // Act
    viewModel.ToggleExpandCommand.Execute(null);

    // Assert
    viewModel.IsExpanded.Should().Be(!initialState);
  }

  #endregion

  #region AddPathCommand Tests

  [TestMethod]
  public void AddPathCommand_AddsNewPathItem() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .AsPathVariable()
      .Build();
    var viewModel = new VariableViewModel(model, null, null);
    var initialCount = viewModel.PathItems.Count;

    // Act
    viewModel.AddPathCommand.Execute(null);

    // Assert
    viewModel.PathItems.Count.Should().Be(initialCount + 1);
    viewModel.PathItems.Last().PathValue.Should().Be(string.Empty);
  }

  #endregion

  #region RemovePathCommand Tests

  [TestMethod]
  public void RemovePathCommand_RemovesSpecifiedPathItem() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .AsPathVariable()
      .Build();
    var viewModel = new VariableViewModel(model, null, null);
    var itemToRemove = viewModel.PathItems.First();
    var initialCount = viewModel.PathItems.Count;

    // Act
    viewModel.RemovePath(itemToRemove);

    // Assert
    viewModel.PathItems.Count.Should().Be(initialCount - 1);
    viewModel.PathItems.Should().NotContain(itemToRemove);
  }

  #endregion

  #region RemoveCommand Tests

  [TestMethod]
  public void RemoveCommand_CallsDeleteCallback() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    Action<VariableViewModel> deleteCallback = v => deletedVariable = v;
    var viewModel = new VariableViewModel(model, deleteCallback, null);

    // Act
    viewModel.RemoveCommand.Execute(null);

    // Assert
    deletedVariable.Should().Be(viewModel);
  }

  [TestMethod]
  public void RemoveCommand_CallsChangeCallback() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, () => changeCallbackCallCount++);
    changeCallbackCallCount = 0;

    // Act
    viewModel.RemoveCommand.Execute(null);

    // Assert
    changeCallbackCallCount.Should().Be(1);
  }

  #endregion

  #region ToggleTypeCommand Tests

  [TestMethod]
  public void ToggleTypeCommand_StringToExpandString_UpdatesModelType() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithType(RegistryValueKind.String)
      .Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.ToggleTypeCommand.Execute(null);

    // Assert
    model.Type.Should().Be(RegistryValueKind.ExpandString);
  }

  [TestMethod]
  public void ToggleTypeCommand_ExpandStringToString_UpdatesModelType() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithType(RegistryValueKind.ExpandString)
      .Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.ToggleTypeCommand.Execute(null);

    // Assert
    model.Type.Should().Be(RegistryValueKind.String);
  }

  [TestMethod]
  public void ToggleTypeCommand_StringToExpandString_SetsIsPathListTrue() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithType(RegistryValueKind.String)
      .Build();
    var viewModel = new VariableViewModel(model, null, null);
    viewModel.IsPathList.Should().BeFalse();

    // Act
    viewModel.ToggleTypeCommand.Execute(null);

    // Assert
    viewModel.IsPathList.Should().BeTrue();
  }

  [TestMethod]
  public void ToggleTypeCommand_ExpandStringToString_ClearsIsPathList() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithData("some;data")
      .WithType(RegistryValueKind.ExpandString)
      .Build();
    var viewModel = new VariableViewModel(model, null, null);
    viewModel.IsPathList.Should().BeTrue();

    // Act
    viewModel.ToggleTypeCommand.Execute(null);

    // Assert
    viewModel.IsPathList.Should().BeFalse();
    viewModel.PathItems.Count.Should().Be(0);
  }

  [TestMethod]
  public void ToggleTypeCommand_LockedVariable_DoesNothing() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default()
      .WithIsVolatile(true)
      .WithType(RegistryValueKind.String)
      .Build();
    var viewModel = new VariableViewModel(model, null, null);

    // Act
    viewModel.ToggleTypeCommand.Execute(null);

    // Assert - type unchanged
    model.Type.Should().Be(RegistryValueKind.String);
  }

  [TestMethod]
  public void ToggleTypeCommand_CallsChangeCallback() {
    // Arrange
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, () => changeCallbackCallCount++);
    changeCallbackCallCount = 0;

    // Act
    viewModel.ToggleTypeCommand.Execute(null);

    // Assert
    changeCallbackCallCount.Should().Be(1);
  }

  [TestMethod]
  public void ToggleTypeCommand_CallsRefreshCallback() {
    // Arrange
    var refreshCalled = false;
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null, _ => refreshCalled = true);

    // Act
    viewModel.ToggleTypeCommand.Execute(null);

    // Assert
    refreshCalled.Should().BeTrue();
  }

  [TestMethod]
  public void ToggleTypeCommand_PassesThisToRefreshCallback() {
    // Arrange
    VariableViewModel? callbackVariable = null;
    var model = EnvironmentVariableBuilder.Default().Build();
    var viewModel = new VariableViewModel(model, null, null, v => callbackVariable = v);

    // Act
    viewModel.ToggleTypeCommand.Execute(null);

    // Assert
    callbackVariable.Should().Be(viewModel);
  }

  #endregion

}
