using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Billing continuity result for cultural events and diaspora community billing operations
/// Tracks the outcome and effectiveness of billing continuity measures during disaster scenarios
/// </summary>
public class BillingContinuityResult
{
    public required string ResultId { get; set; }
    public required string ConfigurationId { get; set; }
    public required BillingContinuityStatus Status { get; set; }
    public required DateTime ExecutionTimestamp { get; set; }
    public required TimeSpan ExecutionDuration { get; set; }
    public required List<BillingSystemResult> SystemResults { get; set; }
    public required Dictionary<string, BillingContinuityMetric> ContinuityMetrics { get; set; }
    public required string ExecutedBy { get; set; }
    public required decimal TotalBillingVolume { get; set; }
    public required decimal FailedBillingAmount { get; set; }
    public required double BillingSuccessRate { get; set; }
    public List<BillingFailoverEvent> FailoverEvents { get; set; } = new();
    public Dictionary<string, object> ExecutionContext { get; set; } = new();
    public string? ContinuitySummary { get; set; }
    public List<string> AffectedBillingStreams { get; set; } = new();
    public Dictionary<string, decimal> StreamBillingResults { get; set; } = new();
    public List<CulturalBillingResult> CulturalBillingResults { get; set; } = new();

    public BillingContinuityResult()
    {
        SystemResults = new List<BillingSystemResult>();
        ContinuityMetrics = new Dictionary<string, BillingContinuityMetric>();
        FailoverEvents = new List<BillingFailoverEvent>();
        ExecutionContext = new Dictionary<string, object>();
        AffectedBillingStreams = new List<string>();
        StreamBillingResults = new Dictionary<string, decimal>();
        CulturalBillingResults = new List<CulturalBillingResult>();
    }

    public bool WasSuccessful()
    {
        return Status == BillingContinuityStatus.Operational && BillingSuccessRate >= 95.0;
    }

    public decimal GetTotalRevenueLoss()
    {
        return FailedBillingAmount;
    }

    public int GetSuccessfulSystemsCount()
    {
        return SystemResults.Count(s => s.OperationStatus == BillingSystemOperationStatus.Successful);
    }
}

/// <summary>
/// Billing continuity status
/// </summary>
public enum BillingContinuityStatus
{
    Operational = 1,
    Degraded = 2,
    Failed = 3,
    Recovering = 4,
    Maintenance = 5,
    Emergency = 6
}

/// <summary>
/// Billing system result
/// </summary>
public class BillingSystemResult
{
    public required string SystemId { get; set; }
    public required string SystemName { get; set; }
    public required BillingSystemType SystemType { get; set; }
    public required BillingSystemOperationStatus OperationStatus { get; set; }
    public required decimal ProcessedAmount { get; set; }
    public required int TransactionCount { get; set; }
    public required double SystemSuccessRate { get; set; }
    public required TimeSpan AverageResponseTime { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? StatusMessage { get; set; }
    public List<BillingSystemIssue> Issues { get; set; } = new();
    public Dictionary<string, double> SystemMetrics { get; set; } = new();
}

/// <summary>
/// Billing system operation status
/// </summary>
public enum BillingSystemOperationStatus
{
    Successful = 1,
    Degraded = 2,
    Failed = 3,
    Timeout = 4,
    Unavailable = 5,
    Maintenance = 6
}

/// <summary>
/// Billing continuity metric
/// </summary>
public class BillingContinuityMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required BillingContinuityMetricType Type { get; set; }
    public required DateTime MeasuredAt { get; set; }
    public double? BaselineValue { get; set; }
    public double? TargetValue { get; set; }
    public string? Description { get; set; }
    public BillingContinuityMetricStatus Status { get; set; } = BillingContinuityMetricStatus.Normal;
}

/// <summary>
/// Billing continuity metric types
/// </summary>
public enum BillingContinuityMetricType
{
    BillingVolume = 1,
    TransactionCount = 2,
    SuccessRate = 3,
    ResponseTime = 4,
    ErrorRate = 5,
    FailoverTime = 6,
    SystemAvailability = 7,
    RevenueImpact = 8
}

/// <summary>
/// Billing continuity metric status
/// </summary>
public enum BillingContinuityMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3,
    Excellent = 4,
    Poor = 5
}

/// <summary>
/// Billing failover event
/// </summary>
public class BillingFailoverEvent
{
    public required string EventId { get; set; }
    public required string FromSystemId { get; set; }
    public required string ToSystemId { get; set; }
    public required BillingFailoverReason Reason { get; set; }
    public required DateTime FailoverTimestamp { get; set; }
    public required TimeSpan FailoverDuration { get; set; }
    public required BillingFailoverOutcome Outcome { get; set; }
    public decimal BillingImpact { get; set; }
    public string? FailoverNotes { get; set; }
    public bool WasAutomated { get; set; }
    public Dictionary<string, object> EventContext { get; set; } = new();
}

/// <summary>
/// Billing failover reasons
/// </summary>
public enum BillingFailoverReason
{
    SystemFailure = 1,
    CapacityExceeded = 2,
    PerformanceDegradation = 3,
    MaintenanceRequired = 4,
    SecurityIssue = 5,
    NetworkProblem = 6,
    ManualSwitch = 7
}

/// <summary>
/// Billing failover outcome
/// </summary>
public enum BillingFailoverOutcome
{
    Successful = 1,
    PartiallySuccessful = 2,
    Failed = 3,
    Cancelled = 4,
    Delayed = 5
}

/// <summary>
/// Billing system issue
/// </summary>
public class BillingSystemIssue
{
    public required string IssueId { get; set; }
    public required BillingSystemIssueType IssueType { get; set; }
    public required BillingSystemIssueSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public BillingSystemIssueStatus Status { get; set; } = BillingSystemIssueStatus.Open;
    public string? ResolutionNotes { get; set; }
    public decimal BillingImpact { get; set; }
    public Dictionary<string, object> IssueContext { get; set; } = new();
}

/// <summary>
/// Billing system issue types
/// </summary>
public enum BillingSystemIssueType
{
    TechnicalFailure = 1,
    ConnectivityProblem = 2,
    CapacityIssue = 3,
    SecurityVulnerability = 4,
    ConfigurationError = 5,
    IntegrationFailure = 6,
    PaymentGatewayIssue = 7
}

/// <summary>
/// Billing system issue severity
/// </summary>
public enum BillingSystemIssueSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Billing system issue status
/// </summary>
public enum BillingSystemIssueStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4,
    Escalated = 5
}

/// <summary>
/// Cultural billing result
/// </summary>
public class CulturalBillingResult
{
    public required CulturalEventType EventType { get; set; }
    public required string EventId { get; set; }
    public required decimal CommunityBillingVolume { get; set; }
    public required double CommunitySuccessRate { get; set; }
    public required CulturalBillingStatus BillingStatus { get; set; }
    public string? CulturalAdaptations { get; set; }
    public List<string> CommunityFeedback { get; set; } = new();
    public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; } = new();
    public bool MaintainedCulturalSensitivity { get; set; } = true;
    public Dictionary<string, object> CulturalMetrics { get; set; } = new();
}

/// <summary>
/// Cultural billing status
/// </summary>
public enum CulturalBillingStatus
{
    Successful = 1,
    PartiallySuccessful = 2,
    RequiresAdaptation = 3,
    CulturallyInappropriate = 4,
    UnderReview = 5,
    Enhanced = 6
}