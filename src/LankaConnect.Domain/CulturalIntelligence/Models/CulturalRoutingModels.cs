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

// Additional types from Stage5MissingTypes.cs

/// <summary>
/// Domain cultural context for core business rules
/// Moved from Stage5MissingTypes.cs
/// </summary>
public class DomainCulturalContext
{
    public string ContextId { get; set; } = string.Empty;
    public string PrimaryCulture { get; set; } = string.Empty;
    public List<string> SecondaryCultures { get; set; } = new();
    public Dictionary<string, string> CulturalAttributes { get; set; } = new();
}

/// <summary>
/// Cultural routing rationale for decision transparency
/// Moved from Stage5MissingTypes.cs
/// </summary>
public class CulturalRoutingRationale
{
    public string RationaleId { get; set; } = string.Empty;
    public string RoutingDecision { get; set; } = string.Empty;
    public Dictionary<string, double> CulturalFactors { get; set; } = new();
    public List<string> ConsideredAlternatives { get; set; } = new();
    public double ConfidenceScore { get; set; }
}

/// <summary>
/// Religious background for cultural sensitivity
/// Moved from Stage5MissingTypes.cs
/// </summary>
public class ReligiousBackground
{
    public string BackgroundId { get; set; } = string.Empty;
    public string Religion { get; set; } = string.Empty;
    public string Denomination { get; set; } = string.Empty;
    public List<string> Practices { get; set; } = new();
    public List<string> Observances { get; set; } = new();
}

/// <summary>
/// User language profile for multilingual support
/// Moved from Stage5MissingTypes.cs (renamed from LanguagePreferences)
/// </summary>
public class UserLanguageProfile
{
    public string ProfileId { get; set; } = string.Empty;
    public string PrimaryLanguage { get; set; } = string.Empty;
    public List<string> SecondaryLanguages { get; set; } = new();
    public Dictionary<string, string> LanguageProficiency { get; set; } = new();
}

/// <summary>
/// Cultural event participation tracking
/// Moved from Stage5MissingTypes.cs
/// </summary>
public class CulturalEventParticipation
{
    public string ParticipationId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ParticipationType ParticipationType { get; set; }
    public DateTime ParticipatedAt { get; set; }
}

/// <summary>
/// Type of cultural event participation
/// </summary>
public enum ParticipationType
{
    Organizer,
    Speaker,
    Attendee,
    Volunteer,
    Sponsor
}
