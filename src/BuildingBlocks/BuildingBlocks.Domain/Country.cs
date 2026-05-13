namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// ISO 3166-1 alpha-2 country code (e.g. <c>"LK"</c>, <c>"US"</c>). Identified
/// only by the two-letter code; the display name is informational.
/// </summary>
/// <remarks>
/// <para>
/// The registry exposes the countries the platform actively supports per ADR-002
/// (tenancy strategy) + ADR-003 (Stripe multi-currency). Adding a country
/// requires Stripe Tax verification + a settlement decision per ADR-003 D1.
/// </para>
/// <para>
/// Country differs from <see cref="Locale"/>: <c>en-US</c> and <c>es-US</c>
/// are different locales but the same country. Country drives tax + settlement
/// + geo gating (<see cref="Currency"/>); locale drives i18n message lookup
/// + formatting.
/// </para>
/// </remarks>
public sealed class Country : ValueObject
{
    /// <summary>ISO 3166-1 alpha-2 code (e.g. <c>"LK"</c>).</summary>
    public string Code { get; }

    /// <summary>Display name (e.g. <c>"Sri Lanka"</c>).</summary>
    public string Name { get; }

    private Country(string code, string name)
    {
        Code = code;
        Name = name;
    }

    /// <summary>Sri Lanka (ISO 3166-1: LK).</summary>
    public static readonly Country LK = new("LK", "Sri Lanka");

    /// <summary>United States (ISO 3166-1: US).</summary>
    public static readonly Country US = new("US", "United States");

    /// <summary>India (ISO 3166-1: IN).</summary>
    public static readonly Country IN = new("IN", "India");

    /// <summary>United Kingdom (ISO 3166-1: GB).</summary>
    public static readonly Country GB = new("GB", "United Kingdom");

    /// <summary>Australia (ISO 3166-1: AU).</summary>
    public static readonly Country AU = new("AU", "Australia");

    /// <summary>Canada (ISO 3166-1: CA).</summary>
    public static readonly Country CA = new("CA", "Canada");

    private static readonly Dictionary<string, Country> Registry = new(StringComparer.OrdinalIgnoreCase)
    {
        [LK.Code] = LK,
        [US.Code] = US,
        [IN.Code] = IN,
        [GB.Code] = GB,
        [AU.Code] = AU,
        [CA.Code] = CA,
    };

    /// <summary>All countries supported by the platform.</summary>
    public static IReadOnlyCollection<Country> All => Registry.Values;

    /// <summary>
    /// Returns the registered country matching <paramref name="code"/>
    /// (case-insensitive). Throws if unknown.
    /// </summary>
    /// <exception cref="ArgumentException">If unknown or null/empty/whitespace.</exception>
    public static Country FromCode(string code)
    {
        Guard.NotNullOrWhitespace(code, nameof(code));
        if (!Registry.TryGetValue(code, out var country))
        {
            throw new ArgumentException(
                $"Country '{code}' is not in the supported registry. Supported: {string.Join(", ", Registry.Keys)}.",
                nameof(code));
        }
        return country;
    }

    /// <summary>Non-throwing variant returning <see cref="Maybe{T}.None"/> when unknown.</summary>
    public static Maybe<Country> TryFromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Maybe<Country>.None;
        }

        return Registry.TryGetValue(code, out var country)
            ? Maybe<Country>.Some(country)
            : Maybe<Country>.None;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    /// <inheritdoc />
    public override string ToString() => Code;
}
