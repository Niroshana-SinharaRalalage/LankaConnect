using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Shared;

/// <summary>
/// Cache optimization request
/// </summary>
public class CacheOptimizationRequest
{
    public List<SouthAsianLanguage> TargetLanguages { get; set; } = new();
    public List<CulturalEvent> UpcomingEvents { get; set; } = new();
    public TimeSpan OptimizationWindow { get; set; }
    public decimal ExpectedTrafficIncrease { get; set; }
    public List<CulturalRegion> PriorityRegions { get; set; } = new();
}

/// <summary>
/// Cache optimization result
/// </summary>
public class CacheOptimizationResult
{
    public decimal L1CacheOptimization { get; set; }
    public decimal L2CacheOptimization { get; set; }
    public decimal OverallPerformanceGain { get; set; }
    public decimal CacheHitRateImprovement { get; set; }
    public DateTime OptimizationTimestamp { get; set; }
    public List<string> OptimizationActions { get; set; } = new();
}

/// <summary>
/// Cache pre-warming result
/// </summary>
public class CachePreWarmingResult
{
    public int PreWarmedEntries { get; set; }
    public decimal CacheReadiness { get; set; }
    public decimal ExpectedHitRateIncrease { get; set; }
    public DateTime PreWarmingCompletedAt { get; set; }
    public List<SouthAsianLanguage> PreWarmedLanguages { get; set; } = new();
}

/// <summary>
/// Cache invalidation result
/// </summary>
public class CacheInvalidationResult
{
    public int InvalidatedEntries { get; set; }
    public CacheInvalidationStrategy RefreshStrategy { get; set; }
    public decimal PerformanceImpact { get; set; }
    public DateTime RefreshCompletedAt { get; set; }
    public List<string> AffectedCacheKeys { get; set; } = new();
}

/// <summary>
/// Rendering requirements for complex scripts
/// </summary>
public class RenderingRequirements
{
    public bool RequiresComplexShaping { get; set; }
    public bool RequiresBidirectionalText { get; set; }
    public bool RequiresAdvancedFontFeatures { get; set; }
    public List<string> RecommendedFonts { get; set; } = new();
    public string FontFeatureSettings { get; set; } = string.Empty;
}

/// <summary>
/// Language complexity analysis
/// </summary>
public class LanguageComplexityAnalysis
{
    public Dictionary<SouthAsianLanguage, ScriptComplexity> ScriptComplexities { get; set; } = new();
    public Dictionary<SouthAsianLanguage, RenderingRequirements> RenderingRequirements { get; set; } = new();
    public decimal OverallComplexityScore { get; set; }
}

/// <summary>
/// Disaster recovery failover context
/// </summary>
public class DisasterRecoveryFailoverContext
{
    public string FailoverTrigger { get; set; } = string.Empty;
    public CulturalRegion SourceRegion { get; set; }
    public CulturalRegion TargetRegion { get; set; }
    public List<SouthAsianLanguage> CriticalLanguages { get; set; } = new();
    public List<CulturalEvent> ActiveEvents { get; set; } = new();
    public DateTime FailoverInitiatedAt { get; set; }
}

/// <summary>
/// Language routing failover result
/// </summary>
public class LanguageRoutingFailoverResult
{
    public bool FailoverExecuted { get; set; }
    public TimeSpan FailoverTime { get; set; }
    public bool CulturalIntelligencePreserved { get; set; }
    public decimal ServiceContinuity { get; set; }
    public DateTime FailoverTimestamp { get; set; }
    public List<string> FailoverActions { get; set; } = new();
}

/// <summary>
/// Cultural intelligence state for preservation
/// </summary>
public class CulturalIntelligenceState
{
    public Dictionary<SouthAsianLanguage, decimal> LanguageAffinities { get; set; } = new();
    public Dictionary<CulturalEvent, decimal> EventParticipation { get; set; } = new();
    public Dictionary<CulturalRegion, int> CommunityDistribution { get; set; } = new();
    public DateTime StateSnapshot { get; set; }
    public string StateChecksum { get; set; } = string.Empty;
}

/// <summary>
/// Cultural intelligence preservation result
/// </summary>
public class CulturalIntelligencePreservationResult
{
    public bool StatePreserved { get; set; }
    public decimal PreservationAccuracy { get; set; }
    public bool SacredEventContinuity { get; set; }
    public bool DiasporaCommunityServiceMaintained { get; set; }
    public DateTime PreservationTimestamp { get; set; }
    public List<string> PreservationActions { get; set; } = new();
}