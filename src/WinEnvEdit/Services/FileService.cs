using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using Tomlyn;

using WinEnvEdit.Models;

namespace WinEnvEdit.Services;

public class FileService : IFileService {
  private static readonly string fileExtension = ".toml";
  private static readonly string fileDescription = "TOML Files";
  private static readonly string suggestedFileName = GetSuggestedFileName();

  public static string FileExtension => fileExtension;
  public static string FileDescription => fileDescription;
  public static string SuggestedFileName => suggestedFileName;

  private static string GetSuggestedFileName() {
    var assembly = Assembly.GetExecutingAssembly();
    var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "environment";
    return $"{product}{fileExtension}";
  }

  public async Task ExportToFile(string filePath, IEnumerable<EnvironmentVariable> variables) {
    var model = variables
      .Where(v => !v.IsRemoved && !v.IsVolatile)
      .GroupBy(v => v.Scope.ToString())
      .ToDictionary(
        g => g.Key,
        g => g.Select(v => new Dictionary<string, object> {
          ["name"] = v.Name,
          ["data"] = v.Data,
          ["type"] = v.Type.ToString()
        }).ToList()
      );

    var tomlContent = Toml.FromModel(model);
    var formattedContent = FormatTomlOutput(tomlContent);

    // Write with LF line endings and UTF-8 encoding (no BOM)
    await File.WriteAllTextAsync(filePath, formattedContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
  }

  internal static string FormatTomlOutput(string content) {
    // Normalize line endings to LF first (handle CRLF from Toml library)
    var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
    var lines = normalized.Split('\n', StringSplitOptions.None);
    var result = new List<string>();

    for (var i = 0; i < lines.Length; i++) {
      var line = lines[i];

      // Add empty line before array table definitions ([[), but not for first line
      if (line.StartsWith("[[") && result.Count > 0) {
        result.Add("");
      }

      result.Add(line);
    }

    // Join with LF, trim all trailing whitespace, then add exactly one newline
    return string.Join("\n", result).TrimEnd() + "\n";
  }

  public async Task<IEnumerable<EnvironmentVariable>> ImportFromFile(string filePath) {
    var content = await File.ReadAllTextAsync(filePath);
    var model = Toml.ToModel(content);
    var result = new List<EnvironmentVariable>();
    var sections = new[] { ("System", VariableScope.System), ("User", VariableScope.User) };

    foreach (var (sectionName, scope) in sections) {
      if (model.TryGetValue(sectionName, out var sectionObj) && sectionObj is IEnumerable varList) {
        foreach (var item in varList) {
          if (item is IDictionary<string, object> varProps) {
            var name = varProps.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? string.Empty : string.Empty;
            var data = varProps.TryGetValue("data", out var dataObj) ? dataObj?.ToString() ?? string.Empty : string.Empty;
            var type = RegistryValueKind.String;
            if (varProps.TryGetValue("type", out var typeObj) && typeObj is string typeStr && Enum.TryParse<RegistryValueKind>(typeStr, out var parsedType)) {
              type = parsedType;
            }

            if (!string.IsNullOrEmpty(name)) {
              result.Add(new EnvironmentVariable {
                Name = name,
                Data = data,
                Type = type,
                Scope = scope,
                IsAdded = false,
                IsRemoved = false,
                IsVolatile = false
              });
            }
          }
        }
      }
    }

    return result;
  }
}
