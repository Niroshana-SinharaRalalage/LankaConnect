namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// A value-bearing operation outcome — either <see cref="Result.IsSuccess"/>
/// carrying a <typeparamref name="T"/>, or a failure with an <see cref="Result.Error"/>.
/// </summary>
/// <remarks>
/// <para>
/// Accessing <see cref="Value"/> on a failure result throws — callers should check
/// <see cref="Result.IsSuccess"/> or use the railway-style <see cref="Map{TOut}"/>
/// / <see cref="Bind{TOut}"/> / <see cref="Match{TOut}"/> combinators which never
/// dereference a missing value.
/// </para>
/// <para>
/// Implicit conversions from <typeparamref name="T"/> and from <see cref="Error"/>
/// let handlers write <c>return value;</c> / <c>return error;</c> directly when
/// the result type is unambiguous from context, removing visual noise from
/// happy-path code.
/// </para>
/// </remarks>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, Error.None)
    {
        _value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
        _value = default;
    }

    /// <summary>
    /// The success value. Accessing this on a failure result throws
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the result is a failure.</exception>
    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException(
                    $"Cannot access Value on a failure result. Error was {Error}. " +
                    "Check IsSuccess before accessing Value, or use Match/Map/Bind.");
            }

            // _value is guaranteed non-null on a success path because the success constructor accepts a T.
            // The nullable annotation reflects that the failure constructor sets default.
            return _value!;
        }
    }

    /// <summary>Wraps a value in a success result.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Wraps an error in a failure result.</summary>
    /// <exception cref="InvalidOperationException">If <paramref name="error"/> is <see cref="Error.None"/>.</exception>
    public static new Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Maps the success value through <paramref name="mapper"/>. Failures pass through unchanged.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="mapper"/> is null.</exception>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess ? Result<TOut>.Success(mapper(Value)) : Result<TOut>.Failure(Error);
    }

    /// <summary>
    /// Chains another result-returning operation when this result is a success.
    /// Failures short-circuit the chain.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="binder"/> is null.</exception>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder(Value) : Result<TOut>.Failure(Error);
    }

    /// <summary>
    /// Forks based on success/failure into a common output type — never throws on
    /// missing values because each branch supplies its own.
    /// </summary>
    /// <exception cref="ArgumentNullException">If either branch is null.</exception>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }

    /// <summary>Implicit promotion of a value to a success result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicit promotion of an error to a failure result.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}
