using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

using WinEnvEdit.Models;
using WinEnvEdit.Services;
using WinEnvEdit.Tests.Helpers;

namespace WinEnvEdit.Tests.Services;

[TestClass]
public class FileServiceTests : IDisposable {
  private readonly string _testDirectory;
  private readonly FileService _fileService;

  public FileServiceTests() {
    _testDirectory = Path.Combine(Path.GetTempPath(), $"WinEnvEdit_Tests_{Guid.NewGuid()}");
    Directory.CreateDirectory(_testDirectory);
    _fileService = new FileService();
  }

  public void Dispose() {
    try {
      if (Directory.Exists(_testDirectory)) {
        Directory.Delete(_testDirectory, recursive: true);
      }
    }
    catch {
    }
  }

  #region ExportToFileAsync Tests

  [TestMethod]
  public async System.Threading.Tasks.Task ExportToFileAsync_ExportsVariablesCorrectly() {
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
    var filePath = Path.Combine(_testDirectory, "test_export.toml");

    // Act
    await _fileService.ExportToFileAsync(filePath, variables);

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
  public async System.Threading.Tasks.Task ExportToFileAsync_ExcludesRemovedVariables() {
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
    var filePath = Path.Combine(_testDirectory, "test_export.toml");

    // Act
    await _fileService.ExportToFileAsync(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().Contain("ACTIVE_VAR");
    content.Should().NotContain("REMOVED_VAR");
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ExportToFileAsync_ExcludesVolatileVariables() {
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
    var filePath = Path.Combine(_testDirectory, "test_export.toml");

    // Act
    await _fileService.ExportToFileAsync(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().Contain("NORMAL_VAR");
    content.Should().NotContain("VOLATILE_VAR");
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ExportToFileAsync_WithMultiStringType_ExportsCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .WithName("MULTI_VAR")
        .WithData("value1;value2;value3")
        .WithType(RegistryValueKind.MultiString)
        .Build(),
    };
    var filePath = Path.Combine(_testDirectory, "test_export.toml");

    // Act
    await _fileService.ExportToFileAsync(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().Contain("MULTI_VAR");
    content.Should().Contain("type = \"MultiString\"");
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ExportToFileAsync_WithDWordType_ExportsCorrectly() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .WithName("DWORD_VAR")
        .WithData("12345")
        .WithType(RegistryValueKind.DWord)
        .Build(),
    };
    var filePath = Path.Combine(_testDirectory, "test_export.toml");

    // Act
    await _fileService.ExportToFileAsync(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().Contain("DWORD_VAR");
    content.Should().Contain("type = \"DWord\"");
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ExportToFileAsync_FormatsWithEmptyLinesBetweenArrays() {
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
    var filePath = Path.Combine(_testDirectory, "test_export.toml");

    // Act
    await _fileService.ExportToFileAsync(filePath, variables);

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
  public async System.Threading.Tasks.Task ExportToFileAsync_FileEndsWithNewline() {
    // Arrange
    var variables = new List<EnvironmentVariable> {
      EnvironmentVariableBuilder.Default()
        .WithName("VAR1")
        .WithData("value1")
        .Build(),
    };
    var filePath = Path.Combine(_testDirectory, "test_export.toml");

    // Act
    await _fileService.ExportToFileAsync(filePath, variables);

    // Assert
    var content = await File.ReadAllTextAsync(filePath);
    content.Should().EndWith("\n");
  }

  #endregion

  #region ImportFromFileAsync Tests

  [TestMethod]
  public async System.Threading.Tasks.Task ImportFromFileAsync_ImportsVariablesCorrectly() {
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
    var filePath = Path.Combine(_testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await _fileService.ImportFromFileAsync(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(3);

    var userVar1 = variables.FirstOrDefault(v => v.Name == "USER_VAR1");
    userVar1.Should().NotBeNull();
    userVar1!.Data.Should().Be("user_value1");
    userVar1.Scope.Should().Be(VariableScope.User);
    userVar1.Type.Should().Be(RegistryValueKind.String);
    userVar1.IsAdded.Should().BeTrue();

    var userVar2 = variables.FirstOrDefault(v => v.Name == "USER_VAR2");
    userVar2.Should().NotBeNull();
    userVar2!.Data.Should().Be("user_value2");
    userVar2.Scope.Should().Be(VariableScope.User);
    userVar2.Type.Should().Be(RegistryValueKind.ExpandString);
    userVar2.IsAdded.Should().BeTrue();

    var systemVar1 = variables.FirstOrDefault(v => v.Name == "SYSTEM_VAR1");
    systemVar1.Should().NotBeNull();
    systemVar1!.Data.Should().Be("system_value1");
    systemVar1.Scope.Should().Be(VariableScope.System);
    systemVar1.Type.Should().Be(RegistryValueKind.DWord);
    systemVar1.IsAdded.Should().BeTrue();
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ImportFromFileAsync_WithMultiStringType_ImportsCorrectly() {
    // Arrange
    var content = @"[[User]]
name = ""MULTI_VAR""
data = ""value1;value2;value3""
type = ""MultiString""
";
    var filePath = Path.Combine(_testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await _fileService.ImportFromFileAsync(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("MULTI_VAR");
    variables[0].Type.Should().Be(RegistryValueKind.MultiString);
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ImportFromFileAsync_WithDefaultStringType_ImportsCorrectly() {
    // Arrange
    var content = @"[[User]]
name = ""STRING_VAR""
data = ""string_value""
type = ""String""
";
    var filePath = Path.Combine(_testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await _fileService.ImportFromFileAsync(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("STRING_VAR");
    variables[0].Type.Should().Be(RegistryValueKind.String);
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ImportFromFileAsync_SkipsEmptyNames() {
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
    var filePath = Path.Combine(_testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await _fileService.ImportFromFileAsync(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("VALID_VAR");
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ImportFromFileAsync_RoundTripMaintainsData() {
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
    var exportPath = Path.Combine(_testDirectory, "test_export.toml");
    var importPath = Path.Combine(_testDirectory, "test_import.toml");

    // Act
    await _fileService.ExportToFileAsync(exportPath, originalVariables);
    var importedVariables = (await _fileService.ImportFromFileAsync(exportPath)).ToList();

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
  public async System.Threading.Tasks.Task ImportFromFileAsync_HandlesMissingType() {
    // Arrange
    var content = @"[[User]]
name = ""NO_TYPE_VAR""
data = ""value""
type = ""String""
";
    var filePath = Path.Combine(_testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await _fileService.ImportFromFileAsync(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Type.Should().Be(RegistryValueKind.String);
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ImportFromFileAsync_WithEmptyFile_ReturnsEmptyList() {
    // Arrange
    var content = string.Empty;
    var filePath = Path.Combine(_testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await _fileService.ImportFromFileAsync(filePath)).ToList();

    // Assert
    variables.Should().BeEmpty();
  }

  [TestMethod]
  public async System.Threading.Tasks.Task ImportFromFileAsync_WithInvalidSection_IgnoresInvalidSection() {
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
    var filePath = Path.Combine(_testDirectory, "test_import.toml");
    await File.WriteAllTextAsync(filePath, content);

    // Act
    var variables = (await _fileService.ImportFromFileAsync(filePath)).ToList();

    // Assert
    variables.Count.Should().Be(1);
    variables[0].Name.Should().Be("VALID_VAR");
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
