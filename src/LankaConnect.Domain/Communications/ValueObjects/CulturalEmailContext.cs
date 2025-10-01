using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Email context with cultural intelligence for community-specific optimization
/// </summary>
public class CulturalEmailContext : ValueObject
{
    public CulturalBackground PrimaryCulture { get; }
    public GeographicRegion TargetRegion { get; }
    public SriLankanLanguage PreferredLanguage { get; }
    public ReligiousContext ReligiousContext { get; }
    public TimeZoneInfo TargetTimeZone { get; }
    public bool RequiresCulturalOptimization { get; }

    private CulturalEmailContext(
        CulturalBackground primaryCulture,
        GeographicRegion targetRegion,
        SriLankanLanguage preferredLanguage,
        ReligiousContext religiousContext,
        TimeZoneInfo targetTimeZone,
        bool requiresCulturalOptimization)
    {
        PrimaryCulture = primaryCulture;
        TargetRegion = targetRegion;
        PreferredLanguage = preferredLanguage;
        ReligiousContext = religiousContext;
        TargetTimeZone = targetTimeZone;
        RequiresCulturalOptimization = requiresCulturalOptimization;
    }

    public static CulturalEmailContext CreateForBuddhistCommunity(GeographicRegion region)
    {
        return new CulturalEmailContext(
            CulturalBackground.SinhalaBuddhist,
            region,
            SriLankanLanguage.Sinhala,
            ReligiousContext.BuddhistPoyaday,
            TimeZoneInfo.Utc,
            true);
    }

    public static CulturalEmailContext CreateForIslamicCommunity(GeographicRegion region)
    {
        return new CulturalEmailContext(
            CulturalBackground.SriLankanMuslim,
            region,
            SriLankanLanguage.Tamil,
            ReligiousContext.Ramadan,
            TimeZoneInfo.Utc,
            true);
    }

    public static CulturalEmailContext CreateForHinduCommunity(GeographicRegion region)
    {
        return new CulturalEmailContext(
            CulturalBackground.TamilHindu,
            region,
            SriLankanLanguage.Tamil,
            ReligiousContext.HinduFestival,
            TimeZoneInfo.Utc,
            true);
    }

    public static CulturalEmailContext CreateForChristianCommunity(GeographicRegion region)
    {
        return new CulturalEmailContext(
            CulturalBackground.SriLankanChristian,
            region,
            SriLankanLanguage.English,
            ReligiousContext.ChristianSabbath,
            TimeZoneInfo.Utc,
            true);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PrimaryCulture;
        yield return TargetRegion;
        yield return PreferredLanguage;
        yield return ReligiousContext;
        yield return TargetTimeZone.Id;
        yield return RequiresCulturalOptimization;
    }
}