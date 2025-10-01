using System.ComponentModel.DataAnnotations;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.Database;

// CulturalEventType enum removed - using LankaConnect.Domain.Common.Enums.CulturalEventType

/// <summary>
/// Sacred event priority levels based on architect's recommendation for conflict resolution
/// Priority Matrix: Sacred Events (Vesak, Eid): Priority 10, Major Festivals (Diwali): Priority 9
/// </summary>
public enum SacredEventPriority
{
    /// <summary>
    /// Level 10 - Highest sacred events (Vesak Poya, Eid celebrations)
    /// </summary>
    Level10Sacred = 10,

    /// <summary>
    /// Level 9 - Major festivals (Diwali, Holi, Thaipusam)
    /// </summary>
    Level9Major = 9,

    /// <summary>
    /// Level 8 - Significant observances (Poyadays, regional festivals)
    /// </summary>
    Level8Significant = 8,

    /// <summary>
    /// Level 7 - National holidays (Sinhala New Year, Thai Pusam)
    /// </summary>
    Level7National = 7,

    /// <summary>
    /// Level 6 - Regional celebrations (Province-specific events)
    /// </summary>
    Level6Regional = 6,

    /// <summary>
    /// Level 5 - Community gatherings (Local temple events)
    /// </summary>
    Level5Community = 5,

    /// <summary>
    /// Level 4 - Educational events (Cultural workshops)
    /// </summary>
    Level4Educational = 4,

    /// <summary>
    /// Level 3 - General cultural activities
    /// </summary>
    Level3General = 3
}

/// <summary>
/// Cultural event distribution scope for Fortune 500 enterprise platform
/// </summary>
public enum CulturalEventScope
{
    /// <summary>
    /// Local to specific community (temple, mosque, church)
    /// </summary>
    LocalCommunity = 1,

    /// <summary>
    /// City-wide coverage
    /// </summary>
    CityWide = 2,

    /// <summary>
    /// Regional coverage (state/province)
    /// </summary>
    Regional = 3,

    /// <summary>
    /// Asia-Pacific focused
    /// </summary>
    AsiaPacific = 4,

    /// <summary>
    /// South America focused
    /// </summary>
    SouthAmerica = 5,

    /// <summary>
    /// Multi-regional (2-3 regions)
    /// </summary>
    MultiRegional = 6
}

/// <summary>
/// Performance monitoring scope for Fortune 500 SLA compliance
/// </summary>
public enum PerformanceScope
{
    /// <summary>
    /// Local single node monitoring
    /// </summary>
    Local = 1,

    /// <summary>
    /// Regional cluster monitoring (3-5 nodes)
    /// </summary>
    Regional = 2,

    /// <summary>
    /// Global distributed monitoring (10+ nodes)
    /// </summary>
    Global = 3,

    /// <summary>
    /// Enterprise multi-tenant monitoring
    /// </summary>
    Enterprise = 4
}

/// <summary>
/// Performance monitoring classification for cultural intelligence
/// </summary>
public enum MonitoringClassification
{
    /// <summary>
    /// Real-time performance tracking (< 100ms response)
    /// </summary>
    RealTime = 1,

    /// <summary>
    /// Near real-time monitoring (100ms - 1s response)
    /// </summary>
    NearRealTime = 2,

    /// <summary>
    /// Batch monitoring (1s - 1min response)
    /// </summary>
    Batch = 3,

    /// <summary>
    /// Scheduled monitoring (> 1min response)
    /// </summary>
    Scheduled = 4,

    /// <summary>
    /// Cultural event-triggered monitoring
    /// </summary>
    EventTriggered = 5,

    /// <summary>
    /// Emergency response monitoring (< 10ms response)
    /// </summary>
    EmergencyResponse = 6
}

/// <summary>
/// Cultural integration status levels
/// </summary>
public enum CulturalIntegrationStatus
{
    /// <summary>
    /// Not integrated with cultural intelligence
    /// </summary>
    NotIntegrated = 0,

    /// <summary>
    /// Basic cultural awareness
    /// </summary>
    BasicAwareness = 1,

    /// <summary>
    /// Moderate cultural integration
    /// </summary>
    ModerateIntegration = 2,

    /// <summary>
    /// Advanced cultural intelligence
    /// </summary>
    AdvancedIntelligence = 3,

    /// <summary>
    /// Full cultural optimization
    /// </summary>
    FullOptimization = 4,

    /// <summary>
    /// Cultural excellence certification
    /// </summary>
    CulturalExcellence = 5
}

/// <summary>
/// Smart scaling strategies for cultural events
/// </summary>
public enum SmartScalingStrategy
{
    /// <summary>
    /// Predictive scaling based on cultural event patterns
    /// </summary>
    PredictiveScaling = 1,

    /// <summary>
    /// Reactive scaling based on real-time metrics
    /// </summary>
    ReactiveScaling = 2,

    /// <summary>
    /// Scheduled scaling for known cultural events
    /// </summary>
    ScheduledScaling = 3,

    /// <summary>
    /// Hybrid scaling combining predictive and reactive approaches
    /// </summary>
    HybridScaling = 4,

    /// <summary>
    /// Cultural intelligence-driven scaling
    /// </summary>
    CulturalIntelligenceScaling = 5
}

/// <summary>
/// Database connection optimization strategies
/// </summary>
public enum ConnectionOptimizationStrategy
{
    /// <summary>
    /// Standard connection pooling
    /// </summary>
    StandardPooling = 1,

    /// <summary>
    /// Cultural event-aware pooling
    /// </summary>
    CulturalAwarePooling = 2,

    /// <summary>
    /// Adaptive pooling based on load patterns
    /// </summary>
    AdaptivePooling = 3,

    /// <summary>
    /// Intelligent pooling with ML predictions
    /// </summary>
    IntelligentPooling = 4,

    /// <summary>
    /// Geographic-aware connection distribution
    /// </summary>
    GeographicAwarePooling = 5,

    /// <summary>
    /// Sacred event priority pooling
    /// </summary>
    SacredEventPriorityPooling = 6
}

/// <summary>
/// Cultural event models data access patterns
/// </summary>
public class CulturalEventMetrics
{
    public Guid EventId { get; set; }
    public CulturalEventType EventType { get; set; }
    public SacredEventPriority Priority { get; set; }
    public CulturalEventScope Scope { get; set; }
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    public int ExpectedParticipants { get; set; }
    public double TrafficMultiplier { get; set; }
    public CulturalIntegrationStatus IntegrationStatus { get; set; }
    public SmartScalingStrategy ScalingStrategy { get; set; }
    public ConnectionOptimizationStrategy ConnectionStrategy { get; set; }
    public GeographicRegion PrimaryRegion { get; set; }
    public List<GeographicRegion> AffectedRegions { get; set; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cultural event performance tracking
/// </summary>
public class CulturalEventPerformanceLog
{
    public Guid LogId { get; set; }
    public Guid EventId { get; set; }
    public PerformanceScope PerformanceScope { get; set; }
    public MonitoringClassification MonitoringType { get; set; }
    public DateTime LogTimestamp { get; set; }
    public double ResponseTime { get; set; }
    public int ConcurrentUsers { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public double DatabaseConnectionPoolUsage { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorDetails { get; set; }
    public CulturalIntegrationStatus IntegrationStatus { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Smart scaling decisions log
/// </summary>
public class SmartScalingDecision
{
    public Guid DecisionId { get; set; }
    public Guid EventId { get; set; }
    public SmartScalingStrategy Strategy { get; set; }
    public DateTime DecisionTimestamp { get; set; }
    public int CurrentInstances { get; set; }
    public int TargetInstances { get; set; }
    public string ScalingReason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public TimeSpan EstimatedScalingDuration { get; set; }
    public bool WasSuccessful { get; set; }
    public string? ExecutionDetails { get; set; }
    public Dictionary<string, object> PredictionMetrics { get; set; } = new();
}