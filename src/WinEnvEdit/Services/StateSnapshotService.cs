using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Win32;

using WinEnvEdit.Models;

namespace WinEnvEdit.Services;

/// <summary>
/// Tracks dirty state by comparing current variables against a captured snapshot.
/// </summary>
public class StateSnapshotService : IStateSnapshotService {
  internal readonly record struct EnvVarSnapshot(string Name, string Data, RegistryValueKind Type, VariableScope Scope);
  internal readonly record struct SnapshotKey(VariableScope Scope, string Name);

  private Dictionary<SnapshotKey, EnvVarSnapshot> snapshot = [];

  public void CaptureSnapshot(IEnumerable<EnvironmentVariable> variables) {
    snapshot = variables
      .Where(v => !v.IsRemoved && !v.IsVolatile)
      .ToDictionary(
        v => new SnapshotKey(v.Scope, v.Name),
        v => new EnvVarSnapshot(v.Name, v.Data, v.Type, v.Scope),
        new SnapshotKeyComparer()
      );
  }

  public bool IsDirty(IEnumerable<EnvironmentVariable> currentVariables) {
    var current = currentVariables.Where(v => !v.IsVolatile).ToList();

    // Check for removed variables (in snapshot but marked removed or not in current)
    foreach (var variable in current) {
      if (variable.IsRemoved) {
        var key = new SnapshotKey(variable.Scope, variable.Name);
        if (snapshot.ContainsKey(key)) {
          return true;
        }
      }
    }

    // Check for added variables
    var addedVars = current.Where(v => v.IsAdded && !v.IsRemoved);
    if (addedVars.Any()) {
      return true;
    }

    // Check for modified variables
    foreach (var variable in current.Where(v => !v.IsAdded && !v.IsRemoved)) {
      var key = new SnapshotKey(variable.Scope, variable.Name);
      if (snapshot.TryGetValue(key, out var snapshotValue)) {
        if (HasChanged(variable, snapshotValue)) {
          return true;
        }
      }
    }

    return false;
  }

  public IEnumerable<EnvironmentVariable> GetChangedVariables(IEnumerable<EnvironmentVariable> currentVariables) {
    var changed = new List<EnvironmentVariable>();
    var current = currentVariables.Where(v => !v.IsVolatile).ToList();

    foreach (var variable in current) {
      if (variable.IsRemoved) {
        // Variable marked for deletion - only include if it was in original snapshot
        var key = new SnapshotKey(variable.Scope, variable.Name);
        if (snapshot.ContainsKey(key)) {
          changed.Add(variable);
        }
        continue;
      }

      if (variable.IsAdded) {
        // Newly added variable
        changed.Add(variable);
        continue;
      }

      // Check if existing variable was modified
      var lookupKey = new SnapshotKey(variable.Scope, variable.Name);
      if (snapshot.TryGetValue(lookupKey, out var snapshotValue)) {
        if (HasChanged(variable, snapshotValue)) {
          changed.Add(variable);
        }
      }
    }

    return changed;
  }

  internal static bool HasChanged(EnvironmentVariable variable, EnvVarSnapshot snapshot) =>
    !string.Equals(variable.Name, snapshot.Name, StringComparison.Ordinal) ||
    !string.Equals(variable.Data, snapshot.Data, StringComparison.Ordinal) ||
    variable.Type != snapshot.Type;

  private class SnapshotKeyComparer : IEqualityComparer<SnapshotKey> {
    public bool Equals(SnapshotKey x, SnapshotKey y) =>
      x.Scope == y.Scope &&
      string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode(SnapshotKey obj) =>
      HashCode.Combine(obj.Scope, obj.Name.ToUpperInvariant());
  }
}
