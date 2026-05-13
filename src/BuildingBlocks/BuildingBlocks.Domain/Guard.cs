namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Tiny argument-checking helpers for domain constructors. Used to enforce
/// invariants at the type boundary; downstream code can then trust the data.
/// </summary>
/// <remarks>
/// <para>
/// Guards throw <see cref="ArgumentException"/> family exceptions because they
/// fire on programmer error (caller violating a contract) — distinct from
/// domain failures which should return <see cref="Result"/> instead. If a
/// failure represents a recoverable business condition, use
/// <see cref="BusinessRule"/> + <see cref="Result"/>; if it's "this should
/// never happen because the type system / API contract forbids it,"
/// use Guard.
/// </para>
/// <para>
/// Why a custom class instead of <c>ArgumentNullException.ThrowIfNull</c> alone?
/// Domain-specific guards (non-empty Guid, non-negative decimal, in-range int)
/// have no built-in helper, and a single namespace for invariant-checks makes
/// them grep-friendly.
/// </para>
/// </remarks>
public static class Guard
{
    /// <summary>Throws if <paramref name="value"/> is null.</summary>
    public static T NotNull<T>(T? value, string parameterName) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }
        return value;
    }

    /// <summary>Throws if <paramref name="value"/> is null, empty, or whitespace.</summary>
    public static string NotNullOrWhitespace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value cannot be null, empty, or whitespace.", parameterName);
        }
        return value;
    }

    /// <summary>Throws if <paramref name="value"/> is <see cref="Guid.Empty"/>.</summary>
    public static Guid NotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Guid cannot be empty.", parameterName);
        }
        return value;
    }

    /// <summary>Throws if <paramref name="value"/> is &lt; 0.</summary>
    public static int NotNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
        }
        return value;
    }

    /// <summary>Throws if <paramref name="value"/> is &lt;= 0.</summary>
    public static int Positive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.");
        }
        return value;
    }

    /// <summary>Throws if <paramref name="value"/> is &lt; 0.</summary>
    public static decimal NotNegative(decimal value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
        }
        return value;
    }

    /// <summary>Throws if <paramref name="value"/> is outside <c>[min, max]</c>.</summary>
    public static int InRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, value, $"Value must be in [{min}, {max}].");
        }
        return value;
    }
}
