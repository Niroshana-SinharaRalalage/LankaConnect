using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class ErrorNotificationConfiguration
    {
        public Guid Id { get; set; }
        public List<string> NotificationChannels { get; set; } = new();
        public Dictionary<string, string> NotificationRecipients { get; set; } = new();
        public string NotificationThreshold { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public class ErrorNotificationResult
    {
        public Guid Id { get; set; }
        public bool NotificationSent { get; set; }
        public List<string> NotificationsSent { get; set; } = new();
        public string NotificationStatus { get; set; } = string.Empty;
        public DateTime NotificationTimestamp { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class DiagnosticsConfiguration
    {
        public Guid Id { get; set; }
        public List<string> DiagnosticChecks { get; set; } = new();
        public TimeSpan DiagnosticInterval { get; set; }
        public string DiagnosticLevel { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public class SystemHealthDiagnostics
    {
        public Guid Id { get; set; }
        public Dictionary<string, bool> HealthChecks { get; set; } = new();
        public string OverallHealth { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
        public DateTime DiagnosticsTimestamp { get; set; }
    }

    public class SystemReadinessConfiguration
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> ReadinessChecks { get; set; } = new();
        public List<string> RequiredServices { get; set; } = new();
        public TimeSpan ReadinessTimeout { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class SystemReadinessResult
    {
        public Guid Id { get; set; }
        public bool IsReady { get; set; }
        public Dictionary<string, bool> ServiceReadiness { get; set; } = new();
        public List<string> BlockingIssues { get; set; } = new();
        public DateTime ReadinessCheckTimestamp { get; set; }
    }

    public class PredictiveMaintenanceConfiguration
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> MaintenanceRules { get; set; } = new();
        public List<string> MonitoredComponents { get; set; } = new();
        public TimeSpan PredictionHorizon { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class PredictiveMaintenanceResult
    {
        public Guid Id { get; set; }
        public List<string> MaintenanceRecommendations { get; set; } = new();
        public Dictionary<string, DateTime> PredictedMaintenanceDates { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
        public DateTime PredictionTimestamp { get; set; }
    }

    public class PerformanceInsightsReport
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> InsightsData { get; set; } = new();
        public List<string> KeyFindings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public DateTime ReportTimestamp { get; set; }
    }

    public class TrendAnalysisParameters
    {
        public Guid Id { get; set; }
        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public List<string> TrendMetrics { get; set; } = new();
        public string AnalysisGranularity { get; set; } = string.Empty;
    }

    public class PerformanceTrendAnalysis
    {
        public Guid Id { get; set; }
        public Dictionary<string, List<object>> TrendData { get; set; } = new();
        public List<string> TrendInsights { get; set; } = new();
        public string TrendDirection { get; set; } = string.Empty;
        public DateTime AnalysisTimestamp { get; set; }
    }

    public class BenchmarkConfiguration
    {
        public Guid Id { get; set; }
        public List<string> BenchmarkTests { get; set; } = new();
        public Dictionary<string, object> BenchmarkParameters { get; set; } = new();
        public TimeSpan BenchmarkDuration { get; set; }
        public string BenchmarkType { get; set; } = string.Empty;
    }

    public class PerformanceBenchmarkReport
    {
        public Guid Id { get; set; }
        public Dictionary<string, double> BenchmarkResults { get; set; } = new();
        public string BenchmarkSummary { get; set; } = string.Empty;
        public List<string> PerformanceBaselines { get; set; } = new();
        public DateTime BenchmarkTimestamp { get; set; }
    }

    public class CapacityPlanningHorizon
    {
        public Guid Id { get; set; }
        public TimeSpan PlanningHorizon { get; set; }
        public string PlanningScope { get; set; } = string.Empty;
        public Dictionary<string, object> PlanningParameters { get; set; } = new();
    }

    public class GrowthProjectionModel
    {
        public Guid Id { get; set; }
        public string ModelType { get; set; } = string.Empty;
        public Dictionary<string, double> GrowthRates { get; set; } = new();
        public List<string> GrowthFactors { get; set; } = new();
        public double ConfidenceLevel { get; set; }
    }

    public class CapacityPlanningReport
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> CapacityProjections { get; set; } = new();
        public List<string> CapacityRecommendations { get; set; } = new();
        public DateTime PlanningTimestamp { get; set; }
    }

    public class DeploymentPerformanceImpact
    {
        public Guid Id { get; set; }
        public string DeploymentId { get; set; } = string.Empty;
        public Dictionary<string, double> ImpactMetrics { get; set; } = new();
        public List<string> PerformanceChanges { get; set; } = new();
        public DateTime DeploymentTimestamp { get; set; }
    }

    public class OptimizationScope
    {
        public Guid Id { get; set; }
        public List<string> ScopeAreas { get; set; } = new();
        public Dictionary<string, object> ScopeParameters { get; set; } = new();
        public string OptimizationLevel { get; set; } = string.Empty;
    }

    public class DatabasePerformanceObjective
    {
        public Guid Id { get; set; }
        public string ObjectiveName { get; set; } = string.Empty;
        public Dictionary<string, double> TargetMetrics { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
        public DateTime TargetDate { get; set; }
    }
}