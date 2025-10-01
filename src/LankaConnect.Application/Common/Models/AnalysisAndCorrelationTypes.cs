using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Application.Common.DTOs;

namespace LankaConnect.Application.Common.Models;

#region Correlation Analysis Types

public class CorrelationAnalysisRequest
{
    public string RequestId { get; set; } = string.Empty;
    public List<string> MetricNames { get; set; } = new();
    public TimeSpan AnalysisPeriod { get; set; }
    public CorrelationMethod Method { get; set; }
    public double MinimumCorrelationThreshold { get; set; }
    public Dictionary<string, object> AnalysisParameters { get; set; } = new();
}

public class CorrelationAnalysisResult
{
    public bool IsSuccessful { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public Dictionary<string, Dictionary<string, double>> CorrelationMatrix { get; set; } = new();
    public List<CorrelationInsight> Insights { get; set; } = new();
    public DateTime AnalysisCompletedAt { get; set; }
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
}

public class CorrelationInsight
{
    public string InsightId { get; set; } = string.Empty;
    public string Metric1 { get; set; } = string.Empty;
    public string Metric2 { get; set; } = string.Empty;
    public double CorrelationCoefficient { get; set; }
    public CorrelationStrength Strength { get; set; }
    public string Interpretation { get; set; } = string.Empty;
}

#endregion

#region Threshold Optimization Types

public class ThresholdOptimizationRequest
{
    public string RequestId { get; set; } = string.Empty;
    public List<string> MetricsToOptimize { get; set; } = new();
    public CulturalEventType EventType { get; set; }
    public OptimizationStrategy Strategy { get; set; }
    public Dictionary<string, double> CurrentThresholds { get; set; } = new();
    public TimeSpan OptimizationPeriod { get; set; }
}

public class ThresholdOptimizationResult
{
    public bool IsSuccessful { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public Dictionary<string, double> OptimizedThresholds { get; set; } = new();
    public Dictionary<string, double> ImprovementMetrics { get; set; } = new();
    public List<string> OptimizationReasons { get; set; } = new();
    public DateTime OptimizationCompletedAt { get; set; }
}

#endregion

#region Anomaly Detection Types

public class AnomalyDetectionConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<string> MonitoredMetrics { get; set; } = new();
    public AnomalyDetectionAlgorithm Algorithm { get; set; }
    public double SensitivityLevel { get; set; }
    public TimeSpan DetectionWindow { get; set; }
    public Dictionary<string, object> AlgorithmParameters { get; set; } = new();
}

public class AnomalyDetectionResult
{
    public bool IsSuccessful { get; set; }
    public List<DetectedAnomaly> DetectedAnomalies { get; set; } = new();
    public DateTime DetectionTime { get; set; }
    public Dictionary<string, double> BaselineMetrics { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class DetectedAnomaly
{
    public string AnomalyId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public double AnomalyScore { get; set; }
    public AnomalySeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> AnomalyDetails { get; set; } = new();
}

#endregion

#region SLA Compliance Types

public class SLAComplianceConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<string> RegionIds { get; set; } = new();
    public Dictionary<string, double> ComplianceTargets { get; set; } = new();
    public ComplianceCalculationMethod CalculationMethod { get; set; }
    public TimeSpan CompliancePeriod { get; set; }
    public bool EnableRealTimeMonitoring { get; set; }
}

public class SLAComplianceResult
{
    public bool IsCompliant { get; set; }
    public Dictionary<string, RegionalCompliance> RegionalResults { get; set; } = new();
    public double OverallCompliancePercentage { get; set; }
    public List<LegacyComplianceViolationDto> Violations { get; set; } = new();
    public DateTime ComplianceCheckTime { get; set; }
}

public class RegionalCompliance
{
    public string RegionId { get; set; } = string.Empty;
    public bool IsCompliant { get; set; }
    public double CompliancePercentage { get; set; }
    public Dictionary<string, double> MetricCompliance { get; set; } = new();
    public List<string> Issues { get; set; } = new();
}

// ComplianceViolation class removed - use LegacyComplianceViolationDto from DTOs namespace
// This enforces Clean Architecture boundaries

#endregion

#region SLA Monitoring and Reporting Types

public class SLAMonitoringParameters
{
    public string ParameterId { get; set; } = string.Empty;
    public List<string> MonitoredSLAs { get; set; } = new();
    public TimeSpan MonitoringInterval { get; set; }
    public Dictionary<string, double> AlertThresholds { get; set; } = new();
    public bool EnablePredictiveAlerts { get; set; }
    public List<string> NotificationChannels { get; set; } = new();
}

public class SLAMonitoringResult
{
    public bool IsSuccessful { get; set; }
    public Dictionary<string, SLAStatus> SLAStatuses { get; set; } = new();
    public List<SLAAlert> GeneratedAlerts { get; set; } = new();
    public DateTime MonitoringTime { get; set; }
    public Dictionary<string, object> MonitoringMetrics { get; set; } = new();
}

public class SLAStatus
{
    public string SLAId { get; set; } = string.Empty;
    public SLAHealthStatus Status { get; set; }
    public double CurrentCompliancePercentage { get; set; }
    public Dictionary<string, double> MetricStatuses { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class SLAAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string SLAId { get; set; } = string.Empty;
    public AlertType AlertType { get; set; }
    public required AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime AlertTime { get; set; }
    public Dictionary<string, object> AlertData { get; set; } = new();
}

public class CulturalSLAComplianceReport
{
    public string ReportId { get; set; } = string.Empty;
    public ReportingPeriod ReportPeriod { get; set; } = new();
    public Dictionary<string, CulturalSLAMetrics> CulturalSLAData { get; set; } = new();
    public List<CulturalComplianceInsight> CulturalInsights { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class CulturalSLAMetrics
{
    public CulturalEventType EventType { get; set; }
    public double EventSpecificCompliance { get; set; }
    public Dictionary<string, double> RegionalCompliance { get; set; } = new();
    public List<string> CulturalFactors { get; set; } = new();
    public Dictionary<string, object> DetailedMetrics { get; set; } = new();
}

public class CulturalComplianceInsight
{
    public string InsightId { get; set; } = string.Empty;
    public CulturalEventType RelatedEventType { get; set; }
    public string Insight { get; set; } = string.Empty;
    public double ImpactScore { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
    public DateTime DiscoveredAt { get; set; }
}

public class CulturalSLAReportConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<CulturalEventType> IncludedEventTypes { get; set; } = new();
    public List<string> IncludedRegions { get; set; } = new();
    public ReportDetailLevel DetailLevel { get; set; }
    public bool IncludeCulturalAnalysis { get; set; } = true;
    public Dictionary<string, object> CustomReportSettings { get; set; } = new();
}

#endregion

#region Supporting Enums

public enum CorrelationMethod
{
    Pearson,
    Spearman,
    Kendall,
    DistanceCorrelation,
    Custom
}

public enum CorrelationStrength
{
    None,
    Weak,
    Moderate,
    Strong,
    VeryStrong
}

public enum OptimizationStrategy
{
    MinimizeFalsePositives,
    MinimizeFalseNegatives,
    BalanceAccuracy,
    MaximizeSensitivity,
    MaximizeSpecificity
}

public enum AnomalyDetectionAlgorithm
{
    StatisticalOutlier,
    IsolationForest,
    LocalOutlierFactor,
    MachineLearning,
    EnsembleMethod
}

public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ComplianceCalculationMethod
{
    Average,
    WeightedAverage,
    WorstCase,
    BestCase,
    Percentile
}

public enum ViolationSeverity
{
    Minor,
    Major,
    Critical,
    Severe
}

public enum SLAHealthStatus
{
    Healthy,
    Warning,
    Critical,
    Breached,
    Unknown
}

public enum AlertType
{
    ComplianceBreach,
    PredictiveWarning,
    ThresholdExceeded,
    ServiceDegradation,
    SystemError
}

public enum ReportDetailLevel
{
    Summary,
    Standard,
    Detailed,
    Comprehensive
}

#endregion