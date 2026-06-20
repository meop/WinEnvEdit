using System.Reflection;
using System.Text;

using Microsoft.Win32;

using Tomlyn;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Types;

namespace WinEnvEdit.Core.Services;

public class FileService : IFileService {
  private static readonly string fileExtension = ".toml";
  private static readonly string fileDescription = "TOML Files";
  private static readonly string suggestedFileName = GetSuggestedFileName();

  public static string FileExtension => fileExtension;
  public static string FileDescription => fileDescription;
  public static string SuggestedFileName => suggestedFileName;

  private static string GetSuggestedFileName() {
    var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
    var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "WinEnvEdit";
    return $"{product}{fileExtension}";
  }

  public async Task ExportToFile(string filePath, IEnumerable<EnvironmentVariableModel> variables) {
    using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
    await ExportToStream(stream, variables);
  }

  public async Task ExportToStream(Stream stream, IEnumerable<EnvironmentVariableModel> variables) {
    var export = new EnvironmentExport();
    foreach (var v in variables.Where(v => !v.IsRemoved && !v.IsVolatile)) {
      var entry = new EnvironmentExportEntry { Name = v.Name, Data = v.Data, Type = v.Type.ToString() };
      (v.Scope == VariableScope.System ? export.System : export.User).Add(entry);
    }

    // The source-gen path honors TableArrayStyle.Headers, so this emits [[System]]/[[User]].
    var tomlContent = TomlSerializer.Serialize(export, TomlExportContext.Default.EnvironmentExport);
    var formattedContent = FormatTomlOutput(tomlContent);

    // Write with LF line endings and UTF-8 encoding (no BOM)
    var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    var bytes = encoding.GetBytes(formattedContent);
    await stream.WriteAsync(bytes);
    await stream.FlushAsync();
  }

  internal static string FormatTomlOutput(string content) {
    // Normalize line endings to LF first (handle CRLF from Toml library)
    var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
    var lines = normalized.Split('\n', StringSplitOptions.None);
    var result = new List<string>();

    foreach (var line in lines) {
      // Collapse consecutive blank lines (and drop leading blanks): the serializer already emits a blank
      // line before each [[..]] header, so without this we get a double gap between variables.
      if (string.IsNullOrWhiteSpace(line)) {
        if (result.Count == 0 || result[^1].Length == 0) {
          continue;
        }

        result.Add("");
        continue;
      }

      // Ensure exactly one blank line before each array-table header
      if (line.StartsWith("[[") && result.Count > 0 && result[^1].Length != 0) {
        result.Add("");
      }

      result.Add(line);
    }

    // Join with LF, trim all trailing whitespace, then add exactly one newline
    return string.Join("\n", result).TrimEnd() + "\n";
  }

  public async Task<IEnumerable<EnvironmentVariableModel>> ImportFromFile(string filePath) {
    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
    return await ImportFromStream(stream);
  }

  public async Task<IEnumerable<EnvironmentVariableModel>> ImportFromStream(Stream stream) {
    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
    var content = await reader.ReadToEndAsync();
    var export = TomlSerializer.Deserialize(content, TomlExportContext.Default.EnvironmentExport) ?? new EnvironmentExport();

    var result = new List<EnvironmentVariableModel>();
    AddEntries(result, export.System, VariableScope.System);
    AddEntries(result, export.User, VariableScope.User);
    return result;
  }

  private static void AddEntries(List<EnvironmentVariableModel> result, List<EnvironmentExportEntry> entries, VariableScope scope) {
    foreach (var entry in entries) {
      if (string.IsNullOrEmpty(entry.Name)) {
        continue;
      }

      var type = Enum.TryParse<RegistryValueKind>(entry.Type, out var parsed) ? parsed : RegistryValueKind.String;
      result.Add(new EnvironmentVariableModel {
        Name = entry.Name,
        Data = entry.Data,
        Type = type,
        Scope = scope,
        IsAdded = false,
        IsRemoved = false,
        IsVolatile = false,
      });
    }
  }
}
