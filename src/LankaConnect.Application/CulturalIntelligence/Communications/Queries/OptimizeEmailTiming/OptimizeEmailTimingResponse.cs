namespace LankaConnect.Application.CulturalIntelligence.Communications.Queries.OptimizeEmailTiming;

public class OptimizeEmailTimingResponse
{
    public List<OptimalSendTimeDto> OptimalSendTimes { get; set; } = new();
    public List<string> CulturalConsiderations { get; set; } = new();
    public PoyadayConflictAnalysisDto PoyadayConflicts { get; set; } = new();
}

public class OptimalSendTimeDto
{
    public DateTime RecommendedTime { get; set; }
    public double OptimalityScore { get; set; }
    public bool ConflictsWithReligiousDay { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public List<string> CulturalFactors { get; set; } = new();
}

public class PoyadayConflictAnalysisDto
{
    public bool HasConflicts { get; set; }
    public List<DateTime> ConflictingPoyadays { get; set; } = new();
    public List<string> AlternativeTimes { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}