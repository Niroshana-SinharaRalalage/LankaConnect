using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Queries;

/// <summary>
/// Cacheable query for cultural appropriateness scoring
/// Uses ML model predictions with intelligent caching for performance
/// </summary>
public class CulturalAppropriatenessQuery : IQuery<CulturalAppropriatenessResponse>, ICacheableQuery
{
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // "text", "image", "video", etc.
    public string CommunityId { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public Dictionary<string, string> AdditionalContext { get; set; } = new();

    public string GetCacheKey()
    {
        // Generate hash of content to create stable cache key
        var contentHash = GenerateContentHash(Content, ContentType, AdditionalContext);
        
        return CulturalCacheKey
            .ForCulturalAppropriateness(CommunityId, GeographicRegion, contentHash, UserId)
            .GenerateKey();
    }

    public TimeSpan GetCacheTtl()
    {
        // Cultural appropriateness scoring can change with model updates
        // But for the same content, results should be stable for a reasonable time
        return string.IsNullOrEmpty(UserId) 
            ? TimeSpan.FromHours(12) // Anonymous queries cache longer
            : TimeSpan.FromHours(4);  // User-specific queries cache shorter
    }

    public bool ShouldCache()
    {
        // Always cache appropriateness queries as ML inference is expensive
        // Skip caching only for very short content or test scenarios
        return !string.IsNullOrWhiteSpace(Content) && Content.Length > 10;
    }

    private static string GenerateContentHash(string content, string contentType, Dictionary<string, string> context)
    {
        var combinedInput = $"{content}|{contentType}|{string.Join("|", context.Select(x => $"{x.Key}={x.Value}"))}";
        return Math.Abs(combinedInput.GetHashCode()).ToString("x8");
    }
}

public class CulturalAppropriatenessResponse
{
    public double AppropriatenessScore { get; set; } // 0.0 to 1.0
    public AppropriatenessLevel Level { get; set; }
    public List<CulturalConcern> Concerns { get; set; } = new();
    public List<CulturalRecommendation> Recommendations { get; set; } = new();
    public string CulturalContext { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime AssessmentTime { get; set; } = DateTime.UtcNow;
    public CommunitySpecificAnalysis CommunityAnalysis { get; set; } = new();
}

public enum AppropriatenessLevel
{
    HighlyAppropriate,
    Appropriate,
    MildConcern,
    ModerateConcern,
    HighConcern,
    Inappropriate
}

public class CulturalConcern
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Severity { get; set; } // 0.0 to 1.0
    public string CulturalBackground { get; set; } = string.Empty;
    public List<string> AffectedCommunities { get; set; } = new();
}

public class CulturalRecommendation
{
    public string Type { get; set; } = string.Empty; // "modify", "context", "disclaimer", etc.
    public string Description { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public double ImpactScore { get; set; } // Expected improvement if followed
}

public class CommunitySpecificAnalysis
{
    public Dictionary<string, double> CommunityScores { get; set; } = new();
    public List<string> CulturalNuances { get; set; } = new();
    public Dictionary<string, string> RegionalConsiderations { get; set; } = new();
}