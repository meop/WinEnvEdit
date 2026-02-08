using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Models;
using WinEnvEdit.Services;
using WinEnvEdit.Tests.Helpers;

namespace WinEnvEdit.Tests.Services;

[TestClass]
public class FileServiceTests : IDisposable {
  private readonly string testDirectory;
  private readonly FileService fileService;

  public FileServiceTests() {
    testDirectory = Path.Combine(Path.GetTempPath(), $"WinEnvEdit_Tests_{Guid.NewGuid()}");
    Directory.CreateDirectory(testDirectory);
    fileService = new FileService();
  }

  public void Dispose() {
    try {
      if (Directory.Exists(testDirectory)) {
        Directory.Delete(testDirectory, recursive: true);
      }
    }
    catch {
    }
  }

  #region ExportToFile Tests

  [TestMethod]
  public async Task ExportToFile_ExportsVariablesCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
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
    var filePath = Path.Combine(testDirectory, "test_export.toml");

    // Act
    await fileService.ExportToFile(filePath, variables);

    // Assert
    File.Exists(filePath).Should().BeTrue();
    var content = await File.ReadAllTextAsync(filePath);
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

  [TestMethod]
  public async Task ExportToFile_ExcludesRemovedVariables() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
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
    var filePath = Path.Combine(testDirectory, "test_export.toml");

    // Act
    await fileService.ExportToFile(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().Contain("ACTIVE_VAR");
    content.Should().NotContain("REMOVED_VAR");
  }

  [TestMethod]
  public async Task ExportToFile_ExcludesVolatileVariables() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
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
    var filePath = Path.Combine(testDirectory, "test_export.toml");

    // Act
    await fileService.ExportToFile(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().Contain("NORMAL_VAR");
    content.Should().NotContain("VOLATILE_VAR");
  }

  [TestMethod]
  public async Task ExportToFile_WithMultiStringType_ExportsCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .WithName("MULTI_VAR")
        .WithData("value1;value2;value3")
        .WithType(RegistryValueKind.MultiString)
        .Build(),
    };
    var filePath = Path.Combine(testDirectory, "test_export.toml");

    // Act
    await fileService.ExportToFile(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().Contain("MULTI_VAR");
    content.Should().Contain("type = \"MultiString\"");
  }

  [TestMethod]
  public async Task ExportToFile_WithDWordType_ExportsCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .WithName("DWORD_VAR")
        .WithData("12345")
        .WithType(RegistryValueKind.DWord)
        .Build(),
    };
    var filePath = Path.Combine(testDirectory, "test_export.toml");

    // Act
    await fileService.ExportToFile(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().Contain("DWORD_VAR");
    content.Should().Contain("type = \"DWord\"");
  }

  [TestMethod]
  public async Task ExportToFile_FormatsWithEmptyLinesBetweenArrays() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .WithName("VAR1")
        .WithData("value1")
        .Build(),
      EnvironmentVariableBuilder.Default()
        .WithName("VAR2")
        .WithData("value2")
        .Build(),
    };
    var filePath = Path.Combine(testDirectory, "test_export.toml");

    // Act
    await fileService.ExportToFile(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    var lines = content.Split('\n');

    // Verify empty lines exist before array table definitions (except first)
    for (var i = 1; i < lines.Length; i++) {
      if (lines[i].StartsWith("[[")) {
        lines[i - 1].Should().BeEmpty("Array table should have empty line before it (except first)");
      }
    }
  }

  [TestMethod]
  public async Task ExportToFile_FileEndsWithNewline() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .WithName("VAR1")
        .WithData("value1")
        .Build(),
    };
    var filePath = Path.Combine(testDirectory, "test_export.toml");

    // Act
    await fileService.ExportToFile(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().EndWith("\n");
  }

  #endregion

  #region ImportFromFile Tests

  [TestMethod]
  public async Task ImportFromFile_ImportsVariablesCorrectly() {
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
    var filePath = Path.Combine(testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await fileService.ImportFromFile(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(3);

    var userVar1 = variables.FirstOrDefault(v => v.Name == "USER_VAR1");
    userVar1.Should().NotBeNull();
    userVar1!.Data.Should().Be("user_value1");
    userVar1.Scope.Should().Be(VariableScope.User);
    userVar1.Type.Should().Be(RegistryValueKind.String);
    userVar1.IsAdded.Should().BeFalse();

    var userVar2 = variables.FirstOrDefault(v => v.Name == "USER_VAR2");
    userVar2.Should().NotBeNull();
    userVar2!.Data.Should().Be("user_value2");
    userVar2.Scope.Should().Be(VariableScope.User);
    userVar2.Type.Should().Be(RegistryValueKind.ExpandString);
    userVar2.IsAdded.Should().BeFalse();

    var systemVar1 = variables.FirstOrDefault(v => v.Name == "SYSTEM_VAR1");
    systemVar1.Should().NotBeNull();
    systemVar1!.Data.Should().Be("system_value1");
    systemVar1.Scope.Should().Be(VariableScope.System);
    systemVar1.Type.Should().Be(RegistryValueKind.DWord);
    systemVar1.IsAdded.Should().BeFalse();
  }

  [TestMethod]
  public async Task ImportFromFile_WithMultiStringType_ImportsCorrectly() {
    // Arrange
    var content = @"[[User]]
name = ""MULTI_VAR""
data = ""value1;value2;value3""
type = ""MultiString""
";
    var filePath = Path.Combine(testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await fileService.ImportFromFile(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("MULTI_VAR");
    variables[0].Type.Should().Be(RegistryValueKind.MultiString);
  }

  [TestMethod]
  public async Task ImportFromFile_WithDefaultStringType_ImportsCorrectly() {
    // Arrange
    var content = @"[[User]]
name = ""STRING_VAR""
data = ""string_value""
type = ""String""
";
    var filePath = Path.Combine(testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await fileService.ImportFromFile(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("STRING_VAR");
    variables[0].Type.Should().Be(RegistryValueKind.String);
  }

  [TestMethod]
  public async Task ImportFromFile_SkipsEmptyNames() {
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
    var filePath = Path.Combine(testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await fileService.ImportFromFile(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("VALID_VAR");
  }

  [TestMethod]
  public async Task ImportFromFile_RoundTripMaintainsData() {
    // Arrange
    var originalVariables = new List<EnvironmentVariable> {
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
    var exportPath = Path.Combine(testDirectory, "test_export.toml");
    var importPath = Path.Combine(testDirectory, "test_import.toml");

    // Act
    await fileService.ExportToFile(exportPath, originalVariables);
    var importedVariables = (await fileService.ImportFromFile(exportPath)).ToList();

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

  [TestMethod]
  public async Task ImportFromFile_HandlesMissingType() {
    // Arrange
    var content = @"[[User]]
name = ""NO_TYPE_VAR""
data = ""value""
type = ""String""
";
    var filePath = Path.Combine(testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await fileService.ImportFromFile(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Type.Should().Be(RegistryValueKind.String);
  }

  [TestMethod]
  public async Task ImportFromFile_WithEmptyFile_ReturnsEmptyList() {
    // Arrange
    var content = string.Empty;
    var filePath = Path.Combine(testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await fileService.ImportFromFile(filePath)).ToList();

    // Assert
    variables.Should().BeEmpty();
  }

  [TestMethod]
  public async Task ImportFromFile_WithInvalidSection_IgnoresInvalidSection() {
    // Arrange
    var content = @"[[InvalidSection]]
name = ""SHOULD_IGNORE""
data = ""value""
type = ""String""

[[User]]
name = ""VALID_VAR""
data = ""valid_value""
type = ""String""
";
    var filePath = Path.Combine(testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await fileService.ImportFromFile(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("VALID_VAR");
  }

  #endregion

  #region FormatTomlOutput Tests

  [TestMethod]
  public void FormatTomlOutput_AddsEmptyLinesBeforeArrayTables() {
    // Arrange
    var input = "[[System]]\nname = \"v1\"\n[[System]]\nname = \"v2\"";

    // Act
    var result = FileService.FormatTomlOutput(input);

    // Assert
    result.Should().Contain("\n\n[[System]]");
    result.Should().EndWith("\n");
  }

  [TestMethod]
  public void FormatTomlOutput_HandlesEmptyString() {
    // Act
    var result = FileService.FormatTomlOutput(string.Empty);

    // Assert
    result.Should().Be("\n");
  }

  [TestMethod]
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

  [TestMethod]
  public void FileExtension_ReturnsTomlExtension() {
    // Act
    var extension = FileService.FileExtension;

    // Assert
    extension.Should().Be(".toml");
  }

  [TestMethod]
  public void FileDescription_ReturnsCorrectDescription() {
    // Act
    var description = FileService.FileDescription;

    // Assert
    description.Should().Be("TOML Files");
  }

  [TestMethod]
  public void SuggestedFileName_ReturnsFileNameBasedOnAssembly() {
    // Act
    var fileName = FileService.SuggestedFileName;

    // Assert
    fileName.Should().NotBeNullOrEmpty();
    fileName.Should().EndWith(".toml");
  }

  #endregion
}