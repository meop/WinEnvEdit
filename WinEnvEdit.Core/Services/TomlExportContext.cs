using Tomlyn.Serialization;

namespace WinEnvEdit.Core.Services;

// Source-generated, AOT-safe (de)serialization for the export DTO. A List<EnvironmentExportEntry> at the
// document root serializes as the [[System]]/[[User]] array-of-tables layout (TableArrayStyle defaults to
// Headers).
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
