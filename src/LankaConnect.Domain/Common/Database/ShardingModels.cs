using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common;

public enum ShardingStrategy
{
    CulturalIntelligence,
    Geographic,
    UserBased,
    DataTypeBased,
    Hybrid
}

public enum CulturalQueryType
{
    BuddhistCalendar,
    HinduCalendar,
    IslamicCalendar,
    SikhCalendar,
    DiasporaAnalytics,
    CulturalAppropriateness,
    EventRecommendations,
    CrossCulturalAnalysis,
    CommunityInsights,
    CulturalContent
}

public enum DataSize
{
    Small,
    Medium,
    Large,
    ExtraLarge
}

public enum PerformanceRequirement
{
    RealTime,
    FastResponse,
    StandardResponse,
    BatchProcessing
}

public enum CachingStrategy
{
    None,
    Conservative,
    Balanced,
    Aggressive,
    Intelligent
}

public enum ConsistencyLevel
{
    Eventual,
    Session,
    BoundedStaleness,
    Strong,
    LinearizableStrong
}

public enum CulturalSignificance
{
    Low,
    Medium,
    High,
    Critical,
    Sacred
}

public class CulturalIntelligenceShardKey
{
    public string ShardId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string CommunityGroup { get; set; } = string.Empty;
    public CulturalDataType DataType { get; set; }
    public double LoadBalancingWeight { get; set; }
    public string ShardingReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CulturalIntelligenceQueryContext
{
    public CulturalQueryType QueryType { get; set; }
    public string CommunityId { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
    public DataSize ExpectedDataSize { get; set; }
    public PerformanceRequirement PerformanceRequirement { get; set; }
    public CachingStrategy CachingPreference { get; set; }
    public Dictionary<string, string> QueryParameters { get; set; } = new();
    public TimeSpan TimeoutLimit { get; set; } = TimeSpan.FromSeconds(30);
}

public class QueryRoutingResult
{
    public CulturalIntelligenceShardKey SelectedShard { get; set; } = null!;
    public List<string> QueryOptimizations { get; set; } = new();
    public TimeSpan EstimatedResponseTime { get; set; }
    public string RoutingReason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<CulturalIntelligenceShardKey> AlternativeShards { get; set; } = new();
}

public class ShardLoadMetrics
{
    public double LoadPercentage { get; set; }
    public int ConnectionCount { get; set; }
    public double QueriesPerSecond { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ShardRebalancingResult
{
    public bool RebalancingRequired { get; set; }
    public List<string> RebalancingActions { get; set; } = new();
    public Dictionary<string, ShardLoadMetrics> ExpectedLoadDistribution { get; set; } = new();
    public TimeSpan EstimatedRebalancingTime { get; set; }
    public string RebalancingStrategy { get; set; } = string.Empty;
    public List<string> RiskMitigations { get; set; } = new();
}

public class CulturalDataDistribution
{
    public string CommunityId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int EstimatedUserCount { get; set; }
    public int AllocatedShards { get; set; }
    public double StorageRequirementGB { get; set; }
    public double ExpectedQueriesPerSecond { get; set; }
    public List<string> OptimizationRecommendations { get; set; } = new();
}

public class CulturalEventSyncData
{
    public Guid EventId { get; set; }
    public string CommunityId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string SourceRegion { get; set; } = string.Empty;
    public string[] TargetRegions { get; set; } = Array.Empty<string>();
    public CulturalSignificance CulturalSignificance { get; set; }
    public TimeSpan RequiredSyncLatency { get; set; }
    public Dictionary<string, object> EventData { get; set; } = new();
}

// CrossRegionSynchronizationResult moved to ConsistencyModels.cs to avoid duplication
// Use LankaConnect.Domain.Common.CrossRegionSynchronizationResult instead

public class ShardPerformanceMetrics
{
    public string ShardId { get; set; } = string.Empty;
    public TimeSpan MonitoringPeriod { get; set; }
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    public TimeSpan AverageResponseTime { get; set; }
    public double ThroughputQueriesPerSecond { get; set; }
    public double HealthScore { get; set; }
    public DateTime LastCollected { get; set; } = DateTime.UtcNow;
    public List<string> PerformanceIssues { get; set; } = new();
}

public class CommunityShardingMetrics
{
    public string CommunityId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int ActiveUsers { get; set; }
    public long DailyQueries { get; set; }
    public double DataGrowthRate { get; set; }
    public PerformanceRequirement PerformanceRequirement { get; set; }
    public double CurrentLoadPercentage { get; set; }
    public TimeSpan PeakResponseTime { get; set; }
}

public class OptimalShardingResult
{
    public int OptimalShardCount { get; set; }
    public string ShardingRationale { get; set; } = string.Empty;
    public Dictionary<string, string> PerformanceProjections { get; set; } = new();
    public List<string> ScalingRecommendations { get; set; } = new();
    public Dictionary<string, object> ConfigurationRecommendations { get; set; } = new();
    public TimeSpan EstimatedMigrationTime { get; set; }
}

public class DatabaseShardingOptions
{
    public bool EnableSharding { get; set; } = true;
    public int MaxShardsPerRegion { get; set; } = 8;
    public bool CulturalCommunitySharding { get; set; } = true;
    public bool GeographicSharding { get; set; } = true;
    public ShardingStrategy ShardingStrategy { get; set; } = ShardingStrategy.CulturalIntelligence;
    public double LoadBalancingThreshold { get; set; } = 0.75;
    public TimeSpan RebalancingInterval { get; set; } = TimeSpan.FromHours(4);
    public bool AutoScaling { get; set; } = true;
    public Dictionary<string, string> RegionConnectionStrings { get; set; } = new();
    public int MaxConnectionsPerShard { get; set; } = 500;
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableCrossRegionReplication { get; set; } = true;
    public ConsistencyLevel DefaultConsistencyLevel { get; set; } = ConsistencyLevel.BoundedStaleness;
}