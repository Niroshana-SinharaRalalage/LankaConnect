using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Revenue impact monitoring configuration for cultural events and diaspora community services
/// Configures real-time monitoring of revenue impact during disaster recovery scenarios
/// </summary>
public class RevenueImpactMonitoringConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required RevenueImpactMonitoringScope MonitoringScope { get; set; }
    public required List<RevenueStreamMonitoringConfig> RevenueStreamConfigs { get; set; }
    public required Dictionary<string, RevenueImpactThreshold> ImpactThresholds { get; set; }
    public required TimeSpan MonitoringInterval { get; set; }
    public required RevenueImpactAlertingConfiguration AlertingConfig { get; set; }
    public required List<string> MonitoredComponents { get; set; }
    public required Dictionary<string, object> MonitoringParameters { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public List<CulturalEventRevenueMonitoringRule> CulturalEventRules { get; set; } = new();
    public Dictionary<string, RevenueImpactEscalationRule> EscalationRules { get; set; } = new();

    public RevenueImpactMonitoringConfiguration()
    {
        RevenueStreamConfigs = new List<RevenueStreamMonitoringConfig>();
        ImpactThresholds = new Dictionary<string, RevenueImpactThreshold>();
        MonitoredComponents = new List<string>();
        MonitoringParameters = new Dictionary<string, object>();
        CulturalEventRules = new List<CulturalEventRevenueMonitoringRule>();
        EscalationRules = new Dictionary<string, RevenueImpactEscalationRule>();
    }

    public bool ShouldMonitorComponent(string componentId)
    {
        return IsEnabled && MonitoredComponents.Contains(componentId);
    }

    public RevenueImpactThreshold? GetThresholdForStream(string streamId)
    {
        return ImpactThresholds.TryGetValue(streamId, out var threshold) ? threshold : null;
    }
}

/// <summary>
/// Revenue impact monitoring scope
/// </summary>
public enum RevenueImpactMonitoringScope
{
    CulturalEvents = 1,
    DiasporaServices = 2,
    AllRevenueStreams = 3,
    CriticalStreams = 4,
    HighRiskStreams = 5,
    CustomScope = 6
}

/// <summary>
/// Revenue stream monitoring configuration
/// </summary>
public class RevenueStreamMonitoringConfig
{
    public required string StreamId { get; set; }
    public required string StreamName { get; set; }
    public required RevenueStreamType StreamType { get; set; }
    public required RevenueStreamMonitoringPriority Priority { get; set; }
    public required List<RevenueImpactMetric> MonitoredMetrics { get; set; }
    public required TimeSpan SamplingInterval { get; set; }
    public bool IsEnabled { get; set; } = true;
    public decimal BaselineRevenue { get; set; }
    public Dictionary<string, object> StreamParameters { get; set; } = new();
    public List<string> DependentComponents { get; set; } = new();
}

/// <summary>
/// Revenue stream monitoring priority
/// </summary>
public enum RevenueStreamMonitoringPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}

/// <summary>
/// Revenue impact threshold
/// </summary>
public class RevenueImpactThreshold
{
    public required string ThresholdId { get; set; }
    public required RevenueImpactThresholdType Type { get; set; }
    public required decimal ThresholdValue { get; set; }
    public required RevenueImpactSeverityLevel SeverityLevel { get; set; }
    public required TimeSpan EvaluationWindow { get; set; }
    public string? Description { get; set; }
    public List<string> TriggerActions { get; set; } = new();
    public Dictionary<string, object> ThresholdMetadata { get; set; } = new();
}

/// <summary>
/// Revenue impact threshold types
/// </summary>
public enum RevenueImpactThresholdType
{
    AbsoluteAmount = 1,
    PercentageDecrease = 2,
    RateOfChange = 3,
    TrendDeviation = 4,
    ComparativeLoss = 5
}

/// <summary>
/// Revenue impact severity levels
/// </summary>
public enum RevenueImpactSeverityLevel
{
    Minimal = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5,
    Catastrophic = 6
}

/// <summary>
/// Revenue impact metric
/// </summary>
public class RevenueImpactMetric
{
    public required string MetricId { get; set; }
    public required string MetricName { get; set; }
    public required RevenueImpactMetricType Type { get; set; }
    public required string Unit { get; set; }
    public required RevenueImpactMetricCalculation Calculation { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? WarningThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Revenue impact metric types
/// </summary>
public enum RevenueImpactMetricType
{
    RevenueAmount = 1,
    TransactionCount = 2,
    PercentageChange = 3,
    RateOfLoss = 4,
    CumulativeImpact = 5,
    PredictedLoss = 6
}

/// <summary>
/// Revenue impact metric calculation
/// </summary>
public enum RevenueImpactMetricCalculation
{
    Sum = 1,
    Average = 2,
    Maximum = 3,
    Minimum = 4,
    Count = 5,
    Rate = 6,
    Percentage = 7,
    Cumulative = 8
}

/// <summary>
/// Revenue impact alerting configuration
/// </summary>
public class RevenueImpactAlertingConfiguration
{
    public required bool IsEnabled { get; set; }
    public required List<RevenueImpactAlertChannel> AlertChannels { get; set; }
    public required Dictionary<RevenueImpactSeverityLevel, TimeSpan> AlertFrequencies { get; set; }
    public required List<string> AlertRecipients { get; set; }
    public List<RevenueImpactAlertRule> AlertRules { get; set; } = new();
    public Dictionary<string, object> AlertingParameters { get; set; } = new();

    public RevenueImpactAlertingConfiguration()
    {
        AlertChannels = new List<RevenueImpactAlertChannel>();
        AlertFrequencies = new Dictionary<RevenueImpactSeverityLevel, TimeSpan>();
        AlertRecipients = new List<string>();
        AlertRules = new List<RevenueImpactAlertRule>();
        AlertingParameters = new Dictionary<string, object>();
    }
}

/// <summary>
/// Revenue impact alert channels
/// </summary>
public enum RevenueImpactAlertChannel
{
    Email = 1,
    SMS = 2,
    Slack = 3,
    Teams = 4,
    Dashboard = 5,
    PagerDuty = 6,
    Webhook = 7
}

/// <summary>
/// Revenue impact alert rule
/// </summary>
public class RevenueImpactAlertRule
{
    public required string RuleId { get; set; }
    public required RevenueImpactAlertCondition Condition { get; set; }
    public required List<RevenueImpactAlertChannel> Channels { get; set; }
    public required RevenueImpactAlertPriority Priority { get; set; }
    public required string MessageTemplate { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> RuleParameters { get; set; } = new();
}

/// <summary>
/// Revenue impact alert conditions
/// </summary>
public enum RevenueImpactAlertCondition
{
    ThresholdExceeded = 1,
    TrendDetected = 2,
    AnomalyDetected = 3,
    SystemFailure = 4,
    RecoveryRequired = 5
}

/// <summary>
/// Revenue impact alert priority
/// </summary>
public enum RevenueImpactAlertPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4,
    Critical = 5
}

/// <summary>
/// Cultural event revenue monitoring rule
/// </summary>
public class CulturalEventRevenueMonitoringRule
{
    public required string RuleId { get; set; }
    public required CulturalEventType EventType { get; set; }
    public required RevenueImpactMonitoringBehavior Behavior { get; set; }
    public required Dictionary<string, decimal> EventSpecificThresholds { get; set; }
    public required TimeSpan PreEventMonitoringWindow { get; set; }
    public required TimeSpan PostEventMonitoringWindow { get; set; }
    public bool IsSeasonallyAdjusted { get; set; }
    public string? DiasporaRegion { get; set; }
    public Dictionary<string, object> CulturalContext { get; set; } = new();
}

/// <summary>
/// Revenue impact monitoring behavior
/// </summary>
public enum RevenueImpactMonitoringBehavior
{
    Continuous = 1,
    EventTriggered = 2,
    Seasonal = 3,
    OnDemand = 4,
    RiskBased = 5
}

/// <summary>
/// Revenue impact escalation rule
/// </summary>
public class RevenueImpactEscalationRule
{
    public required string RuleId { get; set; }
    public required RevenueImpactSeverityLevel TriggerSeverity { get; set; }
    public required TimeSpan EscalationDelay { get; set; }
    public required List<string> EscalationTargets { get; set; }
    public required RevenueImpactEscalationAction Action { get; set; }
    public bool RequiresManualApproval { get; set; }
    public Dictionary<string, object> ActionParameters { get; set; } = new();
}

/// <summary>
/// Revenue impact escalation actions
/// </summary>
public enum RevenueImpactEscalationAction
{
    Notify = 1,
    AutoRecover = 2,
    FailoverActivate = 3,
    EmergencyProtocol = 4,
    ManualIntervention = 5
}