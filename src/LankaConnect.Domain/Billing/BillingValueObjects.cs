using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Billing;

/// <summary>
/// Cultural API usage tracking
/// </summary>
public class CulturalAPIUsage : ValueObject
{
    public APIKey ApiKey { get; }
    public BillingEndpoint Endpoint { get; }
    public UsageCost Cost { get; }
    public CulturalComplexityScore ComplexityScore { get; }
    public DateTime Timestamp { get; }
    public UsageMetadata Metadata { get; }

    public CulturalAPIUsage(
        APIKey apiKey,
        BillingEndpoint endpoint,
        UsageCost cost,
        CulturalComplexityScore complexityScore,
        UsageMetadata metadata)
    {
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Cost = cost ?? throw new ArgumentNullException(nameof(cost));
        ComplexityScore = complexityScore ?? throw new ArgumentNullException(nameof(complexityScore));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Timestamp = DateTime.UtcNow;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ApiKey;
        yield return Endpoint;
        yield return Cost;
        yield return ComplexityScore;
        yield return Timestamp;
    }
}

/// <summary>
/// Billing endpoint for Cultural Intelligence API with cost calculation
/// Renamed from CulturalIntelligenceEndpoint to eliminate duplicate type conflicts
/// </summary>
public class BillingEndpoint : ValueObject
{
    public string Path { get; }
    public EndpointCategory Category { get; }
    public BillingComplexity BillingComplexity { get; }

    public BillingEndpoint(string path, EndpointCategory category, BillingComplexity billingComplexity)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Endpoint path cannot be empty", nameof(path));

        Path = path;
        Category = category;
        BillingComplexity = billingComplexity;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Path;
        yield return Category;
        yield return BillingComplexity;
    }

    public static BillingEndpoint BuddhistCalendar(string path) 
        => new(path, EndpointCategory.BuddhistCalendar, BillingComplexity.Medium);

    public static BillingEndpoint HinduCalendar(string path) 
        => new(path, EndpointCategory.HinduCalendar, BillingComplexity.Medium);

    public static BillingEndpoint CulturalAppropriateness(string path) 
        => new(path, EndpointCategory.CulturalAppropriateness, BillingComplexity.High);

    public static BillingEndpoint DiasporaAnalytics(string path) 
        => new(path, EndpointCategory.DiasporaAnalytics, BillingComplexity.High);

    public static BillingEndpoint MultiLanguage(string path)
        => new(path, EndpointCategory.MultiLanguage, BillingComplexity.Medium);

    public static BillingEndpoint EventRecommendations(string path)
        => new(path, EndpointCategory.EventRecommendations, BillingComplexity.High);

    public static BillingEndpoint CulturalContent(string path)
        => new(path, EndpointCategory.CulturalContent, BillingComplexity.Medium);

    public static BillingEndpoint BusinessDirectory(string path)
        => new(path, EndpointCategory.BusinessDirectory, BillingComplexity.Low);

    public static BillingEndpoint CommunityEngagement(string path)
        => new(path, EndpointCategory.CommunityEngagement, BillingComplexity.High);
}

/// <summary>
/// Usage cost calculation
/// </summary>
public class UsageCost : ValueObject
{
    public decimal BaseAmount { get; }
    public decimal ComplexityMultiplier { get; }
    public decimal TotalAmount { get; }
    public Currency Currency { get; }
    public CostBreakdown Breakdown { get; }

    public UsageCost(
        decimal baseAmount, 
        decimal complexityMultiplier, 
        Currency currency, 
        CostBreakdown breakdown)
    {
        if (baseAmount < 0)
            throw new ArgumentException("Base amount cannot be negative", nameof(baseAmount));
        if (complexityMultiplier <= 0)
            throw new ArgumentException("Complexity multiplier must be positive", nameof(complexityMultiplier));

        BaseAmount = baseAmount;
        ComplexityMultiplier = complexityMultiplier;
        TotalAmount = baseAmount * complexityMultiplier;
        Currency = currency ?? new Currency("USD");
        Breakdown = breakdown ?? throw new ArgumentNullException(nameof(breakdown));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseAmount;
        yield return ComplexityMultiplier;
        yield return TotalAmount;
        yield return Currency;
        yield return Breakdown;
    }
}

/// <summary>
/// Cultural complexity scoring for billing
/// </summary>
public class CulturalComplexityScore : ValueObject
{
    public int Score { get; }
    public ComplexityFactor[] Factors { get; }
    public decimal BillingMultiplier { get; }

    public CulturalComplexityScore(int score, ComplexityFactor[] factors)
    {
        if (score < 0 || score > 100)
            throw new ArgumentException("Score must be between 0 and 100", nameof(score));

        Score = score;
        Factors = factors ?? Array.Empty<ComplexityFactor>();
        BillingMultiplier = CalculateBillingMultiplier(score);
    }

    private static decimal CalculateBillingMultiplier(int score)
    {
        return score switch
        {
            >= 90 => 2.0m,   // Very high complexity
            >= 75 => 1.5m,   // High complexity
            >= 50 => 1.2m,   // Medium complexity
            >= 25 => 1.0m,   // Low complexity
            _ => 0.8m        // Very low complexity
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Score;
        yield return BillingMultiplier;
        foreach (var factor in Factors)
            yield return factor;
    }
}

/// <summary>
/// Billing analytics and reporting
/// </summary>
public class BillingAnalytics : ValueObject
{
    public RevenueMetrics RevenueMetrics { get; }
    public UsageMetrics UsageMetrics { get; }
    public CustomerMetrics CustomerMetrics { get; }
    public CulturalFeatureMetrics CulturalMetrics { get; }
    public TimeRange TimeRange { get; }
    public DateTime GeneratedAt { get; }

    public BillingAnalytics(
        RevenueMetrics revenueMetrics,
        UsageMetrics usageMetrics,
        CustomerMetrics customerMetrics,
        CulturalFeatureMetrics culturalMetrics,
        TimeRange timeRange)
    {
        RevenueMetrics = revenueMetrics ?? throw new ArgumentNullException(nameof(revenueMetrics));
        UsageMetrics = usageMetrics ?? throw new ArgumentNullException(nameof(usageMetrics));
        CustomerMetrics = customerMetrics ?? throw new ArgumentNullException(nameof(customerMetrics));
        CulturalMetrics = culturalMetrics ?? throw new ArgumentNullException(nameof(culturalMetrics));
        TimeRange = timeRange ?? throw new ArgumentNullException(nameof(timeRange));
        GeneratedAt = DateTime.UtcNow;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return RevenueMetrics;
        yield return UsageMetrics;
        yield return CustomerMetrics;
        yield return CulturalMetrics;
        yield return TimeRange;
    }
}

/// <summary>
/// Enterprise client for custom billing
/// </summary>
public class EnterpriseClient : BaseEntity
{
    public CompanyName CompanyName { get; private set; }
    public ContactInfo ContactInfo { get; private set; }
    public CulturalRequirements CulturalRequirements { get; private set; }
    public CustomPricing CustomPricing { get; private set; }
    public DateTime ContractStartDate { get; private set; }
    public DateTime ContractEndDate { get; private set; }

    public EnterpriseClient(
        CompanyName companyName,
        ContactInfo contactInfo,
        CulturalRequirements culturalRequirements,
        CustomPricing customPricing,
        DateTime contractStartDate,
        DateTime contractEndDate) : base()
    {
        CompanyName = companyName ?? throw new ArgumentNullException(nameof(companyName));
        ContactInfo = contactInfo ?? throw new ArgumentNullException(nameof(contactInfo));
        CulturalRequirements = culturalRequirements ?? throw new ArgumentNullException(nameof(culturalRequirements));
        CustomPricing = customPricing ?? throw new ArgumentNullException(nameof(customPricing));
        ContractStartDate = contractStartDate;
        ContractEndDate = contractEndDate;
    }

    public bool IsContractActive() => DateTime.UtcNow >= ContractStartDate && DateTime.UtcNow <= ContractEndDate;
}

/// <summary>
/// Cultural services contract
/// </summary>
public class CulturalServicesContract : ValueObject
{
    public ContractId ContractId { get; }
    public CulturalService[] Services { get; }
    public ContractValue TotalValue { get; }
    public PaymentSchedule PaymentSchedule { get; }
    public CulturalConsultingHours ConsultingHours { get; }
    public WhiteLabelLicensing? WhiteLabelLicensing { get; }

    public CulturalServicesContract(
        ContractId contractId,
        CulturalService[] services,
        ContractValue totalValue,
        PaymentSchedule paymentSchedule,
        CulturalConsultingHours consultingHours,
        WhiteLabelLicensing? whiteLabelLicensing)
    {
        ContractId = contractId ?? throw new ArgumentNullException(nameof(contractId));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        TotalValue = totalValue ?? throw new ArgumentNullException(nameof(totalValue));
        PaymentSchedule = paymentSchedule ?? throw new ArgumentNullException(nameof(paymentSchedule));
        ConsultingHours = consultingHours ?? throw new ArgumentNullException(nameof(consultingHours));
        WhiteLabelLicensing = whiteLabelLicensing;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ContractId;
        yield return TotalValue;
        yield return PaymentSchedule;
        yield return ConsultingHours;
        foreach (var service in Services)
            yield return service;
    }
}

/// <summary>
/// Partnership revenue sharing
/// </summary>
public class RevenueShare : ValueObject
{
    public decimal Percentage { get; }
    public decimal MinimumAmount { get; }
    public decimal MaximumAmount { get; }
    public CulturalAuthenticityBonus AuthenticityBonus { get; }

    public RevenueShare(
        decimal percentage,
        decimal minimumAmount,
        decimal maximumAmount,
        CulturalAuthenticityBonus authenticityBonus)
    {
        if (percentage <= 0 || percentage > 100)
            throw new ArgumentException("Percentage must be between 0 and 100", nameof(percentage));

        Percentage = percentage;
        MinimumAmount = minimumAmount;
        MaximumAmount = maximumAmount;
        AuthenticityBonus = authenticityBonus ?? CulturalAuthenticityBonus.None();
    }

    public decimal CalculateShare(decimal totalRevenue)
    {
        var share = totalRevenue * (Percentage / 100);
        var bonusAmount = AuthenticityBonus.CalculateBonus(totalRevenue);
        var totalShare = share + bonusAmount;

        return Math.Max(MinimumAmount, Math.Min(MaximumAmount, totalShare));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Percentage;
        yield return MinimumAmount;
        yield return MaximumAmount;
        yield return AuthenticityBonus;
    }
}

/// <summary>
/// Supporting enums and value objects
/// </summary>
public enum EndpointCategory
{
    BuddhistCalendar,
    HinduCalendar,
    CulturalAppropriateness,
    DiasporaAnalytics,
    MultiLanguage,
    CulturalConsulting,
    EventRecommendations,
    CulturalContent,
    BusinessDirectory,
    CommunityEngagement
}

public enum BillingComplexity
{
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}