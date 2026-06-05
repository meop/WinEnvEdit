using FluentAssertions;

using WinEnvEdit.Core.Validators;

using Xunit;

namespace WinEnvEdit.Tests.Validators;

public class VariableValidatorTests {
  #region LooksLikePath Tests

  [Theory]
  [InlineData(@"C:\path")]
  [InlineData(@"C:\")] // drive root in backslash form
  [InlineData("z:\\temp")] // lowercase drive letter
  [InlineData("%USERPROFILE%\\go")]
  [InlineData("%USERPROFILE2%\\go")]
  [InlineData("%ProgramFiles(x86)%")]
  [InlineData("%MY_VAR%\\path")]
  [InlineData("%SystemRoot%")]
  [InlineData("%A%")] // minimal valid macro
  [InlineData("%A%B%")] // closes at first matching percent
  [InlineData("  C:\\path  ")] // surrounding whitespace is trimmed
  [InlineData("  %VAR%  ")]
  public void LooksLikePath_PathLikeValues_ReturnsTrue(string value) {
    VariableValidator.LooksLikePath(value).Should().BeTrue();
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("\t")] // whitespace-only
  [InlineData("hello")]
  [InlineData("%%")]
  [InlineData("%")] // bare percent
  [InlineData("%AB")] // no closing percent
  [InlineData("%=VAR%")]
  [InlineData("%AB=CD%")] // '=' terminates the scan before a closing percent
  [InlineData("1:\\temp")] // non-letter drive prefix
  [InlineData("D:")] // bare drive, no path
  [InlineData("C:relative")] // drive-relative, not canonical
  [InlineData("C:/forward")] // forward-slash form is not treated as a path
  [InlineData("foo%BAR%")] // macro not at start
  [InlineData("not a path")]
  public void LooksLikePath_NonPathValues_ReturnsFalse(string value) {
    VariableValidator.LooksLikePath(value).Should().BeFalse();
  }

  #endregion

  #region ValidateName Tests

  [Fact]
  public void ValidateName_Empty_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName(string.Empty);

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot be empty");
  }

  [Fact]
  public void ValidateName_WithEquals_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST=VALUE");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain '=' characters");
  }

  [Fact]
  public void ValidateName_WithNullChar_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST\0VAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain null characters");
  }

  [Fact]
  public void ValidateName_WithSpace_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST VAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain spaces");
  }

  [Fact]
  public void ValidateName_SingleSpace_ReturnsSpacesError() {
    // Act
    var result = VariableValidator.ValidateName(" ");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain spaces");
  }

  [Fact]
  public void ValidateName_Tab_ReturnsSpacesError() {
    // Act
    var result = VariableValidator.ValidateName("TEST\tVAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain spaces");
  }

  [Fact]
  public void ValidateName_WithSemicolon_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST;VAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain ';' characters");
  }

  [Fact]
  public void ValidateName_WithPercent_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST%VAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain '%' characters");
  }

  [Fact]
  public void ValidateName_ExceedsMaxLength_ReturnsError() {
    // Arrange - create name with 256 characters (exceeds 255 limit)
    var longName = new string('A', 256);

    // Act
    var result = VariableValidator.ValidateName(longName);

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot exceed 255 characters");
  }

  [Fact]
  public void ValidateName_AtMaxLength_ReturnsSuccess() {
    // Arrange - create name with exactly 255 characters
    var maxLengthName = new string('A', 255);

    // Act
    var result = VariableValidator.ValidateName(maxLengthName);

    // Assert
    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [Fact]
  public void ValidateName_Valid_ReturnsSuccess() {
    // Act
    var result = VariableValidator.ValidateName("VALID_VAR_NAME");

    // Assert
    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [Fact]
  public void ValidateName_WithParentheses_ReturnsSuccess() {
    // Windows permits parentheses in names (e.g. "ProgramFiles(x86)")
    var result = VariableValidator.ValidateName("ProgramFiles(x86)");

    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [Fact]
  public void ValidateNameAllErrors_ReturnsMultipleErrors() {
    // Act
    var errors = VariableValidator.ValidateNameAllErrors("INVALID=NAME WITH SPACE;");

    // Assert
    errors.Should().Contain("cannot contain '=' characters");
    errors.Should().Contain("cannot contain spaces");
    errors.Should().Contain("cannot contain ';' characters");
  }

  #endregion

  #region ValidateData Tests

  [Fact]
  public void ValidateData_WithNullChar_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateData("test\0value");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain null characters");
  }

  [Fact]
  public void ValidateData_ExceedsMaxLength_ReturnsError() {
    // Arrange - create value with 32768 characters (exceeds 32767 limit)
    var longValue = new string('A', 32768);

    // Act
    var result = VariableValidator.ValidateData(longValue);

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot exceed 32767 characters");
  }

  [Fact]
  public void ValidateData_AtMaxLength_ReturnsSuccess() {
    // Arrange - create value with exactly 32767 characters
    var maxLengthValue = new string('A', 32767);

    // Act
    var result = VariableValidator.ValidateData(maxLengthValue);

    // Assert
    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [Fact]
  public void ValidateData_Empty_ReturnsSuccess() {
    // Empty values are valid (e.g. clearing a variable's data)
    var result = VariableValidator.ValidateData(string.Empty);

    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [Fact]
  public void ValidateData_Valid_ReturnsSuccess() {
    // Act
    var result = VariableValidator.ValidateData("valid value with spaces and special chars !@#$");

    // Assert
    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [Fact]
  public void ValidateDataAllErrors_ReturnsMultipleErrors() {
    // Arrange
    var longValueWithNull = new string('A', 32768) + "\0";

    // Act
    var errors = VariableValidator.ValidateDataAllErrors(longValueWithNull);

    // Assert
    errors.Should().Contain("cannot exceed 32767 characters");
    errors.Should().Contain("cannot contain null characters");
  }

  #endregion

  #region ValidateForAdd Tests

  [Fact]
  public void ValidateForAdd_InvalidName_ReturnsNameError() {
    // Act
    var (isValid, message) = VariableValidator.ValidateForAdd("INVALID=NAME", "value");

    // Assert
    isValid.Should().BeFalse();
    message.Should().Be("cannot contain '=' characters");
  }

  [Fact]
  public void ValidateForAdd_InvalidData_ReturnsDataError() {
    // Act
    var (isValid, message) = VariableValidator.ValidateForAdd("VALID_NAME", "invalid\0value");

    // Assert
    isValid.Should().BeFalse();
    message.Should().Be("cannot contain null characters");
  }

  [Fact]
  public void ValidateForAdd_ValidNameAndData_ReturnsSuccess() {
    // Act
    var (isValid, message) = VariableValidator.ValidateForAdd("VALID_NAME", "valid value");

    // Assert
    isValid.Should().BeTrue();
    message.Should().BeEmpty();
  }

  #endregion
}
