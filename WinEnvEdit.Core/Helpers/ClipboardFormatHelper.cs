namespace WinEnvEdit.Core.Helpers;

/// <summary>
/// Helper for parsing clipboard text in "name=value" format.
/// </summary>
public static class ClipboardFormatHelper {
  /// <summary>
  /// Parses a single line in "name=value" format.
  /// Returns the value part if the format is valid, otherwise returns the entire text.
  /// </summary>
  /// <param name="text">The text to parse</param>
  /// <returns>Tuple of (name, value). Name is empty if format is invalid.</returns>
  public static (string Name, string Value) ParseSingleLine(string text) {
    if (string.IsNullOrWhiteSpace(text)) {
      return (string.Empty, string.Empty);
    }

    text = text.Trim();

    var separatorIndex = text.IndexOf('=');
    if (separatorIndex > 0) {
      // Found "name=value" format - extract both parts
      var name = text.Substring(0, separatorIndex).Trim();
      var value = text.Substring(separatorIndex + 1).Trim();
      return (name, value);
    }

    // No separator found or separator at start - treat entire text as value with no name
    return (string.Empty, text);
  }

  /// <summary>
  /// Parses multiple lines in "name=value" format.
  /// Skips invalid lines and lines without '=' or with empty names.
  /// </summary>
  /// <param name="text">The multi-line text to parse</param>
  /// <returns>List of (name, value) tuples for valid lines</returns>
  public static List<(string Name, string Value)> ParseMultiLine(string text) {
    var result = new List<(string, string)>();

    if (string.IsNullOrWhiteSpace(text)) {
      return result;
    }

    text = text.Trim();

    foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
      var equalsIndex = line.IndexOf('=');
      if (equalsIndex <= 0) {
        // No '=' found or '=' at start - skip this line
        continue;
      }

      var name = line.Substring(0, equalsIndex).Trim();
      var value = line.Substring(equalsIndex + 1).Trim();

      if (!string.IsNullOrEmpty(name)) {
        result.Add((name, value));
      }
    }

    return result;
  }
}
