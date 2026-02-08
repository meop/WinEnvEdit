using System.Collections.Generic;

using WinEnvEdit.Core.Models;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Service for managing undo/redo history of environment variable states.
/// </summary>
public interface IUndoRedoService {
  /// <summary>
  /// Resets the undo/redo history and sets the initial state.
  /// </summary>
  /// <param name="variables">The initial state of environment variables.</param>
  public void Reset(IEnumerable<EnvironmentVariableModel> variables);

  /// <summary>
  /// Pushes the current state to the undo stack and clears the redo stack.
  /// </summary>
  /// <param name="variables">The current collection of environment variables.</param>
  public void PushState(IEnumerable<EnvironmentVariableModel> variables);

  /// <summary>
  /// Undoes the last change by restoring the previous state.
  /// </summary>
  /// <returns>The restored state, or null if no undo is available.</returns>
  public IEnumerable<EnvironmentVariableModel>? Undo();

  /// <summary>
  /// Redoes the last undone change.
  /// </summary>
  /// <returns>The restored state, or null if no redo is available.</returns>
  public IEnumerable<EnvironmentVariableModel>? Redo();

  /// <summary>
  /// Clears all undo and redo history.
  /// </summary>
  public void ClearHistory();

  /// <summary>
  /// Gets whether an undo operation is available.
  /// </summary>
  public bool CanUndo { get; }

  /// <summary>
  /// Gets whether a redo operation is available.
  /// </summary>
  public bool CanRedo { get; }
}
