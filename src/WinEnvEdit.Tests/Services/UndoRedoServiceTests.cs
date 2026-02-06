using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Models;
using WinEnvEdit.Services;
using WinEnvEdit.Tests.Helpers;

namespace WinEnvEdit.Tests.Services;

[TestClass]
public class UndoRedoServiceTests {
  private UndoRedoService service = null!;

  [TestInitialize]
  public void Setup() {
    service = new UndoRedoService();
  }

  [TestMethod]
  public void CanUndo_InitiallyEmpty_ReturnsFalse() {
    // Act & Assert
    service.CanUndo.Should().BeFalse("undo stack is initially empty");
  }

  [TestMethod]
  public void CanRedo_InitiallyEmpty_ReturnsFalse() {
    // Act & Assert
    service.CanRedo.Should().BeFalse("redo stack is initially empty");
  }

  [TestMethod]
  public void PushState_AddsToUndoStack() {
    // Arrange
    var initial = new List<EnvironmentVariable>();
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

  [TestMethod]
  public void PushState_DuplicateState_SkipsPush() {
    // Arrange
    var initial = new List<EnvironmentVariable>();
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

  [TestMethod]
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

  [TestMethod]
  public void Undo_WithEmptyStack_ReturnsNull() {
    // Act
    var result = service.Undo();

    // Assert
    result.Should().BeNull("undo stack is empty");
  }

  [TestMethod]
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

  [TestMethod]
  public void Redo_WithEmptyStack_ReturnsNull() {
    // Act
    var result = service.Redo();

    // Assert
    result.Should().BeNull("redo stack is empty");
  }

  [TestMethod]
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

  [TestMethod]
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

  [TestMethod]
  public void PushState_EnforcesMaxDepth() {
    // Arrange
    var initial = new List<EnvironmentVariable>();
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

  [TestMethod]
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

  [TestMethod]
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

    var modified = new List<EnvironmentVariable>(); // Empty state

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
}
