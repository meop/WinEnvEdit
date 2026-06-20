using System.IO;

using Microsoft.Windows.ApplicationModel.Resources;

namespace WinEnvEdit.Helpers;

/// <summary>
/// Resolves localized UI strings from Strings/&lt;lang&gt;/Resources.resw via the Windows App SDK resource loader
/// (MRT Core). This loader works for unpackaged apps and under Native AOT; the legacy
/// Windows.ApplicationModel.Resources loader requires package identity and fails here. The loader reads
/// resources.pri (next to the exe) once and is cached for the process.
/// </summary>
public static class Localize {
  // Load the app PRI by explicit path. The parameterless ResourceLoader() looks for "resources.pri"; this app's
  // PRI is named WinEnvEdit.pri (after the project) and sits next to the exe, so we point at it directly to be
  // deterministic for the unpackaged build.
  private static readonly ResourceLoader loader =
    new(Path.Combine(AppContext.BaseDirectory, "WinEnvEdit.pri"));

  /// <summary>Returns the string for <paramref name="key"/>, or the key itself if it is missing (so a typo is
  /// visible in the UI rather than silently blank).</summary>
  public static string Get(string key) {
    var value = loader.GetString(key);
    return string.IsNullOrEmpty(value) ? key : value;
  }
}
