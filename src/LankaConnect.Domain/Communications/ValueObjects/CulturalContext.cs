using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Cultural context for email optimization including language, religious background, and geographic considerations
/// </summary>
public class CulturalContext : ValueObject
{
    public SriLankanLanguage Language { get; set; }
    public CulturalBackground CulturalBackground { get; set; }
    public GeographicRegion GeographicRegion { get; set; }
    public ReligiousContext ReligiousContext { get; set; }
    public bool IsReligiousObservanceConsidered { get; set; }
    public bool RequiresCulturalAuthority { get; set; }
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    public string CulturalNotes { get; set; } = string.Empty;

    public CulturalContext() { } // EF Core & JSON serialization

    private CulturalContext(
        SriLankanLanguage language,
        CulturalBackground culturalBackground,
        GeographicRegion geographicRegion,
        ReligiousContext religiousContext,
        bool isReligiousObservanceConsidered,
        bool requiresCulturalAuthority,
        TimeZoneInfo timeZone,
        string culturalNotes)
    {
        Language = language;
        CulturalBackground = culturalBackground;
        GeographicRegion = geographicRegion;
        ReligiousContext = religiousContext;
        IsReligiousObservanceConsidered = isReligiousObservanceConsidered;
        RequiresCulturalAuthority = requiresCulturalAuthority;
        TimeZone = timeZone;
        CulturalNotes = culturalNotes ?? string.Empty;
    }

    public static Result<CulturalContext> Create(
        SriLankanLanguage language,
        CulturalBackground culturalBackground,
        GeographicRegion geographicRegion,
        ReligiousContext religiousContext = ReligiousContext.None,
        bool isReligiousObservanceConsidered = false,
        bool requiresCulturalAuthority = false,
        TimeZoneInfo? timeZone = null,
        string? culturalNotes = null)
    {
        var effectiveTimeZone = timeZone ?? TimeZoneInfo.Utc;
        var effectiveNotes = culturalNotes ?? string.Empty;

        return Result<CulturalContext>.Success(new CulturalContext(
            language,
            culturalBackground,
            geographicRegion,
            religiousContext,
            isReligiousObservanceConsidered,
            requiresCulturalAuthority,
            effectiveTimeZone,
            effectiveNotes));
    }

    public static CulturalContext CreateForBuddhistCommunity(GeographicRegion region)
    {
        return new CulturalContext(
            SriLankanLanguage.Sinhala,
            CulturalBackground.SinhalaBuddhist,
            region,
            ReligiousContext.BuddhistPoyaday,
            true,
            true, // Requires cultural authority for Buddhist ceremonies
            TimeZoneInfo.Utc,
            "Buddhist community with Poyaday observance considerations");
    }

    public static CulturalContext CreateForHinduCommunity(GeographicRegion region)
    {
        return new CulturalContext(
            SriLankanLanguage.Tamil,
            CulturalBackground.TamilHindu,
            region,
            ReligiousContext.HinduFestival,
            true,
            true, // Requires cultural authority for Hindu ceremonies
            TimeZoneInfo.Utc,
            "Tamil Hindu community with festival considerations");
    }

    public static CulturalContext CreateForIslamicCommunity(GeographicRegion region)
    {
        return new CulturalContext(
            SriLankanLanguage.Tamil,
            CulturalBackground.SriLankanMuslim,
            region,
            ReligiousContext.Ramadan,
            true,
            true, // Requires cultural authority for Islamic observances
            TimeZoneInfo.Utc,
            "Sri Lankan Muslim community with Ramadan considerations");
    }

    public static CulturalContext CreateForChristianCommunity(GeographicRegion region)
    {
        return new CulturalContext(
            SriLankanLanguage.English,
            CulturalBackground.SriLankanChristian,
            region,
            ReligiousContext.ChristianSabbath,
            true,
            false, // Generally does not require cultural authority
            TimeZoneInfo.Utc,
            "Sri Lankan Christian community with Sabbath considerations");
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Language;
        yield return CulturalBackground;
        yield return GeographicRegion;
        yield return ReligiousContext;
        yield return IsReligiousObservanceConsidered;
        yield return RequiresCulturalAuthority;
        yield return TimeZone.Id;
        yield return CulturalNotes;
    }
}