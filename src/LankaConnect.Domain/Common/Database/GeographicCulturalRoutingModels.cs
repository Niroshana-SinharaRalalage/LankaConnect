using System.ComponentModel.DataAnnotations;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Geographic Cultural Intelligence Routing domain models
/// Architect-recommended approach: Multi-dimensional routing engine with spatial cultural intelligence
/// Supports global South Asian diaspora communities with <200ms response times and 94%+ cultural accuracy
/// </summary>

/// <summary>
/// Language codes for South Asian diaspora communities
/// Multi-language support: Sinhala, Tamil, Hindi, Urdu, Punjabi, Bengali, Gujarati
/// </summary>
public enum LanguageCode
{
    English = 1,
    Sinhala = 2,
    Tamil = 3,
    Hindi = 4,
    Urdu = 5,
    Punjabi = 6,
    Bengali = 7,
    Gujarati = 8,
    Arabic = 9,
    Marathi = 10,
    Telugu = 11,
    Malayalam = 12,
    Kannada = 13
}

/// <summary>
/// Calendar types for Buddhist/Hindu/Islamic calendar intelligence integration
/// </summary>
public enum CalendarType
{
    Buddhist = 1,
    Hindu = 2,
    Islamic = 3,
    Sikh = 4,
    Christian = 5,
    Lunar = 6,
    Solar = 7
}

/// <summary>
/// Cultural routing conflict resolution strategies
/// Architect recommendation: Balance cultural authenticity with geographic proximity
/// </summary>
public enum GeographicConflictResolutionStrategy
{
    /// <summary>
    /// Prioritize cultural authenticity over performance (recommended for sacred events)
    /// </summary>
    CulturalAuthenticityFirst = 1,

    /// <summary>
    /// Prioritize performance over cultural specificity (recommended for general usage)
    /// </summary>
    PerformanceFirst = 2,

    /// <summary>
    /// Balanced approach weighing authenticity and performance equally
    /// </summary>
    BalancedApproach = 3,

    /// <summary>
    /// Sacred event priority overrides all other considerations (Vesak, Eid priority)
    /// </summary>
    SacredEventPriority = 4,

    /// <summary>
    /// Multi-cultural optimization for overlapping community regions
    /// </summary>
    MultiCulturalOptimization = 5
}

/// <summary>
/// Geographic cultural intelligence routing request
/// Core request model for cultural routing decisions
/// </summary>
public class CulturalRoutingRequest
{
    /// <summary>
    /// Unique identifier for the routing request
    /// </summary>
    [Required]
    public Guid RequestId { get; set; }

    /// <summary>
    /// Geographic location for routing optimization
    /// </summary>
    [Required]
    public GeographicLocation Location { get; set; } = null!;

    /// <summary>
    /// User's preferred languages in priority order
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<LanguageCode> UserLanguages { get; set; } = new();

    /// <summary>
    /// Primary cultural community for routing optimization
    /// </summary>
    [Required]
    public CulturalCommunityType CulturalCommunityType { get; set; }

    /// <summary>
    /// Request timestamp for calendar intelligence integration
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Required response time SLA (<200ms for Fortune 500 compliance)
    /// </summary>
    [Required]
    public TimeSpan RequiredResponseTime { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Cultural context hints for enhanced routing (optional)
    /// Examples: "පොය දිනය" (Buddhist), "दीवाली" (Hindu), "عید" (Islamic)
    /// </summary>
    public List<string> CulturalContextHints { get; set; } = new();

    /// <summary>
    /// Preferred conflict resolution strategy
    /// </summary>
    public GeographicConflictResolutionStrategy PreferredResolutionStrategy { get; set; } = GeographicConflictResolutionStrategy.BalancedApproach;
}

/// <summary>
/// Geographic location with cultural intelligence metadata
/// </summary>
public class GeographicLocation
{
    /// <summary>
    /// Latitude coordinate
    /// </summary>
    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    /// <summary>
    /// Human-readable address
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Country name
    /// </summary>
    [Required]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Regional identifier (NorthAmerica, Europe, AsiaPacific, etc.)
    /// </summary>
    [Required]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// City or metropolitan area
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Timezone for temporal cultural intelligence
    /// </summary>
    public string TimeZone { get; set; } = "UTC";
}

/// <summary>
/// Cultural routing decision response
/// Contains optimized routing decision with cultural intelligence insights
/// </summary>
public class CulturalRoutingDecision
{
    /// <summary>
    /// Unique identifier for this routing decision
    /// </summary>
    public Guid DecisionId { get; set; }

    /// <summary>
    /// Selected optimal cultural region
    /// </summary>
    [Required]
    public CulturalRegion SelectedRegion { get; set; } = null!;

    /// <summary>
    /// Cultural appropriateness score (0.0 to 1.0)
    /// Target: >0.94 for cultural accuracy compliance
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CulturalAppropriateness { get; set; }

    /// <summary>
    /// Expected response time for this routing decision
    /// Target: <200ms for Fortune 500 SLA compliance
    /// </summary>
    public TimeSpan ExpectedResponseTime { get; set; }

    /// <summary>
    /// Confidence level in routing decision (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal RoutingConfidence { get; set; }

    /// <summary>
    /// Applied performance optimizations
    /// </summary>
    public List<string> PerformanceOptimizations { get; set; } = new();

    /// <summary>
    /// Whether sacred event optimization was applied
    /// </summary>
    public bool SacredEventOptimization { get; set; }

    /// <summary>
    /// Calendar influence context for this decision
    /// </summary>
    public CalendarInfluenceContext? CalendarInfluence { get; set; }

    /// <summary>
    /// Language routing optimization details
    /// </summary>
    public LanguageRoutingOptimization? LanguageOptimization { get; set; }

    /// <summary>
    /// SLA compliance validation results
    /// </summary>
    public SlaComplianceValidation? SlaCompliance { get; set; }

    /// <summary>
    /// Explanation for conflict resolution (if applicable)
    /// </summary>
    public string ConflictResolutionReason { get; set; } = string.Empty;

    /// <summary>
    /// Applied conflict resolution strategy
    /// </summary>
    public GeographicConflictResolutionStrategy ConflictResolutionStrategy { get; set; }

    /// <summary>
    /// Decision timestamp
    /// </summary>
    public DateTime DecisionTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Spatial cultural context analysis
/// Result of spatial cultural intelligence engine analysis
/// </summary>
public class SpatialCulturalContext
{
    /// <summary>
    /// Primary cultural region identifier
    /// </summary>
    public string PrimaryCulturalRegion { get; set; } = string.Empty;

    /// <summary>
    /// Community density in the region (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CommunityDensity { get; set; }

    /// <summary>
    /// Distance to nearest sacred site (in meters)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int SacredSiteProximity { get; set; }

    /// <summary>
    /// Cultural authenticity score for the region (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CulturalAuthenticity { get; set; }

    /// <summary>
    /// Secondary cultural communities present in region
    /// </summary>
    public List<CulturalCommunityType> SecondaryCommunities { get; set; } = new();

    /// <summary>
    /// Cultural landmark proximity scores
    /// </summary>
    public Dictionary<string, decimal> CulturalLandmarkProximity { get; set; } = new();
}

/// <summary>
/// Calendar influence context from Buddhist/Hindu/Islamic calendar intelligence
/// </summary>
public class CalendarInfluenceContext
{
    /// <summary>
    /// Active calendar influences affecting routing
    /// </summary>
    public List<CalendarInfluence> ActiveCalendarInfluences { get; set; } = new();

    /// <summary>
    /// Overall calendar influence intensity (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal OverallInfluence { get; set; }

    /// <summary>
    /// Recommended optimizations based on calendar intelligence
    /// </summary>
    public List<string> RecommendedOptimizations { get; set; } = new();

    /// <summary>
    /// Context timestamp
    /// </summary>
    public DateTime ContextTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual calendar influence factor
/// </summary>
public class CalendarInfluence
{
    /// <summary>
    /// Calendar type (Buddhist, Hindu, Islamic, etc.)
    /// </summary>
    public CalendarType Type { get; set; }

    /// <summary>
    /// Specific event name (Vesak, Diwali, Eid, etc.)
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Influence intensity (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal Intensity { get; set; }

    /// <summary>
    /// Routing priority level for this calendar event
    /// </summary>
    public SacredEventPriority RoutingPriority { get; set; }

    /// <summary>
    /// Traffic multiplier for this event (e.g., 5.0 for Vesak)
    /// </summary>
    [Range(1.0, 10.0)]
    public decimal TrafficMultiplier { get; set; }

    /// <summary>
    /// Cultural significance description
    /// </summary>
    public string CulturalSignificance { get; set; } = string.Empty;

    /// <summary>
    /// Geographic regions most affected by this calendar influence
    /// </summary>
    public List<string> AffectedRegions { get; set; } = new();
}

/// <summary>
/// Multi-language routing optimization
/// </summary>
public class LanguageRoutingOptimization
{
    /// <summary>
    /// Primary language for content optimization
    /// </summary>
    public LanguageCode PrimaryLanguage { get; set; }

    /// <summary>
    /// Secondary languages in preference order
    /// </summary>
    public LanguageCode[] SecondaryLanguages { get; set; } = Array.Empty<LanguageCode>();

    /// <summary>
    /// Localized content availability score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal LocalizedContentAvailability { get; set; }

    /// <summary>
    /// Translation quality score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal TranslationQuality { get; set; }

    /// <summary>
    /// Cultural nuance preservation score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CulturalNuancePreservation { get; set; }

    /// <summary>
    /// Language preference priority mapping
    /// </summary>
    public Dictionary<LanguageCode, decimal> LanguagePreferencePriority { get; set; } = new();

    /// <summary>
    /// Whether multi-language content is properly optimized
    /// </summary>
    public bool MultiLanguageCompatibility { get; set; }

    /// <summary>
    /// Regional language variant optimizations
    /// </summary>
    public Dictionary<string, string> RegionalLanguageVariants { get; set; } = new();
}

/// <summary>
/// Cultural appropriateness scoring
/// Multi-criteria decision analysis for cultural routing
/// </summary>
public class CulturalAppropriatenessScore
{
    /// <summary>
    /// Overall cultural appropriateness score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Geographic cultural fit score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal GeographicScore { get; set; }

    /// <summary>
    /// Temporal cultural appropriateness (calendar-aware) (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal TemporalScore { get; set; }

    /// <summary>
    /// Community cultural fit score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CommunityScore { get; set; }

    /// <summary>
    /// Language cultural appropriateness score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal LanguageScore { get; set; }

    /// <summary>
    /// Confidence level in cultural appropriateness assessment (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CulturalConfidenceLevel { get; set; }

    /// <summary>
    /// Detailed scoring breakdown
    /// </summary>
    public Dictionary<string, decimal> DetailedScoreBreakdown { get; set; } = new();
}

/// <summary>
/// Cultural region definition
/// Geographic region with cultural intelligence metadata
/// </summary>
public class CulturalRegion
{
    /// <summary>
    /// Unique region identifier
    /// </summary>
    [Required]
    public string RegionId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable region name
    /// </summary>
    [Required]
    public string RegionName { get; set; } = string.Empty;

    /// <summary>
    /// Primary cultural community for this region
    /// </summary>
    [Required]
    public CulturalCommunityType PrimaryCulturalCommunity { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// Target: <200ms for Fortune 500 SLA compliance
    /// </summary>
    [Range(0, 5000)]
    public int AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Cultural authority rating (0.0 to 1.0)
    /// Higher rating indicates stronger cultural authenticity
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CulturalAuthorityRating { get; set; }

    /// <summary>
    /// Community density in region (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CommunityDensity { get; set; }

    /// <summary>
    /// Geographic boundary (for spatial queries)
    /// </summary>
    public string GeographicBoundary { get; set; } = string.Empty;

    /// <summary>
    /// Supported languages in this region
    /// </summary>
    public List<LanguageCode> SupportedLanguages { get; set; } = new();

    /// <summary>
    /// Cultural landmarks and sacred sites in region
    /// </summary>
    public List<CulturalLandmark> CulturalLandmarks { get; set; } = new();

    /// <summary>
    /// Performance optimization features available
    /// </summary>
    public List<string> AvailableOptimizations { get; set; } = new();
}

/// <summary>
/// Cultural landmark or sacred site
/// </summary>
public class CulturalLandmark
{
    /// <summary>
    /// Landmark identifier
    /// </summary>
    public string LandmarkId { get; set; } = string.Empty;

    /// <summary>
    /// Landmark name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Landmark type (Temple, Mosque, Gurdwara, etc.)
    /// </summary>
    public string LandmarkType { get; set; } = string.Empty;

    /// <summary>
    /// Cultural significance score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CulturalSignificance { get; set; }

    /// <summary>
    /// Geographic location
    /// </summary>
    public GeographicLocation Location { get; set; } = null!;

    /// <summary>
    /// Associated cultural community
    /// </summary>
    public CulturalCommunityType AssociatedCommunity { get; set; }
}

/// <summary>
/// SLA compliance validation results
/// Validates Fortune 500 performance requirements
/// </summary>
public class SlaComplianceValidation
{
    /// <summary>
    /// Whether response time SLA is met (<200ms requirement)
    /// </summary>
    public bool ResponseTimeSlaCompliant { get; set; }

    /// <summary>
    /// Overall global performance rating (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal GlobalPerformanceRating { get; set; }

    /// <summary>
    /// Uptime compliance percentage
    /// Target: 99.9% for Fortune 500 requirements
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal UptimeCompliance { get; set; } = 99.9m;

    /// <summary>
    /// Throughput compliance (requests per second)
    /// </summary>
    public int ThroughputCompliance { get; set; }

    /// <summary>
    /// Cultural accuracy compliance
    /// Target: >94% cultural appropriateness accuracy
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CulturalAccuracyCompliance { get; set; }

    /// <summary>
    /// SLA validation timestamp
    /// </summary>
    public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Unified cultural intelligence request
/// Integrates Cultural Event Load Distribution with Geographic Routing
/// </summary>
public class UnifiedCulturalIntelligenceRequest
{
    /// <summary>
    /// Request identifier
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Cultural event load distribution request
    /// </summary>
    public CulturalEventLoadDistributionRequest LoadDistributionRequest { get; set; } = null!;

    /// <summary>
    /// Geographic cultural routing request
    /// </summary>
    public CulturalRoutingRequest GeographicRoutingRequest { get; set; } = null!;

    /// <summary>
    /// Integration strategy preference
    /// </summary>
    public CulturalIntelligenceIntegrationStrategy IntegrationStrategy { get; set; } = CulturalIntelligenceIntegrationStrategy.Unified;
}

/// <summary>
/// Unified cultural intelligence response
/// Combined load distribution and geographic routing optimization
/// </summary>
public class UnifiedCulturalIntelligenceResponse
{
    /// <summary>
    /// Response identifier
    /// </summary>
    public Guid ResponseId { get; set; }

    /// <summary>
    /// Load distribution results
    /// </summary>
    public CulturalEventLoadDistributionResponse LoadDistribution { get; set; } = null!;

    /// <summary>
    /// Geographic routing results
    /// </summary>
    public CulturalRoutingDecision GeographicRouting { get; set; } = null!;

    /// <summary>
    /// Unified cultural compatibility score (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public decimal CulturalCompatibilityScore { get; set; }

    /// <summary>
    /// Expected global response time after optimization
    /// </summary>
    public TimeSpan ExpectedGlobalResponseTime { get; set; }

    /// <summary>
    /// Revenue impact projection from cultural optimization
    /// </summary>
    public decimal RevenueImpactProjection { get; set; }

    /// <summary>
    /// Integration success indicators
    /// </summary>
    public List<string> IntegrationSuccessIndicators { get; set; } = new();

    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime ResponseTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cultural intelligence integration strategy
/// </summary>
public enum CulturalIntelligenceIntegrationStrategy
{
    /// <summary>
    /// Unified approach combining all cultural intelligence layers
    /// </summary>
    Unified = 1,

    /// <summary>
    /// Geographic routing priority
    /// </summary>
    GeographicPriority = 2,

    /// <summary>
    /// Load distribution priority
    /// </summary>
    LoadDistributionPriority = 3,

    /// <summary>
    /// Performance-optimized integration
    /// </summary>
    PerformanceOptimized = 4,

    /// <summary>
    /// Cultural accuracy optimized integration
    /// </summary>
    CulturalAccuracyOptimized = 5
}

/// <summary>
/// Cultural event load distribution request for diaspora communities
/// </summary>
public class CulturalEventLoadDistributionRequest
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    public List<GeographicRegion> TargetRegions { get; set; } = new();
    public int ExpectedParticipants { get; set; }
    public double TrafficMultiplier { get; set; } = 1.0;
    public SacredEventPriority Priority { get; set; }
    public Dictionary<string, object> LoadParameters { get; set; } = new();
}

/// <summary>
/// Cultural event load distribution response with routing recommendations
/// </summary>
public class CulturalEventLoadDistributionResponse
{
    public Guid ResponseId { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public DateTime ResponseTimestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public List<LoadBalancingRecommendation> LoadRecommendations { get; set; } = new();
    public List<GeographicRoutingResult> RoutingResults { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    public double CulturalAccuracyScore { get; set; }
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Load balancing recommendation for cultural events
/// </summary>
public class LoadBalancingRecommendation
{
    public GeographicRegion Region { get; set; }
    public int RecommendedInstances { get; set; }
    public double ExpectedLoad { get; set; }
    public double ConfidenceScore { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
}

/// <summary>
/// Geographic routing result for cultural event load distribution
/// </summary>
public class GeographicRoutingResult
{
    public GeographicRegion Region { get; set; }
    public bool IsRoutingSuccessful { get; set; }
    public double ResponseLatency { get; set; }
    public int AssignedInstances { get; set; }
    public double CulturalAffinityScore { get; set; }
    public string RoutingPath { get; set; } = string.Empty;
    public Dictionary<string, object> RoutingMetrics { get; set; } = new();
}