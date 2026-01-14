using System.IO;

namespace WinEnvEdit.Models;

/// <summary>
/// Represents a single path entry in a path-like environment variable (e.g., PATH).
/// </summary>
public class PathItem {
  public string PathValue { get; set; } = string.Empty;

  /// <summary>
  /// Gets whether this path exists on the file system.
  /// </summary>
  public bool Exists => !string.IsNullOrWhiteSpace(PathValue) && (Directory.Exists(PathValue) || File.Exists(PathValue));
}
