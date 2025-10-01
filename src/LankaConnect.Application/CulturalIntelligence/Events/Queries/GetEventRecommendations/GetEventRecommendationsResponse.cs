namespace LankaConnect.Application.CulturalIntelligence.Events.Queries.GetEventRecommendations;

/// <summary>
/// Response for event recommendations with cultural intelligence scoring
/// </summary>
public class GetEventRecommendationsResponse
{
    public List<EventRecommendationDto> Recommendations { get; set; } = new();
    public int TotalCount { get; set; }
    public CulturalIntelligenceMetadata Metadata { get; set; } = new();
}

public class EventRecommendationDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public double RecommendationScore { get; set; }
    public CulturalScoreDto CulturalScore { get; set; } = new();
    public GeographicScoreDto GeographicScore { get; set; } = new();
    public List<string> RecommendationReasons { get; set; } = new();
}

public class CulturalScoreDto
{
    public double OverallScore { get; set; }
    public double AppropriatenessScore { get; set; }
    public double DiasporaFriendlinessScore { get; set; }
    public string ConflictLevel { get; set; } = string.Empty;
    public List<string> CulturalFactors { get; set; } = new();
}

public class GeographicScoreDto
{
    public double ProximityScore { get; set; }
    public double CommunityDensityScore { get; set; }
    public double AccessibilityScore { get; set; }
    public double DistanceKm { get; set; }
}

public class CulturalIntelligenceMetadata
{
    public string Version { get; set; } = "1.0";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int ProcessingTimeMs { get; set; }
    public string AlgorithmVersion { get; set; } = string.Empty;
    public List<string> DataSources { get; set; } = new();
}