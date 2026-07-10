using System;
using System.Collections.Generic;
using System.Linq;
namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// TDD GREEN Phase: Enhanced Result Pattern Implementation
/// Foundation pattern for error handling and validation in LankaConnect platform.
/// Slice 5 added <see cref="ErrorKind"/> so the API layer can map failure categories
/// to HTTP status codes without string-sniffing. Legacy Failure(string) defaults to
/// <see cref="ErrorKind.Validation"/>, preserving behavior for all pre-existing callers.
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public IEnumerable<string> Errors { get; private set; }

    /// <summary>
    /// Classifies the failure so the API layer can map to an HTTP status code.
    /// Success results always return <see cref="ErrorKind.None"/>.
    /// </summary>
    public ErrorKind ErrorKind { get; private set; }

    /// <summary>
    /// Gets the first error message, or empty string if no errors exist
    /// </summary>
    public string Error => Errors.FirstOrDefault() ?? string.Empty;

    protected Result(bool isSuccess, IEnumerable<string> errors, ErrorKind errorKind = ErrorKind.None)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? Array.Empty<string>();
        ErrorKind = errorKind;
    }

    public static Result Success()
    {
        return new Result(true, Array.Empty<string>(), ErrorKind.None);
    }

    public static Result Failure(string error)
    {
        return new Result(false, new[] { error }, ErrorKind.Validation);
    }

    /// <summary>
    /// Failure from an <see cref="Error"/> record. Sprint Consult #10 Q4(b):
    /// BusinessRule unifies on Error type instead of string. Preserves the
    /// Error.ToString() format "[Code] Message" in the errors collection.
    /// </summary>
    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, new[] { error.ToString() }, ErrorKind.Validation);
    }

    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, errors, ErrorKind.Validation);
    }

    public static Result Failure(string error, ErrorKind kind)
    {
        return new Result(false, new[] { error }, kind);
    }

    public static Result Forbidden(string error) => new(false, new[] { error }, ErrorKind.Forbidden);
    public static Result NotFound(string error) => new(false, new[] { error }, ErrorKind.NotFound);
    public static Result Conflict(string error) => new(false, new[] { error }, ErrorKind.Conflict);
    public static Result StructuralEditRejected(string error) => new(false, new[] { error }, ErrorKind.StructuralEditRejected);

    public static implicit operator Result(bool success)
    {
        return success ? Success() : Failure("Operation failed");
    }

    public static implicit operator Result(string error)
    {
        return Failure(error);
    }
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("Cannot access value of a failed result");
            return _value!;
        }
    }

    protected Result(bool isSuccess, IEnumerable<string> errors, T? value = default, ErrorKind errorKind = ErrorKind.None)
        : base(isSuccess, errors, errorKind)
    {
        _value = value;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, Array.Empty<string>(), value, ErrorKind.None);
    }

    public static new Result<T> Failure(string error)
    {
        return new Result<T>(false, new[] { error }, default, ErrorKind.Validation);
    }

    /// <summary>
    /// Failure from an <see cref="Error"/> record. Sprint Consult #10 Q4(b):
    /// mirror the Result base overload for Error type on the generic Result&lt;T&gt;.
    /// 'new' keyword follows the pattern of sibling Result&lt;T&gt;.Failure overloads
    /// (line 109-126) which all hide inherited Result members.
    /// </summary>
    public static new Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(false, new[] { error.ToString() }, default, ErrorKind.Validation);
    }

    public static new Result<T> Failure(IEnumerable<string> errors)
    {
        return new Result<T>(false, errors, default, ErrorKind.Validation);
    }

    public static new Result<T> Failure(string error, ErrorKind kind)
    {
        return new Result<T>(false, new[] { error }, default, kind);
    }

    public static new Result<T> Forbidden(string error) => new(false, new[] { error }, default, ErrorKind.Forbidden);
    public static new Result<T> NotFound(string error) => new(false, new[] { error }, default, ErrorKind.NotFound);
    public static new Result<T> Conflict(string error) => new(false, new[] { error }, default, ErrorKind.Conflict);
    public static new Result<T> StructuralEditRejected(string error) => new(false, new[] { error }, default, ErrorKind.StructuralEditRejected);

    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    /// <summary>
    /// Transforms the result using provided functions
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(_value!) : onFailure(Error);
    }

    /// <summary>
    /// Maps the success value to another type
    /// </summary>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess ? Result<TResult>.Success(mapper(_value!)) : Result<TResult>.Failure(Errors);
    }
}
