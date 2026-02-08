using System;
using System.Collections.Generic;
using System.Linq;

using WinEnvEdit.Models;

namespace WinEnvEdit.Services;

/// <summary>
/// Service for managing undo/redo history using snapshot-based stacks.
/// </summary>
public class UndoRedoService : IUndoRedoService {
  private const int MaxHistoryDepth = 50;

  private readonly Stack<List<EnvironmentVariable>> undoStack = new();
  private readonly Stack<List<EnvironmentVariable>> redoStack = new();
  private List<EnvironmentVariable> currentState = [];

  /// <inheritdoc/>
  public bool CanUndo => undoStack.Count > 0;

  public bool CanRedo => redoStack.Count > 0;

  /// <inheritdoc/>
  public void Reset(IEnumerable<EnvironmentVariable> variables) {
    undoStack.Clear();
    redoStack.Clear();
    currentState = DeepCopy(variables);
  }

  /// <inheritdoc/>
  public void PushState(IEnumerable<EnvironmentVariable> variables) {
    var newState = DeepCopy(variables);

    // Skip if state hasn't actually changed
    if (StatesAreEqual(currentState, newState)) {
      return;
    }

    // Push current state to undo history
    undoStack.Push(currentState);

    // Update current state
    currentState = newState;

    // Enforce max history depth
    if (undoStack.Count > MaxHistoryDepth) {
      var tempList = undoStack.ToList();
      tempList.RemoveAt(tempList.Count - 1); // Remove oldest
      undoStack.Clear();
      foreach (var item in tempList.AsEnumerable().Reverse()) {
        undoStack.Push(item);
      }
    }

    // New changes invalidate redo history
    redoStack.Clear();
  }

  /// <inheritdoc/>
  public IEnumerable<EnvironmentVariable>? Undo() {
    if (!CanUndo) {
      return null;
    }

    // Push current state to redo stack
    redoStack.Push(currentState);

    // Pop previous state from undo stack
    var previousState = undoStack.Pop();
    currentState = previousState;

    return DeepCopy(currentState);
  }

  /// <inheritdoc/>
  public IEnumerable<EnvironmentVariable>? Redo() {
    if (!CanRedo) {
      return null;
    }

    // Push current state to undo stack
    undoStack.Push(currentState);

    // Pop next state from redo stack
    var nextState = redoStack.Pop();
    currentState = nextState;

    return DeepCopy(currentState);
  }

  /// <inheritdoc/>
  public void ClearHistory() {
    undoStack.Clear();
    redoStack.Clear();
    currentState.Clear();
  }

  internal static bool StatesAreEqual(List<EnvironmentVariable> a, List<EnvironmentVariable> b) {
    if (a.Count != b.Count) {
      return false;
    }

    var sortedA = a.OrderBy(v => v.Scope).ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
    var sortedB = b.OrderBy(v => v.Scope).ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();

    for (var i = 0; i < sortedA.Count; i++) {
      if (!VariablesMatch(sortedA[i], sortedB[i])) {
        return false;
      }
    }

    return true;
  }

  internal static bool VariablesMatch(EnvironmentVariable a, EnvironmentVariable b) =>
    a.Scope == b.Scope &&
    string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) &&
    string.Equals(a.Data, b.Data, StringComparison.Ordinal) &&
    a.Type == b.Type &&
    a.IsAdded == b.IsAdded &&
    a.IsRemoved == b.IsRemoved &&
    a.IsVolatile == b.IsVolatile;

  /// <summary>
  /// Creates a deep copy of the environment variables collection.
  /// </summary>
  internal static List<EnvironmentVariable> DeepCopy(IEnumerable<EnvironmentVariable> variables) {
    return variables.Select(v => new EnvironmentVariable {
      Name = v.Name,
      Data = v.Data,
      Scope = v.Scope,
      Type = v.Type,
      IsVolatile = v.IsVolatile,
      IsAdded = v.IsAdded,
      IsRemoved = v.IsRemoved,
    }).ToList();
  }
}
