using FluentAssertions;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Core.Types;
using WinEnvEdit.Tests.Builders;

using Xunit;

namespace WinEnvEdit.Tests.Services;

/// <summary>
/// Integration tests over the real StateSnapshotService + UndoRedoService, replicating the exact call
/// sequence MainWindowViewModel performs for each user flow. After an undo/redo the view model rebuilds its
/// collection from the service result, so AllVariables() == the service result — that is what these assert on.
/// </summary>
public class StateUndoFlowTests {
  private readonly StateSnapshotService snapshot = new();
  private readonly UndoRedoService undo = new();

  private static List<EnvironmentVariableModel> Vars(params string[] names) =>
    names.Select(n => EnvironmentVariableBuilder.Default().WithName(n).WithData($"{n}-val").Build()).ToList();

  // Mirrors MainWindowViewModel.LoadVariables()
  private void Load(List<EnvironmentVariableModel> vars) {
    snapshot.CaptureSnapshot(vars);
    undo.Reset(vars);
  }

  // Mirrors MainWindowViewModel.Save() + VariableScopeViewModel.CleanupAfterSave():
  // drop removed vars, clear the IsAdded flag on survivors, recapture snapshot, push committed state.
  private List<EnvironmentVariableModel> Save(List<EnvironmentVariableModel> current) {
    var committed = current.Where(v => !v.IsRemoved).ToList();
    foreach (var v in committed) {
      v.IsAdded = false;
    }

    snapshot.CaptureSnapshot(committed);
    undo.PushState(committed);
    return committed;
  }

  [Fact]
  public void DeleteSaveUndo_RestoredVariableIsDirty() {
    // Load A, B, C
    var current = Vars("A", "B", "C");
    Load(current);

    // Delete B (existing var → marked removed, kept in collection), then push state as the VM does
    current.Single(v => v.Name == "B").IsRemoved = true;
    undo.PushState(current);
    snapshot.IsDirty(current).Should().BeTrue("a pending deletion is dirty");

    // Save → B is gone from the registry/snapshot
    current = Save(current);
    snapshot.IsDirty(current).Should().BeFalse("everything is persisted right after save");

    // Undo → B comes back; relative to the saved registry (which no longer has B) this is a pending change
    var restored = undo.Undo()!.ToList();
    restored.Should().Contain(v => v.Name == "B", "undo restores the deleted variable");
    snapshot.IsDirty(restored).Should().BeTrue("re-adding a variable absent from the saved snapshot is dirty");
  }

  [Fact]
  public void DeleteUndoWithoutSave_IsClean() {
    var current = Vars("A", "B", "C");
    Load(current);

    current.Single(v => v.Name == "B").IsRemoved = true;
    undo.PushState(current);

    // Undo the unsaved deletion → back to the clean loaded state
    var restored = undo.Undo()!.ToList();
    snapshot.IsDirty(restored).Should().BeFalse("undoing an uncommitted deletion returns to the clean state");
  }

  [Fact]
  public void MoveAcrossScopes_PersistsRemovalAndAddition() {
    // A user variable X exists; the move marks the source removed and adds X to System (what the VM does).
    var userX = EnvironmentVariableBuilder.Default().WithName("X").WithData("v").WithScope(VariableScope.User).Build();
    Load([userX]);

    userX.IsRemoved = true; // source.RemoveVariable on an existing (non-added) variable
    var systemX = EnvironmentVariableBuilder.Default().WithName("X").WithData("v").WithScope(VariableScope.System).WithIsAdded(true).Build();
    var current = new List<EnvironmentVariableModel> { userX, systemX };

    snapshot.IsDirty(current).Should().BeTrue("a cross-scope move is a pending change");

    var changed = snapshot.GetChangedVariables(current).ToList();
    changed.Should().Contain(v => v.Scope == VariableScope.User && v.Name == "X" && v.IsRemoved, "the source copy must be deleted");
    changed.Should().Contain(v => v.Scope == VariableScope.System && v.Name == "X" && !v.IsRemoved, "the target copy must be written");
  }

  [Fact]
  public void PasteNewVariable_IsDirtyAndPersists() {
    Load(Vars("A"));

    // Paste adds a brand-new variable (IsAdded) — value-only, default String type.
    var current = Vars("A");
    current.Add(EnvironmentVariableBuilder.Default().WithName("B").WithData("b").WithIsAdded(true).Build());

    snapshot.IsDirty(current).Should().BeTrue();
    snapshot.GetChangedVariables(current).Should().Contain(v => v.Name == "B");
  }

  [Fact]
  public void AddSaveUndo_RemovedVariableIsDirty() {
    var current = Vars("A", "B");
    Load(current);

    // Add C
    current.Add(EnvironmentVariableBuilder.Default().WithName("C").WithData("C-val").WithIsAdded(true).Build());
    undo.PushState(current);
    snapshot.IsDirty(current).Should().BeTrue("a newly added variable is dirty");

    // Save → A, B, C are the committed baseline
    current = Save(current);
    snapshot.IsDirty(current).Should().BeFalse();

    // Undo → C disappears; relative to the saved snapshot (which has C) that is a pending removal
    var restored = undo.Undo()!.ToList();
    restored.Should().NotContain(v => v.Name == "C", "undo removes the added variable");
    snapshot.IsDirty(restored).Should().BeTrue("dropping a variable that exists in the saved snapshot is dirty");
  }
}
