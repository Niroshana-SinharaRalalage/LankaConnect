using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Shared;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Billing;

/// <summary>
/// Core billing service for Cultural Intelligence API monetization
/// Integrates with Stripe for advanced billing and subscription management
/// </summary>
public interface ICulturalIntelligenceBillingService
{
    Task<Result> CreateCulturalSubscriptionAsync(UserId userId, CulturalIntelligenceTier tier, CancellationToken cancellationToken = default);
    Task<Result> ProcessCulturalAPIUsageAsync(APIKey apiKey, CulturalIntelligenceRequest request, CancellationToken cancellationToken = default);
    Task<Result<BillingAnalytics>> GetCulturalRevenueAnalyticsAsync(TimeRange timeRange, CancellationToken cancellationToken = default);
    Task<Result> ProcessEnterpriseContractAsync(EnterpriseClient client, CulturalServicesContract contract, CancellationToken cancellationToken = default);
    Task<Result> ProcessBuddhistCalendarPremiumUsageAsync(APIKey apiKey, BuddhistCalendarRequest request, CancellationToken cancellationToken = default);
    Task<Result> ProcessCulturalAppropriatenesScoringAsync(APIKey apiKey, CulturalAppropriatenessRequest request, CancellationToken cancellationToken = default);
    Task<Result> ProcessDiasporaAnalyticsUsageAsync(APIKey apiKey, DiasporaAnalyticsRequest request, CancellationToken cancellationToken = default);
    Task<Result> ProcessPartnershipRevenueAsync(PartnershipId partnershipId, RevenueShare revenueShare, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cultural Intelligence API billing request
/// </summary>
public class CulturalIntelligenceRequest : ValueObject
{
    public APIEndpoint Endpoint { get; }
    public CulturalComplexityScore ComplexityScore { get; }
    public RequestMetadata Metadata { get; }
    public DateTime Timestamp { get; }

    public CulturalIntelligenceRequest(
        APIEndpoint endpoint, 
        CulturalComplexityScore complexityScore, 
        RequestMetadata metadata)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        ComplexityScore = complexityScore ?? throw new ArgumentNullException(nameof(complexityScore));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Timestamp = DateTime.UtcNow;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Endpoint;
        yield return ComplexityScore;
        yield return Metadata;
        yield return Timestamp;
    }
}

/// <summary>
/// Buddhist Calendar premium feature request
/// </summary>
public class BuddhistCalendarRequest : ValueObject
{
    public CalendarPrecisionLevel PrecisionLevel { get; }
    public AstronomicalCalculationType CalculationType { get; }
    public CustomCalendarVariation[] Variations { get; }
    public DateTime RequestedDate { get; }

    public BuddhistCalendarRequest(
        CalendarPrecisionLevel precisionLevel,
        AstronomicalCalculationType calculationType,
        CustomCalendarVariation[] variations,
        DateTime requestedDate)
    {
        PrecisionLevel = precisionLevel;
        CalculationType = calculationType;
        Variations = variations ?? Array.Empty<CustomCalendarVariation>();
        RequestedDate = requestedDate;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PrecisionLevel;
        yield return CalculationType;
        yield return RequestedDate;
        foreach (var variation in Variations)
            yield return variation;
    }
}

/// <summary>
/// Cultural Appropriateness scoring request
/// </summary>
public class CulturalAppropriatenessRequest : ValueObject
{
    public ContentToValidate Content { get; }
    public CulturalContext[] Contexts { get; }
    public ValidationComplexityLevel ComplexityLevel { get; }
    public RealTimeModeration RealTimeModeration { get; }

    public CulturalAppropriatenessRequest(
        ContentToValidate content,
        CulturalContext[] contexts,
        ValidationComplexityLevel complexityLevel,
        RealTimeModeration realTimeModeration)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Contexts = contexts ?? Array.Empty<CulturalContext>();
        ComplexityLevel = complexityLevel;
        RealTimeModeration = realTimeModeration;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Content;
        yield return ComplexityLevel;
        yield return RealTimeModeration;
        foreach (var context in Contexts)
            yield return context;
    }
}

/// <summary>
/// Diaspora community analytics request
/// </summary>
public class DiasporaAnalyticsRequest : ValueObject
{
    public AnalyticsType AnalyticsType { get; }
    public GeographicRegion[] Regions { get; }
    public CommunitySegment[] Segments { get; }
    public PredictionTimeframe Timeframe { get; }

    public DiasporaAnalyticsRequest(
        AnalyticsType analyticsType,
        GeographicRegion[] regions,
        CommunitySegment[] segments,
        PredictionTimeframe timeframe)
    {
        AnalyticsType = analyticsType;
        Regions = regions ?? Array.Empty<GeographicRegion>();
        Segments = segments ?? Array.Empty<CommunitySegment>();
        Timeframe = timeframe;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return AnalyticsType;
        yield return Timeframe;
        foreach (var region in Regions)
            yield return region;
        foreach (var segment in Segments)
            yield return segment;
    }
}

/// <summary>
/// API Key for authenticated billing
/// </summary>
public class APIKey : ValueObject
{
    public string Value { get; }
    public APIKeyTier Tier { get; }
    public UserId AssociatedUser { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ExpiresAt { get; }

    public APIKey(string value, APIKeyTier tier, UserId associatedUser, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("API Key value cannot be empty", nameof(value));

        Value = value;
        Tier = tier;
        AssociatedUser = associatedUser ?? throw new ArgumentNullException(nameof(associatedUser));
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Tier;
        yield return AssociatedUser;
    }
}

public enum APIKeyTier
{
    Community,
    Professional,
    Enterprise,
    Custom
}

public enum CalendarPrecisionLevel
{
    Basic,
    Advanced,
    AstronomicalPrecision,
    CustomVariation
}

public enum AstronomicalCalculationType
{
    LunarCalculation,
    SolarCalculation,
    PlanetaryAlignment,
    CustomAstronomical
}

public enum ValidationComplexityLevel
{
    Basic,
    Advanced,
    MultiCultural,
    RealTime
}

public enum AnalyticsType
{
    GeographicClustering,
    CommunityEngagement,
    CulturalTrendPrediction,
    CustomMarketResearch
}