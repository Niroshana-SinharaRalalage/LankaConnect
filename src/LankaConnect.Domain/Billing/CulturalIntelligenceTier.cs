using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Billing;

/// <summary>
/// Cultural Intelligence API pricing tier with sophisticated feature access
/// </summary>
public class CulturalIntelligenceTier : ValueObject
{
    public TierName Name { get; }
    public MonthlyPrice BasePrice { get; }
    public APIRequestLimit RequestLimit { get; }
    public CulturalFeatureAccess FeatureAccess { get; }
    public UsageBasedPricing UsagePricing { get; }
    public SLA ServiceLevel { get; }

    public CulturalIntelligenceTier(
        TierName name,
        MonthlyPrice basePrice,
        APIRequestLimit requestLimit,
        CulturalFeatureAccess featureAccess,
        UsageBasedPricing usagePricing,
        SLA serviceLevel)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
        RequestLimit = requestLimit ?? throw new ArgumentNullException(nameof(requestLimit));
        FeatureAccess = featureAccess ?? throw new ArgumentNullException(nameof(featureAccess));
        UsagePricing = usagePricing ?? throw new ArgumentNullException(nameof(usagePricing));
        ServiceLevel = serviceLevel ?? throw new ArgumentNullException(nameof(serviceLevel));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return BasePrice;
        yield return RequestLimit;
        yield return FeatureAccess;
        yield return UsagePricing;
        yield return ServiceLevel;
    }

    public static CulturalIntelligenceTier CreateCommunityTier()
    {
        return new CulturalIntelligenceTier(
            new TierName("Community"),
            MonthlyPrice.Free(),
            APIRequestLimit.Create(1000),
            CulturalFeatureAccess.BasicAccess(),
            UsageBasedPricing.Free(),
            SLA.CommunitySupport()
        );
    }

    public static CulturalIntelligenceTier CreateProfessionalTier()
    {
        return new CulturalIntelligenceTier(
            new TierName("Professional"),
            MonthlyPrice.Create(99.00m),
            APIRequestLimit.Create(25000),
            CulturalFeatureAccess.ProfessionalAccess(),
            UsageBasedPricing.CreateProfessional(),
            SLA.BusinessSupport()
        );
    }

    public static CulturalIntelligenceTier CreateEnterpriseTier()
    {
        return new CulturalIntelligenceTier(
            new TierName("Enterprise"),
            MonthlyPrice.Create(999.00m),
            APIRequestLimit.Unlimited(),
            CulturalFeatureAccess.EnterpriseAccess(),
            UsageBasedPricing.CreateEnterprise(),
            SLA.EnterpriseSupport()
        );
    }

    public static CulturalIntelligenceTier CreateCustomTier(
        decimal basePrice, 
        int requestLimit, 
        CulturalFeatureAccess featureAccess,
        UsageBasedPricing usagePricing)
    {
        return new CulturalIntelligenceTier(
            new TierName("Custom"),
            MonthlyPrice.Create(basePrice),
            APIRequestLimit.Create(requestLimit),
            featureAccess,
            usagePricing,
            SLA.CustomSupport()
        );
    }
}

/// <summary>
/// Tier name with validation
/// </summary>
public class TierName : ValueObject
{
    public string Value { get; }

    public TierName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tier name cannot be empty", nameof(value));

        Value = value;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(TierName tierName) => tierName.Value;
    public static implicit operator TierName(string value) => new(value);
}

/// <summary>
/// Monthly pricing with special handling for free tier
/// </summary>
public class MonthlyPrice : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }
    public bool IsFree { get; }

    private MonthlyPrice(decimal amount, Currency currency, bool isFree = false)
    {
        if (amount < 0)
            throw new ArgumentException("Price cannot be negative", nameof(amount));

        Amount = amount;
        Currency = currency ?? new Currency("USD");
        IsFree = isFree;
    }

    public static MonthlyPrice Free() => new(0, new Currency("USD"), true);
    public static MonthlyPrice Create(decimal amount, Currency? currency = null) 
        => new(amount, currency ?? new Currency("USD"));

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
        yield return IsFree;
    }
}

/// <summary>
/// API request limits with unlimited option
/// </summary>
public class APIRequestLimit : ValueObject
{
    public int Limit { get; }
    public bool IsUnlimited { get; }

    private APIRequestLimit(int limit, bool isUnlimited = false)
    {
        if (!isUnlimited && limit <= 0)
            throw new ArgumentException("Request limit must be positive", nameof(limit));

        Limit = limit;
        IsUnlimited = isUnlimited;
    }

    public static APIRequestLimit Create(int limit) => new(limit);
    public static APIRequestLimit Unlimited() => new(int.MaxValue, true);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Limit;
        yield return IsUnlimited;
    }
}

/// <summary>
/// Cultural feature access levels
/// </summary>
public class CulturalFeatureAccess : ValueObject
{
    public bool BasicCalendarAccess { get; }
    public bool BuddhistCalendarPremium { get; }
    public bool HinduCalendarPremium { get; }
    public bool AIRecommendations { get; }
    public bool CulturalAppropriatenessScoring { get; }
    public bool DiasporaAnalytics { get; }
    public bool MultiLanguageSupport { get; }
    public bool WebhookSupport { get; }
    public bool CustomAIModels { get; }
    public bool WhiteLabelLicensing { get; }
    public bool CulturalConsultingServices { get; }

    public CulturalFeatureAccess(
        bool basicCalendarAccess = true,
        bool buddhistCalendarPremium = false,
        bool hinduCalendarPremium = false,
        bool aiRecommendations = false,
        bool culturalAppropriatenessScoring = false,
        bool diasporaAnalytics = false,
        bool multiLanguageSupport = false,
        bool webhookSupport = false,
        bool customAIModels = false,
        bool whiteLabelLicensing = false,
        bool culturalConsultingServices = false)
    {
        BasicCalendarAccess = basicCalendarAccess;
        BuddhistCalendarPremium = buddhistCalendarPremium;
        HinduCalendarPremium = hinduCalendarPremium;
        AIRecommendations = aiRecommendations;
        CulturalAppropriatenessScoring = culturalAppropriatenessScoring;
        DiasporaAnalytics = diasporaAnalytics;
        MultiLanguageSupport = multiLanguageSupport;
        WebhookSupport = webhookSupport;
        CustomAIModels = customAIModels;
        WhiteLabelLicensing = whiteLabelLicensing;
        CulturalConsultingServices = culturalConsultingServices;
    }

    public static CulturalFeatureAccess BasicAccess() => new(
        basicCalendarAccess: true
    );

    public static CulturalFeatureAccess ProfessionalAccess() => new(
        basicCalendarAccess: true,
        buddhistCalendarPremium: true,
        hinduCalendarPremium: true,
        aiRecommendations: true,
        culturalAppropriatenessScoring: true,
        multiLanguageSupport: true,
        webhookSupport: true
    );

    public static CulturalFeatureAccess EnterpriseAccess() => new(
        basicCalendarAccess: true,
        buddhistCalendarPremium: true,
        hinduCalendarPremium: true,
        aiRecommendations: true,
        culturalAppropriatenessScoring: true,
        diasporaAnalytics: true,
        multiLanguageSupport: true,
        webhookSupport: true,
        customAIModels: true,
        whiteLabelLicensing: true,
        culturalConsultingServices: true
    );

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return BasicCalendarAccess;
        yield return BuddhistCalendarPremium;
        yield return HinduCalendarPremium;
        yield return AIRecommendations;
        yield return CulturalAppropriatenessScoring;
        yield return DiasporaAnalytics;
        yield return MultiLanguageSupport;
        yield return WebhookSupport;
        yield return CustomAIModels;
        yield return WhiteLabelLicensing;
        yield return CulturalConsultingServices;
    }
}

/// <summary>
/// Usage-based pricing for premium cultural intelligence features
/// </summary>
public class UsageBasedPricing : ValueObject
{
    public PricePer CulturalAppropriatenessValidation { get; }
    public PricePer DiasporaAnalysis { get; }
    public PricePer MultiLanguageTranslation { get; }
    public PricePer CulturalConflictResolution { get; }
    public PricePer CustomMarketResearch { get; }

    public UsageBasedPricing(
        PricePer culturalAppropriatenessValidation,
        PricePer diasporaAnalysis,
        PricePer multiLanguageTranslation,
        PricePer culturalConflictResolution,
        PricePer customMarketResearch)
    {
        CulturalAppropriatenessValidation = culturalAppropriatenessValidation ?? PricePer.Free();
        DiasporaAnalysis = diasporaAnalysis ?? PricePer.Free();
        MultiLanguageTranslation = multiLanguageTranslation ?? PricePer.Free();
        CulturalConflictResolution = culturalConflictResolution ?? PricePer.Free();
        CustomMarketResearch = customMarketResearch ?? PricePer.Free();
    }

    public static UsageBasedPricing Free() => new(
        PricePer.Free(),
        PricePer.Free(),
        PricePer.Free(),
        PricePer.Free(),
        PricePer.Free()
    );

    public static UsageBasedPricing CreateProfessional() => new(
        PricePer.Create(0.10m, "validation"),
        PricePer.Create(0.25m, "analysis"),
        PricePer.Create(0.15m, "translation"),
        PricePer.Create(0.20m, "resolution"),
        PricePer.Create(2500.00m, "research")
    );

    public static UsageBasedPricing CreateEnterprise() => new(
        PricePer.Create(0.08m, "validation"),
        PricePer.Create(0.20m, "analysis"),
        PricePer.Create(0.12m, "translation"),
        PricePer.Create(0.15m, "resolution"),
        PricePer.Create(2000.00m, "research")
    );

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CulturalAppropriatenessValidation;
        yield return DiasporaAnalysis;
        yield return MultiLanguageTranslation;
        yield return CulturalConflictResolution;
        yield return CustomMarketResearch;
    }
}

/// <summary>
/// Price per usage unit
/// </summary>
public class PricePer : ValueObject
{
    public decimal Amount { get; }
    public string Unit { get; }
    public Currency Currency { get; }
    public bool IsFree { get; }

    private PricePer(decimal amount, string unit, Currency? currency = null, bool isFree = false)
    {
        if (amount < 0)
            throw new ArgumentException("Price cannot be negative", nameof(amount));

        Amount = amount;
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        Currency = currency ?? new Currency("USD");
        IsFree = isFree;
    }

    public static PricePer Free() => new(0, "request", new Currency("USD"), true);
    public static PricePer Create(decimal amount, string unit, Currency? currency = null)
        => new(amount, unit, currency ?? new Currency("USD"));

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Unit;
        yield return Currency;
        yield return IsFree;
    }
}

/// <summary>
/// Service Level Agreement
/// </summary>
public class SLA : ValueObject
{
    public string Level { get; }
    public TimeSpan ResponseTime { get; }
    public decimal UptimeGuarantee { get; }
    public bool DedicatedSupport { get; }

    public SLA(string level, TimeSpan responseTime, decimal uptimeGuarantee, bool dedicatedSupport = false)
    {
        Level = level ?? throw new ArgumentNullException(nameof(level));
        ResponseTime = responseTime;
        UptimeGuarantee = uptimeGuarantee;
        DedicatedSupport = dedicatedSupport;
    }

    public static SLA CommunitySupport() => new("Community", TimeSpan.FromDays(3), 0.95m);
    public static SLA BusinessSupport() => new("Business", TimeSpan.FromHours(24), 0.99m);
    public static SLA EnterpriseSupport() => new("Enterprise", TimeSpan.FromHours(4), 0.999m, true);
    public static SLA CustomSupport() => new("Custom", TimeSpan.FromHours(1), 0.9999m, true);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Level;
        yield return ResponseTime;
        yield return UptimeGuarantee;
        yield return DedicatedSupport;
    }
}

/// <summary>
/// Currency value object
/// </summary>
public class Currency : ValueObject
{
    public string Code { get; }

    public Currency(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be empty", nameof(code));

        Code = code.ToUpperInvariant();
    }

    public static Currency USD() => new("USD");
    public static Currency EUR() => new("EUR");
    public static Currency LKR() => new("LKR");

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }
}