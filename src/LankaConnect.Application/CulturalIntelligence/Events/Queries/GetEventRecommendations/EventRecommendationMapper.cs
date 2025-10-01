using LankaConnect.Domain.Events.ValueObjects.Recommendations;
using DomainEventRecommendation = LankaConnect.Domain.Events.ValueObjects.Recommendations.EventRecommendation;

namespace LankaConnect.Application.CulturalIntelligence.Events.Queries.GetEventRecommendations;

/// <summary>
/// Mapper to convert Domain EventRecommendation objects to Application DTOs
/// Solves type conversion compilation errors in GetEventRecommendationsQueryHandler
/// </summary>
public static class EventRecommendationMapper
{
    /// <summary>
    /// Converts a domain EventRecommendation to an application EventRecommendationDto
    /// </summary>
    public static EventRecommendationDto ConvertToDto(DomainEventRecommendation domainRecommendation)
    {
        if (domainRecommendation == null)
            throw new ArgumentNullException(nameof(domainRecommendation));

        return new EventRecommendationDto
        {
            EventId = domainRecommendation.Event.Id,
            Title = domainRecommendation.Event.Title.Value,
            Description = domainRecommendation.Event.Description.Value,
            StartDate = domainRecommendation.Event.StartDate,
            EndDate = domainRecommendation.Event.EndDate,
            RecommendationScore = domainRecommendation.Score.CompositeScore,
            RecommendationReasons = new List<string> { domainRecommendation.RecommendationReason }
        };
    }

    /// <summary>
    /// Converts multiple domain EventRecommendations to application DTOs
    /// </summary>
    public static List<EventRecommendationDto> ConvertToDtos(IEnumerable<DomainEventRecommendation> domainRecommendations)
    {
        if (domainRecommendations == null)
            return new List<EventRecommendationDto>();

        return domainRecommendations.Select(ConvertToDto).ToList();
    }

    /// <summary>
    /// Converts a domain EventRecommendation to DTO with cultural scoring
    /// </summary>
    public static EventRecommendationDto ConvertToDtoWithCulturalScore(
        DomainEventRecommendation domainRecommendation,
        ExtendedCulturalScore extendedCulturalScore)
    {
        var dto = ConvertToDto(domainRecommendation);
        dto.CulturalScore = ConvertCulturalScoreToDto(extendedCulturalScore);
        return dto;
    }

    /// <summary>
    /// Converts extended cultural score to DTO
    /// </summary>
    public static CulturalScoreDto ConvertCulturalScoreToDto(ExtendedCulturalScore extendedScore)
    {
        if (extendedScore == null)
            return new CulturalScoreDto();

        return new CulturalScoreDto
        {
            OverallScore = extendedScore.OverallScore,
            AppropriatenessScore = extendedScore.AppropriatenessScore,
            DiasporaFriendlinessScore = extendedScore.DiasporaFriendlinessScore,
            ConflictLevel = extendedScore.ConflictLevel.ToString(),
            CulturalFactors = extendedScore.Factors ?? new List<string>()
        };
    }

    /// <summary>
    /// Converts basic domain CulturalScore to DTO with default values for missing properties
    /// </summary>
    public static CulturalScoreDto ConvertBasicCulturalScoreToDto(CulturalScore domainCulturalScore)
    {
        if (domainCulturalScore == null)
            return new CulturalScoreDto();

        return new CulturalScoreDto
        {
            OverallScore = domainCulturalScore.Value,
            AppropriatenessScore = domainCulturalScore.Value, // Use same value as fallback
            DiasporaFriendlinessScore = CalculateDiasporaFriendliness(domainCulturalScore),
            ConflictLevel = MapAppropriatenessLevelToConflictLevel(domainCulturalScore.Level),
            CulturalFactors = GenerateFactorsFromLevel(domainCulturalScore.Level)
        };
    }

    /// <summary>
    /// Creates an ExtendedCulturalScore from domain recommendation for backward compatibility
    /// </summary>
    public static ExtendedCulturalScore CreateExtendedCulturalScore(
        DomainEventRecommendation recommendation,
        CulturalScore baseCulturalScore)
    {
        var conflictLevel = MapScoreToConflictLevel(baseCulturalScore.Value);
        var factors = GenerateFactorsFromScore(baseCulturalScore.Value, recommendation.Event.Title.Value);

        return new ExtendedCulturalScore(
            overallScore: baseCulturalScore.Value,
            appropriatenessScore: baseCulturalScore.Value,
            diasporaFriendlinessScore: CalculateDiasporaFriendliness(baseCulturalScore),
            conflictLevel: conflictLevel,
            factors: factors
        );
    }

    private static double CalculateDiasporaFriendliness(CulturalScore culturalScore)
    {
        // Calculate diaspora friendliness based on cultural appropriateness
        return culturalScore.Level switch
        {
            CulturalAppropriatenessLevel.HighlyAppropriate => 0.9,
            CulturalAppropriatenessLevel.Appropriate => 0.7,
            CulturalAppropriatenessLevel.Neutral => 0.5,
            CulturalAppropriatenessLevel.Questionable => 0.3,
            CulturalAppropriatenessLevel.Inappropriate => 0.1,
            _ => 0.5
        };
    }

    private static string MapAppropriatenessLevelToConflictLevel(CulturalAppropriatenessLevel level)
    {
        return level switch
        {
            CulturalAppropriatenessLevel.HighlyAppropriate => CulturalConflictLevel.None.ToString(),
            CulturalAppropriatenessLevel.Appropriate => CulturalConflictLevel.Low.ToString(),
            CulturalAppropriatenessLevel.Neutral => CulturalConflictLevel.Medium.ToString(),
            CulturalAppropriatenessLevel.Questionable => CulturalConflictLevel.High.ToString(),
            CulturalAppropriatenessLevel.Inappropriate => CulturalConflictLevel.Critical.ToString(),
            _ => CulturalConflictLevel.Medium.ToString()
        };
    }

    private static CulturalConflictLevel MapScoreToConflictLevel(double score)
    {
        return score switch
        {
            >= 0.8 => CulturalConflictLevel.None,
            >= 0.6 => CulturalConflictLevel.Low,
            >= 0.4 => CulturalConflictLevel.Medium,
            >= 0.2 => CulturalConflictLevel.High,
            _ => CulturalConflictLevel.Critical
        };
    }

    private static List<string> GenerateFactorsFromLevel(CulturalAppropriatenessLevel level)
    {
        return level switch
        {
            CulturalAppropriatenessLevel.HighlyAppropriate => new List<string>
            {
                "Highly culturally appropriate",
                "Aligns with traditional values",
                "Diaspora-friendly"
            },
            CulturalAppropriatenessLevel.Appropriate => new List<string>
            {
                "Culturally appropriate",
                "Suitable for community"
            },
            CulturalAppropriatenessLevel.Neutral => new List<string>
            {
                "Culturally neutral",
                "No specific cultural concerns"
            },
            CulturalAppropriatenessLevel.Questionable => new List<string>
            {
                "Some cultural concerns",
                "May require careful consideration"
            },
            CulturalAppropriatenessLevel.Inappropriate => new List<string>
            {
                "Cultural concerns present",
                "Not recommended for traditional preferences"
            },
            _ => new List<string> { "Cultural factors under evaluation" }
        };
    }

    private static List<string> GenerateFactorsFromScore(double score, string eventTitle)
    {
        var factors = new List<string>();

        if (score >= 0.8)
        {
            factors.Add("High cultural compatibility");
            factors.Add("Strong community appeal");
        }
        else if (score >= 0.6)
        {
            factors.Add("Good cultural fit");
            factors.Add("Community appropriate");
        }
        else if (score >= 0.4)
        {
            factors.Add("Moderate cultural alignment");
        }
        else
        {
            factors.Add("Limited cultural relevance");
        }

        // Add event-specific factors
        if (eventTitle.Contains("Buddhist") || eventTitle.Contains("Temple"))
        {
            factors.Add("Buddhist community event");
        }

        if (eventTitle.Contains("Sri Lankan") || eventTitle.Contains("Ceylon"))
        {
            factors.Add("Sri Lankan cultural heritage");
        }

        return factors;
    }
}

/// <summary>
/// Extended cultural score with all properties needed by the application layer
/// This bridges the gap between domain CulturalScore and application CulturalScoreDto
/// </summary>
public class ExtendedCulturalScore
{
    public double OverallScore { get; }
    public double AppropriatenessScore { get; }
    public double DiasporaFriendlinessScore { get; }
    public CulturalConflictLevel ConflictLevel { get; }
    public List<string> Factors { get; }

    public ExtendedCulturalScore(
        double overallScore,
        double appropriatenessScore,
        double diasporaFriendlinessScore,
        CulturalConflictLevel conflictLevel,
        List<string> factors)
    {
        OverallScore = Math.Max(0.0, Math.Min(1.0, overallScore));
        AppropriatenessScore = Math.Max(0.0, Math.Min(1.0, appropriatenessScore));
        DiasporaFriendlinessScore = Math.Max(0.0, Math.Min(1.0, diasporaFriendlinessScore));
        ConflictLevel = conflictLevel;
        Factors = factors ?? new List<string>();
    }
}

/// <summary>
/// Cultural conflict level enumeration for application layer
/// </summary>
public enum CulturalConflictLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}