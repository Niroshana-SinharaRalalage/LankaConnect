using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Common.Monitoring;

public class CulturalApiPerformanceMetrics
{
    public required CulturalIntelligenceEndpoint Endpoint { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int RequestSize { get; set; }
    public int ResponseSize { get; set; }
    public CulturalContext CulturalContext { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, object> CustomDimensions { get; set; } = new();
}

public class CulturalApiAccuracyMetrics
{
    public required CulturalIntelligenceEndpoint Endpoint { get; set; }
    public double AccuracyScore { get; set; }
    public double ConfidenceScore { get; set; }
    public double CulturalRelevanceScore { get; set; }
    public string CommunityId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ModelVersion { get; set; } = string.Empty;
    public Dictionary<string, double> ComponentScores { get; set; } = new();
}

public class ApiResponseTimeMetrics
{
    public required CulturalIntelligenceEndpoint Endpoint { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public CulturalContext CulturalContext { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSlaBreach { get; set; }
    public TimeSpan SlaTarget { get; set; }
    public Dictionary<string, string> AdditionalContext { get; set; } = new();
}

public class DiasporaEngagementMetrics
{
    public string CommunityId { get; set; } = string.Empty;
    public DiasporaEngagementType EngagementType { get; set; }
    public string GeographicRegion { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public double EngagementScore { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan MeasurementPeriod { get; set; }
    public Dictionary<string, object> EngagementDetails { get; set; } = new();
}

public class EnterpriseClientSlaMetrics
{
    public string ClientId { get; set; } = string.Empty;
    public EnterpriseContractTier ContractTier { get; set; }
    public TimeSpan SlaTarget { get; set; }
    public TimeSpan ActualResponseTime { get; set; }
    public bool SlaCompliance { get; set; }
    public TimeSpan ResponseTimeVariance { get; set; }
    public CulturalFeatureUsageMetrics CulturalFeatureUsage { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public decimal ContractValue { get; set; }
    public Dictionary<string, object> ClientSpecificMetrics { get; set; } = new();
}

public class CulturalFeatureUsageMetrics
{
    public int CulturalCalendarRequests { get; set; }
    public int DiasporaAnalyticsRequests { get; set; }
    public int CulturalAppropriatenessRequests { get; set; }
    public int EventRecommendationRequests { get; set; }
    public int CrossCulturalBridgeRequests { get; set; }
    public int CommunityEngagementRequests { get; set; }
    public int CulturalContentRequests { get; set; }
    public Dictionary<string, int> CustomFeatureUsage { get; set; } = new();
}

public class RevenueImpactMetrics
{
    public required CulturalIntelligenceEndpoint ApiEndpoint { get; set; }
    public decimal RevenuePerRequest { get; set; }
    public int RequestCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; }
    public ClientSegment ClientSegment { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan ReportingPeriod { get; set; }
    public Dictionary<string, decimal> RevenueBreakdown { get; set; } = new();
}

public class CulturalIntelligenceHealthStatus
{
    public double OverallHealthScore { get; set; }
    public Dictionary<string, bool> ComponentHealthStatuses { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<HealthCheckResult> DetailedHealthChecks { get; set; } = new();
    public Dictionary<CulturalIntelligenceEndpoint, double> EndpointHealthScores { get; set; } = new();
    public string Status { get; set; } = "Unknown";
    public List<string> HealthWarnings { get; set; } = new();
}

public class HealthCheckResult
{
    public string Component { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

public class CulturalDataQualityMetrics
{
    public CulturalDataType DataType { get; set; }
    public double AccuracyScore { get; set; }
    public double CompletenessScore { get; set; }
    public double FreshnessScore { get; set; }
    public double CulturalAuthenticityScore { get; set; }
    public string CommunityId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime LastDataUpdate { get; set; }
    public int DataPointCount { get; set; }
    public Dictionary<string, double> QualityDimensions { get; set; } = new();
}

public class CulturalContextPerformanceMetrics
{
    public TimeSpan ContextResolutionTime { get; set; }
    public double CulturalRelevanceScore { get; set; }
    public double LanguageLocalizationScore { get; set; }
    public double RegionalAdaptationScore { get; set; }
    public Dictionary<string, double> PerformanceDimensions { get; set; } = new();
}

public class CulturalContextPerformanceRecord
{
    public CulturalContext CulturalContext { get; set; } = null!;
    public CulturalContextPerformanceMetrics PerformanceMetrics { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

public class CulturalIntelligenceAlert
{
    public CulturalIntelligenceAlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Low;
    public string Description { get; set; } = string.Empty;
    public List<CulturalIntelligenceEndpoint> AffectedEndpoints { get; set; } = new();
    public List<string> ImpactedCommunities { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Dictionary<string, object> AlertDetails { get; set; } = new();
}


public class DiasporaEngagementTrends
{
    public string CommunityId { get; set; } = string.Empty;
    public TimeSpan TimeRange { get; set; }
    public List<DiasporaEngagementMetrics> EngagementHistory { get; set; } = new();
    public double TrendDirection { get; set; } // Positive = increasing, Negative = decreasing
    public Dictionary<DiasporaEngagementType, double> EngagementTypeTrends { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class EnterpriseClientDashboard
{
    public string ClientId { get; set; } = string.Empty;
    public EnterpriseContractTier ContractTier { get; set; }
    public List<EnterpriseClientSlaMetrics> SlaPerformanceHistory { get; set; } = new();
    public CulturalFeatureUsageMetrics OverallFeatureUsage { get; set; } = null!;
    public decimal TotalContractValue { get; set; }
    public double OverallSatisfactionScore { get; set; }
    public List<CulturalIntelligenceAlert> RecentAlerts { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> ClientSpecificKpis { get; set; } = new();
}