using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Application-layer cultural event model for performance monitoring
/// </summary>
public class CulturalEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> AffectedRegions { get; set; } = new();
    public decimal PerformanceImpactFactor { get; set; } = 1.0m;
    public List<string> ParticipatingCommunities { get; set; } = new();
    public long EstimatedAttendees { get; set; }
    public string PrimaryTimeZone { get; set; } = string.Empty;
    public bool RequiresSpecialScaling { get; set; }
    public List<string> PrimaryLanguages { get; set; } = new();
}

/// <summary>
/// Upcoming cultural event with predictive performance data
/// </summary>
public class UpcomingCulturalEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan Duration { get; set; }
    public int PredictedAttendees { get; set; }
    public double LoadMultiplier { get; set; } = 1.0;
    public string Region { get; set; } = string.Empty;
    public int PriorityLevel { get; set; } = 1;
    public bool IsHighImpact { get; set; }
    public List<string> RequiredPreparations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Prediction timeframe for cultural event forecasting
/// </summary>
public class PredictionTimeframe
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan Duration => EndDate - StartDate;
    public string TimeframeName { get; set; } = string.Empty;
    public int DaysAhead { get; set; }
    public bool IncludeWeekends { get; set; } = true;
    public bool IncludeHolidays { get; set; } = true;
    public List<string> SpecialConsiderations { get; set; } = new();

    public static PredictionTimeframe CreateDaily() => new()
    {
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddDays(1),
        TimeframeName = "Daily",
        DaysAhead = 1
    };

    public static PredictionTimeframe CreateWeekly() => new()
    {
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddDays(7),
        TimeframeName = "Weekly",
        DaysAhead = 7
    };

    public static PredictionTimeframe CreateMonthly() => new()
    {
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddDays(30),
        TimeframeName = "Monthly",
        DaysAhead = 30
    };
}