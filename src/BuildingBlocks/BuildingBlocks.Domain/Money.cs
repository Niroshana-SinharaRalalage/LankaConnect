namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// A monetary amount in a specific <see cref="Currency"/>. Arithmetic between
/// mismatched currencies is forbidden — it throws — because silent currency
/// coercion is the source of most monetary bugs.
/// </summary>
/// <remarks>
/// <para>
/// Per ADR-005 (Money DTO migration), monetary values persist as a composite
/// (<c>_amount</c> + <c>_currency_code</c>) via the EF value converter that
/// lands in BuildingBlocks.Infrastructure W2.5. Use this type as the property
/// surface; the converter handles columns.
/// </para>
/// <para>
/// Arithmetic helpers (<see cref="op_Addition"/>, <see cref="op_Subtraction"/>,
/// scalar multiplication / division) preserve the currency. Comparison
/// operators (<c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>, <c>&gt;=</c>) only work
/// between same-currency Money instances; cross-currency comparison throws.
/// </para>
/// <para>
/// Amounts are NOT rounded on construction — that's a presentation/persistence
/// concern. <see cref="RoundToCurrency"/> available for explicit rounding to
/// the currency's <see cref="Currency.DecimalDigits"/>.
/// </para>
/// </remarks>
public sealed class Money : ValueObject
{
    /// <summary>The raw monetary amount, not rounded.</summary>
    public decimal Amount { get; }

    /// <summary>The currency this amount is denominated in.</summary>
    public Currency Currency { get; }

    /// <summary>
    /// Constructs a <see cref="Money"/> from an amount + currency.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="currency"/> is null.</exception>
    public Money(decimal amount, Currency currency)
    {
        Guard.NotNull(currency, nameof(currency));
        Amount = amount;
        Currency = currency;
    }

    /// <summary>A zero amount in the supplied currency.</summary>
    public static Money Zero(Currency currency) => new(0m, currency);

    /// <summary>True if <see cref="Amount"/> is exactly zero.</summary>
    public bool IsZero => Amount == 0m;

    /// <summary>True if <see cref="Amount"/> is strictly positive.</summary>
    public bool IsPositive => Amount > 0m;

    /// <summary>True if <see cref="Amount"/> is strictly negative.</summary>
    public bool IsNegative => Amount < 0m;

    /// <summary>Returns the amount rounded to <see cref="Currency.DecimalDigits"/> using banker's rounding.</summary>
    public Money RoundToCurrency() =>
        new(Math.Round(Amount, Currency.DecimalDigits, MidpointRounding.ToEven), Currency);

    /// <summary>Returns a new <see cref="Money"/> with negated amount, same currency.</summary>
    public Money Negate() => new(-Amount, Currency);

    /// <summary>Returns a new <see cref="Money"/> with absolute amount, same currency.</summary>
    public Money Abs() => new(Math.Abs(Amount), Currency);

    // ---------- Arithmetic ----------

    /// <summary>Adds two same-currency amounts. Throws on cross-currency.</summary>
    public static Money operator +(Money left, Money right)
    {
        AssertSameCurrency(left, right, "+");
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>Subtracts two same-currency amounts. Throws on cross-currency.</summary>
    public static Money operator -(Money left, Money right)
    {
        AssertSameCurrency(left, right, "-");
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>Negates an amount (unary minus), preserving currency.</summary>
    public static Money operator -(Money value)
    {
        Guard.NotNull(value, nameof(value));
        return value.Negate();
    }

    /// <summary>Scales an amount by a scalar (e.g. quantity), preserving currency.</summary>
    public static Money operator *(Money value, decimal scalar)
    {
        Guard.NotNull(value, nameof(value));
        return new Money(value.Amount * scalar, value.Currency);
    }

    /// <summary>Scales an amount by a scalar (e.g. quantity), preserving currency. Commutative form.</summary>
    public static Money operator *(decimal scalar, Money value) => value * scalar;

    /// <summary>Divides an amount by a non-zero scalar, preserving currency.</summary>
    /// <exception cref="DivideByZeroException">If <paramref name="divisor"/> is zero.</exception>
    public static Money operator /(Money value, decimal divisor)
    {
        Guard.NotNull(value, nameof(value));
        if (divisor == 0m)
        {
            throw new DivideByZeroException("Cannot divide Money by zero.");
        }
        return new Money(value.Amount / divisor, value.Currency);
    }

    // ---------- Comparison ----------

    /// <summary>Strictly-less-than comparison between same-currency amounts.</summary>
    public static bool operator <(Money left, Money right)
    {
        AssertSameCurrency(left, right, "<");
        return left.Amount < right.Amount;
    }

    /// <summary>Strictly-greater-than comparison between same-currency amounts.</summary>
    public static bool operator >(Money left, Money right)
    {
        AssertSameCurrency(left, right, ">");
        return left.Amount > right.Amount;
    }

    /// <summary>Less-than-or-equal comparison between same-currency amounts.</summary>
    public static bool operator <=(Money left, Money right)
    {
        AssertSameCurrency(left, right, "<=");
        return left.Amount <= right.Amount;
    }

    /// <summary>Greater-than-or-equal comparison between same-currency amounts.</summary>
    public static bool operator >=(Money left, Money right)
    {
        AssertSameCurrency(left, right, ">=");
        return left.Amount >= right.Amount;
    }

    // ---------- Value-object equality (inherited; components below) ----------

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Currency.Symbol}{Amount.ToString($"F{Currency.DecimalDigits}", System.Globalization.CultureInfo.InvariantCulture)} {Currency.Code}";

    // ---------- Helpers ----------

    private static void AssertSameCurrency(Money left, Money right, string op)
    {
        Guard.NotNull(left, nameof(left));
        Guard.NotNull(right, nameof(right));
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot apply operator '{op}' between {left.Currency.Code} and {right.Currency.Code}: " +
                "mixed-currency operations are forbidden. Convert one operand to the target currency first.");
        }
    }
}
