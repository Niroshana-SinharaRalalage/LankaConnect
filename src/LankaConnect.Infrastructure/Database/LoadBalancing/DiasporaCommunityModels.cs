using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Infrastructure.Database.LoadBalancing;

/// <summary>
/// Result of identifying target diaspora communities for business optimization
/// Supports cultural business promotion across 6M+ South Asian American diaspora
/// </summary>
public class TargetDiasporaCommunitiesResult
{
    public DiasporaCommunityClustering PrimaryCommunityCluster { get; set; }
    public List<DiasporaCommunityClustering> SecondaryCommunityQueries { get; set; } = [];
    public Dictionary<DiasporaCommunityClustering, double> CulturalAlignmentScores { get; set; } = [];
    public double BusinessEngagementPotential { get; set; }
    public List<string> RecommendedMarketingStrategies { get; set; } = [];
}

/// <summary>
/// Community clustering density analysis for geographic optimization
/// Calculates population, cultural institution, and business directory density metrics
/// </summary>
public class CommunityClusteringDensityAnalysis
{
    public DiasporaCommunityClustering CommunityType { get; set; }
    public GeographicRegion Region { get; set; } = null!;
    public double PopulationDensity { get; set; }
    public double CulturalInstitutionDensity { get; set; }
    public double BusinessDirectoryDensity { get; set; }
    public double CommunityEngagementScore { get; set; }
    public double CulturalEventParticipationRate { get; set; }
    public double CrossCommunityConnectionPotential { get; set; }
    public double RevenueGenerationPotential { get; set; }
    public double OptimalLoadBalancingScore { get; set; }
}

/// <summary>
/// Regional cultural profile for cultural affinity routing
/// Supports dominant religions, languages, and cultural event patterns
/// </summary>
public class RegionCulturalProfile
{
    public string RegionName { get; set; } = string.Empty;
    public List<string> DominantReligions { get; set; } = [];
    public List<string> SupportedLanguages { get; set; } = [];
    public Dictionary<string, double> CulturalEventPatterns { get; set; } = [];
    public double CulturalDiversityIndex { get; set; }
    public int PopulationSize { get; set; }
}

/// <summary>
/// Diaspora load balancing request for cultural affinity routing
/// Integrates with 94% accuracy cultural affinity system
/// </summary>
public class DiasporaLoadBalancingRequest
{
    public Guid RequestId { get; set; }
    public CulturalCommunityType CulturalCommunityType { get; set; }
    public GeographicScope GeographicScope { get; set; }
    public int ExpectedConcurrentUsers { get; set; }
    public TimeSpan RequiredResponseTime { get; set; }
    public LoadBalancingStrategy LoadBalancingStrategy { get; set; }
    public LoadBalancingPriority PriorityLevel { get; set; }
}

/// <summary>
/// Diaspora load balancing response with optimal cultural affinity routes
/// Provides culturally-optimized server allocation for enhanced user engagement
/// </summary>
public class DiasporaLoadBalancingResponse
{
    public Guid RequestId { get; set; }
    public bool IsSuccessful { get; set; }
    public List<CulturalAffinityRoute> OptimalRoutes { get; set; } = [];
    public Dictionary<string, double> CulturalAffinityScores { get; set; } = [];
    public TimeSpan EstimatedResponseTime { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

// Supporting enums and types
/// <summary>
/// Cultural community types for diaspora segmentation
/// </summary>
public enum CulturalCommunityType
{
    SriLankanBuddhist,
    IndianHindu,
    PakistaniMuslim,
    SikhPunjabi,
    TamilHindu,
    BengaliHindu,
    MultiCultural
}

/// <summary>
/// Load balancing strategy for cultural affinity optimization
/// </summary>
public enum LoadBalancingStrategy
{
    CulturalAffinityOptimized,
    GeographicProximity,
    Hybrid
}

/// <summary>
/// Load balancing request priority levels
/// </summary>
public enum LoadBalancingPriority
{
    Low,
    Standard,
    High,
    Critical
}

/// <summary>
/// Cultural affinity route for optimized server selection
/// </summary>
public class CulturalAffinityRoute
{
    public string ServerEndpoint { get; set; } = string.Empty;
    public double CulturalAffinityScore { get; set; }
    public double GeographicProximityScore { get; set; }
    public int EstimatedLatencyMs { get; set; }
}
