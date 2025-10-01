using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models;

#region Reporting Types

public class ReportingPeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportingInterval Interval { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public bool IncludeWeekends { get; set; } = true;
}

public class ReportConfiguration
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public List<string> IncludedMetrics { get; set; } = new();
    public List<string> ExcludedMetrics { get; set; } = new();
    public ReportFormat Format { get; set; }
    public bool IncludeCharts { get; set; }
    public Dictionary<string, object> CustomParameters { get; set; } = new();
}

public class CulturalPerformanceReport
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReportingPeriod Period { get; set; } = new();
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    public List<CulturalInsight> CulturalInsights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> RawData { get; set; } = new();
}

public class CulturalInsight
{
    public string InsightId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<string> SupportingData { get; set; } = new();
    public DateTime DiscoveredAt { get; set; }
}

#endregion

#region Performance and Response Types

public class PerformanceDegradationEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DegradationType DegradationType { get; set; }
    public double SeverityLevel { get; set; }
    public string AffectedComponent { get; set; } = string.Empty;
    public Dictionary<string, double> MetricValues { get; set; } = new();
    public List<string> Symptoms { get; set; } = new();
}

public class ResponseConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<ResponseAction> AutomatedActions { get; set; } = new();
    public Dictionary<string, double> ActionThresholds { get; set; } = new();
    public TimeSpan MaxResponseTime { get; set; }
    public bool RequireManualApproval { get; set; }
    public List<string> NotificationChannels { get; set; } = new();
}

public class ResponseAction
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> ActionParameters { get; set; } = new();
    public int Priority { get; set; }
    public TimeSpan EstimatedExecutionTime { get; set; }
}

public class AutomatedResponseResult
{
    public bool IsSuccessful { get; set; }
    public string ResponseId { get; set; } = string.Empty;
    public List<string> ExecutedActions { get; set; } = new();
    public TimeSpan TotalExecutionTime { get; set; }
    public Dictionary<string, object> ActionResults { get; set; } = new();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

#endregion

#region SLA and Validation Types

public class PerformanceMetrics
{
    public double AverageResponseTime { get; set; }
    public double ThroughputRate { get; set; }
    public double ErrorRate { get; set; }
    public double AvailabilityPercentage { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
    public DateTime MeasurementTime { get; set; }
}

public class SLAConfiguration
{
    public string SLAId { get; set; } = string.Empty;
    public double RequiredAvailability { get; set; }
    public double MaxResponseTime { get; set; }
    public double MaxErrorRate { get; set; }
    public double MinThroughput { get; set; }
    public Dictionary<string, double> CustomSLATargets { get; set; } = new();
}

public class SLAValidationResult
{
    public bool IsCompliant { get; set; }
    public string SLAId { get; set; } = string.Empty;
    public Dictionary<string, SLAMetricResult> MetricResults { get; set; } = new();
    public double OverallComplianceScore { get; set; }
    public List<string> Violations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class SLAMetricResult
{
    public string MetricName { get; set; } = string.Empty;
    public double ActualValue { get; set; }
    public double TargetValue { get; set; }
    public bool IsCompliant { get; set; }
    public double CompliancePercentage { get; set; }
}

#endregion

#region Scaling and Pool Types

public class PoolScalingParameters
{
    public int TargetSize { get; set; }
    public ScalingDirection Direction { get; set; }
    public TimeSpan ScalingWindow { get; set; }
    public double UtilizationTarget { get; set; }
    public Dictionary<string, object> ScalingConstraints { get; set; } = new();
}

public class PoolScalingResult
{
    public bool IsSuccessful { get; set; }
    public string PoolId { get; set; } = string.Empty;
    public int PreviousSize { get; set; }
    public int NewSize { get; set; }
    public ScalingDirection Direction { get; set; }
    public TimeSpan ScalingDuration { get; set; }
    public List<string> ScalingActions { get; set; } = new();
}

public class LifecycleConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public LifecycleStage InitialStage { get; set; }
    public Dictionary<LifecycleStage, LifecycleAction> StageActions { get; set; } = new();
    public TimeSpan MaxStageTransitionTime { get; set; }
    public bool AutomaticTransitions { get; set; }
}

public class LifecycleAction
{
    public string ActionName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int RetryCount { get; set; }
    public TimeSpan Timeout { get; set; }
}

public class LifecycleManagementResult
{
    public bool IsSuccessful { get; set; }
    public LifecycleStage CurrentStage { get; set; }
    public LifecycleStage TargetStage { get; set; }
    public List<string> ExecutedActions { get; set; } = new();
    public TimeSpan TransitionTime { get; set; }
    public Dictionary<string, object> StageMetrics { get; set; } = new();
}

public class CulturalRequirements
{
    public List<CulturalEventType> SupportedEventTypes { get; set; } = new();
    public List<string> RequiredLanguages { get; set; } = new();
    public Dictionary<string, object> CulturalConstraints { get; set; } = new();
    public CulturalSignificance MinimumSignificance { get; set; }
    public bool RequireCulturalValidation { get; set; }
}

#endregion

#region Predictive Monitoring Types

public class PredictiveMonitoringConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public TimeSpan PredictionWindow { get; set; }
    public List<string> PredictionModels { get; set; } = new();
    public Dictionary<string, double> ModelWeights { get; set; } = new();
    public double ConfidenceThreshold { get; set; }
    public bool EnableRealTimePrediction { get; set; }
}

public class PredictiveMonitoringResult
{
    public bool IsSuccessful { get; set; }
    public List<PerformancePrediction> Predictions { get; set; } = new();
    public Dictionary<string, double> ModelAccuracy { get; set; } = new();
    public List<string> PredictedIssues { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}

public class PerformancePrediction
{
    public string PredictionId { get; set; } = string.Empty;
    public DateTime PredictionTime { get; set; }
    public DateTime PredictedEventTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Dictionary<string, double> PredictedMetrics { get; set; } = new();
}

#endregion

#region Supporting Enums

public enum ReportingInterval
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Yearly,
    Custom
}

public enum ReportFormat
{
    Json,
    Xml,
    Csv,
    Pdf,
    Html,
    Excel
}

public enum DegradationType
{
    ResponseTimeIncrease,
    ThroughputDecrease,
    ErrorRateIncrease,
    AvailabilityDrop,
    ResourceExhaustion
}

public enum ScalingDirection
{
    Up,
    Down,
    Maintain
}

public enum LifecycleStage
{
    Initializing,
    Active,
    Scaling,
    Maintenance,
    Degraded,
    Shutdown
}

#endregion