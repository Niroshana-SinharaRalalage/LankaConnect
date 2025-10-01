using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.CulturalIntelligence.Events.Queries.GetEventRecommendations;

/// <summary>
/// Query to get personalized event recommendations using cultural intelligence algorithms
/// Leverages existing IEventRecommendationEngine from domain services
/// </summary>
public class GetEventRecommendationsQuery : IQuery<GetEventRecommendationsResponse>
{
    public Guid UserId { get; set; }
    public int MaxResults { get; set; } = 10;
    public bool IncludeCulturalScoring { get; set; } = true;
    public bool IncludeGeographicScoring { get; set; } = true;
    public DateTime? ForDate { get; set; }
    public List<string> EventTypes { get; set; } = new();
    public GeographicFilter? GeographicFilter { get; set; }
    public CulturalFilter? CulturalFilter { get; set; }
}

public class GeographicFilter
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? MaxDistanceKm { get; set; }
    public string? PreferredRegion { get; set; }
}

public class CulturalFilter
{
    public string? CulturalBackground { get; set; }
    public List<string> PreferredLanguages { get; set; } = new();
    public bool AvoidReligiousConflicts { get; set; } = true;
    public bool PreferDiasporaFriendlyEvents { get; set; }
}