using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.CulturalIntelligence.Models;

/// <summary>
/// Cross-cultural discovery routing result with cultural affinity optimization
/// </summary>
public sealed class CrossCulturalDiscoveryRouting
{
    public Guid RoutingId { get; init; } = Guid.NewGuid();
    public List<CulturalAffinityCalculation> AffinityScores { get; init; } = new();
    public GeographicRegion TargetRegion { get; init; }
    public List<string> RecommendedCommunities { get; init; } = new();
    public decimal RoutingScore { get; init; }
    public DateTime RoutingTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request for cross-cultural discovery routing
/// </summary>
public sealed class CrossCulturalDiscoveryRequest
{
    public string UserProfileId { get; init; } = string.Empty;
    public string UserLocation { get; init; } = string.Empty;
    public List<string> TargetCulturalCommunities { get; init; } = new();
    public int MaxDiscoveryRadius { get; init; } = 50; // miles
    public DateTime RequestTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Cultural business routing decision with diaspora optimization
/// </summary>
public sealed class CulturalBusinessRoutingDecision
{
    public Guid DecisionId { get; init; } = Guid.NewGuid();
    public string RecommendedRegion { get; init; } = string.Empty;
    public List<string> TargetDiasporaCommunities { get; init; } = new();
    public decimal CulturalAffinityScore { get; init; }
    public decimal EstimatedRevenueImpact { get; init; }
    public string RoutingRationale { get; init; } = string.Empty;
    public DateTime DecisionTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request for cultural business routing
/// </summary>
public sealed class CulturalBusinessRequest
{
    public BusinessCulturalContext BusinessContext { get; init; } = new();
    public string BusinessLocation { get; init; } = string.Empty;
    public List<string> TargetCulturalGroups { get; init; } = new();
    public string BusinessCategory { get; init; } = string.Empty;
    public DateTime RequestTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Cultural routing health status for 99.99% uptime validation
/// </summary>
public sealed class CulturalRoutingHealthStatus
{
    public Guid HealthCheckId { get; init; } = Guid.NewGuid();
    public bool IsHealthy { get; init; }
    public bool IsCulturallyAppropriate { get; init; }
    public decimal RoutingAccuracy { get; init; }
    public List<string> HealthWarnings { get; init; } = new();
    public DateTime HealthCheckTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Cultural affinity calculation with community engagement scores
/// </summary>
public sealed class CulturalAffinityCalculation
{
    public Guid CalculationId { get; init; } = Guid.NewGuid();
    public string CulturalCommunity { get; init; } = string.Empty;
    public decimal AffinityScore { get; init; } // 0.0-1.0
    public Dictionary<string, decimal> CulturalMetrics { get; init; } = new();
    public GeographicProximityScore ProximityScore { get; init; } = new();
    public DateTime CalculationTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Cross-community connection opportunities for revenue optimization
/// </summary>
public sealed class CrossCommunityConnectionOpportunities
{
    public Guid OpportunityId { get; init; } = Guid.NewGuid();
    public List<CommunityConnectionPair> ConnectionPairs { get; init; } = new();
    public decimal EstimatedRevenueImpact { get; init; }
    public List<string> RecommendedStrategies { get; init; } = new();
    public DateTime AnalysisTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business cultural context for routing decisions
/// </summary>
public sealed class BusinessCulturalContext
{
    public Guid ContextId { get; init; } = Guid.NewGuid();
    public string BusinessId { get; init; } = string.Empty;
    public List<string> CulturalAffiliations { get; init; } = new();
    public Dictionary<string, object> CulturalAttributes { get; init; } = new();
    public string PrimaryCulturalIdentity { get; init; } = string.Empty;
    public DateTime ContextTimestamp { get; init; } = DateTime.UtcNow;
}

// Supporting types
/// <summary>
/// Geographic proximity score for routing optimization
/// </summary>
public sealed class GeographicProximityScore
{
    public decimal DistanceScore { get; init; }
    public int DistanceMiles { get; init; }
    public string ProximityTier { get; init; } = "Medium";
}

/// <summary>
/// Community connection pair for cross-community analysis
/// </summary>
public sealed class CommunityConnectionPair
{
    public string SourceCommunity { get; init; } = string.Empty;
    public string TargetCommunity { get; init; } = string.Empty;
    public decimal ConnectionStrength { get; init; }
    public List<string> SharedInterests { get; init; } = new();
}
