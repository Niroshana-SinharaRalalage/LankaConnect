using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing a user's cultural profile for personalized event recommendations
/// </summary>
public sealed class CulturalProfile : ValueObject
{
    public string Location { get; }
    public LanguagePreference Language { get; }
    public CulturalBackground PrimaryCulture { get; }
    public IEnumerable<CulturalBackground> SecondaryCultures { get; }
    public ReligiousObservanceLevel ObservanceLevel { get; }
    public bool IncludeTempleEvents { get; }
    public bool IncludeCommunityEvents { get; }

    private CulturalProfile(
        string location,
        LanguagePreference language,
        CulturalBackground primaryCulture,
        IEnumerable<CulturalBackground> secondaryCultures,
        ReligiousObservanceLevel observanceLevel,
        bool includeTempleEvents,
        bool includeCommunityEvents)
    {
        Location = location;
        Language = language;
        PrimaryCulture = primaryCulture;
        SecondaryCultures = secondaryCultures;
        ObservanceLevel = observanceLevel;
        IncludeTempleEvents = includeTempleEvents;
        IncludeCommunityEvents = includeCommunityEvents;
    }

    public static CulturalProfile CreateBuddhistProfile(string location, LanguagePreference language)
    {
        return new CulturalProfile(
            location,
            language,
            CulturalBackground.SinhalaBuddhist,
            new[] { CulturalBackground.SinhalaBuddhist },
            ReligiousObservanceLevel.High,
            true,
            true);
    }

    public static CulturalProfile CreateHinduProfile(string location, LanguagePreference language)
    {
        return new CulturalProfile(
            location,
            language,
            CulturalBackground.TamilHindu,
            new[] { CulturalBackground.TamilSriLankan },
            ReligiousObservanceLevel.High,
            true,
            true);
    }

    public static CulturalProfile CreateMultiCulturalProfile(string location, LanguagePreference language)
    {
        return new CulturalProfile(
            location,
            language,
            CulturalBackground.TamilSriLankan,
            new[] { CulturalBackground.SinhalaBuddhist, CulturalBackground.TamilHindu },
            ReligiousObservanceLevel.Medium,
            true,
            true);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Location;
        yield return Language;
        yield return PrimaryCulture;
        yield return string.Join(",", SecondaryCultures.OrderBy(c => c));
        yield return ObservanceLevel;
        yield return IncludeTempleEvents;
        yield return IncludeCommunityEvents;
    }
}