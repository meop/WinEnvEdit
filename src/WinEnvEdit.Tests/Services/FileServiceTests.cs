using System.Text;

using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Core.Types;
using WinEnvEdit.Tests.Helpers;

using Xunit;

namespace WinEnvEdit.Tests.Services;

public class FileServiceTests {
  private readonly FileService fileService;

  public FileServiceTests() {
    fileService = new FileService();
  }

  #region ExportToStream Tests

  [Fact]
  public async Task ExportToStream_ExportsVariablesCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default()
        .WithName("USER_VAR1")
        .WithData("user_value1")
        .WithScope(VariableScope.User)
        .WithType(RegistryValueKind.String)
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("SYSTEM_VAR1")
        .WithData("system_value1")
        .WithScope(VariableScope.System)
        .WithType(RegistryValueKind.ExpandString)
        .Build(),
    };
    using var stream = new MemoryStream();

    // Act
    await fileService.ExportToStream(stream, variables);

    // Assert
    stream.Position = 0;
    using var reader = new StreamReader(stream, Encoding.UTF8);
    var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

    content.Should().Contain("[User]");
    content.Should().Contain("[[User]]");
    content.Should().Contain("name = \"USER_VAR1\"");
    content.Should().Contain("data = \"user_value1\"");
    content.Should().Contain("type = \"String\"");
    content.Should().Contain("[[System]]");
    content.Should().Contain("name = \"SYSTEM_VAR1\"");
    content.Should().Contain("data = \"system_value1\"");
    content.Should().Contain("type = \"ExpandString\"");
  }

  [Fact]
  public async Task ExportToStream_ExcludesRemovedVariables() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default()
        .WithName("ACTIVE_VAR")
        .WithData("active_value")
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("REMOVED_VAR")
        .WithData("removed_value")
        .WithIsRemoved(true)
        .Build(),
    };
    using var stream = new MemoryStream();

    // Act
    await fileService.ExportToStream(stream, variables);

    // Assert
    stream.Position = 0;
    using var reader = new StreamReader(stream, Encoding.UTF8);
    var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

    content.Should().Contain("ACTIVE_VAR");
    content.Should().NotContain("REMOVED_VAR");
  }

  [Fact]
  public async Task ExportToStream_ExcludesVolatileVariables() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default()
        .WithName("NORMAL_VAR")
        .WithData("normal_value")
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("VOLATILE_VAR")
        .WithData("volatile_value")
        .WithIsVolatile(true)
        .Build(),
    };
    using var stream = new MemoryStream();

    // Act
    await fileService.ExportToStream(stream, variables);

    // Assert
    stream.Position = 0;
    using var reader = new StreamReader(stream, Encoding.UTF8);
    var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

    content.Should().Contain("NORMAL_VAR");
    content.Should().NotContain("VOLATILE_VAR");
  }

  [Fact]
  public async Task ExportToStream_FormatsWithEmptyLinesBetweenArrays() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default()
        .WithName("VAR1")
        .WithData("value1")
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("VAR2")
        .WithData("value2")
        .Build(),
    };
    using var stream = new MemoryStream();

    // Act
    await fileService.ExportToStream(stream, variables);

    // Assert
    stream.Position = 0;
    using var reader = new StreamReader(stream, Encoding.UTF8);
    var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    var lines = content.Split('\n');

    // Verify empty lines exist before array table definitions (except first)
    for (var i = 1; i < lines.Length; i++) {
      if (lines[i].StartsWith("[[")) {
        lines[i - 1].Should().BeEmpty("Array table should have empty line before it (except first)");
      }
    }
  }

  [Fact]
  public async Task ExportToStream_WithMultiStringType_ExportsCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default()
        .WithName("MULTI_VAR")
        .WithData("value1;value2;value3")
        .WithType(RegistryValueKind.MultiString)
        .Build(),
    };
    using var stream = new MemoryStream();

    // Act
    await fileService.ExportToStream(stream, variables);

    // Assert
    stream.Position = 0;
    using var reader = new StreamReader(stream, Encoding.UTF8);
    var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    content.Should().Contain("MULTI_VAR");
    content.Should().Contain("type = \"MultiString\"");
  }

  [Fact]
  public async Task ExportToStream_WithDWordType_ExportsCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default()
        .WithName("DWORD_VAR")
        .WithData("12345")
        .WithType(RegistryValueKind.DWord)
        .Build(),
    };
    using var stream = new MemoryStream();

    // Act
    await fileService.ExportToStream(stream, variables);

    // Assert
    stream.Position = 0;
    using var reader = new StreamReader(stream, Encoding.UTF8);
    var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    content.Should().Contain("DWORD_VAR");
    content.Should().Contain("type = \"DWord\"");
  }

  [Fact]
  public async Task ExportToStream_EndsWithNewline() {
    // Arrange
    var variables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default().WithName("VAR").WithData("VAL").Build(),
    };
    using var stream = new MemoryStream();

    // Act
    await fileService.ExportToStream(stream, variables);

    // Assert
    stream.Position = 0;
    using var reader = new StreamReader(stream, Encoding.UTF8);
    var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    content.Should().EndWith("\n");
  }

  #endregion

  #region ImportFromStream Tests

  [Fact]
  public async Task ImportFromStream_ImportsVariablesCorrectly() {
    // Arrange
    var content = @"[[User]]
name = ""USER_VAR1""
data = ""user_value1""
type = ""String""

[[User]]
name = ""USER_VAR2""
data = ""user_value2""
type = ""ExpandString""

[[System]]
name = ""SYSTEM_VAR1""
data = ""system_value1""
type = ""DWord""
";
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

    // Act
    var variables = (await fileService.ImportFromStream(stream)).ToList();

    // Assert
    variables.Count.Should().Be(3);

    var userVar1 = variables.FirstOrDefault(v => v.Name == "USER_VAR1");
    userVar1.Should().NotBeNull();
    userVar1!.Data.Should().Be("user_value1");
    userVar1.Scope.Should().Be(VariableScope.User);
    userVar1.Type.Should().Be(RegistryValueKind.String);

    var systemVar1 = variables.FirstOrDefault(v => v.Name == "SYSTEM_VAR1");
    systemVar1.Should().NotBeNull();
    systemVar1!.Data.Should().Be("system_value1");
    systemVar1.Scope.Should().Be(VariableScope.System);
    systemVar1.Type.Should().Be(RegistryValueKind.DWord);
  }

  [Fact]
  public async Task ImportFromStream_WithMultiStringType_ImportsCorrectly() {
    // Arrange
    var content = @"[[User]]
name = ""MULTI_VAR""
data = ""value1;value2;value3""
type = ""MultiString""
";
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

    // Act
    var variables = (await fileService.ImportFromStream(stream)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("MULTI_VAR");
    variables[0].Type.Should().Be(RegistryValueKind.MultiString);
  }

  [Fact]
  public async Task ImportFromStream_WithDefaultStringType_ImportsCorrectly() {
    // Arrange
    var content = @"[[User]]
name = ""STRING_VAR""
data = ""string_value""
type = ""String""
";
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

    // Act
    var variables = (await fileService.ImportFromStream(stream)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("STRING_VAR");
    variables[0].Type.Should().Be(RegistryValueKind.String);
  }

  [Fact]
  public async Task ImportFromStream_SkipsEmptyNames() {
    // Arrange
    var content = @"[[User]]
name = """"
data = ""value""
type = ""String""

[[User]]
name = ""VALID_VAR""
data = ""valid_value""
type = ""String""
";
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

    // Act
    var variables = (await fileService.ImportFromStream(stream)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("VALID_VAR");
  }

  [Fact]
  public async Task ImportFromStream_HandlesMissingType() {
    // Arrange
    var content = @"[[User]]
name = ""NO_TYPE_VAR""
data = ""value""
";
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

    // Act
    var variables = (await fileService.ImportFromStream(stream)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Type.Should().Be(RegistryValueKind.String, "default type should be String");
  }

  [Fact]
  public async Task ImportFromStream_WithEmptyFile_ReturnsEmptyList() {
    // Arrange
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));

    // Act
    var variables = (await fileService.ImportFromStream(stream)).ToList();

    // Assert
    variables.Should().BeEmpty();
  }

  [Fact]
  public async Task ImportFromStream_WithInvalidSection_IgnoresInvalidSection() {
    // Arrange
    var content = @"[[InvalidSection]]
name = ""SHOULD_IGNORE""
data = ""value""

[[User]]
name = ""VALID_VAR""
data = ""valid_value""
";
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

    // Act
    var variables = (await fileService.ImportFromStream(stream)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("VALID_VAR");
  }

  [Fact]
  public async Task ImportFromStream_RoundTripMaintainsData() {
    // Arrange
    var originalVariables = new List<EnvironmentVariableModel> {
      EnvironmentVariableBuilder.Default()
        .WithName("USER_VAR")
        .WithData("user_value")
        .WithScope(VariableScope.User)
        .WithType(RegistryValueKind.String)
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("SYSTEM_VAR")
        .WithData("system_value")
        .WithScope(VariableScope.System)
        .WithType(RegistryValueKind.ExpandString)
        .Build(),
    };
    using var stream = new MemoryStream();

    // Act
    await fileService.ExportToStream(stream, originalVariables);
    stream.Position = 0;
    var importedVariables = (await fileService.ImportFromStream(stream)).ToList();

    // Assert
    importedVariables.Count.Should().Be(originalVariables.Count);
    foreach (var original in originalVariables) {
      var imported = importedVariables.FirstOrDefault(v =>
        v.Name == original.Name &&
        v.Scope == original.Scope);
      imported.Should().NotBeNull();
      imported!.Data.Should().Be(original.Data);
      imported.Type.Should().Be(original.Type);
    }
  }

  #endregion

  #region FormatTomlOutput Tests

  [Fact]
  public void FormatTomlOutput_AddsEmptyLinesBeforeArrayTables() {
    // Arrange
    var input = "[[System]]\nname = \"v1\"\n[[System]]\nname = \"v2\"";

    // Act
    var result = FileService.FormatTomlOutput(input);

    // Assert
    result.Should().Contain("\n\n[[System]]");
    result.Should().EndWith("\n");
  }

  [Fact]
  public void FormatTomlOutput_HandlesEmptyString() {
    // Act
    var result = FileService.FormatTomlOutput(string.Empty);

    // Assert
    result.Should().Be("\n");
  }

  [Fact]
  public void FormatTomlOutput_NormalizesMixedLineEndings() {
    // Arrange
    var input = "[[User]]\r\nname = \"v1\"\r[[User]]\nname = \"v2\"";

    // Act
    var result = FileService.FormatTomlOutput(input);

    // Assert
    result.Should().NotContain("\r");
    result.Should().Contain("\n\n[[User]]");
  }

  #endregion

  #region Static Properties Tests

  [Fact]
  public void FileExtension_ReturnsTomlExtension() {
    // Act
    var extension = FileService.FileExtension;

    // Assert
    extension.Should().Be(".toml");
  }

  [Fact]
  public void FileDescription_ReturnsCorrectDescription() {
    // Act
    var description = FileService.FileDescription;

    // Assert
    description.Should().Be("TOML Files");
  }

  [Fact]
  public void SuggestedFileName_ReturnsFileNameBasedOnAssembly() {
    // Act
    var fileName = FileService.SuggestedFileName;

    // Assert
    fileName.Should().NotBeNullOrEmpty();
    fileName.Should().EndWith(".toml");
  }

  #endregion
}
