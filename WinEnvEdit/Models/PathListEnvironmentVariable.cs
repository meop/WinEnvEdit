using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace WinEnvEdit.Models;

/// <summary>
/// Represents an environment variable that contains a semicolon-delimited list of paths.
/// </summary>
public class PathListEnvironmentVariable : EnvironmentVariable {
  public ObservableCollection<PathItem> PathItems { get; set; } = [];

  public bool IsExpanded { get; set; }

  /// <summary>
  /// Synchronizes the Value property from the PathItems collection.
  /// </summary>
  public void SyncValueFromPaths() {
    Value = string.Join(";", PathItems.Select(p => p.Path));
  }

  /// <summary>
  /// Parses the Value property into PathItems collection.
  /// </summary>
  public void ParsePathsFromValue() {
    PathItems.Clear();
    if (string.IsNullOrWhiteSpace(Value)) {
      return;
    }

    var paths = Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
    foreach (var path in paths) {
      PathItems.Add(new PathItem { Path = path.Trim() });
    }
  }
}
