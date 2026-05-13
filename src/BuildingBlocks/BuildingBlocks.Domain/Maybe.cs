namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// A nullable-aware optional value. Either contains a <typeparamref name="T"/>
/// (<see cref="HasValue"/> = true) or is empty (<see cref="None"/>).
/// </summary>
/// <remarks>
/// <para>
/// Use when "absence is normal" — e.g. a repository lookup for an entity that
/// may not exist. Prefer <see cref="Result{T}"/> when absence is an error
/// condition that callers must handle distinctly.
/// </para>
/// <para>
/// Equality is value-based; two <see cref="Maybe{T}"/> are equal when both
/// are <see cref="None"/> or when both contain equal values per
/// <see cref="EqualityComparer{T}.Default"/>.
/// </para>
/// </remarks>
public readonly struct Maybe<T> : IEquatable<Maybe<T>>
{
    private readonly T? _value;

    private Maybe(T value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    /// <summary>True if this Maybe carries a value.</summary>
    public bool HasValue { get; }

    /// <summary>Convenience inverse of <see cref="HasValue"/>.</summary>
    public bool IsEmpty => !HasValue;

    /// <summary>
    /// The contained value. Throws <see cref="InvalidOperationException"/> on
    /// <see cref="None"/> — prefer <see cref="GetValueOrDefault(T)"/> or
    /// <see cref="Match{TOut}"/> for safe extraction.
    /// </summary>
    /// <exception cref="InvalidOperationException">If <see cref="HasValue"/> is false.</exception>
    public T Value
    {
        get
        {
            if (!HasValue)
            {
                throw new InvalidOperationException(
                    "Maybe<T>.Value accessed on an empty (None) instance. " +
                    "Check HasValue or use GetValueOrDefault / Match.");
            }

            return _value!;
        }
    }

    /// <summary>The empty (None) singleton for <typeparamref name="T"/>.</summary>
    public static Maybe<T> None => default;

    /// <summary>Wraps a value into a Some(value) instance.</summary>
    /// <exception cref="ArgumentNullException">If <paramref name="value"/> is null.</exception>
    public static Maybe<T> Some(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Maybe<T>(value, hasValue: true);
    }

    /// <summary>
    /// Promotes a possibly-null reference into a <see cref="Maybe{T}"/>:
    /// null becomes <see cref="None"/>, non-null becomes <c>Some(value)</c>.
    /// </summary>
    public static Maybe<T> From(T? value) =>
        value is null ? None : Some(value);

    /// <summary>Returns the contained value, or <paramref name="fallback"/> when empty.</summary>
    public T GetValueOrDefault(T fallback) => HasValue ? _value! : fallback;

    /// <summary>
    /// Forks based on presence into a common output type — never dereferences
    /// missing values because each branch supplies its own.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSome, Func<TOut> onNone)
    {
        ArgumentNullException.ThrowIfNull(onSome);
        ArgumentNullException.ThrowIfNull(onNone);
        return HasValue ? onSome(_value!) : onNone();
    }

    /// <summary>Maps the value through <paramref name="mapper"/>; None passes through.</summary>
    public Maybe<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return HasValue ? Maybe<TOut>.Some(mapper(_value!)) : Maybe<TOut>.None;
    }

    /// <summary>Chains another Maybe-returning operation; None short-circuits.</summary>
    public Maybe<TOut> Bind<TOut>(Func<T, Maybe<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return HasValue ? binder(_value!) : Maybe<TOut>.None;
    }

    // ---------- Equality ----------

    /// <inheritdoc />
    public bool Equals(Maybe<T> other) =>
        HasValue == other.HasValue &&
        (!HasValue || EqualityComparer<T>.Default.Equals(_value, other._value));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Maybe<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HasValue ? HashCode.Combine(true, _value) : 0;

    /// <summary>Structural equality operator.</summary>
    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    /// <summary>Structural inequality operator.</summary>
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => HasValue ? $"Some({_value})" : "None";
}
