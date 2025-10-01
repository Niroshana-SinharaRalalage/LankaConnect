using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.CulturalIntelligence.Communications.Queries.OptimizeEmailTiming;

public class OptimizeEmailTimingQuery : IQuery<OptimizeEmailTimingResponse>
{
    public Guid RecipientId { get; set; }
    public string EmailType { get; set; } = string.Empty;
    public string CulturalBackground { get; set; } = string.Empty;
    public EmailTimingPreferences PreferredTiming { get; set; } = new();
    public SchedulingWindow SchedulingWindow { get; set; } = new();
}

public class EmailTimingPreferences
{
    public string TimeZone { get; set; } = string.Empty;
    public List<string> PreferredDays { get; set; } = new();
    public bool AvoidReligiousDays { get; set; } = true;
    public TimeSpan? PreferredStartTime { get; set; }
    public TimeSpan? PreferredEndTime { get; set; }
}

public class SchedulingWindow
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}