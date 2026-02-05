using System.Collections.Generic;

using WinEnvEdit.Models;

namespace WinEnvEdit.Services;

/// <summary>
/// Service for tracking dirty state via snapshots of environment variables.
/// </summary>
public interface IStateSnapshotService {
  /// <summary>
  /// Captures a snapshot of the current state of variables.
  /// Call after loading from registry or saving changes.
  /// </summary>
  public void CaptureSnapshot(IEnumerable<EnvironmentVariable> variables);

  /// <summary>
  /// Checks if the current variables differ from the snapshot.
  /// </summary>
  public bool IsDirty(IEnumerable<EnvironmentVariable> currentVariables);

  /// <summary>
  /// Gets the variables that have changed from the snapshot.
  /// Includes added, removed, and modified variables.
  /// </summary>
  public IEnumerable<EnvironmentVariable> GetChangedVariables(IEnumerable<EnvironmentVariable> currentVariables);
}
