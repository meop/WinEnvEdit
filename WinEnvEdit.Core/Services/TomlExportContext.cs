using Tomlyn.Serialization;

namespace WinEnvEdit.Core.Services;

// Source-generated, AOT-safe (de)serialization for the export DTO. The source-gen path honors TableArrayStyle
// (default Headers), so a List<EnvironmentExportEntry> at the document root emits the [[System]]/[[User]]
// array-of-tables layout natively - no hand-built TomlTable model needed.
[TomlSerializable(typeof(EnvironmentExport))]
internal partial class TomlExportContext : TomlSerializerContext {
}

internal sealed class EnvironmentExport {
  public List<EnvironmentExportEntry> System { get; set; } = [];
  public List<EnvironmentExportEntry> User { get; set; } = [];
}

internal sealed class EnvironmentExportEntry {
  [TomlPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [TomlPropertyName("data")]
  public string Data { get; set; } = string.Empty;

  [TomlPropertyName("type")]
  public string Type { get; set; } = string.Empty;
}
