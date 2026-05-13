using System.Globalization;

namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// A BCP 47 / .NET-culture locale identifier (e.g. <c>"en-US"</c>, <c>"si-LK"</c>,
/// <c>"ta-LK"</c>). Drives i18n message lookup, date/time formatting, and
/// pluralization.
/// </summary>
/// <remarks>
/// <para>
/// Locale is distinct from <see cref="Country"/>: a single country may support
/// many locales (Sri Lanka: <c>si-LK</c>, <c>ta-LK</c>, <c>en-LK</c>) and a
/// single locale may apply across many countries (<c>en-US</c> vs <c>en-GB</c>).
/// </para>
/// <para>
/// Construction validates the tag against <see cref="CultureInfo"/> to catch
/// typos at the boundary. Unknown tags throw rather than silently falling back
/// to invariant culture — silent fallback is the #1 source of "why are my dates
/// in the wrong format" bugs.
/// </para>
/// </remarks>
public sealed class Locale : ValueObject
{
    /// <summary>BCP 47 / .NET-culture name (e.g. <c>"en-US"</c>).</summary>
    public string Tag { get; }

    private Locale(string tag)
    {
        Tag = tag;
    }

    /// <summary>English (United States) — platform default locale.</summary>
    public static readonly Locale EnUs = new("en-US");

    /// <summary>Sinhala (Sri Lanka).</summary>
    public static readonly Locale SiLk = new("si-LK");

    /// <summary>Tamil (Sri Lanka).</summary>
    public static readonly Locale TaLk = new("ta-LK");

    /// <summary>English (United Kingdom).</summary>
    public static readonly Locale EnGb = new("en-GB");

    /// <summary>
    /// Constructs a <see cref="Locale"/> from a BCP 47 / .NET-culture tag,
    /// validating against <see cref="CultureInfo"/>. Unknown tags throw.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="tag"/> is null/empty/whitespace or unknown to .NET.</exception>
    public static Locale FromTag(string tag)
    {
        Guard.NotNullOrWhitespace(tag, nameof(tag));

        try
        {
            // CultureInfo throws CultureNotFoundException for unknown tags
            // when the OS doesn't ship that locale's data.
            _ = CultureInfo.GetCultureInfo(tag, predefinedOnly: true);
        }
        catch (CultureNotFoundException ex)
        {
            throw new ArgumentException(
                $"Locale tag '{tag}' is not a known BCP 47 / .NET culture identifier. " +
                "See https://learn.microsoft.com/dotnet/api/system.globalization.cultureinfo " +
                "for the valid list.",
                nameof(tag),
                ex);
        }

        return new Locale(tag);
    }

    /// <summary>Non-throwing variant returning <see cref="Maybe{T}.None"/> when unknown.</summary>
    public static Maybe<Locale> TryFromTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return Maybe<Locale>.None;
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(tag, predefinedOnly: true);
            return Maybe<Locale>.Some(new Locale(tag));
        }
        catch (CultureNotFoundException)
        {
            return Maybe<Locale>.None;
        }
    }

    /// <summary>Returns the .NET <see cref="CultureInfo"/> for this locale.</summary>
    public CultureInfo ToCultureInfo() => CultureInfo.GetCultureInfo(Tag);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Tag;
    }

    /// <inheritdoc />
    public override string ToString() => Tag;
}
