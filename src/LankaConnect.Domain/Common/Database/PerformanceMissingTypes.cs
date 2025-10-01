using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class PerformanceOptimizationRecommendations
    {
        public Guid Id { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, double> ExpectedImprovements { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    public class ThroughputCapacityAnalysis
    {
        public Guid Id { get; set; }
        public Dictionary<string, double> CapacityMetrics { get; set; } = new();
        public List<string> BottleneckAreas { get; set; } = new();
        public double UtilizationPercentage { get; set; }
        public DateTime AnalysisTimestamp { get; set; }
    }

    public class DatabaseLoadForecastParameters
    {
        public Guid Id { get; set; }
        public TimeSpan ForecastWindow { get; set; }
        public List<string> ForecastVariables { get; set; } = new();
        public Dictionary<string, object> HistoricalData { get; set; } = new();
        public string ForecastModel { get; set; } = string.Empty;
    }

    public class LoadForecastResult
    {
        public Guid Id { get; set; }
        public Dictionary<string, double> ForecastedLoad { get; set; } = new();
        public double ConfidenceLevel { get; set; }
        public List<string> ForecastInsights { get; set; } = new();
        public DateTime ForecastTimestamp { get; set; }
    }

    public class DatabasePerformanceBenchmark
    {
        public Guid Id { get; set; }
        public string BenchmarkName { get; set; } = string.Empty;
        public Dictionary<string, double> BenchmarkScores { get; set; } = new();
        public List<string> PerformanceBaselines { get; set; } = new();
        public DateTime BenchmarkDate { get; set; }
    }

    public class PerformanceDeviationAnalysis
    {
        public Guid Id { get; set; }
        public Dictionary<string, double> DeviationMetrics { get; set; } = new();
        public List<string> DeviationCauses { get; set; } = new();
        public string DeviationSeverity { get; set; } = string.Empty;
        public DateTime AnalysisTimestamp { get; set; }
    }

    public class DatabaseMaintenanceWindow
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> ScheduledActivities { get; set; } = new();
        public string MaintenanceType { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
    }

    public class MaintenanceImpactAnalysis
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> ImpactMetrics { get; set; } = new();
        public List<string> AffectedSystems { get; set; } = new();
        public TimeSpan EstimatedDowntime { get; set; }
        public DateTime AnalysisTimestamp { get; set; }
    }

    public class DatabaseResourceUtilization
    {
        public Guid Id { get; set; }
        public Dictionary<string, double> ResourceMetrics { get; set; } = new();
        public List<string> ResourceBottlenecks { get; set; } = new();
        public double OverallUtilizationPercentage { get; set; }
        public DateTime UtilizationTimestamp { get; set; }
    }

    public class ResourceScalingRecommendations
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> ScalingActions { get; set; } = new();
        public List<string> ResourceOptimizations { get; set; } = new();
        public string ScalingPriority { get; set; } = string.Empty;
        public DateTime RecommendationDate { get; set; }
    }

    public class PerformanceMonitoringConfiguration
    {
        public Guid Id { get; set; }
        public List<string> MonitoredMetrics { get; set; } = new();
        public Dictionary<string, double> AlertThresholds { get; set; } = new();
        public TimeSpan MonitoringInterval { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class PerformanceMonitoringResults
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> MonitoringData { get; set; } = new();
        public List<string> TriggeredAlerts { get; set; } = new();
        public DateTime MonitoringTimestamp { get; set; }
    }

    public class DatabasePerformanceTuningPlan
    {
        public Guid Id { get; set; }
        public List<string> TuningActions { get; set; } = new();
        public Dictionary<string, object> TuningParameters { get; set; } = new();
        public string TuningPriority { get; set; } = string.Empty;
        public DateTime PlanCreationDate { get; set; }
    }

    public class TuningExecutionResult
    {
        public Guid Id { get; set; }
        public bool TuningSuccessful { get; set; }
        public Dictionary<string, double> PerformanceChanges { get; set; } = new();
        public List<string> ExecutedActions { get; set; } = new();
        public DateTime ExecutionTimestamp { get; set; }
    }

    public class DatabaseHealthCheckConfiguration
    {
        public Guid Id { get; set; }
        public List<string> HealthChecks { get; set; } = new();
        public Dictionary<string, object> CheckParameters { get; set; } = new();
        public TimeSpan CheckInterval { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class DatabaseHealthStatus
    {
        public Guid Id { get; set; }
        public Dictionary<string, bool> HealthResults { get; set; } = new();
        public string OverallHealthStatus { get; set; } = string.Empty;
        public List<string> HealthIssues { get; set; } = new();
        public DateTime HealthCheckTimestamp { get; set; }
    }
}