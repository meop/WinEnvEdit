using Tomlyn.Model;
using Tomlyn.Serialization;

namespace WinEnvEdit.Core.Services;

// Source-generated metadata for Tomlyn's TomlTable model. Serializing/deserializing via this
// context uses the AOT-safe TomlSerializer overloads (no RequiresUnreferencedCode/RequiresDynamicCode)
// while still routing through the model text writer, which preserves the [[System]]/[[User]]
// array-of-tables layout (the POCO source-gen path can only emit inline arrays).
[TomlSerializable(typeof(TomlTable))]
internal partial class TomlExportContext : TomlSerializerContext {
}
