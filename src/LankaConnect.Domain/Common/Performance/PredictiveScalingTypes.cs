using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Performance
{
    /// <summary>
    /// Performance forecast data for predictive scaling decisions
    /// </summary>
    public class PerformanceForecast
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ForecastId { get; set; } = string.Empty;
        public DateTimeOffset ForecastTime { get; set; } = DateTimeOffset.UtcNow;
        public TimeSpan ForecastHorizon { get; set; }
        public Dictionary<string, double> PredictedMetrics { get; set; } = new();
        public double ConfidenceLevel { get; set; }
        public string PredictionModel { get; set; } = string.Empty;
        public List<string> InfluencingFactors { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Policy configuration for predictive scaling operations
    /// </summary>
    public class PredictiveScalingPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public TimeSpan LookAheadTime { get; set; }
        public double ScalingThreshold { get; set; }
        public int MinInstances { get; set; }
        public int MaxInstances { get; set; }
        public TimeSpan CooldownPeriod { get; set; }
        public Dictionary<string, object> PolicyParameters { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Coordination result for predictive scaling operations
    /// </summary>
    public class PredictiveScalingCoordination
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CoordinationId { get; set; } = string.Empty;
        public PerformanceForecast Forecast { get; set; } = new();
        public PredictiveScalingPolicy Policy { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public bool IsCoordinated { get; set; }
        public string CoordinationStatus { get; set; } = string.Empty;
        public Dictionary<string, object> CoordinationResults { get; set; } = new();
        public DateTimeOffset CoordinatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Metrics collected during scaling operations
    /// </summary>
    public class ScalingMetrics
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string MetricsId { get; set; } = string.Empty;
        public DateTimeOffset CollectionTime { get; set; } = DateTimeOffset.UtcNow;
        public int CurrentInstances { get; set; }
        public int TargetInstances { get; set; }
        public double CpuUtilization { get; set; }
        public double MemoryUtilization { get; set; }
        public double NetworkUtilization { get; set; }
        public double ResponseTime { get; set; }
        public long RequestsPerSecond { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
        public bool IsHealthy { get; set; } = true;
    }

    /// <summary>
    /// Threshold configuration for anomaly detection
    /// </summary>
    public class AnomalyDetectionThreshold
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ThresholdId { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public double UpperThreshold { get; set; }
        public double LowerThreshold { get; set; }
        public TimeSpan DetectionWindow { get; set; }
        public double SensitivityLevel { get; set; }
        public string DetectionAlgorithm { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Result of scaling anomaly detection analysis
    /// </summary>
    public class ScalingAnomalyDetectionResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResultId { get; set; } = string.Empty;
        public ScalingMetrics ScalingMetrics { get; set; } = new();
        public AnomalyDetectionThreshold Threshold { get; set; } = new();
        public List<string> DetectedAnomalies { get; set; } = new();
        public double AnomalyScore { get; set; }
        public bool HasAnomalies { get; set; }
        public string AnalysisDetails { get; set; } = string.Empty;
        public List<string> RecommendedActions { get; set; } = new();
        public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Record of a scaling event occurrence
    /// </summary>
    public class ScalingEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTimeOffset EventTime { get; set; } = DateTimeOffset.UtcNow;
        public int PreviousInstances { get; set; }
        public int NewInstances { get; set; }
        public string TriggerReason { get; set; } = string.Empty;
        public Dictionary<string, object> EventData { get; set; } = new();
        public bool IsSuccessful { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation result for scaling effectiveness
    /// </summary>
    public class ScalingEffectivenessValidation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ValidationId { get; set; } = string.Empty;
        public List<ScalingEvent> EvaluatedEvents { get; set; } = new();
        public double EffectivenessScore { get; set; }
        public bool IsEffective { get; set; }
        public List<string> ImprovementRecommendations { get; set; } = new();
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
        public string ValidationSummary { get; set; } = string.Empty;
        public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}