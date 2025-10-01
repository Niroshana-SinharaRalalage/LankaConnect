using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Endpoints;

/// <summary>
/// Represents a cultural intelligence API endpoint with cultural context awareness
/// </summary>
public class CulturalIntelligenceEndpoint
{
    public required string EndpointId { get; set; }
    public required string EndpointPath { get; set; }
    public required string EndpointName { get; set; }
    public required CulturalIntelligenceEndpointType EndpointType { get; set; }
    public required List<CulturalEventType> SupportedCulturalEvents { get; set; }
    public required CulturalSensitivityLevel SensitivityLevel { get; set; }
    public required Dictionary<string, string> CulturalParameters { get; set; }
    public required bool RequiresCulturalAuthentication { get; set; }
    public required List<string> RequiredPermissions { get; set; }
    public required TimeSpan CacheTimeout { get; set; }
    public required bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of cultural intelligence endpoints
/// </summary>
public enum CulturalIntelligenceEndpointType
{
    EventPrediction,
    CulturalAnalytics,
    FestivalNotification,
    CommunityInsights,
    TraditionMapping,
    LanguageDetection,
    CulturalRecommendation,
    SacredEventAlert
}

/// <summary>
/// Cultural sensitivity levels for endpoints
/// </summary>
public enum CulturalSensitivityLevel
{
    Public = 1,
    Community = 2,
    Sensitive = 3,
    Sacred = 4,
    Restricted = 5
}

/// <summary>
/// Cultural intelligence endpoint configuration
/// </summary>
public class CulturalIntelligenceEndpointConfiguration
{
    public required string ConfigurationId { get; set; }
    public required CulturalIntelligenceEndpoint Endpoint { get; set; }
    public required Dictionary<string, object> EndpointSettings { get; set; }
    public required bool EnableCulturalCaching { get; set; }
    public required bool EnableRateLimiting { get; set; }
    public required int MaxRequestsPerMinute { get; set; }
    public required List<string> AllowedCulturalContexts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}