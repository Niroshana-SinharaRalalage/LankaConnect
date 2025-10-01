namespace LankaConnect.Domain.Common;

/// <summary>
/// Cultural context-aware cache key value object
/// Ensures consistent cache key generation across cultural intelligence operations
/// </summary>
public class CulturalCacheKey : ValueObject
{
    public string Prefix { get; }
    public string CommunityId { get; }
    public string GeographicRegion { get; }
    public string Language { get; }
    public string? UserId { get; }
    public string DataType { get; }
    public Dictionary<string, string> AdditionalContext { get; }

    public CulturalCacheKey(
        string prefix,
        string communityId,
        string geographicRegion,
        string language,
        string dataType,
        string? userId = null,
        Dictionary<string, string>? additionalContext = null)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Cache key prefix cannot be empty", nameof(prefix));
        
        if (string.IsNullOrWhiteSpace(communityId))
            throw new ArgumentException("Community ID cannot be empty", nameof(communityId));
        
        if (string.IsNullOrWhiteSpace(geographicRegion))
            throw new ArgumentException("Geographic region cannot be empty", nameof(geographicRegion));
        
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be empty", nameof(language));
        
        if (string.IsNullOrWhiteSpace(dataType))
            throw new ArgumentException("Data type cannot be empty", nameof(dataType));

        Prefix = prefix;
        CommunityId = communityId;
        GeographicRegion = geographicRegion;
        Language = language;
        UserId = userId;
        DataType = dataType;
        AdditionalContext = additionalContext ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Generates the complete cache key string
    /// Format: prefix:community:region:language:datatype:userid:hash
    /// </summary>
    public string GenerateKey()
    {
        var keyComponents = new List<string>
        {
            Prefix,
            CommunityId,
            GeographicRegion,
            Language,
            DataType
        };

        if (!string.IsNullOrWhiteSpace(UserId))
        {
            keyComponents.Add(UserId);
        }

        // Add additional context as hash to keep key length manageable
        if (AdditionalContext.Any())
        {
            var contextHash = GenerateContextHash(AdditionalContext);
            keyComponents.Add(contextHash);
        }

        return string.Join(":", keyComponents.Select(EscapeCacheKeyComponent));
    }

    /// <summary>
    /// Generates a pattern key for bulk operations (e.g., cache invalidation)
    /// </summary>
    /// <param name="wildcardUserId">Whether to wildcard the user ID component</param>
    /// <param name="wildcardContext">Whether to wildcard the additional context</param>
    public string GeneratePatternKey(bool wildcardUserId = false, bool wildcardContext = false)
    {
        var keyComponents = new List<string>
        {
            Prefix,
            CommunityId,
            GeographicRegion,
            Language,
            DataType
        };

        if (wildcardUserId || string.IsNullOrWhiteSpace(UserId))
        {
            keyComponents.Add("*");
        }
        else
        {
            keyComponents.Add(UserId);
        }

        if (wildcardContext || !AdditionalContext.Any())
        {
            keyComponents.Add("*");
        }
        else
        {
            var contextHash = GenerateContextHash(AdditionalContext);
            keyComponents.Add(contextHash);
        }

        return string.Join(":", keyComponents.Select(EscapeCacheKeyComponent));
    }

    /// <summary>
    /// Creates cache key for Buddhist calendar queries
    /// </summary>
    public static CulturalCacheKey ForBuddhistCalendar(string region, string language, DateTime date, string? userId = null)
    {
        var additionalContext = new Dictionary<string, string>
        {
            ["date"] = date.ToString("yyyy-MM-dd"),
            ["calendar_type"] = "buddhist"
        };

        return new CulturalCacheKey(
            "cal_buddhist",
            "buddhist",
            region,
            language,
            "calendar",
            userId,
            additionalContext);
    }

    /// <summary>
    /// Creates cache key for Hindu calendar queries
    /// </summary>
    public static CulturalCacheKey ForHinduCalendar(string region, string language, DateTime date, string? userId = null)
    {
        var additionalContext = new Dictionary<string, string>
        {
            ["date"] = date.ToString("yyyy-MM-dd"),
            ["calendar_type"] = "hindu"
        };

        return new CulturalCacheKey(
            "cal_hindu",
            "hindu",
            region,
            language,
            "calendar",
            userId,
            additionalContext);
    }

    /// <summary>
    /// Creates cache key for cultural appropriateness scoring
    /// </summary>
    public static CulturalCacheKey ForCulturalAppropriateness(string communityId, string region, string contentHash, string? userId = null)
    {
        var additionalContext = new Dictionary<string, string>
        {
            ["content_hash"] = contentHash,
            ["scoring_model"] = "v2.1"
        };

        return new CulturalCacheKey(
            "cultural_score",
            communityId,
            region,
            "multi",
            "appropriateness",
            userId,
            additionalContext);
    }

    /// <summary>
    /// Creates cache key for diaspora community analytics
    /// </summary>
    public static CulturalCacheKey ForDiasporaAnalytics(string communityId, string region, string analyticsType, string? userId = null)
    {
        var additionalContext = new Dictionary<string, string>
        {
            ["analytics_type"] = analyticsType,
            ["aggregation_level"] = "community"
        };

        return new CulturalCacheKey(
            "diaspora_analytics",
            communityId,
            region,
            "multi",
            "analytics",
            userId,
            additionalContext);
    }

    /// <summary>
    /// Creates cache key for cultural event recommendations
    /// </summary>
    public static CulturalCacheKey ForEventRecommendations(string communityId, string region, string language, string userId)
    {
        var additionalContext = new Dictionary<string, string>
        {
            ["recommendation_engine"] = "cultural_ml_v1.0",
            ["personalization"] = "enabled"
        };

        return new CulturalCacheKey(
            "event_recommendations",
            communityId,
            region,
            language,
            "recommendations",
            userId,
            additionalContext);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Prefix;
        yield return CommunityId;
        yield return GeographicRegion;
        yield return Language;
        yield return UserId ?? string.Empty;
        yield return DataType;
        
        foreach (var kvp in AdditionalContext.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }

    private static string GenerateContextHash(Dictionary<string, string> context)
    {
        var sortedContext = context.OrderBy(x => x.Key).ToList();
        var contextString = string.Join("|", sortedContext.Select(x => $"{x.Key}={x.Value}"));
        
        // Simple hash for context - in production, consider using a more robust hash
        return Math.Abs(contextString.GetHashCode()).ToString("x8");
    }

    private static string EscapeCacheKeyComponent(string component)
    {
        // Replace problematic characters in cache keys
        return component.Replace(":", "_colon_")
                      .Replace("*", "_star_")
                      .Replace(" ", "_space_")
                      .Replace("/", "_slash_")
                      .Replace("\\", "_backslash_");
    }
}