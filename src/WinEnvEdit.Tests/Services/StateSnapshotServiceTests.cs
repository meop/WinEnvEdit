using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Services;
using WinEnvEdit.Tests.Helpers;

namespace WinEnvEdit.Tests.Services;

[TestClass]
public class StateSnapshotServiceTests {
  private StateSnapshotService service = null!;

  [TestInitialize]
  public void Setup() {
    service = new StateSnapshotService();
  }

  #region CaptureSnapshot Tests

  [TestMethod]
  public void CaptureSnapshot_WithVariables_StoresSnapshot() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("value")
      .Build();

    // Act
    service.CaptureSnapshot(new[] { variable });

    // Assert - verify snapshot captured by checking IsDirty returns false for unchanged
    service.IsDirty(new[] { variable }).Should().BeFalse();
  }

  [TestMethod]
  public void CaptureSnapshot_ExcludesRemovedVariables() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithIsRemoved(true)
      .Build();

    // Act
    service.CaptureSnapshot(new[] { variable });

    // Assert - removed variable should not be in snapshot
    // When we un-remove it and mark as added, it should be dirty
    variable.IsRemoved = false;
    variable.IsAdded = true;
    service.IsDirty(new[] { variable }).Should().BeTrue("variable wasn't in snapshot and is now added");
  }

  [TestMethod]
  public void CaptureSnapshot_ExcludesVolatileVariables() {
    // Arrange
    var persistentVar = EnvironmentVariableBuilder.Default()
      .WithName("PERSISTENT")
      .WithData("value")
      .Build();
    var volatileVar = EnvironmentVariableBuilder.Default()
      .WithName("VOLATILE")
      .WithData("value")
      .WithIsVolatile(true)
      .Build();

    // Act
    service.CaptureSnapshot(new[] { persistentVar, volatileVar });

    // Assert - volatile var is not in snapshot, so changing it is invisible
    volatileVar.Data = "changed";
    service.IsDirty(new[] { persistentVar, volatileVar }).Should().BeFalse("volatile vars are excluded from snapshot and dirty check");
  }

  #endregion

  #region IsDirty Tests

  [TestMethod]
  public void IsDirty_VariableRemoved_ReturnsTrue() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .Build();
    service.CaptureSnapshot(new[] { variable });

    variable.IsRemoved = true;

    // Act
    var result = service.IsDirty(new[] { variable });

    // Assert
    result.Should().BeTrue("removed variable exists in snapshot");
  }

  [TestMethod]
  public void IsDirty_VariableAdded_ReturnsTrue() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("NEW_VAR")
      .WithIsAdded(true)
      .Build();

    // Act
    var result = service.IsDirty(new[] { variable });

    // Assert
    result.Should().BeTrue("variable is marked as added");
  }

  [TestMethod]
  public void IsDirty_DataChanged_ReturnsTrue() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("original")
      .Build();
    service.CaptureSnapshot(new[] { variable });

    variable.Data = "modified";

    // Act
    var result = service.IsDirty(new[] { variable });

    // Assert
    result.Should().BeTrue("data value changed");
  }


  [TestMethod]
  public void IsDirty_TypeChanged_ReturnsTrue() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithType(RegistryValueKind.String)
      .Build();
    service.CaptureSnapshot(new[] { variable });

    variable.Type = RegistryValueKind.ExpandString;

    // Act
    var result = service.IsDirty(new[] { variable });

    // Assert
    result.Should().BeTrue("type changed from String to ExpandString");
  }

  [TestMethod]
  public void IsDirty_NoChanges_ReturnsFalse() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("value")
      .WithType(RegistryValueKind.String)
      .Build();
    service.CaptureSnapshot(new[] { variable });

    // Act
    var result = service.IsDirty(new[] { variable });

    // Assert
    result.Should().BeFalse("nothing changed");
  }

  [TestMethod]
  public void IsDirty_EmptySnapshot_EmptyCurrent_ReturnsFalse() {
    // Arrange
    service.CaptureSnapshot([]);

    // Act
    var result = service.IsDirty([]);

    // Assert
    result.Should().BeFalse("both snapshot and current are empty");
  }

  [TestMethod]
  public void IsDirty_CaseInsensitiveLookup_FindsVariable() {
    // Arrange - capture with lowercase name
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("test")
      .WithData("value")
      .Build();
    service.CaptureSnapshot(new[] { variable });

    // Act - check with uppercase name (same variable, just case difference)
    variable.Name = "TEST";
    var result = service.IsDirty(new[] { variable });

    // Assert - should detect as changed (Ordinal comparison in HasChanged)
    result.Should().BeTrue("name case changed from 'test' to 'TEST'");
  }

  [TestMethod]
  public void IsDirty_VolatileVariableAdded_ReturnsFalse() {
    // Arrange
    service.CaptureSnapshot([]);

    var volatileVar = EnvironmentVariableBuilder.Default()
      .WithName("VOLATILE")
      .WithIsVolatile(true)
      .WithIsAdded(true)
      .Build();

    // Act
    var result = service.IsDirty(new[] { volatileVar });

    // Assert
    result.Should().BeFalse("volatile vars are excluded from dirty check");
  }

  #endregion

  #region GetChangedVariables Tests

  [TestMethod]
  public void GetChangedVariables_RemovedVariable_InSnapshot_Included() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .Build();
    service.CaptureSnapshot(new[] { variable });

    variable.IsRemoved = true;

    // Act
    var changed = service.GetChangedVariables(new[] { variable }).ToList();

    // Assert
    changed.Should().HaveCount(1);
    changed[0].Name.Should().Be("TEST");
    changed[0].IsRemoved.Should().BeTrue();
  }

  [TestMethod]
  public void GetChangedVariables_RemovedVariable_NotInSnapshot_Excluded() {
    // Arrange - capture snapshot without the variable
    service.CaptureSnapshot([]);

    var variable = EnvironmentVariableBuilder.Default()
      .WithName("NEW_VAR")
      .WithIsRemoved(true)
      .Build();

    // Act
    var changed = service.GetChangedVariables(new[] { variable }).ToList();

    // Assert
    changed.Should().BeEmpty("variable wasn't in original snapshot");
  }

  [TestMethod]
  public void GetChangedVariables_AddedVariable_Included() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("NEW_VAR")
      .WithIsAdded(true)
      .Build();

    // Act
    var changed = service.GetChangedVariables(new[] { variable }).ToList();

    // Assert
    changed.Should().HaveCount(1);
    changed[0].IsAdded.Should().BeTrue();
  }

  [TestMethod]
  public void GetChangedVariables_ModifiedVariable_Included() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .WithData("original")
      .Build();
    service.CaptureSnapshot(new[] { variable });

    variable.Data = "modified";

    // Act
    var changed = service.GetChangedVariables(new[] { variable }).ToList();

    // Assert
    changed.Should().HaveCount(1);
    changed[0].Data.Should().Be("modified");
  }

  [TestMethod]
  public void GetChangedVariables_VolatileVariable_Excluded() {
    // Arrange
    service.CaptureSnapshot([]);

    var volatileVar = EnvironmentVariableBuilder.Default()
      .WithName("VOLATILE")
      .WithIsVolatile(true)
      .WithIsAdded(true)
      .Build();

    // Act
    var changed = service.GetChangedVariables(new[] { volatileVar }).ToList();

    // Assert
    changed.Should().BeEmpty("volatile vars are excluded from changed variables");
  }

  [TestMethod]
  public void GetChangedVariables_NoChanges_ReturnsEmpty() {
    // Arrange
    var variable = EnvironmentVariableBuilder.Default()
      .WithName("TEST")
      .Build();
    service.CaptureSnapshot(new[] { variable });

    // Act
    var changed = service.GetChangedVariables(new[] { variable }).ToList();

    // Assert
    changed.Should().BeEmpty("no variables changed");
  }

  #endregion
}
