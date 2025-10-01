using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Revenue impact monitoring result for cultural events and diaspora community revenue tracking
/// Provides comprehensive analysis of revenue impact during disaster recovery scenarios
/// </summary>
public class RevenueImpactMonitoringResult
{
    public required string MonitoringId { get; set; }
    public required string ConfigurationId { get; set; }
    public required RevenueImpactMonitoringStatus Status { get; set; }
    public required DateTime MonitoringTimestamp { get; set; }
    public required TimeSpan MonitoringDuration { get; set; }
    public required List<RevenueStreamImpact> RevenueStreamImpacts { get; set; }
    public required Dictionary<string, RevenueImpactMetricResult> ImpactMetrics { get; set; }
    public required string MonitoredBy { get; set; }
    public required RevenueImpactSeverityLevel OverallImpactSeverity { get; set; }
    public required decimal TotalRevenueImpact { get; set; }
    public List<RevenueImpactAlert> GeneratedAlerts { get; set; } = new();
    public Dictionary<string, object> MonitoringContext { get; set; } = new();
    public string? ImpactSummary { get; set; }
    public List<string> AffectedComponents { get; set; } = new();
    public Dictionary<string, decimal> ImpactBreakdown { get; set; } = new();
    public List<CulturalEventRevenueImpact> CulturalEventImpacts { get; set; } = new();

    public RevenueImpactMonitoringResult()
    {
        RevenueStreamImpacts = new List<RevenueStreamImpact>();
        ImpactMetrics = new Dictionary<string, RevenueImpactMetricResult>();
        GeneratedAlerts = new List<RevenueImpactAlert>();
        MonitoringContext = new Dictionary<string, object>();
        AffectedComponents = new List<string>();
        ImpactBreakdown = new Dictionary<string, decimal>();
        CulturalEventImpacts = new List<CulturalEventRevenueImpact>();
    }

    public bool HasCriticalImpact()
    {
        return OverallImpactSeverity >= RevenueImpactSeverityLevel.Critical ||
               RevenueStreamImpacts.Any(r => r.ImpactSeverity >= RevenueImpactSeverityLevel.Critical);
    }

    public decimal GetTotalProjectedLoss()
    {
        return TotalRevenueImpact + ImpactBreakdown.Values.Sum();
    }

    public int GetCriticalAlertsCount()
    {
        return GeneratedAlerts.Count(a => a.Priority >= RevenueImpactAlertPriority.Critical);
    }
}

/// <summary>
/// Revenue impact monitoring status
/// </summary>
public enum RevenueImpactMonitoringStatus
{
    Active = 1,
    Warning = 2,
    Critical = 3,
    Failed = 4,
    Suspended = 5,
    Completed = 6
}

/// <summary>
/// Revenue stream impact
/// </summary>
public class RevenueStreamImpact
{
    public required string StreamId { get; set; }
    public required string StreamName { get; set; }
    public required RevenueStreamType StreamType { get; set; }
    public required RevenueStreamImpactStatus ImpactStatus { get; set; }
    public required RevenueImpactSeverityLevel ImpactSeverity { get; set; }
    public required decimal BaselineRevenue { get; set; }
    public required decimal CurrentRevenue { get; set; }
    public required decimal ImpactAmount { get; set; }
    public required double ImpactPercentage { get; set; }
    public DateTime ImpactStartTime { get; set; }
    public DateTime? ImpactEndTime { get; set; }
    public string? ImpactCause { get; set; }
    public List<RevenueImpactFactor> ImpactFactors { get; set; } = new();
    public Dictionary<string, object> StreamContext { get; set; } = new();
}

/// <summary>
/// Revenue stream impact status
/// </summary>
public enum RevenueStreamImpactStatus
{
    Normal = 1,
    Declining = 2,
    Impacted = 3,
    CriticallyImpacted = 4,
    Failed = 5,
    Recovering = 6
}

/// <summary>
/// Revenue impact metric result
/// </summary>
public class RevenueImpactMetricResult
{
    public required string MetricId { get; set; }
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required RevenueImpactMetricStatus Status { get; set; }
    public required DateTime MeasuredAt { get; set; }
    public double? PreviousValue { get; set; }
    public double? ChangeAmount { get; set; }
    public double? ChangePercentage { get; set; }
    public string? MetricDescription { get; set; }
    public Dictionary<string, object> MetricMetadata { get; set; } = new();
}

/// <summary>
/// Revenue impact metric status
/// </summary>
public enum RevenueImpactMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3,
    Degraded = 4,
    Failed = 5,
    Unknown = 6
}

/// <summary>
/// Revenue impact alert
/// </summary>
public class RevenueImpactAlert
{
    public required string AlertId { get; set; }
    public required RevenueImpactAlertType AlertType { get; set; }
    public required RevenueImpactAlertPriority Priority { get; set; }
    public required string Message { get; set; }
    public required DateTime GeneratedAt { get; set; }
    public required string AffectedComponent { get; set; }
    public required decimal ImpactAmount { get; set; }
    public RevenueImpactAlertStatus Status { get; set; } = RevenueImpactAlertStatus.Active;
    public string? RecommendedAction { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public Dictionary<string, object> AlertContext { get; set; } = new();
}

/// <summary>
/// Revenue impact alert types
/// </summary>
public enum RevenueImpactAlertType
{
    ThresholdBreached = 1,
    TrendDetected = 2,
    AnomalyDetected = 3,
    SystemFailure = 4,
    RecoveryNeeded = 5,
    EscalationRequired = 6
}

/// <summary>
/// Revenue impact alert status
/// </summary>
public enum RevenueImpactAlertStatus
{
    Active = 1,
    Acknowledged = 2,
    Resolved = 3,
    Escalated = 4,
    Suppressed = 5
}

/// <summary>
/// Revenue impact factor
/// </summary>
public class RevenueImpactFactor
{
    public required string FactorId { get; set; }
    public required RevenueImpactFactorType Type { get; set; }
    public required string Description { get; set; }
    public required double ImpactContribution { get; set; }
    public required RevenueImpactFactorSeverity Severity { get; set; }
    public DateTime DetectedAt { get; set; }
    public string? MitigationAction { get; set; }
    public Dictionary<string, object> FactorMetadata { get; set; } = new();
}

/// <summary>
/// Revenue impact factor types
/// </summary>
public enum RevenueImpactFactorType
{
    SystemOutage = 1,
    NetworkIssue = 2,
    DatabaseProblem = 3,
    PaymentGatewayFailure = 4,
    CulturalEventDisruption = 5,
    SeasonalVariation = 6,
    ExternalDependency = 7,
    UserBehaviorChange = 8
}

/// <summary>
/// Revenue impact factor severity
/// </summary>
public enum RevenueImpactFactorSeverity
{
    Minor = 1,
    Moderate = 2,
    Major = 3,
    Severe = 4,
    Critical = 5
}

/// <summary>
/// Cultural event revenue impact
/// </summary>
public class CulturalEventRevenueImpact
{
    public required string EventId { get; set; }
    public required string EventName { get; set; }
    public required CulturalEventType EventType { get; set; }
    public required RevenueImpactSeverityLevel ImpactSeverity { get; set; }
    public required decimal BaselineEventRevenue { get; set; }
    public required decimal ActualEventRevenue { get; set; }
    public required decimal RevenueImpact { get; set; }
    public required DateTime EventDate { get; set; }
    public string? DiasporaRegion { get; set; }
    public List<string> ImpactedServices { get; set; } = new();
    public Dictionary<string, decimal> ServiceImpactBreakdown { get; set; } = new();
    public string? RecoveryStrategy { get; set; }
    public CulturalEventImpactStatus RecoveryStatus { get; set; } = CulturalEventImpactStatus.Assessing;
}

/// <summary>
/// Cultural event impact status
/// </summary>
public enum CulturalEventImpactStatus
{
    Assessing = 1,
    Mitigating = 2,
    Recovering = 3,
    Recovered = 4,
    PartiallyRecovered = 5,
    Failed = 6
}