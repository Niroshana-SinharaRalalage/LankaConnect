using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Common;

/// <summary>
/// TDD GREEN Phase: Enhanced Result Pattern Implementation  
/// Foundation pattern for error handling and validation in LankaConnect platform
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public IEnumerable<string> Errors { get; private set; }
    
    /// <summary>
    /// Gets the first error message, or empty string if no errors exist
    /// </summary>
    public string Error => Errors.FirstOrDefault() ?? string.Empty;

    protected Result(bool isSuccess, IEnumerable<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? Array.Empty<string>();
    }

    public static Result Success()
    {
        return new Result(true, Array.Empty<string>());
    }

    public static Result Failure(string error)
    {
        return new Result(false, new[] { error });
    }

    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, errors);
    }

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

    protected Result(bool isSuccess, IEnumerable<string> errors, T? value = default)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, Array.Empty<string>(), value);
    }

    public static new Result<T> Failure(string error)
    {
        return new Result<T>(false, new[] { error });
    }

    public static new Result<T> Failure(IEnumerable<string> errors)
    {
        return new Result<T>(false, errors);
    }

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

