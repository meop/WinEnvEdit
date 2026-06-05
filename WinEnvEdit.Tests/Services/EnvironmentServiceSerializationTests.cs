using FluentAssertions;

using Microsoft.Win32;

using WinEnvEdit.Core.Models;
using WinEnvEdit.Core.Services;
using WinEnvEdit.Core.Types;

using Xunit;

namespace WinEnvEdit.Tests.Services;

public class EnvironmentServiceSerializationTests {
  [Fact]
  public void SerializeDeserialize_RoundTripsAllFields() {
    var original = new List<EnvironmentVariableModel> {
      new() { Name = "PATH", Data = @"C:\a;C:\b", Type = RegistryValueKind.ExpandString, Scope = VariableScope.System },
      new() { Name = "GOPATH", Data = @"C:\go", Type = RegistryValueKind.String, Scope = VariableScope.System },
      new() { Name = "OLD", Data = "", Type = RegistryValueKind.String, Scope = VariableScope.System, IsRemoved = true },
    };

    var result = EnvironmentService.DeserializeChanges(EnvironmentService.SerializeChanges(original));

    result.Should().HaveCount(3);
    result[0].Name.Should().Be("PATH");
    result[0].Data.Should().Be(@"C:\a;C:\b");
    result[0].Type.Should().Be(RegistryValueKind.ExpandString);
    result[0].IsRemoved.Should().BeFalse();
    result[2].Name.Should().Be("OLD");
    result[2].IsRemoved.Should().BeTrue();
    result.Should().OnlyContain(v => v.Scope == VariableScope.System);
  }

  [Theory]
  [InlineData("value with\ttab")]
  [InlineData("value\nwith newline")]
  [InlineData("trailing spaces   ")]
  [InlineData("%MACRO%\\and;semis")]
  [InlineData("")]
  public void SerializeDeserialize_PreservesSpecialCharacters(string data) {
    var original = new List<EnvironmentVariableModel> {
      new() { Name = "VAR", Data = data, Type = RegistryValueKind.String, Scope = VariableScope.System },
    };

    var result = EnvironmentService.DeserializeChanges(EnvironmentService.SerializeChanges(original));

    result.Should().ContainSingle();
    result[0].Data.Should().Be(data);
  }

  [Fact]
  public void DeserializeChanges_EmptyInput_ReturnsEmpty() {
    EnvironmentService.DeserializeChanges(string.Empty).Should().BeEmpty();
  }
}
