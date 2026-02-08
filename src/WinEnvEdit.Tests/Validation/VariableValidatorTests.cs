using FluentAssertions;

using WinEnvEdit.Validation;

namespace WinEnvEdit.Tests.Validation;

[TestClass]
public class VariableValidatorTests {
  #region ValidateName Tests

  [TestMethod]
  public void ValidateName_Empty_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName(string.Empty);

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot be empty");
  }

  [TestMethod]
  public void ValidateName_WithEquals_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST=VALUE");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain '=' characters");
  }

  [TestMethod]
  public void ValidateName_WithNullChar_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST\0VAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain null characters");
  }

  [TestMethod]
  public void ValidateName_WithSpace_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST VAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain spaces");
  }

  [TestMethod]
  public void ValidateName_SingleSpace_ReturnsSpacesError() {
    // Act
    var result = VariableValidator.ValidateName(" ");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain spaces");
  }

  [TestMethod]
  public void ValidateName_Tab_ReturnsSpacesError() {
    // Act
    var result = VariableValidator.ValidateName("TEST\tVAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain spaces");
  }

  [TestMethod]
  public void ValidateName_WithSemicolon_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST;VAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain ';' characters");
  }

  [TestMethod]
  public void ValidateName_WithPercent_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateName("TEST%VAR");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain '%' characters");
  }

  [TestMethod]
  public void ValidateName_ExceedsMaxLength_ReturnsError() {
    // Arrange - create name with 256 characters (exceeds 255 limit)
    var longName = new string('A', 256);

    // Act
    var result = VariableValidator.ValidateName(longName);

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot exceed 255 characters");
  }

  [TestMethod]
  public void ValidateName_AtMaxLength_ReturnsSuccess() {
    // Arrange - create name with exactly 255 characters
    var maxLengthName = new string('A', 255);

    // Act
    var result = VariableValidator.ValidateName(maxLengthName);

    // Assert
    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [TestMethod]
  public void ValidateName_Valid_ReturnsSuccess() {
    // Act
    var result = VariableValidator.ValidateName("VALID_VAR_NAME");

    // Assert
    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [TestMethod]
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

  [TestMethod]
  public void ValidateData_WithNullChar_ReturnsError() {
    // Act
    var result = VariableValidator.ValidateData("test\0value");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot contain null characters");
  }

  [TestMethod]
  public void ValidateData_ExceedsMaxLength_ReturnsError() {
    // Arrange - create value with 32768 characters (exceeds 32767 limit)
    var longValue = new string('A', 32768);

    // Act
    var result = VariableValidator.ValidateData(longValue);

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Be("cannot exceed 32767 characters");
  }

  [TestMethod]
  public void ValidateData_AtMaxLength_ReturnsSuccess() {
    // Arrange - create value with exactly 32767 characters
    var maxLengthValue = new string('A', 32767);

    // Act
    var result = VariableValidator.ValidateData(maxLengthValue);

    // Assert
    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [TestMethod]
  public void ValidateData_Valid_ReturnsSuccess() {
    // Act
    var result = VariableValidator.ValidateData("valid value with spaces and special chars !@#$");

    // Assert
    result.IsValid.Should().BeTrue();
    result.ErrorMessage.Should().BeEmpty();
  }

  [TestMethod]
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

  [TestMethod]
  public void ValidateForAdd_InvalidName_ReturnsNameError() {
    // Act
    var (isValid, message) = VariableValidator.ValidateForAdd("INVALID=NAME", "value");

    // Assert
    isValid.Should().BeFalse();
    message.Should().Be("cannot contain '=' characters");
  }

  [TestMethod]
  public void ValidateForAdd_InvalidData_ReturnsDataError() {
    // Act
    var (isValid, message) = VariableValidator.ValidateForAdd("VALID_NAME", "invalid\0value");

    // Assert
    isValid.Should().BeFalse();
    message.Should().Be("cannot contain null characters");
  }

  [TestMethod]
  public void ValidateForAdd_ValidNameAndData_ReturnsSuccess() {
    // Act
    var (isValid, message) = VariableValidator.ValidateForAdd("VALID_NAME", "valid value");

    // Assert
    isValid.Should().BeTrue();
    message.Should().BeEmpty();
  }

  #endregion
}
