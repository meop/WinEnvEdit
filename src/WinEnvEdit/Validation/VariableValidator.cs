using System;
using System.Collections.Generic;
using System.Linq;

namespace WinEnvEdit.Validation;

public static class VariableValidator {
  private const int MaxNameLength = 255;
  private const int MaxValueLength = 32767;

  private static readonly (Func<string, bool>, string)[] nameValidationRules = [
    (name => name.Length <= MaxNameLength, $"cannot exceed {MaxNameLength} characters"),
    (name => !name.Contains('\0'), "cannot contain null characters"),
    (name => !name.Contains('='), "cannot contain '=' characters"),
    (name => !name.Contains(';'), "cannot contain ';' characters"),
    (name => !name.Contains('%'), "cannot contain '%' characters"),
    (name => !name.Any(char.IsWhiteSpace), "cannot contain spaces"),
    (name => !string.IsNullOrEmpty(name), "cannot be empty"),
  ];

  private static readonly (Func<string, bool>, string)[] dataValidationRules = [
    (data => data!.Length <= MaxValueLength, $"cannot exceed {MaxValueLength} characters"),
    (data => !data!.Contains('\0'), "cannot contain null characters"),
  ];

  public static ValidationResult ValidateName(string name) {
    foreach (var (rule, errorMessage) in nameValidationRules) {
      if (!rule(name)) {
        return new ValidationResult(false, errorMessage);
      }
    }

    return new ValidationResult(true, string.Empty);
  }

  public static ValidationResult ValidateData(string value) {
    foreach (var (rule, errorMessage) in dataValidationRules) {
      if (!rule(value)) {
        return new ValidationResult(false, errorMessage);
      }
    }

    return new ValidationResult(true, string.Empty);
  }

  public static List<string> ValidateNameAllErrors(string name) {
    var errors = new List<string>();
    foreach (var (rule, errorMessage) in nameValidationRules) {
      if (!rule(name)) {
        errors.Add(errorMessage);
      }
    }
    return errors;
  }

  public static List<string> ValidateDataAllErrors(string value) {
    var errors = new List<string>();
    foreach (var (rule, errorMessage) in dataValidationRules) {
      if (!rule(value)) {
        errors.Add(errorMessage);
      }
    }
    return errors;
  }

  public static (bool IsValid, string Message) ValidateForAdd(string name, string value) {
    var nameResult = ValidateName(name);
    if (!nameResult.IsValid) {
      return (false, nameResult.ErrorMessage);
    }

    var dataResult = ValidateData(value);
    if (!dataResult.IsValid) {
      return (false, dataResult.ErrorMessage);
    }

    return (true, string.Empty);
  }
}

public record ValidationResult(bool IsValid, string ErrorMessage);
