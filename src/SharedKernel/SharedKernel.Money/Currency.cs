using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.SharedKernel.Money;

/// <summary>
/// ISO 4217 currency. Identified by its 3-letter code (e.g. <c>"USD"</c>),
/// paired with a display name, symbol, and decimal-precision setting that
/// drives rounding and persistence.
/// </summary>
/// <remarks>
/// <para>
/// Per ADR-005 (Money DTO migration), monetary values are stored as a
/// composite (<c>_amount</c> + <c>_currency_code</c>) so the currency
/// travels with every amount and arithmetic between mismatched currencies
/// is a domain failure rather than a silent rounding bug.
/// </para>
/// <para>
/// The registry exposes the currencies the platform actively supports per
/// architect review of ADR-003 (Stripe multi-currency). Adding a new
/// currency requires (a) Stripe Tax verification for the target country,
/// (b) a settlement-bank confirmation per ADR-003 D1, and (c) a test
/// covering its <see cref="DecimalDigits"/> behavior — currencies like JPY
/// (0 digits) and BHD (3 digits) demand explicit handling.
/// </para>
/// </remarks>
public sealed class Currency : ValueObject
{
    /// <summary>ISO 4217 three-letter uppercase code (e.g. <c>"USD"</c>).</summary>
    public string Code { get; }

    /// <summary>Human-readable display name (e.g. <c>"US Dollar"</c>).</summary>
    public string Name { get; }

    /// <summary>Currency symbol (e.g. <c>"$"</c>, <c>"Rs"</c>, <c>"€"</c>).</summary>
    public string Symbol { get; }

    /// <summary>
    /// Number of decimal digits used for amounts in this currency
    /// (USD/EUR/LKR/INR/GBP/AUD/CAD = 2; reserved for future BHD = 3, JPY = 0).
    /// </summary>
    public int DecimalDigits { get; }

    private Currency(string code, string name, string symbol, int decimalDigits)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
        DecimalDigits = decimalDigits;
    }

    /// <summary>United States Dollar (ISO 4217: USD).</summary>
    public static readonly Currency USD = new("USD", "US Dollar", "$", 2);

    /// <summary>Sri Lankan Rupee (ISO 4217: LKR).</summary>
    public static readonly Currency LKR = new("LKR", "Sri Lankan Rupee", "Rs", 2);

    /// <summary>Indian Rupee (ISO 4217: INR).</summary>
    public static readonly Currency INR = new("INR", "Indian Rupee", "₹", 2);

    /// <summary>British Pound Sterling (ISO 4217: GBP).</summary>
    public static readonly Currency GBP = new("GBP", "British Pound", "£", 2);

    /// <summary>Euro (ISO 4217: EUR).</summary>
    public static readonly Currency EUR = new("EUR", "Euro", "€", 2);

    /// <summary>Australian Dollar (ISO 4217: AUD).</summary>
    public static readonly Currency AUD = new("AUD", "Australian Dollar", "A$", 2);

    /// <summary>Canadian Dollar (ISO 4217: CAD).</summary>
    public static readonly Currency CAD = new("CAD", "Canadian Dollar", "C$", 2);

    /// <summary>The complete supported-currency registry, indexed by code.</summary>
    private static readonly Dictionary<string, Currency> Registry = new(StringComparer.OrdinalIgnoreCase)
    {
        [USD.Code] = USD,
        [LKR.Code] = LKR,
        [INR.Code] = INR,
        [GBP.Code] = GBP,
        [EUR.Code] = EUR,
        [AUD.Code] = AUD,
        [CAD.Code] = CAD,
    };

    /// <summary>All currencies supported by the platform.</summary>
    public static IReadOnlyCollection<Currency> All => Registry.Values;

    /// <summary>
    /// Returns the registered currency matching <paramref name="code"/>
    /// (case-insensitive). Throws if unknown.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="code"/> is null/empty/whitespace, or unsupported.</exception>
    public static Currency FromCode(string code)
    {
        Guard.NotNullOrWhitespace(code, nameof(code));
        if (!Registry.TryGetValue(code, out var currency))
        {
            throw new ArgumentException(
                $"Currency '{code}' is not in the supported registry. " +
                $"Supported: {string.Join(", ", Registry.Keys)}.",
                nameof(code));
        }
        return currency;
    }

    /// <summary>
    /// Non-throwing variant of <see cref="FromCode"/>. Returns <see cref="Maybe{T}.None"/>
    /// when the code is null/empty/whitespace or not in the registry.
    /// </summary>
    public static Maybe<Currency> TryFromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Maybe<Currency>.None;
        }

        return Registry.TryGetValue(code, out var currency)
            ? Maybe<Currency>.Some(currency)
            : Maybe<Currency>.None;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    /// <inheritdoc />
    public override string ToString() => Code;
}
