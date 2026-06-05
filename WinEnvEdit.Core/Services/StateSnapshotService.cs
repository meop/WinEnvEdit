using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Types;

namespace WinEnvEdit.Core.Services;

/// <summary>
/// Tracks dirty state by comparing current variables against a captured snapshot.
/// </summary>
public class StateSnapshotService : IStateSnapshotService {
  internal readonly record struct EnvVarSnapshot(string Name, string Data, RegistryValueKind Type, VariableScope Scope);
  internal readonly record struct SnapshotKey(VariableScope Scope, string Name);

  private Dictionary<SnapshotKey, EnvVarSnapshot> snapshot = [];

  public void CaptureSnapshot(IEnumerable<EnvironmentVariableModel> variables) {
    snapshot = variables
      .Where(v => !v.IsRemoved && !v.IsVolatile)
      .ToDictionary(
        v => new SnapshotKey(v.Scope, v.Name),
        v => new EnvVarSnapshot(v.Name, v.Data, v.Type, v.Scope),
        new SnapshotKeyComparer()
      );
  }

  public bool IsDirty(IEnumerable<EnvironmentVariableModel> currentVariables) {
    var presentKeys = new HashSet<SnapshotKey>(new SnapshotKeyComparer());

    foreach (var variable in currentVariables.Where(v => !v.IsVolatile && !v.IsRemoved)) {
      var key = new SnapshotKey(variable.Scope, variable.Name);
      presentKeys.Add(key);

      if (!snapshot.TryGetValue(key, out var snapshotValue)) {
        return true;
      }

      if (variable.IsAdded || HasChanged(variable, snapshotValue)) {
        return true;
      }
    }

    foreach (var key in snapshot.Keys) {
      if (!presentKeys.Contains(key)) {
        return true;
      }
    }

    return false;
  }

  public IEnumerable<EnvironmentVariableModel> GetChangedVariables(IEnumerable<EnvironmentVariableModel> currentVariables) {
    var changed = new List<EnvironmentVariableModel>();
    var presentKeys = new HashSet<SnapshotKey>(new SnapshotKeyComparer());

    foreach (var variable in currentVariables.Where(v => !v.IsVolatile && !v.IsRemoved)) {
      var key = new SnapshotKey(variable.Scope, variable.Name);
      presentKeys.Add(key);

      if (!snapshot.TryGetValue(key, out var snapshotValue) || variable.IsAdded || HasChanged(variable, snapshotValue)) {
        changed.Add(variable);
      }
    }

    // Saved variables no longer present are pending deletes; rebuild a removal model from the snapshot.
    foreach (var (key, snapshotValue) in snapshot) {
      if (!presentKeys.Contains(key)) {
        changed.Add(new EnvironmentVariableModel {
          Name = snapshotValue.Name,
          Data = snapshotValue.Data,
          Type = snapshotValue.Type,
          Scope = snapshotValue.Scope,
          IsRemoved = true,
        });
      }
    }

    return changed;
  }

  internal static bool HasChanged(EnvironmentVariableModel variable, EnvVarSnapshot snapshot) =>
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
