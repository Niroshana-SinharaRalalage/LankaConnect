using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Domain.Common;

public class DashboardSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan RefreshInterval { get; set; }
    public double OverallHealthScore { get; set; }
    public Dictionary<string, bool> ComponentHealthStatuses { get; set; } = new();
    public ApiPerformanceOverview ApiPerformanceOverview { get; set; } = null!;
    public DiasporaEngagementSummary DiasporaEngagementSummary { get; set; } = null!;
    public EnterpriseClientsSummary EnterpriseClientsSummary { get; set; } = null!;
    public RevenueImpactSummary RevenueImpactSummary { get; set; } = null!;
    public CulturalDataQualityOverview CulturalDataQualityOverview { get; set; } = null!;
    public long GenerationTimeMs { get; set; }
}

public class ApiPerformanceOverview
{
    public TimeSpan AverageResponseTime { get; set; }
    public int RequestsPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public Dictionary<CulturalIntelligenceEndpoint, TimeSpan> EndpointResponseTimes { get; set; } = new();
    public Dictionary<CulturalIntelligenceEndpoint, double> EndpointErrorRates { get; set; } = new();
}

public class DiasporaEngagementSummary
{
    public int ActiveCommunities { get; set; }
    public int TotalEngagedUsers { get; set; }
    public int DailyActiveUsers { get; set; }
    public double AverageEngagementScore { get; set; }
    public string[] TopEngagedCommunities { get; set; } = Array.Empty<string>();
    public Dictionary<string, int> CommunityUserCounts { get; set; } = new();
    public Dictionary<DiasporaEngagementType, int> EngagementByType { get; set; } = new();
}

public class EnterpriseClientsSummary
{
    public int TotalEnterpriseClients { get; set; }
    public int Fortune500Clients { get; set; }
    public double AverageSlaCompliance { get; set; }
    public long TotalApiCallsToday { get; set; }
    public decimal RevenueToday { get; set; }
    public Dictionary<EnterpriseContractTier, int> ClientsByTier { get; set; } = new();
    public List<SlaViolation> RecentSlaViolations { get; set; } = new();
}

public class RevenueImpactSummary
{
    public decimal DailyRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AverageRevenuePerUser { get; set; }
    public required CulturalIntelligenceEndpoint HighestValueEndpoint { get; set; }
    public double RevenueGrowthRate { get; set; }
    public Dictionary<SubscriptionTier, decimal> RevenueByTier { get; set; } = new();
    public Dictionary<ClientSegment, decimal> RevenueBySegment { get; set; } = new();
}

public class CulturalDataQualityOverview
{
    public double OverallDataQualityScore { get; set; }
    public double DataAccuracyScore { get; set; }
    public double DataCompletenessScore { get; set; }
    public double DataFreshnessScore { get; set; }
    public double CulturalAuthenticityScore { get; set; }
    public Dictionary<CulturalDataType, double> QualityByDataType { get; set; } = new();
    public List<DataQualityIssue> ActiveQualityIssues { get; set; } = new();
}

public class DiasporaAnalyticsDashboard
{
    public string CommunityId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public IEnumerable<DiasporaEngagementTrends> EngagementTrends { get; set; } = new List<DiasporaEngagementTrends>();
    public double CommunityGrowthRate { get; set; }
    public int CulturalContentEngagement { get; set; }
    public int CrossCulturalInteractions { get; set; }
    public Dictionary<string, int> GeographicDistribution { get; set; } = new();
    public List<PopularCulturalContent> PopularContent { get; set; } = new();
    public List<CommunityInsight> Insights { get; set; } = new();
}

public class RevenueAnalyticsDashboard
{
    public TimeSpan TimeRange { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalRevenue { get; set; }
    public Dictionary<CulturalIntelligenceEndpoint, decimal> RevenueByEndpoint { get; set; } = new();
    public Dictionary<ClientSegment, decimal> RevenueByClientSegment { get; set; } = new();
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal CustomerLifetimeValue { get; set; }
    public double RevenueGrowthRate { get; set; }
    public List<RevenueOpportunity> GrowthOpportunities { get; set; } = new();
    public List<RevenueRisk> RevenueRisks { get; set; } = new();
}

public class CulturalIntelligenceHealthDashboard
{
    public double OverallHealthScore { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, bool> ComponentHealthStatuses { get; set; } = new();
    public Dictionary<CulturalIntelligenceEndpoint, double> EndpointHealthScores { get; set; } = new();
    public List<SystemAlert> SystemAlerts { get; set; } = new();
    public List<PerformanceTrend> PerformanceTrends { get; set; } = new();
    public List<HealthRecommendation> HealthRecommendations { get; set; } = new();
    public Dictionary<string, HealthMetric> DetailedHealthMetrics { get; set; } = new();
}

// Supporting Models
public class SlaViolation
{
    public string ClientId { get; set; } = string.Empty;
    public required CulturalIntelligenceEndpoint Endpoint { get; set; }
    public TimeSpan ExpectedResponseTime { get; set; }
    public TimeSpan ActualResponseTime { get; set; }
    public DateTime ViolationTime { get; set; }
    public bool IsResolved { get; set; }
    public string Resolution { get; set; } = string.Empty;
}

public class DataQualityIssue
{
    public CulturalDataType DataType { get; set; }
    public string CommunityId { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
    public string ResolutionAction { get; set; } = string.Empty;
}

public class PopularCulturalContent
{
    public string ContentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public double EngagementScore { get; set; }
    public string CommunityId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CommunityInsight
{
    public string InsightId { get; set; } = string.Empty;
    public string CommunityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InsightType { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class RevenueOpportunity
{
    public string OpportunityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedValue { get; set; }
    public string ConfidenceLevel { get; set; } = string.Empty;
    public DateTime IdentifiedAt { get; set; }
    public List<string> ActionItems { get; set; } = new();
}

public class RevenueRisk
{
    public string RiskId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialLoss { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime IdentifiedAt { get; set; }
    public List<string> MitigationActions { get; set; } = new();
}

public class SystemAlert
{
    public string AlertId { get; set; } = string.Empty;
    public CulturalIntelligenceAlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Low;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool IsResolved { get; set; }
    public List<CulturalIntelligenceEndpoint> AffectedEndpoints { get; set; } = new();
}

public class PerformanceTrend
{
    public required CulturalIntelligenceEndpoint Endpoint { get; set; }
    public TimeSpan TimeRange { get; set; }
    public List<PerformanceDataPoint> DataPoints { get; set; } = new();
    public double TrendDirection { get; set; } // Positive = improving, Negative = degrading
    public string TrendAnalysis { get; set; } = string.Empty;
}

public class PerformanceDataPoint
{
    public DateTime Timestamp { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int RequestCount { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

// HealthRecommendation moved to LankaConnect.Domain.Common.Monitoring.HealthMonitoringTypes.HealthRecommendation to resolve CS0104 conflict

public class HealthMetric
{
    public string MetricName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public List<HealthMetricDataPoint> HistoricalData { get; set; } = new();
}

public class HealthMetricDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}