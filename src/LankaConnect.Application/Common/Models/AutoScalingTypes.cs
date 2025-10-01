using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models;

#region Scaling Request and Response Types

public class CulturalScalingRequest
{
    public string EventId { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public string Region { get; set; } = string.Empty;
    public DateTime RequestedScalingTime { get; set; }
    public int RequestedCapacity { get; set; }
    public CulturalScalingPriority Priority { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CulturalCalendarContext
{
    public string CalendarId { get; set; } = string.Empty;
    public CulturalEventType PrimaryEventType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> AffectedRegions { get; set; } = new();
    public Dictionary<string, object> CalendarMetadata { get; set; } = new();
}

public class OptimizationParameters
{
    public double PerformanceTarget { get; set; }
    public double CostConstraint { get; set; }
    public TimeSpan OptimizationWindow { get; set; }
    public List<string> OptimizationCriteria { get; set; } = new();
    public Dictionary<string, double> Weights { get; set; } = new();
}

public class OptimizationResult
{
    public bool IsSuccessful { get; set; }
    public double AchievedPerformance { get; set; }
    public double TotalCost { get; set; }
    public TimeSpan OptimizationDuration { get; set; }
    public List<string> OptimizationActions { get; set; } = new();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

public class EmergencyScalingRequest
{
    public string EmergencyId { get; set; } = string.Empty;
    public EmergencyScalingTrigger Trigger { get; set; }
    public double SeverityLevel { get; set; }
    public string Region { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public int RequiredCapacityIncrease { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
}

public class EmergencyScalingResult
{
    public bool IsSuccessful { get; set; }
    public string EmergencyId { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public int CapacityAdded { get; set; }
    public EmergencyScalingStatus Status { get; set; }
    public List<string> ActionsExecuted { get; set; } = new();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

public class MonitoringParameters
{
    public TimeSpan MonitoringInterval { get; set; }
    public List<string> MetricsToMonitor { get; set; } = new();
    public Dictionary<string, double> AlertThresholds { get; set; } = new();
    public bool EnableRealTimeMonitoring { get; set; }
    public string MonitoringScope { get; set; } = string.Empty;
}

public class CulturalImpactMetrics
{
    public double LoadIncrease { get; set; }
    public double PerformanceImpact { get; set; }
    public TimeSpan EventDuration { get; set; }
    public Dictionary<string, double> RegionalImpact { get; set; } = new();
    public List<string> AffectedServices { get; set; } = new();
    public Dictionary<string, object> DetailedMetrics { get; set; } = new();
}

public class ThresholdConfiguration
{
    public double CpuThreshold { get; set; }
    public double MemoryThreshold { get; set; }
    public double NetworkThreshold { get; set; }
    public double ResponseTimeThreshold { get; set; }
    public Dictionary<string, double> CustomThresholds { get; set; } = new();
}

public class ThresholdAdjustmentResult
{
    public bool IsSuccessful { get; set; }
    public Dictionary<string, double> PreviousThresholds { get; set; } = new();
    public Dictionary<string, double> NewThresholds { get; set; } = new();
    public string AdjustmentReason { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; }
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
}

#endregion

#region Database Security Types

public class CulturalHeritageData
{
    public string DataId { get; set; } = string.Empty;
    public CulturalDataType DataType { get; set; }
    public CulturalSignificance Significance { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string> AccessRestrictions { get; set; } = new();
    public Dictionary<string, object> SecurityMetadata { get; set; } = new();
}

public class PreservationSecurityConfig
{
    public string ConfigurationId { get; set; } = string.Empty;
    public CulturalDataType DataType { get; set; }
    public EncryptionLevel EncryptionLevel { get; set; }
    public List<string> AuthorizedRoles { get; set; } = new();
    public Dictionary<string, object> SecuritySettings { get; set; } = new();
}

public class HeritageDataSecurityResult
{
    public bool IsSuccessful { get; set; }
    public string DataId { get; set; } = string.Empty;
    public SecurityStatus SecurityStatus { get; set; }
    public List<string> AppliedSecurityMeasures { get; set; } = new();
    public Dictionary<string, object> SecurityMetrics { get; set; } = new();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

public class MonitoringConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public TimeSpan MonitoringInterval { get; set; }
    public List<string> MonitoredMetrics { get; set; } = new();
    public Dictionary<string, double> AlertThresholds { get; set; } = new();
    public bool EnableAutomaticResponse { get; set; }
}

public class IntegrationMetrics
{
    public double ThroughputRate { get; set; }
    public double ErrorRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public Dictionary<string, double> ComponentMetrics { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class IntegrationStatus
{
    public string IntegrationId { get; set; } = string.Empty;
    public IntegrationState State { get; set; }
    public double HealthScore { get; set; }
    public List<IntegrationComponent> Components { get; set; } = new();
    public DateTime LastHealthCheck { get; set; }
    public IEnumerable<string> Issues { get; set; } = new List<string>();
}

public class IntegrationComponent
{
    public string ComponentId { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public ComponentStatus Status { get; set; }
    public double HealthScore { get; set; }
    public DateTime LastCheck { get; set; }
    public Dictionary<string, object> ComponentMetrics { get; set; } = new();
}

#endregion

#region Supporting Enums

public enum CulturalScalingPriority
{
    Low,
    Normal,
    High,
    Critical,
    Emergency
}

public enum EmergencyScalingTrigger
{
    PerformanceDegradation,
    CapacityExceeded,
    CulturalEventSurge,
    SystemFailure,
    SecurityIncident
}

public enum EmergencyScalingStatus
{
    Triggered,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted
}

public enum EncryptionLevel
{
    None,
    Basic,
    Standard,
    Enhanced,
    Maximum
}

public enum SecurityStatus
{
    Secure,
    AtRisk,
    Compromised,
    Unknown
}

public enum IntegrationState
{
    Healthy,
    Warning,
    Critical,
    Failed,
    Maintenance
}

public enum ComponentStatus
{
    Online,
    Offline,
    Degraded,
    Failed,
    Maintenance
}

#endregion