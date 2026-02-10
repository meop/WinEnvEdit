namespace WinEnvEdit.Core.Helpers;

/// <summary>
/// Helper for parsing and formatting PATH-style environment variable values.
/// </summary>
public static class PathListHelper {
  /// <summary>
  /// Splits a semicolon-delimited path list into individual paths.
  /// </summary>
  /// <param name="pathList">The semicolon-delimited path string</param>
  /// <returns>List of trimmed, non-empty paths</returns>
  public static List<string> SplitPathList(string pathList) {
    if (string.IsNullOrWhiteSpace(pathList)) {
      return [];
    }

    return pathList
      .Split(';', StringSplitOptions.RemoveEmptyEntries)
      .Select(p => p.Trim())
      .ToList();
  }

  /// <summary>
  /// Joins a collection of paths into a semicolon-delimited string.
  /// </summary>
  /// <param name="paths">The paths to join</param>
  /// <returns>Semicolon-delimited path string</returns>
  public static string JoinPathList(IEnumerable<string> paths) => string.Join(";", paths);

  /// <summary>
  /// Computes the reconciliation actions needed to update an existing path list to match a new list.
  /// Returns lists of items to update, add, and remove.
  /// </summary>
  /// <param name="currentPaths">The current path list</param>
  /// <param name="newPaths">The new path list to reconcile to</param>
  /// <returns>Tuple of (itemsToUpdate, itemsToAdd, countToRemove)</returns>
  public static (List<(int Index, string NewValue)> ItemsToUpdate, List<string> ItemsToAdd, int CountToRemove) ReconcilePathLists(List<string> currentPaths, List<string> newPaths) {
    var itemsToUpdate = new List<(int, string)>();
    var itemsToAdd = new List<string>();
    var countToRemove = 0;

    // Update existing items or mark for adding
    for (var i = 0; i < newPaths.Count; i++) {
      if (i < currentPaths.Count) {
        // Update existing item if value changed
        if (currentPaths[i] != newPaths[i]) {
          itemsToUpdate.Add((i, newPaths[i]));
        }
      }
      else {
        // Add new item
        itemsToAdd.Add(newPaths[i]);
      }
    }

    // Mark extra items for removal
    if (currentPaths.Count > newPaths.Count) {
      countToRemove = currentPaths.Count - newPaths.Count;
    }

    return (itemsToUpdate, itemsToAdd, countToRemove);
  }
}
