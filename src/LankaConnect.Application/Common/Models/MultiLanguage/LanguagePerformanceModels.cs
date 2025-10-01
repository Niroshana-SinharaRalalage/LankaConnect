using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.MultiLanguage
{
    public class LanguageRoutingPerformanceMetrics : BaseEntity
    {
        public string MetricId { get; set; } = string.Empty;
        public string LanguagePair { get; set; } = string.Empty;
        public TimeSpan AverageResponseTime { get; set; }
        public decimal ThroughputPerHour { get; set; }
        public decimal SuccessRate { get; set; }
        public List<string> PerformanceBottlenecks { get; set; } = new();
        public decimal ResourceUtilization { get; set; }
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Active";
    }

    public class LanguageRoutingAnalyticsRequest : BaseEntity
    {
        public string RequestId { get; set; } = string.Empty;
        public List<string> LanguagePairs { get; set; } = new();
        public DateTime AnalysisPeriodStart { get; set; }
        public DateTime AnalysisPeriodEnd { get; set; }
        public List<string> MetricsRequested { get; set; } = new();
        public string ReportFormat { get; set; } = string.Empty;
        public bool IncludeTrends { get; set; } = true;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string RequestedBy { get; set; } = string.Empty;
    }

    public class LanguageRoutingAnalytics : BaseEntity
    {
        public string AnalyticsId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
        public List<string> TrendAnalysis { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Completed";
    }

    public class CulturalEventPerformanceBenchmark : BaseEntity
    {
        public string BenchmarkId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public decimal BaselinePerformance { get; set; }
        public decimal PeakPerformance { get; set; }
        public List<string> PerformanceFactors { get; set; } = new();
        public TimeSpan OptimalResponseTime { get; set; }
        public decimal ResourceRequirements { get; set; }
        public DateTime BenchmarkedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class CacheOptimizationRequest : BaseEntity
    {
        public string RequestId { get; set; } = string.Empty;
        public string CacheType { get; set; } = string.Empty;
        public List<string> TargetLanguages { get; set; } = new();
        public string OptimizationGoal { get; set; } = string.Empty;
        public decimal CurrentHitRate { get; set; }
        public decimal TargetHitRate { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string RequestedBy { get; set; } = string.Empty;
    }

    public class CacheOptimizationResult : BaseEntity
    {
        public string RequestId { get; set; } = string.Empty;
        public string OptimizationStrategy { get; set; } = string.Empty;
        public decimal AchievedHitRate { get; set; }
        public decimal PerformanceImprovement { get; set; }
        public List<string> OptimizationActions { get; set; } = new();
        public TimeSpan ImplementationTime { get; set; }
        public decimal ResourceSavings { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Implemented";
    }


}