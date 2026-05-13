namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// A non-generic operation outcome — either <see cref="IsSuccess"/> with no value,
/// or a failure carrying an <see cref="Error"/>. Used for void-returning domain
/// operations that can fail without producing a value.
/// </summary>
/// <remarks>
/// <para>
/// Construction is via the static factories <see cref="Success"/> / <see cref="Failure(Error)"/>
/// rather than a public constructor — this enforces the invariant
/// <c>IsSuccess XOR Error.IsNone</c> and makes intent grep-friendly at call sites.
/// </para>
/// </remarks>
public class Result
{
    /// <summary>True when the operation completed successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>Convenience inverse of <see cref="IsSuccess"/>.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Error payload; <see cref="BuildingBlocks.Domain.Error.None"/> on success.</summary>
    public Error Error { get; }

    /// <summary>
    /// Protected so subclasses (e.g. <see cref="Result{T}"/>) can extend without breaking
    /// the success/failure invariant.
    /// </summary>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && !error.IsNone)
        {
            throw new InvalidOperationException(
                "A success result cannot carry a non-none error. Either the caller passed an inconsistent pair, or Error.None was mutated.");
        }

        if (!isSuccess && error.IsNone)
        {
            throw new InvalidOperationException(
                "A failure result requires a non-none error. Did you mean Result.Failure(someError) instead of Result.Failure(Error.None)?");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Returns a successful result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Returns a failure result carrying the supplied <paramref name="error"/>.</summary>
    /// <exception cref="InvalidOperationException">If <paramref name="error"/> is <see cref="Error.None"/>.</exception>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Factory for value-bearing successes. Sugar around <see cref="Result{T}.Success"/>.
    /// </summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Factory for value-bearing failures. Sugar around <see cref="Result{T}.Failure"/>.
    /// </summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>
    /// Combines multiple results: success only if ALL inputs succeed, else returns the first failure.
    /// Empty input is treated as success.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        if (results is null)
        {
            return Failure(Error.NullValue);
        }

        foreach (var r in results)
        {
            if (r.IsFailure)
            {
                return r;
            }
        }

        return Success();
    }
}
