using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Application.Common.Models.Routing;

/// <summary>
/// TDD GREEN Phase: Routing Foundation Types Implementation
/// Cultural intelligence integrated routing with disaster recovery for LankaConnect platform
/// </summary>

#region RoutingFailureContext

/// <summary>
/// Context for routing failures with cultural event awareness
/// </summary>
public class RoutingFailureContext
{
    public string ContextId { get; private set; }
    public string RoutingId { get; private set; }
    public string FailureReason { get; private set; }
    public IReadOnlyList<string> AffectedRegions { get; private set; }
    public RoutingFailureType FailureType { get; private set; }
    public string? CulturalContext { get; private set; }
    public DateTime FailureDetectedAt { get; private set; }

    /// <summary>
    /// Gets whether this failure is related to cultural events
    /// </summary>
    public bool IsCulturalEventRelated => !string.IsNullOrEmpty(CulturalContext) || 
        FailureType == RoutingFailureType.CulturalEventOverload;

    /// <summary>
    /// Gets whether immediate failover is required
    /// </summary>
    public bool RequiresImmediateFailover => FailureType == RoutingFailureType.CulturalEventOverload ||
        FailureType == RoutingFailureType.DatabaseUnavailable ||
        FailureType == RoutingFailureType.ServiceUnavailable;

    /// <summary>
    /// Gets the estimated impact severity
    /// </summary>
    public FailureImpactLevel ImpactLevel => FailureType switch
    {
        RoutingFailureType.CulturalEventOverload => FailureImpactLevel.High,
        RoutingFailureType.DatabaseUnavailable => FailureImpactLevel.Critical,
        RoutingFailureType.ServiceUnavailable => FailureImpactLevel.High,
        RoutingFailureType.NetworkLatency => FailureImpactLevel.Medium,
        RoutingFailureType.ServiceDegradation => FailureImpactLevel.Medium,
        _ => FailureImpactLevel.Low
    };

    private RoutingFailureContext(string routingId, string failureReason, IEnumerable<string> affectedRegions,
        RoutingFailureType failureType, string? culturalContext)
    {
        ContextId = Guid.NewGuid().ToString();
        RoutingId = routingId;
        FailureReason = failureReason;
        AffectedRegions = affectedRegions.ToList().AsReadOnly();
        FailureType = failureType;
        CulturalContext = culturalContext;
        FailureDetectedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates routing failure context with cultural awareness
    /// </summary>
    public static RoutingFailureContext Create(string routingId, string failureReason, 
        IEnumerable<string> affectedRegions, RoutingFailureType failureType, string? culturalContext = null)
    {
        return new RoutingFailureContext(routingId, failureReason, affectedRegions, failureType, culturalContext);
    }
}

/// <summary>
/// Types of routing failures
/// </summary>
public enum RoutingFailureType
{
    NetworkLatency,
    ServiceDegradation,
    ServiceUnavailable,
    DatabaseUnavailable,
    CulturalEventOverload,
    RegionUnavailable
}

/// <summary>
/// Impact levels for routing failures
/// </summary>
public enum FailureImpactLevel
{
    Low,
    Medium,
    High,
    Critical
}

#endregion

#region RoutingFallbackStrategy

/// <summary>
/// Routing fallback strategy with cultural intelligence
/// </summary>
public class RoutingFallbackStrategy
{
    public string StrategyId { get; private set; }
    public string StrategyName { get; private set; }
    public IReadOnlyList<string> PrimaryRoutes { get; private set; }
    public IReadOnlyList<string> FallbackRoutes { get; private set; }
    public IReadOnlyList<CulturalEventType> SupportedCulturalEvents { get; private set; }
    public TimeSpan FailoverThreshold { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether this is a cultural-aware strategy
    /// </summary>
    public bool IsCulturallyAware => SupportedCulturalEvents.Any();

    /// <summary>
    /// Gets maximum expected failover time
    /// </summary>
    public TimeSpan MaxFailoverTime => IsCulturallyAware ? 
        TimeSpan.FromSeconds(600) : // 10 minutes for cultural events
        TimeSpan.FromMinutes(30); // 30 minutes for standard operations

    /// <summary>
    /// Gets priority level for failover operations
    /// </summary>
    public FailoverPriority Priority => IsCulturallyAware ? FailoverPriority.CulturalEvent : FailoverPriority.Standard;

    private RoutingFallbackStrategy(string strategyName, IEnumerable<string> primaryRoutes, 
        IEnumerable<string> fallbackRoutes, IEnumerable<CulturalEventType> supportedCulturalEvents, 
        TimeSpan failoverThreshold)
    {
        StrategyId = Guid.NewGuid().ToString();
        StrategyName = strategyName;
        PrimaryRoutes = primaryRoutes.ToList().AsReadOnly();
        FallbackRoutes = fallbackRoutes.ToList().AsReadOnly();
        SupportedCulturalEvents = supportedCulturalEvents.ToList().AsReadOnly();
        FailoverThreshold = failoverThreshold;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates cultural-aware routing fallback strategy
    /// </summary>
    public static RoutingFallbackStrategy CreateCulturalAware(string strategyName, 
        IEnumerable<string> primaryRoutes, IEnumerable<string> fallbackRoutes, 
        IEnumerable<CulturalEventType> culturalEventTypes, TimeSpan failoverThreshold)
    {
        return new RoutingFallbackStrategy(strategyName, primaryRoutes, fallbackRoutes, 
            culturalEventTypes, failoverThreshold);
    }

    /// <summary>
    /// Creates standard routing fallback strategy
    /// </summary>
    public static RoutingFallbackStrategy CreateStandard(string strategyName, 
        IEnumerable<string> primaryRoutes, IEnumerable<string> fallbackRoutes, TimeSpan failoverThreshold)
    {
        return new RoutingFallbackStrategy(strategyName, primaryRoutes, fallbackRoutes, 
            Array.Empty<CulturalEventType>(), failoverThreshold);
    }

    /// <summary>
    /// Determines if failover should be triggered based on failure context
    /// </summary>
    public bool ShouldTriggerFailover(RoutingFailureContext context)
    {
        // Cultural events get immediate failover
        if (context.IsCulturalEventRelated && IsCulturallyAware)
            return true;

        // High impact failures trigger immediate failover
        if (context.RequiresImmediateFailover)
            return true;

        // Standard logic based on threshold and failure type
        return context.ImpactLevel >= FailureImpactLevel.High;
    }

    /// <summary>
    /// Gets next available fallback route
    /// </summary>
    public string? GetNextFallbackRoute(IEnumerable<string> failedRoutes)
    {
        var availableRoutes = FallbackRoutes.Except(failedRoutes);
        return availableRoutes.FirstOrDefault();
    }
}

/// <summary>
/// Failover priority levels
/// </summary>
public enum FailoverPriority
{
    Standard,
    High,
    Critical,
    CulturalEvent
}

#endregion

#region DisasterRecoveryFailoverContext

/// <summary>
/// Disaster recovery failover context with cultural intelligence
/// </summary>
public class DisasterRecoveryFailoverContext
{
    public string ContextId { get; private set; }
    public string DisasterId { get; private set; }
    public DisasterType DisasterType { get; private set; }
    public IReadOnlyList<string> AffectedServices { get; private set; }
    public RecoveryStrategy RecoveryStrategy { get; private set; }
    public string TargetRegion { get; private set; }
    public string? CulturalContext { get; private set; }
    public DateTime TriggeredAt { get; private set; }

    /// <summary>
    /// Gets whether this is a cultural event disaster
    /// </summary>
    public bool IsCulturalDisaster => !string.IsNullOrEmpty(CulturalContext) || 
        DisasterType == DisasterType.CulturalEventDDoS;

    /// <summary>
    /// Gets whether specialized recovery procedures are required
    /// </summary>
    public bool RequiresSpecializedRecovery => DisasterType == DisasterType.CulturalEventDDoS ||
        DisasterType == DisasterType.DataCenterFailure ||
        DisasterType == DisasterType.RegionWideOutage;

    /// <summary>
    /// Gets recovery time objective based on disaster type and cultural context
    /// </summary>
    public TimeSpan RecoveryTimeObjective => DisasterType switch
    {
        DisasterType.CulturalEventDDoS => TimeSpan.FromMinutes(2), // Fast recovery for cultural events
        DisasterType.DataCenterFailure => TimeSpan.FromMinutes(15),
        DisasterType.RegionWideOutage => TimeSpan.FromMinutes(30),
        DisasterType.NetworkPartition => TimeSpan.FromMinutes(10),
        DisasterType.ServiceDegradation => TimeSpan.FromMinutes(5),
        _ => TimeSpan.FromMinutes(15)
    };

    /// <summary>
    /// Gets required resource scaling factor for recovery
    /// </summary>
    public double ResourceScalingFactor => IsCulturalDisaster ? 2.5 : 1.5; // Cultural events need more resources

    private DisasterRecoveryFailoverContext(string disasterId, DisasterType disasterType, 
        IEnumerable<string> affectedServices, RecoveryStrategy recoveryStrategy, 
        string targetRegion, string? culturalContext)
    {
        ContextId = Guid.NewGuid().ToString();
        DisasterId = disasterId;
        DisasterType = disasterType;
        AffectedServices = affectedServices.ToList().AsReadOnly();
        RecoveryStrategy = recoveryStrategy;
        TargetRegion = targetRegion;
        CulturalContext = culturalContext;
        TriggeredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates disaster recovery failover context
    /// </summary>
    public static DisasterRecoveryFailoverContext Create(string disasterId, DisasterType disasterType,
        IEnumerable<string> affectedServices, RecoveryStrategy recoveryStrategy, 
        string targetRegion, string? culturalContext = null)
    {
        return new DisasterRecoveryFailoverContext(disasterId, disasterType, affectedServices, 
            recoveryStrategy, targetRegion, culturalContext);
    }

    /// <summary>
    /// Estimates recovery time based on disaster type and cultural context
    /// </summary>
    public TimeSpan EstimateRecoveryTime()
    {
        var baseTime = RecoveryTimeObjective;
        
        // Cultural disasters get priority and faster recovery
        if (IsCulturalDisaster)
            return TimeSpan.FromTicks((long)(baseTime.Ticks * 0.6)); // 40% faster

        // Multiple affected services increase recovery time
        if (AffectedServices.Count > 3)
            return TimeSpan.FromTicks((long)(baseTime.Ticks * 1.3)); // 30% longer

        return baseTime;
    }

    /// <summary>
    /// Gets required coordination teams for recovery
    /// </summary>
    public IReadOnlyList<string> GetRequiredCoordinationTeams()
    {
        var teams = new List<string> { "disaster-recovery", "infrastructure" };

        if (IsCulturalDisaster)
        {
            teams.AddRange(new[] { "cultural-intelligence", "community-coordination" });
        }

        if (RequiresSpecializedRecovery)
        {
            teams.Add("executive-leadership");
        }

        return teams.AsReadOnly();
    }
}

/// <summary>
/// Types of disasters for recovery planning
/// </summary>
public enum DisasterType
{
    NetworkPartition,
    ServiceDegradation,
    DataCenterFailure,
    RegionWideOutage,
    CulturalEventDDoS,
    SecurityBreach
}

/// <summary>
/// Recovery strategies for disaster scenarios
/// </summary>
public enum RecoveryStrategy
{
    StandardFailover,
    CulturalEventLoadDistribution,
    GeographicFailover,
    ServiceIsolation,
    CapacityScaling,
    EmergencyMode
}

#endregion

#region Multi-Language Routing Models

/// <summary>
/// Multi-language routing request for South Asian diaspora language preferences
/// </summary>
public class MultiLanguageRoutingRequest
{
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public string ContentText { get; set; } = string.Empty;
    public SouthAsianLanguage PreferredLanguage { get; set; }
    public List<SouthAsianLanguage> AlternativeLanguages { get; set; } = new();
    public string GeographicRegion { get; set; } = string.Empty;
    public int GenerationInDiaspora { get; set; }
    public bool IsCulturalEventPeriod { get; set; }
    public Dictionary<string, object> PerformanceHints { get; set; } = new();
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    public int Priority { get; set; } = 5; // 1-10 scale
}

/// <summary>
/// Multi-language routing response with optimization metrics
/// </summary>
public class MultiLanguageRoutingResponse
{
    public Guid RequestId { get; set; }
    public SouthAsianLanguage SelectedLanguage { get; set; }
    public List<SouthAsianLanguage> FallbackLanguages { get; set; } = new();
    public string RoutingStrategy { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, decimal> PerformanceMetrics { get; set; } = new();
    public List<string> OptimizationNotes { get; set; } = new();
    public DateTime ResponseTimestamp { get; set; } = DateTime.UtcNow;
    public bool UsedCulturalOptimization { get; set; }
}

/// <summary>
/// Batch multi-language routing response for high-concurrency scenarios
/// </summary>
public class BatchMultiLanguageRoutingResponse
{
    public List<MultiLanguageRoutingResponse> IndividualResponses { get; set; } = new();
    public TimeSpan TotalProcessingTime { get; set; }
    public decimal AverageConfidenceScore { get; set; }
    public int SuccessfulRoutings { get; set; }
    public int FailedRoutings { get; set; }
    public Dictionary<string, decimal> BatchPerformanceMetrics { get; set; } = new();
    public List<string> BatchOptimizationNotes { get; set; } = new();
    public DateTime BatchResponseTimestamp { get; set; } = DateTime.UtcNow;
}

#endregion