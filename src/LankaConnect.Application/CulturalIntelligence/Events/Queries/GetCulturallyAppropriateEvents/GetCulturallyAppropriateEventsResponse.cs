namespace LankaConnect.Application.CulturalIntelligence.Events.Queries.GetCulturallyAppropriateEvents;

/// <summary>
/// Response containing cultural appropriateness analysis for events
/// </summary>
public class GetCulturallyAppropriateEventsResponse
{
    public double AppropriatenessScore { get; set; }
    public string ConflictLevel { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public CalendarValidationDto CalendarValidation { get; set; } = new();
    public CulturalAnalysisDto CulturalAnalysis { get; set; } = new();
    public List<ConflictDetailsDto> Conflicts { get; set; } = new();
}

public class CalendarValidationDto
{
    public bool IsPoyadayConflict { get; set; }
    public bool IsFestivalConflict { get; set; }
    public List<string> ConflictDetails { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
    public List<DateTime> ConflictingDates { get; set; } = new();
}

public class CulturalAnalysisDto
{
    public string EventNature { get; set; } = string.Empty;
    public List<string> CulturalFactors { get; set; } = new();
    public Dictionary<string, double> AudienceAppropriatenessScores { get; set; } = new();
    public ReligiousConsiderationsDto ReligiousConsiderations { get; set; } = new();
}

public class ReligiousConsiderationsDto
{
    public bool IsBuddhistAppropriate { get; set; }
    public bool IsHinduAppropriate { get; set; }
    public bool IsChristianAppropriate { get; set; }
    public List<string> ReligiousGuidelines { get; set; } = new();
}

public class ConflictDetailsDto
{
    public string ConflictType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> Mitigation { get; set; } = new();
}