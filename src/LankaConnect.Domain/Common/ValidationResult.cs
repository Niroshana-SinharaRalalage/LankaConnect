using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Common;

/// <summary>
/// TDD GREEN Phase: ValidationResult Implementation
/// Specialized result type for validation operations in LankaConnect platform
/// </summary>
public class ValidationResult
{
    private readonly List<string> _errors;

    private ValidationResult()
    {
        _errors = new List<string>();
        IsValid = true;
    }

    private ValidationResult(IEnumerable<string> errors)
    {
        _errors = errors.ToList();
        IsValid = !_errors.Any();
    }

    /// <summary>
    /// Gets whether validation passed
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets whether validation failed
    /// </summary>
    public bool IsInvalid => !IsValid;

    /// <summary>
    /// Gets the validation errors
    /// </summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Gets the first error message, or empty string if valid
    /// </summary>
    public string FirstError => _errors.FirstOrDefault() ?? string.Empty;

    /// <summary>
    /// Creates a valid result
    /// </summary>
    public static ValidationResult Valid() => new();

    /// <summary>
    /// Creates an invalid result with a single error
    /// </summary>
    public static ValidationResult Invalid(string error) => new(new[] { error });

    /// <summary>
    /// Creates an invalid result with multiple errors
    /// </summary>
    public static ValidationResult Invalid(IEnumerable<string> errors) => new(errors);

    /// <summary>
    /// Creates an invalid result with multiple error parameters
    /// </summary>
    public static ValidationResult Invalid(params string[] errors) => new(errors);

    /// <summary>
    /// Combines multiple validation results
    /// </summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var allErrors = results.SelectMany(r => r.Errors).ToList();
        return allErrors.Any() ? Invalid(allErrors) : Valid();
    }

    /// <summary>
    /// Combines multiple validation results from enumerable
    /// </summary>
    public static ValidationResult Combine(IEnumerable<ValidationResult> results)
    {
        var allErrors = results.SelectMany(r => r.Errors).ToList();
        return allErrors.Any() ? Invalid(allErrors) : Valid();
    }

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    public ValidationResult AddError(string error)
    {
        var allErrors = _errors.Concat(new[] { error });
        return Invalid(allErrors);
    }

    /// <summary>
    /// Adds multiple errors to the validation result
    /// </summary>
    public ValidationResult AddErrors(IEnumerable<string> errors)
    {
        var allErrors = _errors.Concat(errors);
        return Invalid(allErrors);
    }

    /// <summary>
    /// Converts to Result<T>
    /// </summary>
    public Result<T> ToResult<T>(T value)
    {
        return IsValid ? Result<T>.Success(value) : Result<T>.Failure(Errors);
    }

    /// <summary>
    /// Converts to non-generic Result
    /// </summary>
    public Result ToResult()
    {
        return IsValid ? Result.Success() : Result.Failure(Errors);
    }

    /// <summary>
    /// Implicit conversion from bool
    /// </summary>
    public static implicit operator ValidationResult(bool isValid)
    {
        return isValid ? Valid() : Invalid("Validation failed");
    }

    /// <summary>
    /// Implicit conversion from string error
    /// </summary>
    public static implicit operator ValidationResult(string error)
    {
        return Invalid(error);
    }

    /// <summary>
    /// Implicit conversion to bool
    /// </summary>
    public static implicit operator bool(ValidationResult result)
    {
        return result.IsValid;
    }
}