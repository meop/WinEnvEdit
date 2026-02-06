using System;
using System.Collections.Generic;
using System.IO;
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

  /// <summary>
  /// Returns true if the value looks like a filesystem path (drive letter or %MACRO%).
  /// </summary>
  public static bool LooksLikePath(string value) {
    if (string.IsNullOrWhiteSpace(value)) {
      return false;
    }

    var trimmed = value.Trim();

    // Drive letter pattern: single letter followed by colon (e.g., "C:\", "D:")
    if (trimmed.Length >= 2 && char.IsAsciiLetter(trimmed[0]) && trimmed[1] == ':') {
      return true;
    }

    // Environment variable macro: %LETTERS% (e.g., "%SystemRoot%", "%USERPROFILE%\go")
    if (trimmed.Length >= 3 && trimmed[0] == '%') {
      for (var i = 1; i < trimmed.Length; i++) {
        if (trimmed[i] == '%') {
          return i > 1;
        }
        if (!char.IsAsciiLetter(trimmed[i])) {
          break;
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Returns true if the path exists on disk (after expanding environment variables).
  /// </summary>
  public static bool IsValidPath(string path) {
    if (string.IsNullOrWhiteSpace(path)) {
      return false;
    }
    try {
      // Environment variables in path (e.g. %SystemRoot%) need expansion to check existence
      var expanded = Environment.ExpandEnvironmentVariables(path);
      return Directory.Exists(expanded) || File.Exists(expanded);
    }
    catch {
      return false;
    }
  }

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
