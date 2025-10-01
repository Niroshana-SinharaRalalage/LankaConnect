using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Common;

public enum DiasporaRoutingStrategy
{
    CulturalAffinity,
    GeographicProximity,
    Balanced,
    CrossCulturalDiscovery,
    PerformanceOptimized
}

public enum CulturalCommunityType
{
    SriLankanBuddhist,
    IndianHindu,
    PakistaniMuslim,
    BangladeshiMuslim,
    SikhPunjabi,
    TamilHindu,
    GujaratiJain,
    NepaleseBuddhist,
    MaldivianMuslim,
    BhutaneseBuddhist
}

public enum LoadBalancingHealthStatus
{
    Healthy,
    Degraded,
    Overloaded,
    Failed,
    Maintenance
}

public enum CulturalLanguage
{
    Sinhala,
    Tamil,
    Hindi,
    Urdu,
    Punjabi,
    Bengali,
    Gujarati,
    Marathi,
    Telugu,
    Malayalam,
    Kannada
}

public class DiasporaLoadBalancingRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string SourceRegion { get; set; } = string.Empty;
    public CulturalContext? CulturalContext { get; set; }
    public DiasporaRoutingStrategy RoutingStrategy { get; set; } = DiasporaRoutingStrategy.CulturalAffinity;
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromMilliseconds(200);
    public List<string> RequiredServices { get; set; } = new();
    public List<CulturalLanguage> PreferredLanguages { get; set; } = new();
    public CulturalEventLoadContext? CulturalEventContext { get; set; }
}

public class CulturalAffinityScore
{
    public string ScoreId { get; set; } = Guid.NewGuid().ToString();
    public CulturalCommunityType SourceCommunity { get; set; } = CulturalCommunityType.SriLankanBuddhist;
    public CulturalCommunityType TargetCommunity { get; set; } = CulturalCommunityType.SriLankanBuddhist;
    public double AffinityScore { get; set; }
    public double ReligiousAffinityScore { get; set; }
    public double LanguageAffinityScore { get; set; }
    public double CulturalEventAffinityScore { get; set; }
    public double GeographicProximityScore { get; set; }
    public DateTime CalculationTimestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, double> DetailedAffinityMetrics { get; set; } = new();
}

public class DiasporaLoadBalancingResult
{
    public string ResultId { get; set; } = Guid.NewGuid().ToString();
    public string OptimalRegion { get; set; } = string.Empty;
    public string RoutingReason { get; set; } = string.Empty;
    public double CulturalAffinityScore { get; set; }
    public double GeographicProximityScore { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public List<string> AlternativeRegions { get; set; } = new();
    public Dictionary<string, object> LoadBalancingMetrics { get; set; } = new();
    public DateTime RoutingTimestamp { get; set; } = DateTime.UtcNow;
}

public class GeographicCulturalRegion
{
    public string RegionId { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public GeographicCoordinates GeographicCoordinates { get; set; } = new();
    public List<CulturalCommunityType> DominantCommunities { get; set; } = new();
    public int CommunityDensity { get; set; }
    public int BusinessDirectoryCount { get; set; }
    public List<string> CulturalInstitutions { get; set; } = new();
    public Dictionary<CulturalLanguage, double> LanguageDistribution { get; set; } = new();
    public double CulturalDiversityIndex { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class GeographicCoordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double Accuracy { get; set; }
}

public class DiasporaLoadBalancingHealth
{
    public string HealthId { get; set; } = Guid.NewGuid().ToString();
    public LoadBalancingHealthStatus OverallHealthStatus { get; set; } = LoadBalancingHealthStatus.Healthy;
    public Dictionary<string, LoadBalancingHealthStatus> RegionalHealthStatuses { get; set; } = new();
    public TimeSpan AvgResponseTime { get; set; }
    public double CulturalAffinityAccuracy { get; set; }
    public DateTime HealthCheckTimestamp { get; set; } = DateTime.UtcNow;
    public List<CulturalEventType> ActiveCulturalEvents { get; set; } = new();
    public double LoadBalancingEfficiency { get; set; }
    public List<string> PerformanceIssues { get; set; } = new();
}

public class CulturalEventLoadContext
{
    public string ContextId { get; set; } = Guid.NewGuid().ToString();
    public CulturalEventType EventType { get; set; }
    public DateTime EventDate { get; set; }
    public int ExpectedAttendance { get; set; }
    public CulturalSignificance CulturalSignificance { get; set; } = CulturalSignificance.Medium;
    public List<CulturalCommunityType> AffectedCommunities { get; set; } = new();
    public double TrafficMultiplier { get; set; } = 1.0;
    public List<string> GeographicRegions { get; set; } = new();
    public Dictionary<string, object> EventMetadata { get; set; } = new();
}

public class DiasporaLoadBalancingMetrics
{
    public string MetricsId { get; set; } = Guid.NewGuid().ToString();
    public int TotalRoutingDecisions { get; set; }
    public int SuccessfulRoutings { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public double CulturalAffinityAccuracy { get; set; }
    public double CrossCulturalDiscoveryRate { get; set; }
    public double CulturalEventOptimizationSuccess { get; set; }
    public DateTime MetricsCollectionTimestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, TimeSpan> RegionalPerformanceMetrics { get; set; } = new();
}

public class DiasporaLoadBalancingOptions
{
    public bool EnableCulturalAffinityRouting { get; set; } = true;
    public double GeographicProximityWeight { get; set; } = 0.30;
    public double CulturalAffinityWeight { get; set; } = 0.70;
    public double LanguagePreferenceWeight { get; set; } = 0.15;
    public double ReligiousObservanceWeight { get; set; } = 0.25;
    public double CulturalEventParticipationWeight { get; set; } = 0.20;
    public double CrossCulturalDiscoveryWeight { get; set; } = 0.10;
    public int MaxResponseTimeMs { get; set; } = 200;
    public int CacheExpirationMinutes { get; set; } = 15;
    public bool EnableLoadBalancingHealthChecks { get; set; } = true;
    public DiasporaRoutingStrategy DefaultRoutingStrategy { get; set; } = DiasporaRoutingStrategy.CulturalAffinity;
    public Dictionary<string, string> RegionalEndpoints { get; set; } = new();
}

public class CulturalCommunityClusterAnalysis
{
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
    public string GeographicRegion { get; set; } = string.Empty;
    public CulturalCommunityType PrimaryCommunityType { get; set; }
    public int CommunityPopulation { get; set; }
    public double CommunityDensity { get; set; }
    public List<string> CulturalInstitutions { get; set; } = new();
    public Dictionary<CulturalLanguage, int> LanguageSpeakers { get; set; } = new();
    public List<CulturalEventType> PopularCulturalEvents { get; set; } = new();
    public double BusinessDirectoryDensity { get; set; }
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
}

public class CrossCulturalDiscoveryRecommendation
{
    public string RecommendationId { get; set; } = Guid.NewGuid().ToString();
    public CulturalCommunityType SourceCommunity { get; set; }
    public CulturalCommunityType RecommendedCommunity { get; set; }
    public double SimilarityScore { get; set; }
    public List<string> SharedCulturalElements { get; set; } = new();
    public List<CulturalEventType> CrossCulturalEvents { get; set; } = new();
    public string RecommendationReason { get; set; } = string.Empty;
    public double ExpectedEngagementIncrease { get; set; }
    public DateTime RecommendationTimestamp { get; set; } = DateTime.UtcNow;
}

public class DiasporaLoadBalancingConfiguration
{
    public string ConfigurationId { get; set; } = Guid.NewGuid().ToString();
    public DiasporaRoutingStrategy DefaultStrategy { get; set; } = DiasporaRoutingStrategy.CulturalAffinity;
    public Dictionary<CulturalCommunityType, double> CommunityPriorityWeights { get; set; } = new();
    public Dictionary<string, GeographicCulturalRegion> CulturalRegions { get; set; } = new();
    public TimeSpan MaxAllowedResponseTime { get; set; } = TimeSpan.FromMilliseconds(200);
    public bool EnableCrossCulturalRecommendations { get; set; } = true;
    public Dictionary<CulturalEventType, double> EventTrafficMultipliers { get; set; } = new();
    public DateTime ConfigurationLastUpdated { get; set; } = DateTime.UtcNow;
}

public class CulturalEventLoadOptimizationResult
{
    public string OptimizationId { get; set; } = Guid.NewGuid().ToString();
    public CulturalEventType EventType { get; set; }
    public List<string> OptimizedRegions { get; set; } = new();
    public Dictionary<string, double> RegionalLoadDistribution { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public TimeSpan EstimatedOptimizationTime { get; set; }
    public double ExpectedPerformanceImprovement { get; set; }
    public DateTime OptimizationTimestamp { get; set; } = DateTime.UtcNow;
}

public class DiasporaRoutingDecision
{
    public string DecisionId { get; set; } = Guid.NewGuid().ToString();
    public string SelectedRegion { get; set; } = string.Empty;
    public DiasporaRoutingStrategy StrategyUsed { get; set; }
    public double ConfidenceScore { get; set; }
    public Dictionary<string, double> DecisionFactors { get; set; } = new();
    public TimeSpan DecisionTime { get; set; }
    public bool CulturalContextConsidered { get; set; }
    public DateTime DecisionTimestamp { get; set; } = DateTime.UtcNow;
}

public class CulturalAffinityMatrix
{
    public string MatrixId { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<(CulturalCommunityType, CulturalCommunityType), double> AffinityScores { get; set; } = new();
    public Dictionary<CulturalCommunityType, List<CulturalLanguage>> CommunityLanguages { get; set; } = new();
    public Dictionary<CulturalCommunityType, List<CulturalEventType>> CommunityEvents { get; set; } = new();
    public DateTime MatrixLastUpdated { get; set; } = DateTime.UtcNow;
    public double MatrixAccuracy { get; set; } = 0.95;
}

public class DiasporaLoadBalancingAlert
{
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public LoadBalancingHealthStatus AlertSeverity { get; set; }
    public string AffectedRegion { get; set; } = string.Empty;
    public string AlertMessage { get; set; } = string.Empty;
    public DateTime AlertTimestamp { get; set; } = DateTime.UtcNow;
    public List<string> RecommendedActions { get; set; } = new();
    public bool CulturalEventRelated { get; set; }
    public CulturalEventType? RelatedCulturalEvent { get; set; }
    public Dictionary<string, object> AlertMetadata { get; set; } = new();
}