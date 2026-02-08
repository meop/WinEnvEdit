using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Core.Types;
using WinEnvEdit.Tests.Helpers;

using Xunit;

namespace WinEnvEdit.Tests.Services;

public class UndoRedoServiceTests {
  private UndoRedoService service;


  public UndoRedoServiceTests() {
    service = new UndoRedoService();
  }

  [Fact]
  public void CanUndo_InitiallyEmpty_ReturnsFalse() {
    // Act & Assert
    service.CanUndo.Should().BeFalse("undo stack is initially empty");
  }

  [Fact]
  public void CanRedo_InitiallyEmpty_ReturnsFalse() {
    // Act & Assert
    service.CanRedo.Should().BeFalse("redo stack is initially empty");
  }

  [Fact]
  public void PushState_AddsToUndoStack() {
    // Arrange
    var initial = new List<EnvironmentVariableModel>();
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST").Build(),
    };

    service.Reset(initial);

    // Act
    service.PushState(variables);

    // Assert
    service.CanUndo.Should().BeTrue("state was pushed to undo stack");
    service.CanRedo.Should().BeFalse("redo stack should be cleared");
  }

  [Fact]
  public void PushState_DuplicateState_SkipsPush() {
    // Arrange
    var initial = new List<EnvironmentVariableModel>();
    var variables = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST").Build(),
    };

    service.Reset(initial);
    service.PushState(variables); // Current: TEST, Undo: [initial]

    // Act - push identical state again
    service.PushState(variables);

    // Assert - only one undo entry, not two
    var restored = service.Undo();
    restored.Should().HaveCount(0, "single undo returns initial empty state");
    service.CanUndo.Should().BeFalse("duplicate push was skipped");
  }

  [Fact]
  public void PushState_ClearsRedoStack() {
    // Arrange
    var initial = new[] {
      EnvironmentVariableBuilder.Default().WithName("INIT").Build(),
    };
    var variables1 = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST1").Build(),
    };
    var variables2 = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST2").Build(),
    };

    service.Reset(initial);
    service.PushState(variables1); // Current: TEST1, Undo: [INIT]
    service.Undo(); // Current: INIT, Redo: [TEST1]
    service.CanRedo.Should().BeTrue();

    // Act
    service.PushState(variables2); // Current: TEST2, Undo: [INIT], Redo: Cleared

    // Assert
    service.CanRedo.Should().BeFalse("new state clears redo stack");
  }

  [Fact]
  public void Undo_WithEmptyStack_ReturnsNull() {
    // Act
    var result = service.Undo();

    // Assert
    result.Should().BeNull("undo stack is empty");
  }

  [Fact]
  public void Undo_RestoresPreviousState() {
    // Arrange
    var initial = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST").WithData("Initial").Build(),
    };
    var modified = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST").WithData("Modified").Build(),
    };

    service.Reset(initial);
    service.PushState(modified);

    // Act
    var restored = service.Undo();

    // Assert
    restored.Should().NotBeNull();
    restored.Should().HaveCount(1);
    restored!.First().Name.Should().Be("TEST");
    restored!.First().Data.Should().Be("Initial");
  }

  [Fact]
  public void Redo_WithEmptyStack_ReturnsNull() {
    // Act
    var result = service.Redo();

    // Assert
    result.Should().BeNull("redo stack is empty");
  }

  [Fact]
  public void Redo_RestoresNextState() {
    // Arrange
    var initial = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST").WithData("Initial").Build(),
    };
    var modified = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST").WithData("Modified").Build(),
    };

    service.Reset(initial);
    service.PushState(modified);
    service.Undo(); // Back to Initial

    // Act
    var restored = service.Redo(); // Forward to Modified

    // Assert
    restored.Should().NotBeNull();
    restored.Should().HaveCount(1);
    restored!.First().Name.Should().Be("TEST");
    restored!.First().Data.Should().Be("Modified");
  }

  [Fact]
  public void ClearHistory_ClearsBothStacks() {
    // Arrange
    var initial = new[] {
      EnvironmentVariableBuilder.Default().WithName("INIT").Build(),
    };
    var modified = new[] {
      EnvironmentVariableBuilder.Default().WithName("MOD").Build(),
    };

    service.Reset(initial);
    service.PushState(modified);
    service.Undo(); // Populate Redo

    // Act
    service.ClearHistory();

    // Assert
    service.CanUndo.Should().BeFalse("undo stack cleared");
    service.CanRedo.Should().BeFalse("redo stack cleared");
  }

  [Fact]
  public void PushState_EnforcesMaxDepth() {
    // Arrange
    var initial = new List<EnvironmentVariableModel>();
    service.Reset(initial);

    // Act - Push 60 states
    for (var i = 0; i < 60; i++) {
      var variables = new[] {
        EnvironmentVariableBuilder.Default().WithName($"TEST{i}").Build(),
      };
      service.PushState(variables);
    }

    // Assert - Should only keep last 50
    var count = 0;
    while (service.CanUndo) {
      service.Undo();
      count++;
    }

    count.Should().Be(50, "max history depth is 50");
  }

  [Fact]
  public void DeepCopy_CreatesIndependentCopy() {
    // Arrange
    var initial = new[] {
      EnvironmentVariableBuilder.Default()
        .WithName("TEST")
        .WithData("Initial")
        .Build(),
    };

    service.Reset(initial);

    // Modify original object passed to Reset (simulate external modification without PushState)
    // NOTE: In real usage, we should push state before modifying, or the service holds a copy.
    // service.Reset() makes a deep copy, so modifying 'initial' array objects shouldn't affect service state.
    initial[0].Data = "ExternalChange";

    // Act
    service.PushState(initial); // Push "ExternalChange" state? 
                                // Wait, if Reset made a copy, changing initial[0] doesn't change service internal state.
                                // But PushState takes the modified object.

    // Let's test that Undo returns the COPY of what was pushed/reset, not reference.

    var restored = service.Undo(); // Should return the state from Reset (Initial)

    // Assert
    restored.Should().NotBeNull();
    restored!.First().Data.Should().Be("Initial", "Undo should return the stored copy");
  }

  [Fact]
  public void DeepCopy_PreservesAllProperties() {
    // Arrange
    var initial = new[] {
      EnvironmentVariableBuilder.Default()
        .WithName("TEST")
        .WithData("Value")
        .WithScope(VariableScope.User)
        .WithType(RegistryValueKind.String)
        .WithIsVolatile(true)
        .WithIsAdded(true)
        .WithIsRemoved(false)
        .Build(),
    };

    var modified = new List<EnvironmentVariableModel>(); // Empty state

    service.Reset(initial);
    service.PushState(modified);

    // Act
    var restored = service.Undo();

    // Assert
    restored.Should().NotBeNull();
    var restoredVar = restored!.First();
    restoredVar.Name.Should().Be("TEST");
    restoredVar.Data.Should().Be("Value");
    restoredVar.Scope.Should().Be(VariableScope.User);
    restoredVar.Type.Should().Be(RegistryValueKind.String);
    restoredVar.IsVolatile.Should().BeTrue();
    restoredVar.IsAdded.Should().BeTrue();
    restoredVar.IsRemoved.Should().BeFalse();
  }

  #region Internal Helper Tests

  [Fact]
  public void DeepCopy_CreatesIndependentInstances() {
    // Arrange
    var original = new List<EnvironmentVariableModel> {
      new() { Name = "VAR", Data = "val" }
    };

    // Act
    var copy = UndoRedoService.DeepCopy(original);

    // Assert
    copy.Should().NotBeSameAs(original);
    copy[0].Should().NotBeSameAs(original[0]);
    copy[0].Name.Should().Be(original[0].Name);
  }

  [Fact]
  public void ComputeDelta_NoChanges_ReturnsEmptyDelta() {
    // Arrange
    var state = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("value1").Build(),
    };

    // Act
    var delta = UndoRedoService.ComputeDelta(state, state);

    // Assert
    delta.IsEmpty.Should().BeTrue("no changes between identical states");
  }

  [Fact]
  public void ComputeDelta_AddedVariable_CreatesVariableAdded() {
    // Arrange
    var fromState = new List<EnvironmentVariableModel>();
    var toState = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("NEW_VAR").WithData("value").Build(),
    };

    // Act
    var delta = UndoRedoService.ComputeDelta(fromState, toState);

    // Assert
    delta.Changes.Should().HaveCount(1);
    delta.Changes[0].Should().BeOfType<VariableAdded>();
    var added = (VariableAdded)delta.Changes[0];
    added.Name.Should().Be("NEW_VAR");
    added.Data.Should().Be("value");
  }

  [Fact]
  public void ComputeDelta_RemovedVariable_CreatesVariableRemoved() {
    // Arrange
    var fromState = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("OLD_VAR").WithData("value").Build(),
    };
    var toState = new List<EnvironmentVariableModel>();

    // Act
    var delta = UndoRedoService.ComputeDelta(fromState, toState);

    // Assert
    delta.Changes.Should().HaveCount(1);
    delta.Changes[0].Should().BeOfType<VariableRemoved>();
    var removed = (VariableRemoved)delta.Changes[0];
    removed.Name.Should().Be("OLD_VAR");
    removed.Data.Should().Be("value");
  }

  [Fact]
  public void ComputeDelta_ModifiedVariable_CreatesVariableModified() {
    // Arrange
    var fromState = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("old").Build(),
    };
    var toState = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("new").Build(),
    };

    // Act
    var delta = UndoRedoService.ComputeDelta(fromState, toState);

    // Assert
    delta.Changes.Should().HaveCount(1);
    delta.Changes[0].Should().BeOfType<VariableModified>();
    var modified = (VariableModified)delta.Changes[0];
    modified.Name.Should().Be("VAR1");
    modified.OldData.Should().Be("old");
    modified.NewData.Should().Be("new");
  }

  [Fact]
  public void ApplyDelta_VariableAdded_AddsVariable() {
    // Arrange
    var state = new List<EnvironmentVariableModel>();
    var delta = new StateDelta([
      new VariableAdded(VariableScope.User, "NEW_VAR", "value", RegistryValueKind.String, false, false, false)
    ]);

    // Act
    var result = UndoRedoService.ApplyDelta(state, delta);

    // Assert
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("NEW_VAR");
    result[0].Data.Should().Be("value");
  }

  [Fact]
  public void ApplyDelta_VariableRemoved_RemovesVariable() {
    // Arrange
    var state = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").Build(),
    };
    var delta = new StateDelta([
      new VariableRemoved(VariableScope.User, "VAR1", "data", RegistryValueKind.String, false, false, false)
    ]);

    // Act
    var result = UndoRedoService.ApplyDelta(state, delta);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void ApplyDelta_VariableModified_UpdatesVariable() {
    // Arrange
    var state = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("old").Build(),
    };
    var delta = new StateDelta([
      new VariableModified(VariableScope.User, "VAR1", "old", "new", RegistryValueKind.String, RegistryValueKind.String)
    ]);

    // Act
    var result = UndoRedoService.ApplyDelta(state, delta);

    // Assert
    result.Should().HaveCount(1);
    result[0].Data.Should().Be("new");
  }

  [Fact]
  public void ReverseDelta_VariableAdded_BecomesVariableRemoved() {
    // Arrange
    var delta = new StateDelta([
      new VariableAdded(VariableScope.User, "VAR1", "value", RegistryValueKind.String, false, true, false)
    ]);

    // Act
    var reversed = UndoRedoService.ReverseDelta(delta);

    // Assert
    reversed.Changes.Should().HaveCount(1);
    reversed.Changes[0].Should().BeOfType<VariableRemoved>();
    var removed = (VariableRemoved)reversed.Changes[0];
    removed.Name.Should().Be("VAR1");
    removed.IsAdded.Should().BeTrue("flags should be preserved");
  }

  [Fact]
  public void ReverseDelta_VariableRemoved_BecomesVariableAdded() {
    // Arrange
    var delta = new StateDelta([
      new VariableRemoved(VariableScope.User, "VAR1", "value", RegistryValueKind.String, true, false, false)
    ]);

    // Act
    var reversed = UndoRedoService.ReverseDelta(delta);

    // Assert
    reversed.Changes.Should().HaveCount(1);
    reversed.Changes[0].Should().BeOfType<VariableAdded>();
    var added = (VariableAdded)reversed.Changes[0];
    added.Name.Should().Be("VAR1");
    added.IsVolatile.Should().BeTrue("flags should be preserved");
  }

  [Fact]
  public void ReverseDelta_VariableModified_SwapsOldAndNew() {
    // Arrange
    var delta = new StateDelta([
      new VariableModified(VariableScope.User, "VAR1", "old", "new", RegistryValueKind.String, RegistryValueKind.ExpandString)
    ]);

    // Act
    var reversed = UndoRedoService.ReverseDelta(delta);

    // Assert
    reversed.Changes.Should().HaveCount(1);
    reversed.Changes[0].Should().BeOfType<VariableModified>();
    var modified = (VariableModified)reversed.Changes[0];
    modified.OldData.Should().Be("new", "old and new should be swapped");
    modified.NewData.Should().Be("old", "old and new should be swapped");
    modified.OldType.Should().Be(RegistryValueKind.ExpandString);
    modified.NewType.Should().Be(RegistryValueKind.String);
  }

  [Fact]
  public void ApplyDelta_MultipleChanges_ProcessesAll() {
    // Arrange - Start with one variable
    var state = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR1").WithData("old").Build(),
    };

    // Create delta with THREE changes
    var delta = new StateDelta([
      new VariableAdded(VariableScope.User, "VAR2", "value2", RegistryValueKind.String, false, false, false),
      new VariableAdded(VariableScope.User, "VAR3", "value3", RegistryValueKind.String, false, false, false),
      new VariableModified(VariableScope.User, "VAR1", "old", "new", RegistryValueKind.String, RegistryValueKind.String)
    ]);

    // Act
    var result = UndoRedoService.ApplyDelta(state, delta);

    // Assert - All THREE changes should be applied
    result.Should().HaveCount(3, "all three changes should be processed");
    result.Should().Contain(v => v.Name == "VAR1" && v.Data == "new", "VAR1 should be modified");
    result.Should().Contain(v => v.Name == "VAR2" && v.Data == "value2", "VAR2 should be added");
    result.Should().Contain(v => v.Name == "VAR3" && v.Data == "value3", "VAR3 should be added");
  }

  [Fact]
  public void UndoRedo_ChangeBackAndForth_WorksCorrectly() {
    // Arrange
    var stateA = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST").WithData("A").Build(),
    };
    var stateB = new[] {
      EnvironmentVariableBuilder.Default().WithName("TEST").WithData("B").Build(),
    };
    // Note: stateA is also the third state

    service.Reset(stateA);
    service.PushState(stateB); // A -> B
    service.PushState(stateA); // B -> A

    // Assert initial state
    service.CanUndo.Should().BeTrue();
    service.CanRedo.Should().BeFalse();

    // Act - Undo 1 (Back to B)
    var undo1 = service.Undo();
    undo1.Should().NotBeNull();
    undo1!.First(v => v.Name == "TEST").Data.Should().Be("B");
    service.CanUndo.Should().BeTrue();
    service.CanRedo.Should().BeTrue();

    // Act - Undo 2 (Back to A)
    var undo2 = service.Undo();
    undo2.Should().NotBeNull();
    undo2!.First(v => v.Name == "TEST").Data.Should().Be("A");
    service.CanUndo.Should().BeFalse();
    service.CanRedo.Should().BeTrue();

    // Act - Redo 1 (Forward to B)
    var redo1 = service.Redo();
    redo1.Should().NotBeNull();
    redo1!.First(v => v.Name == "TEST").Data.Should().Be("B");
    service.CanUndo.Should().BeTrue();
    service.CanRedo.Should().BeTrue();

    // Act - Redo 2 (Forward to A)
    var redo2 = service.Redo();
    redo2.Should().NotBeNull();
    redo2!.First(v => v.Name == "TEST").Data.Should().Be("A");
    service.CanUndo.Should().BeTrue();
    service.CanRedo.Should().BeFalse();
  }

  #endregion
}
