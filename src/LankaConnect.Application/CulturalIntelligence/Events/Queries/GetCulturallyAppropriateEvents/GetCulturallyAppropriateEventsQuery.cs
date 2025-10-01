using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.CulturalIntelligence.Events.Queries.GetCulturallyAppropriateEvents;

/// <summary>
/// Query to analyze cultural appropriateness of events for specific users and cultural contexts
/// </summary>
public class GetCulturallyAppropriateEventsQuery : IQuery<GetCulturallyAppropriateEventsResponse>
{
    public Guid? EventId { get; set; }
    public Guid UserId { get; set; }
    public string CulturalBackground { get; set; } = string.Empty;
    public DateTime? AnalysisDate { get; set; }
    public List<string> TargetAudiences { get; set; } = new();
    public bool IncludeConflictAnalysis { get; set; } = true;
    public bool IncludeRecommendations { get; set; } = true;
    public bool IncludeCalendarValidation { get; set; } = true;
}