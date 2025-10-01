using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Alternative revenue channel result for cultural event and diaspora community revenue recovery
/// Tracks the performance and effectiveness of alternative revenue channels during disaster scenarios
/// </summary>
public class AlternativeRevenueChannelResult
{
    public required string ResultId { get; set; }
    public required string ChannelConfigurationId { get; set; }
    public required AlternativeRevenueChannelStatus Status { get; set; }
    public required DateTime ActivationTimestamp { get; set; }
    public required TimeSpan OperationDuration { get; set; }
    public required List<ChannelPerformanceResult> ChannelPerformances { get; set; }
    public required Dictionary<string, AlternativeChannelMetric> ChannelMetrics { get; set; }
    public required string ActivatedBy { get; set; }
    public required decimal TotalRevenueProcessed { get; set; }
    public required decimal RevenueDiversionPercentage { get; set; }
    public List<ChannelFailoverEvent> FailoverEvents { get; set; } = new();
    public Dictionary<string, object> OperationContext { get; set; } = new();
    public string? OperationSummary { get; set; }
    public List<string> ActiveChannels { get; set; } = new();
    public Dictionary<string, decimal> ChannelRevenueBreakdown { get; set; } = new();
    public List<CulturalChannelResult> CulturalChannelResults { get; set; } = new();

    public AlternativeRevenueChannelResult()
    {
        ChannelPerformances = new List<ChannelPerformanceResult>();
        ChannelMetrics = new Dictionary<string, AlternativeChannelMetric>();
        FailoverEvents = new List<ChannelFailoverEvent>();
        OperationContext = new Dictionary<string, object>();
        ActiveChannels = new List<string>();
        ChannelRevenueBreakdown = new Dictionary<string, decimal>();
        CulturalChannelResults = new List<CulturalChannelResult>();
    }

    public bool WasSuccessful()
    {
        return Status == AlternativeRevenueChannelStatus.Operational ||
               Status == AlternativeRevenueChannelStatus.PartiallyOperational;
    }

    public decimal GetChannelEffectiveness()
    {
        if (TotalRevenueProcessed <= 0) return 0;
        return (RevenueDiversionPercentage / 100) * TotalRevenueProcessed;
    }

    public int GetSuccessfulChannelsCount()
    {
        return ChannelPerformances.Count(c => c.PerformanceStatus == ChannelPerformanceStatus.Successful);
    }
}

/// <summary>
/// Alternative revenue channel status
/// </summary>
public enum AlternativeRevenueChannelStatus
{
    Operational = 1,
    PartiallyOperational = 2,
    Failed = 3,
    Activating = 4,
    Deactivating = 5,
    Maintenance = 6
}

/// <summary>
/// Channel performance result
/// </summary>
public class ChannelPerformanceResult
{
    public required string ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required AlternativeChannelType ChannelType { get; set; }
    public required ChannelPerformanceStatus PerformanceStatus { get; set; }
    public required decimal RevenueProcessed { get; set; }
    public required int TransactionCount { get; set; }
    public required double SuccessRate { get; set; }
    public required TimeSpan AverageResponseTime { get; set; }
    public required decimal ProcessingCost { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? PerformanceNotes { get; set; }
    public List<ChannelIssue> Issues { get; set; } = new();
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Channel performance status
/// </summary>
public enum ChannelPerformanceStatus
{
    Successful = 1,
    Degraded = 2,
    Failed = 3,
    Intermittent = 4,
    Recovering = 5,
    Testing = 6
}

/// <summary>
/// Alternative channel metric
/// </summary>
public class AlternativeChannelMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required AlternativeChannelMetricType Type { get; set; }
    public required DateTime MeasuredAt { get; set; }
    public double? BaselineValue { get; set; }
    public double? TargetValue { get; set; }
    public string? Description { get; set; }
    public AlternativeChannelMetricStatus Status { get; set; } = AlternativeChannelMetricStatus.Normal;
}

/// <summary>
/// Alternative channel metric types
/// </summary>
public enum AlternativeChannelMetricType
{
    RevenueVolume = 1,
    TransactionVolume = 2,
    SuccessRate = 3,
    ResponseTime = 4,
    ErrorRate = 5,
    UtilizationRate = 6,
    CostEfficiency = 7,
    CustomerSatisfaction = 8
}

/// <summary>
/// Alternative channel metric status
/// </summary>
public enum AlternativeChannelMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3,
    Excellent = 4,
    Poor = 5
}

/// <summary>
/// Channel failover event
/// </summary>
public class ChannelFailoverEvent
{
    public required string EventId { get; set; }
    public required string FromChannelId { get; set; }
    public required string ToChannelId { get; set; }
    public required ChannelFailoverReason Reason { get; set; }
    public required DateTime FailoverTimestamp { get; set; }
    public required TimeSpan FailoverDuration { get; set; }
    public required ChannelFailoverOutcome Outcome { get; set; }
    public decimal RevenueImpact { get; set; }
    public string? FailoverNotes { get; set; }
    public bool WasAutomated { get; set; }
    public Dictionary<string, object> EventContext { get; set; } = new();
}

/// <summary>
/// Channel failover reasons
/// </summary>
public enum ChannelFailoverReason
{
    ServiceFailure = 1,
    CapacityExceeded = 2,
    PerformanceDegradation = 3,
    MaintenanceRequired = 4,
    SecurityIssue = 5,
    RegulatoryChange = 6,
    ManualSwitch = 7
}

/// <summary>
/// Channel failover outcome
/// </summary>
public enum ChannelFailoverOutcome
{
    Successful = 1,
    PartiallySuccessful = 2,
    Failed = 3,
    Cancelled = 4,
    Delayed = 5
}

/// <summary>
/// Channel issue
/// </summary>
public class ChannelIssue
{
    public required string IssueId { get; set; }
    public required ChannelIssueType IssueType { get; set; }
    public required ChannelIssueSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public ChannelIssueStatus Status { get; set; } = ChannelIssueStatus.Open;
    public string? ResolutionNotes { get; set; }
    public decimal RevenueImpact { get; set; }
    public Dictionary<string, object> IssueContext { get; set; } = new();
}

/// <summary>
/// Channel issue types
/// </summary>
public enum ChannelIssueType
{
    TechnicalFailure = 1,
    ConnectivityProblem = 2,
    CapacityIssue = 3,
    SecurityVulnerability = 4,
    ConfigurationError = 5,
    IntegrationFailure = 6,
    UserExperienceIssue = 7
}

/// <summary>
/// Channel issue severity
/// </summary>
public enum ChannelIssueSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Channel issue status
/// </summary>
public enum ChannelIssueStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4,
    Escalated = 5
}

/// <summary>
/// Cultural channel result
/// </summary>
public class CulturalChannelResult
{
    public required CulturalEventType EventType { get; set; }
    public required string ChannelId { get; set; }
    public required CulturalChannelAdaptationStatus AdaptationStatus { get; set; }
    public required decimal CommunityRevenueProcessed { get; set; }
    public required double CommunityEngagementRate { get; set; }
    public string? CulturalAdaptations { get; set; }
    public List<string> CommunityFeedback { get; set; } = new();
    public Dictionary<string, object> CulturalMetrics { get; set; } = new();
    public bool MaintainedCulturalIntegrity { get; set; } = true;
}

/// <summary>
/// Cultural channel adaptation status
/// </summary>
public enum CulturalChannelAdaptationStatus
{
    FullyAdapted = 1,
    PartiallyAdapted = 2,
    RequiresImprovement = 3,
    CulturallyInappropriate = 4,
    UnderReview = 5
}