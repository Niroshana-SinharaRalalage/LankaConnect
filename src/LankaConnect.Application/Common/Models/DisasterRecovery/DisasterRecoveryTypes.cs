using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Application.Common.Models.DisasterRecovery;

/// <summary>
/// TDD GREEN Phase: Disaster Recovery Types Implementation
/// Cultural intelligence integrated disaster recovery for LankaConnect platform
/// </summary>

#region FailbackResult

/// <summary>
/// Result of failback operation after disaster recovery
/// </summary>
public class FailbackResult
{
    public string FailbackId { get; private set; }
    public string DisasterRecoveryId { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string SourceRegion { get; private set; }
    public string TargetRegion { get; private set; }
    public IReadOnlyList<string> RestoredServices { get; private set; }
    public TimeSpan FailbackDuration { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets failback success rate
    /// </summary>
    public double SuccessRate => RestoredServices.Count > 0 ? 1.0 : 0.0;

    /// <summary>
    /// Gets whether failback completed within SLA
    /// </summary>
    public bool MeetsSLA => FailbackDuration <= TimeSpan.FromMinutes(30);

    private FailbackResult(string disasterRecoveryId, string sourceRegion, string targetRegion,
        IEnumerable<string> restoredServices, TimeSpan duration, bool isSuccessful, string? errorMessage)
    {
        FailbackId = Guid.NewGuid().ToString();
        DisasterRecoveryId = disasterRecoveryId;
        SourceRegion = sourceRegion;
        TargetRegion = targetRegion;
        RestoredServices = restoredServices.ToList().AsReadOnly();
        FailbackDuration = duration;
        IsSuccessful = isSuccessful;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates successful failback result
    /// </summary>
    public static FailbackResult Success(string disasterRecoveryId, string sourceRegion, string targetRegion,
        IEnumerable<string> restoredServices, TimeSpan duration)
    {
        return new FailbackResult(disasterRecoveryId, sourceRegion, targetRegion, restoredServices, duration, true, null);
    }

    /// <summary>
    /// Creates failed failback result
    /// </summary>
    public static FailbackResult Failure(string disasterRecoveryId, string sourceRegion, string targetRegion,
        string errorMessage, TimeSpan duration)
    {
        return new FailbackResult(disasterRecoveryId, sourceRegion, targetRegion, 
            Array.Empty<string>(), duration, false, errorMessage);
    }
}

#endregion

#region CulturalIntelligenceFailover

/// <summary>
/// Cultural intelligence failover configuration
/// </summary>
public class CulturalIntelligenceFailoverConfiguration
{
    public string ConfigurationId { get; private set; }
    public string ConfigurationName { get; private set; }
    public IReadOnlyList<CulturalEventType> SupportedEvents { get; private set; }
    public IReadOnlyList<string> CulturalRegions { get; private set; }
    public TimeSpan FailoverThreshold { get; private set; }
    public double CulturalLoadMultiplier { get; private set; }
    public bool AutoScalingEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether configuration supports high-impact cultural events
    /// </summary>
    public bool SupportsHighImpactEvents => SupportedEvents.Contains(CulturalEventType.ReligiousFestival) ||
        SupportedEvents.Contains(CulturalEventType.NationalHoliday);

    private CulturalIntelligenceFailoverConfiguration(string configurationName, 
        IEnumerable<CulturalEventType> supportedEvents, IEnumerable<string> culturalRegions,
        TimeSpan failoverThreshold, double culturalLoadMultiplier, bool autoScalingEnabled)
    {
        ConfigurationId = Guid.NewGuid().ToString();
        ConfigurationName = configurationName;
        SupportedEvents = supportedEvents.ToList().AsReadOnly();
        CulturalRegions = culturalRegions.ToList().AsReadOnly();
        FailoverThreshold = failoverThreshold;
        CulturalLoadMultiplier = culturalLoadMultiplier;
        AutoScalingEnabled = autoScalingEnabled;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates cultural intelligence failover configuration
    /// </summary>
    public static CulturalIntelligenceFailoverConfiguration Create(string configurationName, 
        IEnumerable<CulturalEventType> supportedEvents, IEnumerable<string> culturalRegions,
        TimeSpan failoverThreshold, double culturalLoadMultiplier = 2.5, bool autoScalingEnabled = true)
    {
        return new CulturalIntelligenceFailoverConfiguration(configurationName, supportedEvents, 
            culturalRegions, failoverThreshold, culturalLoadMultiplier, autoScalingEnabled);
    }
}

/// <summary>
/// Cultural intelligence failover result
/// </summary>
public class CulturalIntelligenceFailoverResult
{
    public string FailoverId { get; private set; }
    public string ConfigurationId { get; private set; }
    public CulturalEventType CulturalEventType { get; private set; }
    public IReadOnlyList<string> FailedOverServices { get; private set; }
    public string TargetRegion { get; private set; }
    public bool IsSuccessful { get; private set; }
    public TimeSpan FailoverTime { get; private set; }
    public DateTime ExecutedAt { get; private set; }

    /// <summary>
    /// Gets cultural impact mitigation score
    /// </summary>
    public double CulturalImpactMitigationScore => IsSuccessful ? 0.95 : 0.0;

    private CulturalIntelligenceFailoverResult(string configurationId, CulturalEventType culturalEventType,
        IEnumerable<string> failedOverServices, string targetRegion, bool isSuccessful, TimeSpan failoverTime)
    {
        FailoverId = Guid.NewGuid().ToString();
        ConfigurationId = configurationId;
        CulturalEventType = culturalEventType;
        FailedOverServices = failedOverServices.ToList().AsReadOnly();
        TargetRegion = targetRegion;
        IsSuccessful = isSuccessful;
        FailoverTime = failoverTime;
        ExecutedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates cultural intelligence failover result
    /// </summary>
    public static CulturalIntelligenceFailoverResult Create(string configurationId, CulturalEventType culturalEventType,
        IEnumerable<string> failedOverServices, string targetRegion, bool isSuccessful, TimeSpan failoverTime)
    {
        return new CulturalIntelligenceFailoverResult(configurationId, culturalEventType, 
            failedOverServices, targetRegion, isSuccessful, failoverTime);
    }
}

#endregion

#region DisasterRecoveryLoadProfile

/// <summary>
/// Load profile for disaster recovery scenarios
/// </summary>
public class DisasterRecoveryLoadProfile
{
    public string ProfileId { get; private set; }
    public string ProfileName { get; private set; }
    public Dictionary<string, double> ServiceLoadFactors { get; private set; }
    public Dictionary<string, int> RegionCapacities { get; private set; }
    public CulturalEventType? AssociatedCulturalEvent { get; private set; }
    public TimeSpan PeakDuration { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether this is a cultural event profile
    /// </summary>
    public bool IsCulturalEventProfile => AssociatedCulturalEvent.HasValue;

    /// <summary>
    /// Gets total required capacity
    /// </summary>
    public int TotalRequiredCapacity => RegionCapacities.Values.Sum();

    private DisasterRecoveryLoadProfile(string profileName, Dictionary<string, double> serviceLoadFactors,
        Dictionary<string, int> regionCapacities, CulturalEventType? associatedCulturalEvent, TimeSpan peakDuration)
    {
        ProfileId = Guid.NewGuid().ToString();
        ProfileName = profileName;
        ServiceLoadFactors = serviceLoadFactors;
        RegionCapacities = regionCapacities;
        AssociatedCulturalEvent = associatedCulturalEvent;
        PeakDuration = peakDuration;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates disaster recovery load profile
    /// </summary>
    public static DisasterRecoveryLoadProfile Create(string profileName, 
        Dictionary<string, double> serviceLoadFactors, Dictionary<string, int> regionCapacities,
        CulturalEventType? associatedCulturalEvent = null, TimeSpan? peakDuration = null)
    {
        return new DisasterRecoveryLoadProfile(profileName, serviceLoadFactors, regionCapacities,
            associatedCulturalEvent, peakDuration ?? TimeSpan.FromHours(6));
    }
}

#endregion

#region CapacityScalingResult

/// <summary>
/// Result of capacity scaling operations during disaster recovery
/// </summary>
public class CapacityScalingResult
{
    public string ScalingId { get; private set; }
    public string DisasterRecoveryId { get; private set; }
    public Dictionary<string, int> PreScalingCapacities { get; private set; }
    public Dictionary<string, int> PostScalingCapacities { get; private set; }
    public IReadOnlyList<string> ScaledServices { get; private set; }
    public bool IsSuccessful { get; private set; }
    public TimeSpan ScalingDuration { get; private set; }
    public DateTime CompletedAt { get; private set; }

    /// <summary>
    /// Gets scaling factor achieved
    /// </summary>
    public double ScalingFactor
    {
        get
        {
            if (PreScalingCapacities.Count == 0) return 1.0;
            var preTotal = PreScalingCapacities.Values.Sum();
            var postTotal = PostScalingCapacities.Values.Sum();
            return preTotal > 0 ? (double)postTotal / preTotal : 1.0;
        }
    }

    /// <summary>
    /// Gets whether scaling met targets
    /// </summary>
    public bool MeetsScalingTargets => ScalingFactor >= 1.5; // 50% increase minimum

    private CapacityScalingResult(string disasterRecoveryId, Dictionary<string, int> preScalingCapacities,
        Dictionary<string, int> postScalingCapacities, IEnumerable<string> scaledServices,
        bool isSuccessful, TimeSpan scalingDuration)
    {
        ScalingId = Guid.NewGuid().ToString();
        DisasterRecoveryId = disasterRecoveryId;
        PreScalingCapacities = preScalingCapacities;
        PostScalingCapacities = postScalingCapacities;
        ScaledServices = scaledServices.ToList().AsReadOnly();
        IsSuccessful = isSuccessful;
        ScalingDuration = scalingDuration;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates capacity scaling result
    /// </summary>
    public static CapacityScalingResult Create(string disasterRecoveryId, 
        Dictionary<string, int> preScalingCapacities, Dictionary<string, int> postScalingCapacities,
        IEnumerable<string> scaledServices, bool isSuccessful, TimeSpan scalingDuration)
    {
        return new CapacityScalingResult(disasterRecoveryId, preScalingCapacities, postScalingCapacities,
            scaledServices, isSuccessful, scalingDuration);
    }
}

#endregion

#region CommunityFailoverResult

/// <summary>
/// Result of community-specific failover operations
/// </summary>
public class CommunityFailoverResult
{
    public string FailoverId { get; private set; }
    public IReadOnlyList<string> AffectedCommunities { get; private set; }
    public Dictionary<string, string> CommunityRegionMappings { get; private set; }
    public IReadOnlyList<string> FailedOverServices { get; private set; }
    public bool IsSuccessful { get; private set; }
    public TimeSpan FailoverTime { get; private set; }
    public DateTime ExecutedAt { get; private set; }

    /// <summary>
    /// Gets community continuity score
    /// </summary>
    public double CommunityContinuityScore => IsSuccessful ? 
        Math.Min(1.0, (double)FailedOverServices.Count / Math.Max(1, AffectedCommunities.Count)) : 0.0;

    private CommunityFailoverResult(IEnumerable<string> affectedCommunities,
        Dictionary<string, string> communityRegionMappings, IEnumerable<string> failedOverServices,
        bool isSuccessful, TimeSpan failoverTime)
    {
        FailoverId = Guid.NewGuid().ToString();
        AffectedCommunities = affectedCommunities.ToList().AsReadOnly();
        CommunityRegionMappings = communityRegionMappings;
        FailedOverServices = failedOverServices.ToList().AsReadOnly();
        IsSuccessful = isSuccessful;
        FailoverTime = failoverTime;
        ExecutedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates community failover result
    /// </summary>
    public static CommunityFailoverResult Create(IEnumerable<string> affectedCommunities,
        Dictionary<string, string> communityRegionMappings, IEnumerable<string> failedOverServices,
        bool isSuccessful, TimeSpan failoverTime)
    {
        return new CommunityFailoverResult(affectedCommunities, communityRegionMappings, 
            failedOverServices, isSuccessful, failoverTime);
    }
}

#endregion

#region BusinessContinuity

/// <summary>
/// Synchronization strategy for disaster recovery
/// </summary>
public class SynchronizationStrategy
{
    public string StrategyId { get; private set; }
    public string StrategyName { get; private set; }
    public SynchronizationType SyncType { get; private set; }
    public TimeSpan SynchronizationInterval { get; private set; }
    public IReadOnlyList<string> SynchronizedServices { get; private set; }
    public bool IsActive { get; private set; }

    private SynchronizationStrategy(string strategyName, SynchronizationType syncType,
        TimeSpan synchronizationInterval, IEnumerable<string> synchronizedServices)
    {
        StrategyId = Guid.NewGuid().ToString();
        StrategyName = strategyName;
        SyncType = syncType;
        SynchronizationInterval = synchronizationInterval;
        SynchronizedServices = synchronizedServices.ToList().AsReadOnly();
        IsActive = true;
    }

    public static SynchronizationStrategy Create(string strategyName, SynchronizationType syncType,
        TimeSpan synchronizationInterval, IEnumerable<string> synchronizedServices)
    {
        return new SynchronizationStrategy(strategyName, syncType, synchronizationInterval, synchronizedServices);
    }
}

/// <summary>
/// Business continuity scope definition
/// </summary>
public class BusinessContinuityScope
{
    public string ScopeId { get; private set; }
    public string ScopeName { get; private set; }
    public IReadOnlyList<string> CriticalBusinessProcesses { get; private set; }
    public IReadOnlyList<string> SupportingServices { get; private set; }
    public TimeSpan MaxTolerableDowntime { get; private set; }
    public ContinuityPriority Priority { get; private set; }

    private BusinessContinuityScope(string scopeName, IEnumerable<string> criticalBusinessProcesses,
        IEnumerable<string> supportingServices, TimeSpan maxTolerableDowntime, ContinuityPriority priority)
    {
        ScopeId = Guid.NewGuid().ToString();
        ScopeName = scopeName;
        CriticalBusinessProcesses = criticalBusinessProcesses.ToList().AsReadOnly();
        SupportingServices = supportingServices.ToList().AsReadOnly();
        MaxTolerableDowntime = maxTolerableDowntime;
        Priority = priority;
    }

    public static BusinessContinuityScope Create(string scopeName, IEnumerable<string> criticalBusinessProcesses,
        IEnumerable<string> supportingServices, TimeSpan maxTolerableDowntime, ContinuityPriority priority)
    {
        return new BusinessContinuityScope(scopeName, criticalBusinessProcesses, supportingServices, 
            maxTolerableDowntime, priority);
    }
}

/// <summary>
/// Business continuity assessment
/// </summary>
public class BusinessContinuityAssessment
{
    public string AssessmentId { get; private set; }
    public string ScopeId { get; private set; }
    public Dictionary<string, ContinuityStatus> ProcessStatuses { get; private set; }
    public double OverallContinuityScore { get; private set; }
    public IReadOnlyList<string> RiskFactors { get; private set; }
    public DateTime AssessedAt { get; private set; }

    private BusinessContinuityAssessment(string scopeId, Dictionary<string, ContinuityStatus> processStatuses,
        double overallContinuityScore, IEnumerable<string> riskFactors)
    {
        AssessmentId = Guid.NewGuid().ToString();
        ScopeId = scopeId;
        ProcessStatuses = processStatuses;
        OverallContinuityScore = overallContinuityScore;
        RiskFactors = riskFactors.ToList().AsReadOnly();
        AssessedAt = DateTime.UtcNow;
    }

    public static BusinessContinuityAssessment Create(string scopeId, 
        Dictionary<string, ContinuityStatus> processStatuses, double overallContinuityScore,
        IEnumerable<string> riskFactors)
    {
        return new BusinessContinuityAssessment(scopeId, processStatuses, overallContinuityScore, riskFactors);
    }
}

/// <summary>
/// Continuity activation reason
/// </summary>
public enum ContinuityActivationReason
{
    DisasterRecovery,
    PlannedMaintenance,
    CulturalEventPreparation,
    SecurityIncident,
    PerformanceDegradation,
    RegionalOutage
}

#endregion

#region Enumerations

/// <summary>
/// Synchronization types
/// </summary>
public enum SynchronizationType
{
    RealTime,
    NearRealTime,
    Periodic,
    OnDemand
}

/// <summary>
/// Continuity priority levels
/// </summary>
public enum ContinuityPriority
{
    Low,
    Medium,
    High,
    Critical,
    CulturalEvent
}

/// <summary>
/// Continuity status levels
/// </summary>
public enum ContinuityStatus
{
    Operational,
    Degraded,
    AtRisk,
    Failed,
    Recovering
}

#endregion