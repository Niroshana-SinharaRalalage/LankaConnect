using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using EventsCulturalConflict = LankaConnect.Domain.Events.ValueObjects.CulturalConflict;
using CommsCulturalConflict = LankaConnect.Domain.Communications.ValueObjects.CulturalConflict;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Cross-Cultural Event value object for multi-community coordination
/// Enables cultural bridge-building and community integration across South Asian diaspora
/// Core component of Phase 8 Global Multi-Cultural Platform Expansion
/// </summary>
public sealed class CrossCulturalEvent : ValueObject
{
    public CulturalEvent PrimaryEvent { get; }
    public IEnumerable<CulturalCommunityRelevance> CommunityRelevance { get; }
    public CrossCulturalAppropriatenessLevel AppropriatenessLevel { get; }
    public IEnumerable<CulturalAdaptation> CommunityAdaptations { get; }
    public IEnumerable<CommsCulturalConflict> PotentialConflicts { get; }
    public CrossCulturalBridgingOpportunity BridgingOpportunity { get; }
    public bool EnablesCommunityIntegration { get; }
    public decimal CulturalSensitivityScore { get; }
    public IEnumerable<string> MultiLingualNames { get; }
    public IEnumerable<CulturalCelebrationStyle> CelebrationStyles { get; }

    /// <summary>
    /// Simple constructor for basic cross-cultural event creation
    /// </summary>
    public CrossCulturalEvent(string id, string name, DateTime dateTime)
        : this(
            new CulturalEvent(dateTime, name, name, id, CulturalCommunity.MultiCulturalSouthAsian),
            new List<CulturalCommunityRelevance>(),
            CrossCulturalAppropriatenessLevel.Medium,
            new List<CulturalAdaptation>(),
            new List<CommsCulturalConflict>(),
            new CrossCulturalBridgingOpportunity("Cross-cultural engagement opportunity", BridgingPotential.Medium),
            true,
            0.75m,
            new[] { name },
            new[] { CulturalCelebrationStyle.CommunityFestival })
    {
    }

    private CrossCulturalEvent(
        CulturalEvent primaryEvent,
        IEnumerable<CulturalCommunityRelevance> communityRelevance,
        CrossCulturalAppropriatenessLevel appropriatenessLevel,
        IEnumerable<CulturalAdaptation> communityAdaptations,
        IEnumerable<CommsCulturalConflict> potentialConflicts,
        CrossCulturalBridgingOpportunity bridgingOpportunity,
        bool enablesCommunityIntegration,
        decimal culturalSensitivityScore,
        IEnumerable<string> multiLingualNames,
        IEnumerable<CulturalCelebrationStyle> celebrationStyles)
    {
        PrimaryEvent = primaryEvent;
        CommunityRelevance = communityRelevance?.ToList() ?? new List<CulturalCommunityRelevance>();
        AppropriatenessLevel = appropriatenessLevel;
        CommunityAdaptations = communityAdaptations?.ToList() ?? new List<CulturalAdaptation>();
        PotentialConflicts = potentialConflicts?.ToList() ?? new List<CommsCulturalConflict>();
        BridgingOpportunity = bridgingOpportunity;
        EnablesCommunityIntegration = enablesCommunityIntegration;
        CulturalSensitivityScore = culturalSensitivityScore;
        MultiLingualNames = multiLingualNames?.ToList() ?? new List<string>();
        CelebrationStyles = celebrationStyles?.ToList() ?? new List<CulturalCelebrationStyle>();
    }

    /// <summary>
    /// Create Diwali cross-cultural event - celebrated across Hindu, Sikh, and Jain communities
    /// High bridge-building potential for Indian diaspora unity
    /// </summary>
    public static CrossCulturalEvent CreateDiwaliCrossCultural(CulturalEvent diwaliEvent)
    {
        var communityRelevance = new[]
        {
            new CulturalCommunityRelevance(CulturalCommunity.IndianHinduNorth, RelevanceLevel.Primary, "Festival of Lights - Lakshmi Worship"),
            new CulturalCommunityRelevance(CulturalCommunity.IndianHinduSouth, RelevanceLevel.Primary, "Deepavali - Victory of Light over Darkness"),
            new CulturalCommunityRelevance(CulturalCommunity.IndianSikh, RelevanceLevel.Secondary, "Bandi Chhor Divas - Liberation of Guru Hargobind"),
            new CulturalCommunityRelevance(CulturalCommunity.IndianJain, RelevanceLevel.Primary, "Mahavir Nirvana Day"),
            new CulturalCommunityRelevance(CulturalCommunity.SriLankanTamilHindu, RelevanceLevel.Primary, "Tamil Deepavali")
        };

        var adaptations = new[]
        {
            new CulturalAdaptation(CulturalCommunity.IndianSikh, "Include Gurudwara celebrations and langar", AdaptationType.ReligiousIntegration),
            new CulturalAdaptation(CulturalCommunity.IndianJain, "Emphasize non-violence and spiritual purification", AdaptationType.ReligiousInterpretation),
            new CulturalAdaptation(CulturalCommunity.MultiCulturalSouthAsian, "Community potluck with diverse regional foods", AdaptationType.CulinaryIntegration)
        };

        return new CrossCulturalEvent(
            diwaliEvent,
            communityRelevance,
            CrossCulturalAppropriatenessLevel.High,
            adaptations,
            new List<CommsCulturalConflict>(),
            new CrossCulturalBridgingOpportunity("Inter-faith harmony celebration", BridgingPotential.High),
            true,
            0.92m,
            new[] { "Diwali (Hindi)", "Deepavali (Tamil)", "Bandi Chhor Divas (Punjabi)", "Festival of Lights (English)" },
            new[] { CulturalCelebrationStyle.FamilyGathering, CulturalCelebrationStyle.CommunityFestival, CulturalCelebrationStyle.ReligiousObservance }
        );
    }

    /// <summary>
    /// Create Eid cross-cultural event - celebrated across Muslim communities with cultural variations
    /// Bridges Pakistani, Bangladeshi, and Indian Muslim communities
    /// </summary>
    public static CrossCulturalEvent CreateEidCrossCultural(CulturalEvent eidEvent)
    {
        var communityRelevance = new[]
        {
            new CulturalCommunityRelevance(CulturalCommunity.PakistaniSunniMuslim, RelevanceLevel.Primary, "Eid ul-Fitr - End of Ramadan"),
            new CulturalCommunityRelevance(CulturalCommunity.BangladeshiSunniMuslim, RelevanceLevel.Primary, "Eid ul-Fitr - Bengali traditions"),
            new CulturalCommunityRelevance(CulturalCommunity.IndianMuslim, RelevanceLevel.Primary, "Eid ul-Fitr - Indian Muslim heritage"),
            new CulturalCommunityRelevance(CulturalCommunity.SriLankanMuslim, RelevanceLevel.Primary, "Eid ul-Fitr - Sri Lankan Moor traditions")
        };

        var adaptations = new[]
        {
            new CulturalAdaptation(CulturalCommunity.BangladeshiSunniMuslim, "Include Bengali sweets and traditional clothing", AdaptationType.CulinaryAndCultural),
            new CulturalAdaptation(CulturalCommunity.PakistaniSunniMuslim, "Pakistani biryani and cultural music", AdaptationType.CulinaryAndCultural),
            new CulturalAdaptation(CulturalCommunity.IndianMuslim, "Hyderabadi cuisine and Bollywood celebration", AdaptationType.CulinaryAndCultural)
        };

        return new CrossCulturalEvent(
            eidEvent,
            communityRelevance,
            CrossCulturalAppropriatenessLevel.High,
            adaptations,
            new List<CommsCulturalConflict>(),
            new CrossCulturalBridgingOpportunity("Pan-Islamic unity across South Asian cultures", BridgingPotential.High),
            true,
            0.95m,
            new[] { "Eid ul-Fitr (Arabic)", "Eid Mubarak (Urdu)", "Eid er Shubhechha (Bengali)" },
            new[] { CulturalCelebrationStyle.ReligiousObservance, CulturalCelebrationStyle.CommunityFestival, CulturalCelebrationStyle.FamilyGathering }
        );
    }

    /// <summary>
    /// Create Vaisakhi cross-cultural event - celebrated by Sikhs and some Hindu communities
    /// Significant for Punjabi diaspora integration
    /// </summary>
    public static CrossCulturalEvent CreateVaisakhiCrossCultural(CulturalEvent vaisakhiEvent)
    {
        var communityRelevance = new[]
        {
            new CulturalCommunityRelevance(CulturalCommunity.IndianSikh, RelevanceLevel.Primary, "Formation of Khalsa - Sikh New Year"),
            new CulturalCommunityRelevance(CulturalCommunity.IndianHinduNorth, RelevanceLevel.Secondary, "Spring harvest festival in Punjab"),
            new CulturalCommunityRelevance(CulturalCommunity.PakistaniSikh, RelevanceLevel.Primary, "Khalsa formation celebration")
        };

        var adaptations = new[]
        {
            new CulturalAdaptation(CulturalCommunity.IndianSikh, "Nagar Kirtan procession and langar", AdaptationType.ReligiousProcession),
            new CulturalAdaptation(CulturalCommunity.IndianHinduNorth, "Harvest celebration elements", AdaptationType.SeasonalCelebration)
        };

        return new CrossCulturalEvent(
            vaisakhiEvent,
            communityRelevance,
            CrossCulturalAppropriatenessLevel.Medium,
            adaptations,
            new List<CommsCulturalConflict>(),
            new CrossCulturalBridgingOpportunity("Punjabi cultural unity across religious lines", BridgingPotential.Medium),
            true,
            0.85m,
            new[] { "Vaisakhi (Punjabi)", "Baisakhi (Hindi)", "Punjabi New Year (English)" },
            new[] { CulturalCelebrationStyle.ReligiousProcession, CulturalCelebrationStyle.CommunityFestival }
        );
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PrimaryEvent;
        yield return string.Join(",", CommunityRelevance.OrderBy(cr => cr.Community));
        yield return AppropriatenessLevel;
        yield return string.Join(",", CommunityAdaptations.OrderBy(ca => ca.Community));
        yield return EnablesCommunityIntegration;
        yield return CulturalSensitivityScore;
        yield return BridgingOpportunity;
    }
}

// Supporting types for Cross-Cultural Event system
public record CulturalCommunityRelevance(
    CulturalCommunity Community,
    RelevanceLevel Level,
    string CulturalSignificance);

public record CulturalAdaptation(
    CulturalCommunity Community,
    string AdaptationDescription,
    AdaptationType Type);

public record CrossCulturalBridgingOpportunity(
    string Description,
    BridgingPotential Potential);

public enum RelevanceLevel
{
    None = 0,
    Low = 1,
    Secondary = 2,
    Primary = 3,
    Essential = 4
}

public enum CrossCulturalAppropriatenessLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}

public enum AdaptationType
{
    ReligiousIntegration = 1,
    ReligiousInterpretation = 2,
    CulinaryIntegration = 3,
    CulinaryAndCultural = 4,
    SeasonalCelebration = 5,
    ReligiousProcession = 6,
    CulturalBridging = 7
}

public enum CulturalCelebrationStyle
{
    FamilyGathering = 1,
    CommunityFestival = 2,
    ReligiousObservance = 3,
    ReligiousProcession = 4,
    CulturalPerformance = 5,
    EducationalEvent = 6
}

public enum BridgingPotential
{
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}