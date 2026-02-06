using FluentAssertions;

using Microsoft.UI.Xaml;

using WinEnvEdit.Extensions;
using WinEnvEdit.Models;
using WinEnvEdit.Services;
using WinEnvEdit.Tests.Helpers;
using WinEnvEdit.ViewModels;

namespace WinEnvEdit.Tests.ViewModels;

[TestClass]
public class MainWindowViewModelTests {
  private MainWindowViewModel viewModel = null!;
  private MockEnvironmentService environmentService = null!;
  private MockFileService fileService = null!;
  private StateSnapshotService stateService = null!;
  private UndoRedoService undoRedoService = null!;

  [TestInitialize]
  public void Setup() {
    environmentService = new MockEnvironmentService();
    fileService = new MockFileService();
    stateService = new StateSnapshotService();
    undoRedoService = new UndoRedoService();

    // Create ViewModel without window (null parameter)
    viewModel = new MainWindowViewModel(environmentService, null!, fileService, stateService, undoRedoService);
  }

  #region State Management Tests

  [TestMethod]
  public void Constructor_InitializesWithNoPendingChanges() {
    // Assert
    viewModel.HasPendingChanges.Should().BeFalse();
  }

  [TestMethod]
  public void Constructor_LoadsVariablesFromEnvironment() {
    // Assert
    viewModel.SystemVariables.Variables.Should().NotBeEmpty();
    viewModel.UserVariables.Variables.Should().NotBeEmpty();
  }

  [TestMethod]
  public void UpdatePendingChangesState_VariableDataChanged_SetsPendingChanges() {
    // Arrange
    var variable = viewModel.UserVariables.Variables.First();
    variable.Data = "modified_value";

    // Act
    viewModel.UpdatePendingChangesState();

    // Assert
    viewModel.HasPendingChanges.Should().BeTrue();
  }

  [TestMethod]
  public void UpdatePendingChangesState_VariableDataUnchanged_ClearsPendingChanges() {
    // Arrange
    var variable = viewModel.UserVariables.Variables.First();
    var originalData = variable.Data;
    variable.Data = "modified_value";
    viewModel.UpdatePendingChangesState();
    viewModel.HasPendingChanges.Should().BeTrue();

    // Act - revert to original
    variable.Data = originalData;
    viewModel.UpdatePendingChangesState();

    // Assert
    viewModel.HasPendingChanges.Should().BeFalse();
  }

  [TestMethod]
  public void UpdatePendingChangesState_PushesToUndoStack() {
    // Arrange
    var variable = viewModel.UserVariables.Variables.First();
    variable.Data = "modified_value";

    // Act
    viewModel.UpdatePendingChangesState();

    // Assert
    viewModel.CanUndoState.Should().BeTrue("undo should be available after first change");
  }

  #endregion

  #region Undo/Redo Tests

  [TestMethod]
  public void UndoCommand_AfterDataChange_CanUndoIsTrue() {
    // Arrange
    var variable = viewModel.UserVariables.Variables.First();
    variable.Data = "modified_value";
    viewModel.UpdatePendingChangesState();

    // Act - before undo
    var canUndoBeforeUndo = viewModel.CanUndoState;

    // Assert
    canUndoBeforeUndo.Should().BeTrue("undo should be available after a change");
  }

  [TestMethod]
  public void RedoCommand_AfterUndo_CanRedoIsTrue() {
    // Arrange
    var variable = viewModel.UserVariables.Variables.First();
    variable.Data = "modified_value";
    viewModel.UpdatePendingChangesState();
    viewModel.UndoCommand.Execute(null);

    // Act - after undo, redo should be available
    var canRedoAfterUndo = viewModel.CanRedoState;

    // Assert
    canRedoAfterUndo.Should().BeTrue("redo should be available after undo");
  }

  [TestMethod]
  public void UndoRedo_PreservesPathListExpansionState() {
    // Arrange
    var pathVariable = viewModel.SystemVariables.Variables.First(v => v.IsPathList);

    pathVariable.IsExpanded = true;
    pathVariable.Data = "C:\\modified;C:\\path";
    viewModel.UpdatePendingChangesState();

    // Act
    viewModel.UndoCommand.Execute(null);
    var expandedAfterUndo = pathVariable.IsExpanded;
    viewModel.RedoCommand.Execute(null);
    var expandedAfterRedo = pathVariable.IsExpanded;

    // Assert
    expandedAfterUndo.Should().BeTrue("expansion state should be preserved");
    expandedAfterRedo.Should().BeTrue("expansion state should be preserved after redo");
  }

  #endregion

  #region Search Tests

  [TestMethod]
  public void OnSearchTextChanged_UpdatesSearchInBothScopes() {
    // Arrange
    viewModel.SearchText = string.Empty;

    // Act
    viewModel.SearchText = "test";

    // Assert
    viewModel.SystemVariables.SearchText.Should().Be("test");
    viewModel.UserVariables.SearchText.Should().Be("test");
  }

  [TestMethod]
  public void OnSearchTextChanged_FiltersVariables() {
    // Arrange
    var allSystemCount = viewModel.SystemVariables.Variables.Count;

    // Act - search for something that won't match anything
    viewModel.SearchText = "NONEXISTENT_VAR_XYZ";
    var filteredCount = viewModel.SystemVariables.FilteredVariables.Count;

    // Assert
    filteredCount.Should().Be(0, "search should filter out all non-matching variables");
  }

  #endregion

  #region Volatile Variables Toggle Tests

  [TestMethod]
  public void OnShowVolatileVariablesChanged_UpdatesBothScopes() {
    // Arrange
    viewModel.ShowVolatileVariables = false;

    // Act
    viewModel.ShowVolatileVariables = true;

    // Assert
    viewModel.SystemVariables.ShowVolatileVariables.Should().BeTrue();
    viewModel.UserVariables.ShowVolatileVariables.Should().BeTrue();
  }

  [TestMethod]
  public void OnShowVolatileVariablesChanged_ToggleVisiblity() {
    // Arrange
    var volatileVarCount = viewModel.SystemVariables.Variables.Count(v => v.Model.IsVolatile);
    volatileVarCount.Should().BeGreaterThan(0, "Mock data should include volatile variables");

    viewModel.ShowVolatileVariables = false;
    var hiddenCount = viewModel.SystemVariables.FilteredVariables.Count;

    // Act
    viewModel.ShowVolatileVariables = true;
    var shownCount = viewModel.SystemVariables.FilteredVariables.Count;

    // Assert
    shownCount.Should().BeGreaterThan(hiddenCount);
  }

  #endregion

  #region Expand All Paths Tests

  [TestMethod]
  public void OnExpandAllPathsChanged_ExpandsAllPathVariables() {
    // Arrange
    viewModel.ExpandAllPaths = false;
    var pathVariables = viewModel.SystemVariables.Variables
      .Concat(viewModel.UserVariables.Variables)
      .Where(v => v.IsPathList)
      .ToList();

    pathVariables.Should().NotBeEmpty("Mock data should include path variables");

    // Act
    viewModel.ExpandAllPaths = true;

    // Assert
    foreach (var pathVar in pathVariables) {
      pathVar.IsExpanded.Should().BeTrue();
    }
  }

  [TestMethod]
  public void OnExpandAllPathsChanged_CollapsesAllPathVariables() {
    // Arrange
    viewModel.ExpandAllPaths = true;
    var pathVariables = viewModel.SystemVariables.Variables
      .Concat(viewModel.UserVariables.Variables)
      .Where(v => v.IsPathList)
      .ToList();

    pathVariables.Should().NotBeEmpty("Mock data should include path variables");

    // Act
    viewModel.ExpandAllPaths = false;

    // Assert
    foreach (var pathVar in pathVariables) {
      pathVar.IsExpanded.Should().BeFalse();
    }
  }

  #endregion

  #region Computed Property Tests

  [TestMethod]
  public void VolatileToggleGlyph_WhenShowingVolatile_ReturnsHideGlyph() {
    // Arrange
    viewModel.ShowVolatileVariables = true;

    // Assert
    viewModel.VolatileToggleGlyph.Should().Be(Glyph.Hide);
  }

  [TestMethod]
  public void VolatileToggleGlyph_WhenHidingVolatile_ReturnsViewGlyph() {
    // Arrange
    viewModel.ShowVolatileVariables = false;

    // Assert
    viewModel.VolatileToggleGlyph.Should().Be(Glyph.View);
  }

  [TestMethod]
  public void SearchVisibility_WhenSearchNotVisible_ReturnsCollapsed() {
    // Arrange
    viewModel.IsSearchVisible = false;

    // Assert
    viewModel.SearchVisibility.Should().Be(Visibility.Collapsed);
  }

  [TestMethod]
  public void SearchVisibility_WhenSearchVisible_ReturnsVisible() {
    // Arrange
    viewModel.IsSearchVisible = true;

    // Assert
    viewModel.SearchVisibility.Should().Be(Visibility.Visible);
  }

  #endregion

  #region Import Tests

  [TestMethod]
  public void Import_PreservesVolatileVariables() {
    // Arrange - Add a volatile variable to the environment
    var volatileVar = EnvironmentVariableBuilder.Default()
      .WithName("VOLATILE_VAR")
      .WithData("volatile_value")
      .WithScope(VariableScope.User)
      .WithIsVolatile(true)
      .Build();
    environmentService.AddVolatileVariable(volatileVar);

    // Create a fresh ViewModel to load the volatile variable
    viewModel = new MainWindowViewModel(environmentService, null!, fileService, stateService, undoRedoService);
    var volatileCountBefore = viewModel.UserVariables.Variables.Count(v => v.Model.IsVolatile);
    volatileCountBefore.Should().BeGreaterThan(0, "should have volatile variables before import");

    // Simulate import by directly calling RestoreFromVariables (bypassing file picker UI)
    var importedVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .WithName("IMPORTED_VAR")
        .WithData("imported_value")
        .WithScope(VariableScope.User)
        .WithIsAdded(false)
        .WithIsRemoved(false)
        .WithIsVolatile(false)
        .Build(),
    };

    // Preserve volatile variables (same logic as Import command)
    var userVolatile = viewModel.UserVariables.Variables
      .Where(v => v.Model.IsVolatile)
      .Select(v => v.Model)
      .ToList();
    var combined = importedVars.Concat(userVolatile).OrderBy(v => v.Name).ToList();

    // Act
    viewModel.UserVariables.RestoreFromVariables(combined);

    // Assert - Volatile variables should still be present
    var volatileCountAfter = viewModel.UserVariables.Variables.Count(v => v.Model.IsVolatile);
    volatileCountAfter.Should().Be(volatileCountBefore, "volatile variables should be preserved during import");

    // Imported variable should also be present
    viewModel.UserVariables.Variables.Should().Contain(v => v.Name == "IMPORTED_VAR");
  }

  #endregion
}

// Mock implementations for testing
internal class MockEnvironmentService : IEnvironmentService {
  private readonly List<EnvironmentVariable> volatileVariables = [];

  public List<EnvironmentVariable> GetVariables() {
    var baseVars = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .AsPathVariable()
        .WithScope(VariableScope.System)
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("PATHEXT")
        .WithData(".COM;.EXE;.BAT")
        .WithScope(VariableScope.System)
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("VOLATILE_SYSTEM_VAR")
        .WithData("volatile_value")
        .WithScope(VariableScope.System)
        .WithIsVolatile(true)
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("TEMP")
        .WithData("C:\\Temp")
        .WithScope(VariableScope.User)
        .Build(),
    };
    return baseVars.Concat(volatileVariables).ToList();
  }

  public void AddVolatileVariable(EnvironmentVariable variable) {
    volatileVariables.Add(variable);
  }

  public Task SaveVariables(IEnumerable<EnvironmentVariable> variables) =>
    Task.CompletedTask;
}

internal class MockFileService : IFileService {
  public IEnumerable<EnvironmentVariable> ImportData { get; set; } = Enumerable.Empty<EnvironmentVariable>();

  public Task<IEnumerable<EnvironmentVariable>> ImportFromFile(string filePath) =>
    Task.FromResult(ImportData);

  public Task ExportToFile(string filePath, IEnumerable<EnvironmentVariable> variables) =>
    Task.CompletedTask;
}
