using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Profile representing Sri Lankan diaspora community preferences for targeted email optimization
/// </summary>
public class DiasporaCommunityProfile : ValueObject
{
    public GeographicRegion PrimaryRegion { get; }
    public CulturalBackground CulturalBackground { get; }
    public SriLankanLanguage PreferredLanguage { get; }
    public TimeZoneInfo LocalTimeZone { get; }
    public IReadOnlyList<ReligiousContext> ReligiousObservances { get; }
    public bool IsActiveInLocalCommunity { get; }
    public string CommunityNotes { get; }
    public DateTime? LastCulturalEventParticipation { get; }

    private DiasporaCommunityProfile(
        GeographicRegion primaryRegion,
        CulturalBackground culturalBackground,
        SriLankanLanguage preferredLanguage,
        TimeZoneInfo localTimeZone,
        IReadOnlyList<ReligiousContext> religiousObservances,
        bool isActiveInLocalCommunity,
        string communityNotes,
        DateTime? lastCulturalEventParticipation)
    {
        PrimaryRegion = primaryRegion;
        CulturalBackground = culturalBackground;
        PreferredLanguage = preferredLanguage;
        LocalTimeZone = localTimeZone;
        ReligiousObservances = religiousObservances;
        IsActiveInLocalCommunity = isActiveInLocalCommunity;
        CommunityNotes = communityNotes;
        LastCulturalEventParticipation = lastCulturalEventParticipation;
    }

    public static Result<DiasporaCommunityProfile> Create(
        GeographicRegion primaryRegion,
        CulturalBackground culturalBackground,
        SriLankanLanguage preferredLanguage,
        TimeZoneInfo? localTimeZone = null,
        IReadOnlyList<ReligiousContext>? religiousObservances = null,
        bool isActiveInLocalCommunity = false,
        string? communityNotes = null,
        DateTime? lastCulturalEventParticipation = null)
    {
        var timeZone = localTimeZone ?? TimeZoneInfo.Utc;
        var observances = religiousObservances ?? Array.Empty<ReligiousContext>();
        var notes = communityNotes ?? string.Empty;

        return Result<DiasporaCommunityProfile>.Success(new DiasporaCommunityProfile(
            primaryRegion,
            culturalBackground,
            preferredLanguage,
            timeZone,
            observances,
            isActiveInLocalCommunity,
            notes,
            lastCulturalEventParticipation));
    }

    public static DiasporaCommunityProfile CreateBayAreaSinhaleseBuddhist()
    {
        var pstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        
        return new DiasporaCommunityProfile(
            GeographicRegion.UnitedStates,
            CulturalBackground.SinhalaBuddhist,
            SriLankanLanguage.Sinhala,
            pstTimeZone,
            new[] { ReligiousContext.BuddhistPoyaday, ReligiousContext.VesakDay },
            true,
            "Active in Bay Area Sinhalese Buddhist community, participates in temple activities",
            DateTime.UtcNow.AddDays(-30));
    }

    public static DiasporaCommunityProfile CreateLondonTamilHindu()
    {
        var gmtTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        
        return new DiasporaCommunityProfile(
            GeographicRegion.London,
            CulturalBackground.TamilHindu,
            SriLankanLanguage.Tamil,
            gmtTimeZone,
            new[] { ReligiousContext.HinduFestival, ReligiousContext.Deepavali },
            true,
            "Active in London Tamil community, regularly attends cultural events",
            DateTime.UtcNow.AddDays(-15));
    }

    public static DiasporaCommunityProfile CreateTorontoMultiCultural()
    {
        var estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        
        return new DiasporaCommunityProfile(
            GeographicRegion.Toronto,
            CulturalBackground.Other,
            SriLankanLanguage.English,
            estTimeZone,
            new[] { ReligiousContext.GeneralReligiousObservance },
            true,
            "Participates in multi-cultural Sri Lankan community events",
            DateTime.UtcNow.AddDays(-7));
    }

    public bool ObservesReligiousContext(ReligiousContext context)
    {
        return ReligiousObservances.Contains(context);
    }

    public TimeZoneInfo GetEffectiveTimeZone()
    {
        return LocalTimeZone;
    }

    public bool IsRecentlyActive(TimeSpan threshold)
    {
        if (!LastCulturalEventParticipation.HasValue)
            return false;

        return DateTime.UtcNow - LastCulturalEventParticipation.Value <= threshold;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PrimaryRegion;
        yield return CulturalBackground;
        yield return PreferredLanguage;
        yield return LocalTimeZone.Id;
        yield return IsActiveInLocalCommunity;
        yield return CommunityNotes;
        yield return LastCulturalEventParticipation ?? DateTime.MinValue;
        
        foreach (var observance in ReligiousObservances.OrderBy(o => o))
            yield return observance;
    }
}