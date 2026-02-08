using System;
using System.Collections.Generic;
using System.Linq;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Types;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for managing undo/redo history using delta-based changes.
/// Stores only what changed between states for memory efficiency.
/// </summary>
public class UndoRedoService : IUndoRedoService {
  private const int MaxHistoryDepth = 50;

  private readonly Stack<StateDelta> undoStack = new();
  private readonly Stack<StateDelta> redoStack = new();
  private List<EnvironmentVariableModel> currentState = [];

  public bool CanUndo => undoStack.Count > 0;
  public bool CanRedo => redoStack.Count > 0;

  public void Reset(IEnumerable<EnvironmentVariableModel> variables) {
    undoStack.Clear();
    redoStack.Clear();
    currentState = DeepCopy(variables);
  }

  public void PushState(IEnumerable<EnvironmentVariableModel> variables) {
    var newState = DeepCopy(variables);
    var delta = ComputeDelta(currentState, newState);

    if (delta.IsEmpty) {
      return;
    }

    undoStack.Push(delta);
    currentState = newState;

    if (undoStack.Count > MaxHistoryDepth) {
      // Remove oldest state efficiently
      var tempList = undoStack.ToList();
      tempList.RemoveAt(tempList.Count - 1);
      undoStack.Clear();
      foreach (var item in tempList.AsEnumerable().Reverse()) {
        undoStack.Push(item);
      }
    }

    redoStack.Clear();
  }

  public IEnumerable<EnvironmentVariableModel>? Undo() {
    if (!CanUndo) {
      return null;
    }

    var delta = undoStack.Pop();
    var reversedDelta = ReverseDelta(delta);

    redoStack.Push(delta);
    currentState = ApplyDelta(currentState, reversedDelta);

    return DeepCopy(currentState);
  }

  public IEnumerable<EnvironmentVariableModel>? Redo() {
    if (!CanRedo) {
      return null;
    }

    var delta = redoStack.Pop();
    undoStack.Push(delta);
    currentState = ApplyDelta(currentState, delta);

    return DeepCopy(currentState);
  }

  public void ClearHistory() {
    undoStack.Clear();
    redoStack.Clear();
    currentState.Clear();
  }

  /// <summary>
  /// Computes the delta (changes) needed to transform fromState into toState.
  /// Ignores transient UI flags (IsAdded, IsRemoved, IsVolatile) in comparison.
  /// </summary>
  internal static StateDelta ComputeDelta(List<EnvironmentVariableModel> fromState, List<EnvironmentVariableModel> toState) {
    var changes = new List<VariableDelta>();

    // Build lookup dictionaries (ignore removed variables - they're pending deletes)
    var comparer = new TupleComparer();
    var fromDict = fromState
      .Where(v => !v.IsRemoved)
      .ToDictionary(
        v => (v.Scope, v.Name),
        v => v,
        comparer
      );

    var toDict = toState
      .Where(v => !v.IsRemoved)
      .ToDictionary(
        v => (v.Scope, v.Name),
        v => v,
        comparer
      );

    // Find added and modified variables
    foreach (var toVar in toState.Where(v => !v.IsRemoved)) {
      var key = (toVar.Scope, toVar.Name);
      if (!fromDict.TryGetValue(key, out var fromVar)) {
        // Added
        changes.Add(new VariableAdded(
          toVar.Scope,
          toVar.Name,
          toVar.Data,
          toVar.Type,
          toVar.IsVolatile,
          toVar.IsAdded,
          toVar.IsRemoved
        ));
      }
      else if (fromVar.Data != toVar.Data || fromVar.Type != toVar.Type) {
        // Modified
        changes.Add(new VariableModified(
          toVar.Scope,
          toVar.Name,
          fromVar.Data,
          toVar.Data,
          fromVar.Type,
          toVar.Type
        ));
      }
    }

    // Find removed variables
    foreach (var fromVar in fromState.Where(v => !v.IsRemoved)) {
      var key = (fromVar.Scope, fromVar.Name);
      if (!toDict.ContainsKey(key)) {
        changes.Add(new VariableRemoved(
          fromVar.Scope,
          fromVar.Name,
          fromVar.Data,
          fromVar.Type,
          fromVar.IsVolatile,
          fromVar.IsAdded,
          fromVar.IsRemoved
        ));
      }
    }

    return new StateDelta(changes);
  }

  /// <summary>
  /// Applies a delta to a state, producing a new state.
  /// Creates a new list with changes applied.
  /// </summary>
  internal static List<EnvironmentVariableModel> ApplyDelta(List<EnvironmentVariableModel> state, StateDelta delta) {
    var result = DeepCopy(state);

    foreach (var change in delta.Changes) {
      switch (change) {
        case VariableAdded added:
          result.RemoveAll(v =>
            v.Scope == added.Scope &&
            string.Equals(v.Name, added.Name, StringComparison.OrdinalIgnoreCase)
          );
          result.Add(new EnvironmentVariableModel {
            Scope = added.Scope,
            Name = added.Name,
            Data = added.Data,
            Type = added.Type,
            IsVolatile = added.IsVolatile,
            IsAdded = added.IsAdded,
            IsRemoved = added.IsRemoved,
          });
          break;

        case VariableRemoved removed:
          result.RemoveAll(v =>
            v.Scope == removed.Scope &&
            string.Equals(v.Name, removed.Name, StringComparison.OrdinalIgnoreCase)
          );
          break;

        case VariableModified modified:
          var existing = result.FirstOrDefault(v =>
            v.Scope == modified.Scope &&
            string.Equals(v.Name, modified.Name, StringComparison.OrdinalIgnoreCase)
          );
          if (existing != null) {
            existing.Data = modified.NewData;
            existing.Type = modified.NewType;
          }
          break;
      }
    }

    return result.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
  }

  /// <summary>
  /// Reverses a delta so it can be applied for undo.
  /// Swaps adds/removes and old/new values.
  /// </summary>
  internal static StateDelta ReverseDelta(StateDelta delta) {
    var reversed = new List<VariableDelta>();

    foreach (var change in delta.Changes) {
      reversed.Add(change switch {
        VariableAdded added => new VariableRemoved(
          added.Scope,
          added.Name,
          added.Data,
          added.Type,
          added.IsVolatile,
          added.IsAdded,
          added.IsRemoved
        ),
        VariableRemoved removed => new VariableAdded(
          removed.Scope,
          removed.Name,
          removed.Data,
          removed.Type,
          removed.IsVolatile,
          removed.IsAdded,
          removed.IsRemoved
        ),
        VariableModified modified => new VariableModified(
          modified.Scope,
          modified.Name,
          modified.NewData,  // Swap: old becomes new
          modified.OldData,  // Swap: new becomes old
          modified.NewType,
          modified.OldType
        ),
        _ => throw new InvalidOperationException($"Unknown delta type: {change.GetType().Name}")
      });
    }

    return new StateDelta(reversed);
  }

  internal static List<EnvironmentVariableModel> DeepCopy(IEnumerable<EnvironmentVariableModel> variables) {
    return variables.Select(v => new EnvironmentVariableModel {
      Name = v.Name,
      Data = v.Data,
      Scope = v.Scope,
      Type = v.Type,
      IsVolatile = v.IsVolatile,
      IsAdded = v.IsAdded,
      IsRemoved = v.IsRemoved,
    }).ToList();
  }

  // Custom comparer for case-insensitive tuple keys
  private class TupleComparer : IEqualityComparer<(VariableScope Scope, string Name)> {
    public bool Equals((VariableScope Scope, string Name) x, (VariableScope Scope, string Name) y) =>
      x.Scope == y.Scope && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode((VariableScope Scope, string Name) obj) =>
      HashCode.Combine(obj.Scope, obj.Name.ToUpperInvariant());
  }
}
