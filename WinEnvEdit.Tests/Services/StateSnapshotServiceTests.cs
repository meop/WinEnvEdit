using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Core.Types;
using WinEnvEdit.Tests.Builders;

using Xunit;

namespace WinEnvEdit.Tests.Services;

public class StateSnapshotServiceTests {
  private StateSnapshotService service;


  public StateSnapshotServiceTests() {
    service = new StateSnapshotService();
  }

  #region CaptureSnapshot Tests

  [Fact]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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


  [Fact]
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

  [Fact]
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

  [Fact]
  public void IsDirty_EmptySnapshot_EmptyCurrent_ReturnsFalse() {
    // Arrange
    service.CaptureSnapshot([]);

    // Act
    var result = service.IsDirty([]);

    // Assert
    result.Should().BeFalse("both snapshot and current are empty");
  }

  [Fact]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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

  #region Internal Logic Tests

  [Fact]
  public void HasChanged_DetectsNameChange() {
    // Arrange
    var variable = new EnvironmentVariableModel { Name = "NEW", Data = "VAL", Type = RegistryValueKind.String };
    var snapshot = new StateSnapshotService.EnvVarSnapshot("OLD", "VAL", RegistryValueKind.String, VariableScope.User);

    // Act & Assert
    StateSnapshotService.HasChanged(variable, snapshot).Should().BeTrue();
  }

  [Fact]
  public void HasChanged_DetectsDataChange() {
    // Arrange
    var variable = new EnvironmentVariableModel { Name = "VAR", Data = "NEW", Type = RegistryValueKind.String };
    var snapshot = new StateSnapshotService.EnvVarSnapshot("VAR", "OLD", RegistryValueKind.String, VariableScope.User);

    // Act & Assert
    StateSnapshotService.HasChanged(variable, snapshot).Should().BeTrue();
  }

  [Fact]
  public void HasChanged_DetectsTypeChange() {
    // Arrange
    var variable = new EnvironmentVariableModel { Name = "VAR", Data = "VAL", Type = RegistryValueKind.ExpandString };
    var snapshot = new StateSnapshotService.EnvVarSnapshot("VAR", "VAL", RegistryValueKind.String, VariableScope.User);

    // Act & Assert
    StateSnapshotService.HasChanged(variable, snapshot).Should().BeTrue();
  }

  #endregion
}
