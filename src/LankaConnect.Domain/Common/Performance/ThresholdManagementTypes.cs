using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Performance
{
    /// <summary>
    /// Configuration for dynamic threshold adjustment
    /// </summary>
    public class DynamicThresholdConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ConfigurationId { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public bool IsDynamic { get; set; } = true;
        public double BaselineValue { get; set; }
        public double VariabilityFactor { get; set; }
        public TimeSpan AdjustmentInterval { get; set; }
        public double MinThreshold { get; set; }
        public double MaxThreshold { get; set; }
        public string AdjustmentAlgorithm { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Result of threshold validation process
    /// </summary>
    public class ThresholdValidationResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ValidationId { get; set; } = string.Empty;
        public List<string> ValidatedThresholds { get; set; } = new();
        public Dictionary<string, double> ValidationScores { get; set; } = new();
        public bool IsValid { get; set; }
        public List<string> ValidationIssues { get; set; } = new();
        public List<string> RecommendedAdjustments { get; set; } = new();
        public string ValidationSummary { get; set; } = string.Empty;
        public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Objective for threshold optimization
    /// </summary>
    public class ThresholdOptimizationObjective
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ObjectiveId { get; set; } = string.Empty;
        public string ObjectiveName { get; set; } = string.Empty;
        public string OptimizationGoal { get; set; } = string.Empty;
        public Dictionary<string, double> TargetMetrics { get; set; } = new();
        public List<string> OptimizationConstraints { get; set; } = new();
        public double Priority { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Recommendation for threshold optimization
    /// </summary>
    public class ThresholdRecommendation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RecommendationId { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public double CurrentThreshold { get; set; }
        public double RecommendedThreshold { get; set; }
        public string RecommendationReason { get; set; } = string.Empty;
        public double ExpectedImprovement { get; set; }
        public double ConfidenceLevel { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public string ImplementationGuidance { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}