using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

/// <summary>
/// Enterprise-grade cultural intelligence API request with complexity scoring and SLA requirements
/// </summary>
public class EnterpriseCulturalRequest : ValueObject
{
    public EnterpriseAPIKey ApiKey { get; }
    public CulturalIntelligenceEndpoint Endpoint { get; }
    public EnterpriseComplexityLevel ComplexityLevel { get; }
    public SLAResponseTime RequiredResponseTime { get; }
    public CulturalValidationLevel ValidationLevel { get; }
    public RequestMetadata Metadata { get; }
    public DateTime RequestTimestamp { get; }
    public string CorrelationId { get; }
    public EnterpriseClientId ClientId { get; }

    public EnterpriseCulturalRequest(
        EnterpriseAPIKey apiKey,
        CulturalIntelligenceEndpoint endpoint,
        EnterpriseComplexityLevel complexityLevel,
        SLAResponseTime requiredResponseTime,
        CulturalValidationLevel validationLevel,
        RequestMetadata metadata,
        EnterpriseClientId clientId,
        string? correlationId = null)
    {
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        ComplexityLevel = complexityLevel ?? throw new ArgumentNullException(nameof(complexityLevel));
        RequiredResponseTime = requiredResponseTime ?? throw new ArgumentNullException(nameof(requiredResponseTime));
        ValidationLevel = validationLevel ?? throw new ArgumentNullException(nameof(validationLevel));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        RequestTimestamp = DateTime.UtcNow;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    }

    public bool RequiresRealTimeProcessing => RequiredResponseTime.IsRealTime;
    public bool RequiresAdvancedValidation => ValidationLevel.IsAdvanced;
    public bool IsHighComplexity => ComplexityLevel.IsHigh;
    public bool RequiresSLAMonitoring => RequiredResponseTime.RequiresMonitoring;
    public TimeSpan MaxAllowedProcessingTime => RequiredResponseTime.MaxProcessingTime;
    public int EstimatedProcessingCost => ComplexityLevel.EstimatedCost + ValidationLevel.EstimatedCost;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ApiKey;
        yield return Endpoint;
        yield return ComplexityLevel;
        yield return RequiredResponseTime;
        yield return ValidationLevel;
        yield return ClientId;
        yield return CorrelationId;
    }
}

/// <summary>
/// Enterprise API Key with advanced authentication and rate limiting capabilities
/// </summary>
public class EnterpriseAPIKey : ValueObject
{
    public string KeyValue { get; }
    public APIKeyTier Tier { get; }
    public EnterpriseClientId ClientId { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ExpiresAt { get; }
    public List<string> AllowedEndpoints { get; }
    public List<string> AllowedIPAddresses { get; }
    public bool IsActive { get; }
    public int DailyRequestLimit { get; }
    public int CurrentDailyUsage { get; private set; }

    public EnterpriseAPIKey(
        string keyValue,
        APIKeyTier tier,
        EnterpriseClientId clientId,
        List<string>? allowedEndpoints = null,
        List<string>? allowedIPAddresses = null,
        DateTime? expiresAt = null,
        int dailyRequestLimit = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(keyValue))
            throw new ArgumentException("API key value cannot be empty.", nameof(keyValue));
            
        if (keyValue.Length < 32)
            throw new ArgumentException("API key must be at least 32 characters.", nameof(keyValue));

        KeyValue = keyValue;
        Tier = tier;
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        AllowedEndpoints = allowedEndpoints ?? new List<string>();
        AllowedIPAddresses = allowedIPAddresses ?? new List<string>();
        IsActive = true;
        DailyRequestLimit = dailyRequestLimit;
        CurrentDailyUsage = 0;
    }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public bool IsValid => IsActive && !IsExpired;
    public bool HasEndpointRestrictions => AllowedEndpoints.Any();
    public bool HasIPRestrictions => AllowedIPAddresses.Any();
    public bool IsEnterpriseTier => Tier == APIKeyTier.Enterprise || Tier == APIKeyTier.Fortune500;
    public bool IsUnlimited => DailyRequestLimit == int.MaxValue;
    public double DailyUsagePercentage => IsUnlimited ? 0 : (double)CurrentDailyUsage / DailyRequestLimit * 100;

    public void RecordUsage(int requestCount = 1)
    {
        if (!IsValid)
            throw new InvalidOperationException("Cannot record usage for invalid API key.");
            
        CurrentDailyUsage += requestCount;
    }

    public void ResetDailyUsage()
    {
        CurrentDailyUsage = 0;
    }

    public bool CanAccessEndpoint(string endpoint)
    {
        return !HasEndpointRestrictions || AllowedEndpoints.Contains(endpoint, StringComparer.OrdinalIgnoreCase);
    }

    public bool CanAccessFromIP(string ipAddress)
    {
        return !HasIPRestrictions || AllowedIPAddresses.Contains(ipAddress);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return KeyValue;
        yield return Tier;
        yield return ClientId;
    }
}

public enum APIKeyTier
{
    Standard,
    Professional,
    Enterprise,
    Fortune500,
    Government,
    Educational
}

/// <summary>
/// Cultural Intelligence API endpoint specification
/// </summary>
public class CulturalIntelligenceEndpoint : ValueObject
{
    public string Path { get; }
    public string Method { get; }
    public EndpointCategory Category { get; }
    public bool RequiresAuthentication { get; }
    public bool RequiresEnterpriseTier { get; }
    public int BaseComplexityScore { get; }
    public TimeSpan EstimatedProcessingTime { get; }

    public CulturalIntelligenceEndpoint(
        string path,
        string method,
        EndpointCategory category,
        bool requiresAuthentication = true,
        bool requiresEnterpriseTier = false,
        int baseComplexityScore = 1,
        TimeSpan? estimatedProcessingTime = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Endpoint path cannot be empty.", nameof(path));
            
        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("HTTP method cannot be empty.", nameof(method));

        Path = path;
        Method = method.ToUpper();
        Category = category;
        RequiresAuthentication = requiresAuthentication;
        RequiresEnterpriseTier = requiresEnterpriseTier;
        BaseComplexityScore = Math.Max(1, baseComplexityScore);
        EstimatedProcessingTime = estimatedProcessingTime ?? TimeSpan.FromMilliseconds(200);
    }

    public static CulturalIntelligenceEndpoint BuddhistCalendar(string specificPath = "/cultural/buddhist-calendar")
        => new(specificPath, "GET", EndpointCategory.BuddhistCalendar, true, true, 3, TimeSpan.FromMilliseconds(150));

    public static CulturalIntelligenceEndpoint HinduCalendar(string specificPath = "/cultural/hindu-calendar")
        => new(specificPath, "GET", EndpointCategory.HinduCalendar, true, true, 3, TimeSpan.FromMilliseconds(150));

    public static CulturalIntelligenceEndpoint CulturalAppropriateness(string specificPath = "/cultural/appropriateness")
        => new(specificPath, "POST", EndpointCategory.CulturalAppropriateness, true, true, 5, TimeSpan.FromMilliseconds(400));

    public static CulturalIntelligenceEndpoint DiasporaAnalytics(string specificPath = "/cultural/diaspora-analytics")
        => new(specificPath, "POST", EndpointCategory.DiasporaAnalytics, true, true, 8, TimeSpan.FromMilliseconds(800));

    public static CulturalIntelligenceEndpoint EventRecommendations(string specificPath = "/cultural/event-recommendations")
        => new(specificPath, "POST", EndpointCategory.EventRecommendations, true, false, 4, TimeSpan.FromMilliseconds(300));

    public bool IsCalendarEndpoint => Category == EndpointCategory.BuddhistCalendar || Category == EndpointCategory.HinduCalendar;
    public bool IsAnalyticsEndpoint => Category == EndpointCategory.DiasporaAnalytics;
    public bool IsHighComplexity => BaseComplexityScore >= 5;
    public bool RequiresSpecializedProcessing => RequiresEnterpriseTier;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Path;
        yield return Method;
        yield return Category;
    }
}

public enum EndpointCategory
{
    BuddhistCalendar,
    HinduCalendar,
    CulturalAppropriateness,
    DiasporaAnalytics,
    EventRecommendations,
    CommunityEngagement,
    CulturalTrends,
    CustomAI
}

/// <summary>
/// Enterprise complexity level for cultural intelligence processing
/// </summary>
public class EnterpriseComplexityLevel : ValueObject
{
    public ComplexityTier Tier { get; }
    public int ComplexityScore { get; }
    public List<ComplexityFactor> Factors { get; }
    public TimeSpan EstimatedProcessingTime { get; }
    public int EstimatedCost { get; }
    public bool RequiresAdvancedAI { get; }

    public EnterpriseComplexityLevel(
        ComplexityTier tier,
        int complexityScore,
        List<ComplexityFactor>? factors = null,
        bool requiresAdvancedAI = false)
    {
        if (complexityScore <= 0)
            throw new ArgumentException("Complexity score must be positive.", nameof(complexityScore));

        Tier = tier;
        ComplexityScore = complexityScore;
        Factors = factors ?? new List<ComplexityFactor>();
        RequiresAdvancedAI = requiresAdvancedAI;
        
        EstimatedProcessingTime = CalculateEstimatedTime();
        EstimatedCost = CalculateEstimatedCost();
    }

    private TimeSpan CalculateEstimatedTime()
    {
        var baseTime = Tier switch
        {
            ComplexityTier.Low => TimeSpan.FromMilliseconds(100),
            ComplexityTier.Medium => TimeSpan.FromMilliseconds(300),
            ComplexityTier.High => TimeSpan.FromMilliseconds(800),
            ComplexityTier.VeryHigh => TimeSpan.FromSeconds(2),
            ComplexityTier.Extreme => TimeSpan.FromSeconds(5),
            _ => TimeSpan.FromMilliseconds(300)
        };

        var multiplier = RequiresAdvancedAI ? 2.0 : 1.0;
        return TimeSpan.FromMilliseconds(baseTime.TotalMilliseconds * multiplier);
    }

    private int CalculateEstimatedCost()
    {
        var baseCost = Tier switch
        {
            ComplexityTier.Low => 1,
            ComplexityTier.Medium => 3,
            ComplexityTier.High => 7,
            ComplexityTier.VeryHigh => 15,
            ComplexityTier.Extreme => 30,
            _ => 3
        };

        var aiMultiplier = RequiresAdvancedAI ? 3 : 1;
        var factorMultiplier = Math.Max(1, Factors.Count);
        
        return baseCost * aiMultiplier * factorMultiplier;
    }

    public static EnterpriseComplexityLevel Low(List<ComplexityFactor>? factors = null)
        => new(ComplexityTier.Low, 1, factors);

    public static EnterpriseComplexityLevel Medium(List<ComplexityFactor>? factors = null)
        => new(ComplexityTier.Medium, 3, factors);

    public static EnterpriseComplexityLevel High(List<ComplexityFactor>? factors = null, bool requiresAdvancedAI = false)
        => new(ComplexityTier.High, 7, factors, requiresAdvancedAI);

    public static EnterpriseComplexityLevel VeryHigh(List<ComplexityFactor>? factors = null, bool requiresAdvancedAI = true)
        => new(ComplexityTier.VeryHigh, 15, factors, requiresAdvancedAI);

    public static EnterpriseComplexityLevel Extreme(List<ComplexityFactor>? factors = null, bool requiresAdvancedAI = true)
        => new(ComplexityTier.Extreme, 30, factors, requiresAdvancedAI);

    public bool IsLow => Tier == ComplexityTier.Low;
    public bool IsMedium => Tier == ComplexityTier.Medium;
    public bool IsHigh => Tier >= ComplexityTier.High;
    public bool RequiresSpecializedProcessing => Tier >= ComplexityTier.VeryHigh;
    public bool RequiresPriorityQueue => Tier >= ComplexityTier.High;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Tier;
        yield return ComplexityScore;
        yield return RequiresAdvancedAI;
    }
}

public enum ComplexityTier
{
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4,
    Extreme = 5
}

public class ComplexityFactor : ValueObject
{
    public string Name { get; }
    public int ImpactScore { get; }
    public string Description { get; }

    public ComplexityFactor(string name, int impactScore, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Factor name cannot be empty.", nameof(name));
            
        if (impactScore <= 0)
            throw new ArgumentException("Impact score must be positive.", nameof(impactScore));

        Name = name;
        ImpactScore = impactScore;
        Description = description;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return ImpactScore;
    }
}

/// <summary>
/// SLA-compliant response time requirements
/// </summary>
public class SLAResponseTime : ValueObject
{
    public TimeSpan MaxProcessingTime { get; }
    public TimeSpan WarningThreshold { get; }
    public ResponseTimeTier Tier { get; }
    public bool RequiresMonitoring { get; }
    public bool RequiresAlerts { get; }

    public SLAResponseTime(
        TimeSpan maxProcessingTime,
        ResponseTimeTier tier,
        bool requiresMonitoring = true,
        bool requiresAlerts = true)
    {
        if (maxProcessingTime <= TimeSpan.Zero)
            throw new ArgumentException("Max processing time must be positive.", nameof(maxProcessingTime));

        MaxProcessingTime = maxProcessingTime;
        WarningThreshold = TimeSpan.FromMilliseconds(maxProcessingTime.TotalMilliseconds * 0.8); // 80% threshold
        Tier = tier;
        RequiresMonitoring = requiresMonitoring;
        RequiresAlerts = requiresAlerts;
    }

    public static SLAResponseTime RealTime() => new(TimeSpan.FromMilliseconds(100), ResponseTimeTier.RealTime);
    public static SLAResponseTime Enterprise() => new(TimeSpan.FromMilliseconds(200), ResponseTimeTier.Enterprise);
    public static SLAResponseTime Premium() => new(TimeSpan.FromMilliseconds(500), ResponseTimeTier.Premium);
    public static SLAResponseTime Standard() => new(TimeSpan.FromMilliseconds(1000), ResponseTimeTier.Standard, false, false);

    public bool IsRealTime => Tier == ResponseTimeTier.RealTime;
    public bool IsEnterprise => Tier == ResponseTimeTier.Enterprise;
    public bool IsViolated(TimeSpan actualTime) => actualTime > MaxProcessingTime;
    public bool IsNearViolation(TimeSpan actualTime) => actualTime > WarningThreshold;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return MaxProcessingTime;
        yield return Tier;
    }
}

public enum ResponseTimeTier
{
    Standard,
    Premium,
    Enterprise,
    RealTime
}

/// <summary>
/// Cultural validation level for enterprise requests
/// </summary>
public class CulturalValidationLevel : ValueObject
{
    public ValidationTier Tier { get; }
    public int ValidationDepth { get; }
    public bool RequiresHumanReview { get; }
    public bool RequiresCommunityValidation { get; }
    public List<string> RequiredValidationSteps { get; }
    public int EstimatedCost { get; }

    public CulturalValidationLevel(
        ValidationTier tier,
        int validationDepth,
        bool requiresHumanReview = false,
        bool requiresCommunityValidation = false,
        List<string>? requiredValidationSteps = null)
    {
        if (validationDepth <= 0)
            throw new ArgumentException("Validation depth must be positive.", nameof(validationDepth));

        Tier = tier;
        ValidationDepth = validationDepth;
        RequiresHumanReview = requiresHumanReview;
        RequiresCommunityValidation = requiresCommunityValidation;
        RequiredValidationSteps = requiredValidationSteps ?? new List<string>();
        EstimatedCost = CalculateEstimatedCost();
    }

    private int CalculateEstimatedCost()
    {
        var baseCost = Tier switch
        {
            ValidationTier.Basic => 1,
            ValidationTier.Standard => 2,
            ValidationTier.Advanced => 5,
            ValidationTier.Expert => 10,
            ValidationTier.CommunityValidated => 15,
            _ => 2
        };

        var humanReviewMultiplier = RequiresHumanReview ? 3 : 1;
        var communityMultiplier = RequiresCommunityValidation ? 2 : 1;
        
        return baseCost * humanReviewMultiplier * communityMultiplier;
    }

    public static CulturalValidationLevel Basic() 
        => new(ValidationTier.Basic, 1);

    public static CulturalValidationLevel Standard() 
        => new(ValidationTier.Standard, 3);

    public static CulturalValidationLevel Advanced(bool requiresHumanReview = false) 
        => new(ValidationTier.Advanced, 5, requiresHumanReview);

    public static CulturalValidationLevel Expert(bool requiresHumanReview = true) 
        => new(ValidationTier.Expert, 8, requiresHumanReview);

    public static CulturalValidationLevel CommunityValidated() 
        => new(ValidationTier.CommunityValidated, 10, true, true);

    public bool IsBasic => Tier == ValidationTier.Basic;
    public bool IsAdvanced => Tier >= ValidationTier.Advanced;
    public bool RequiresSpecializedReview => RequiresHumanReview || RequiresCommunityValidation;
    public bool IsHighCost => EstimatedCost >= 10;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Tier;
        yield return ValidationDepth;
        yield return RequiresHumanReview;
        yield return RequiresCommunityValidation;
    }
}

public enum ValidationTier
{
    Basic,
    Standard,
    Advanced,
    Expert,
    CommunityValidated
}

/// <summary>
/// Request metadata for enterprise cultural intelligence requests
/// </summary>
public class RequestMetadata : ValueObject
{
    public Dictionary<string, object> Properties { get; }
    public string ClientVersion { get; }
    public string UserAgent { get; }
    public string SourceIP { get; }
    public List<string> Tags { get; }
    public DateTime CreatedAt { get; }

    public RequestMetadata(
        Dictionary<string, object> properties,
        string? clientVersion = null,
        string? userAgent = null,
        string? sourceIP = null,
        List<string>? tags = null)
    {
        Properties = properties ?? new Dictionary<string, object>();
        ClientVersion = clientVersion ?? "1.0.0";
        UserAgent = userAgent ?? "LankaConnect-Enterprise-Client";
        SourceIP = sourceIP ?? "Unknown";
        Tags = tags ?? new List<string>();
        CreatedAt = DateTime.UtcNow;
    }

    public bool HasProperty(string key) => Properties.ContainsKey(key);
    public T? GetProperty<T>(string key, T? defaultValue = default) 
        => Properties.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;
    public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    public bool IsFromKnownSource => !string.IsNullOrWhiteSpace(SourceIP);
    public int PropertyCount => Properties.Count;
    public int TagCount => Tags.Count;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Properties.Count;
        yield return ClientVersion;
        yield return UserAgent;
        yield return SourceIP ?? string.Empty;
    }
}