using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Billing;

/// <summary>
/// Supporting types for Cultural Intelligence billing with fixed access modifiers
/// </summary>

public class ComplexityFactor : ValueObject
{
    public string Name { get; }
    public int Score { get; }
    public string Description { get; }

    public ComplexityFactor(string name, int score, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (score < 0)
            throw new ArgumentException("Score cannot be negative", nameof(score));

        Name = name;
        Score = score;
        Description = description;
    }

    public static ComplexityFactor Create(string name, int score, string description = "")
        => new(name, score, description);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Score;
        yield return Description;
    }
}

public class CostBreakdown : ValueObject
{
    public decimal BaseAmount { get; }
    public decimal ComplexityMultiplier { get; }
    public decimal TierDiscount { get; }
    public Dictionary<string, decimal> AdditionalCharges { get; }

    public CostBreakdown(
        decimal baseAmount, 
        decimal complexityMultiplier, 
        decimal tierDiscount,
        Dictionary<string, decimal>? additionalCharges = null)
    {
        BaseAmount = baseAmount;
        ComplexityMultiplier = complexityMultiplier;
        TierDiscount = tierDiscount;
        AdditionalCharges = additionalCharges ?? new Dictionary<string, decimal>();
    }

    public static CostBreakdown Create(decimal baseAmount, decimal complexityMultiplier, decimal tierDiscount)
        => new(baseAmount, complexityMultiplier, tierDiscount);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseAmount;
        yield return ComplexityMultiplier;
        yield return TierDiscount;
        foreach (var charge in AdditionalCharges)
        {
            yield return charge.Key;
            yield return charge.Value;
        }
    }
}

public class CustomCalendarVariation : ValueObject
{
    public string Name { get; }
    public string Description { get; }
    public Dictionary<string, object> Parameters { get; }

    public CustomCalendarVariation(string name, string description = "", Dictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        Parameters = parameters ?? new Dictionary<string, object>();
    }

    public static CustomCalendarVariation Create(string name, string description = "")
        => new(name, description);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Description;
        foreach (var param in Parameters)
        {
            yield return param.Key;
            yield return param.Value;
        }
    }
}

public class ContentToValidate : ValueObject
{
    public string Content { get; }
    public ContentType ContentType { get; }
    public string Language { get; }
    public Dictionary<string, object> Metadata { get; }

    public ContentToValidate(string content, ContentType contentType, string language = "en", Dictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        Content = content;
        ContentType = contentType;
        Language = language;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    public static ContentToValidate Create(string content, ContentType contentType, string language = "en")
        => new(content, contentType, language);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Content;
        yield return ContentType;
        yield return Language;
        foreach (var item in Metadata)
        {
            yield return item.Key;
            yield return item.Value;
        }
    }
}

// CulturalContext removed - using canonical version from Domain.Communications.ValueObjects.CulturalContext

public class RealTimeModeration : ValueObject
{
    public bool IsEnabled { get; }
    public int TimeoutMs { get; }
    public ModerationLevel Level { get; }

    public RealTimeModeration(bool isEnabled, int timeoutMs = 5000, ModerationLevel level = ModerationLevel.Standard)
    {
        IsEnabled = isEnabled;
        TimeoutMs = timeoutMs;
        Level = level;
    }

    public static RealTimeModeration Enabled(int timeoutMs = 5000, ModerationLevel level = ModerationLevel.Standard)
        => new(true, timeoutMs, level);

    public static RealTimeModeration Disabled() => new(false);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return TimeoutMs;
        yield return Level;
    }
}

public class GeographicRegion : ValueObject
{
    public string Name { get; }
    public string Country { get; }
    public string Continent { get; }

    public GeographicRegion(string name, string country = "", string continent = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        Country = country;
        Continent = continent;
    }

    public static GeographicRegion Create(string name, string country = "", string continent = "")
        => new(name, country, continent);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Country;
        yield return Continent;
    }
}

public class CommunitySegment : ValueObject
{
    public string Name { get; }
    public string Description { get; }
    public AgeRange AgeRange { get; }

    public CommunitySegment(string name, string description = "", AgeRange? ageRange = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        AgeRange = ageRange ?? AgeRange.All();
    }

    public static CommunitySegment Create(string name, string description = "")
        => new(name, description);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Description;
        yield return AgeRange;
    }
}

public class PredictionTimeframe : ValueObject
{
    public int Months { get; }
    public string Description { get; }

    public PredictionTimeframe(int months, string description)
    {
        if (months <= 0)
            throw new ArgumentException("Months must be positive", nameof(months));

        Months = months;
        Description = description;
    }

    public static PredictionTimeframe ThreeMonths() => new(3, "3 months");
    public static PredictionTimeframe SixMonths() => new(6, "6 months");
    public static PredictionTimeframe OneYear() => new(12, "1 year");

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Months;
        yield return Description;
    }
}

public class CulturalAuthenticityBonus : ValueObject
{
    public decimal BonusPercentage { get; }
    public string Reason { get; }
    public bool IsActive { get; }

    public CulturalAuthenticityBonus(decimal bonusPercentage, string reason, bool isActive = true)
    {
        if (bonusPercentage < 0)
            throw new ArgumentException("Bonus percentage cannot be negative", nameof(bonusPercentage));

        BonusPercentage = bonusPercentage;
        Reason = reason;
        IsActive = isActive;
    }

    public static CulturalAuthenticityBonus Create(decimal bonusPercentage, string reason)
        => new(bonusPercentage, reason);

    public static CulturalAuthenticityBonus None() => new(0, "No bonus");

    public decimal CalculateBonus(decimal baseAmount)
    {
        if (!IsActive) return 0;
        return baseAmount * (BonusPercentage / 100);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return BonusPercentage;
        yield return Reason;
        yield return IsActive;
    }
}

public class CompanyName : ValueObject
{
    public string Value { get; }

    public CompanyName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Company name cannot be empty", nameof(value));

        Value = value;
    }

    public static CompanyName Create(string value) => new(value);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(CompanyName companyName) => companyName.Value;
}

public class ContactInfo : ValueObject
{
    public string Email { get; }
    public string Phone { get; }
    public string Address { get; }

    public ContactInfo(string email, string phone, string address = "")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        Email = email;
        Phone = phone;
        Address = address;
    }

    public static ContactInfo Create(string email, string phone, string address = "")
        => new(email, phone, address);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return Phone;
        yield return Address;
    }
}

public class CulturalRequirements : ValueObject
{
    public string[] RequiredCultures { get; }
    public string[] RequiredLanguages { get; }
    public string[] SpecialRequirements { get; }

    public CulturalRequirements(string[] requiredCultures, string[] requiredLanguages, string[]? specialRequirements = null)
    {
        RequiredCultures = requiredCultures ?? Array.Empty<string>();
        RequiredLanguages = requiredLanguages ?? Array.Empty<string>();
        SpecialRequirements = specialRequirements ?? Array.Empty<string>();
    }

    public static CulturalRequirements Create(string[] cultures, string[] languages, string[]? special = null)
        => new(cultures, languages, special);

    public override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var culture in RequiredCultures)
            yield return culture;
        foreach (var language in RequiredLanguages)
            yield return language;
        foreach (var requirement in SpecialRequirements)
            yield return requirement;
    }
}

public class CustomPricing : ValueObject
{
    public decimal Amount { get; }
    public string Description { get; }
    public Dictionary<string, decimal> FeaturePricing { get; }

    public CustomPricing(decimal amount, string description, Dictionary<string, decimal>? featurePricing = null)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        Amount = amount;
        Description = description;
        FeaturePricing = featurePricing ?? new Dictionary<string, decimal>();
    }

    public static CustomPricing Create(decimal amount, string description)
        => new(amount, description);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Description;
        foreach (var pricing in FeaturePricing)
        {
            yield return pricing.Key;
            yield return pricing.Value;
        }
    }
}

public class UsageMetadata : ValueObject
{
    public string RequestId { get; }
    public string ClientId { get; }
    public DateTime RequestTime { get; }
    public Dictionary<string, object> AdditionalData { get; }

    public UsageMetadata(string requestId, string clientId, DateTime requestTime, Dictionary<string, object>? additionalData = null)
    {
        RequestId = requestId;
        ClientId = clientId;
        RequestTime = requestTime;
        AdditionalData = additionalData ?? new Dictionary<string, object>();
    }

    public static UsageMetadata Create(string requestId, string clientId = "", Dictionary<string, object>? additionalData = null)
        => new(requestId, clientId, DateTime.UtcNow, additionalData);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return RequestId;
        yield return ClientId;
        yield return RequestTime;
        foreach (var data in AdditionalData)
        {
            yield return data.Key;
            yield return data.Value;
        }
    }
}

public class AgeRange : ValueObject
{
    public int MinAge { get; }
    public int MaxAge { get; }

    public AgeRange(int minAge, int maxAge)
    {
        if (minAge < 0) throw new ArgumentException("Min age cannot be negative", nameof(minAge));
        if (maxAge < minAge) throw new ArgumentException("Max age cannot be less than min age", nameof(maxAge));

        MinAge = minAge;
        MaxAge = maxAge;
    }

    public static AgeRange All() => new(0, 120);
    public static AgeRange Young() => new(18, 35);
    public static AgeRange MiddleAge() => new(36, 55);
    public static AgeRange Senior() => new(56, 120);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return MinAge;
        yield return MaxAge;
    }
}

// Additional types for billing metrics
public class RevenueMetrics : ValueObject
{
    public decimal TotalRevenue { get; }
    public decimal MonthlyRecurringRevenue { get; }
    public decimal AverageRevenuePerUser { get; }
    public int TotalSubscriptions { get; }

    public RevenueMetrics(decimal totalRevenue, decimal monthlyRecurringRevenue, decimal averageRevenuePerUser, int totalSubscriptions)
    {
        TotalRevenue = totalRevenue;
        MonthlyRecurringRevenue = monthlyRecurringRevenue;
        AverageRevenuePerUser = averageRevenuePerUser;
        TotalSubscriptions = totalSubscriptions;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalRevenue;
        yield return MonthlyRecurringRevenue;
        yield return AverageRevenuePerUser;
        yield return TotalSubscriptions;
    }
}

public class UsageMetrics : ValueObject
{
    public long TotalAPIRequests { get; }
    public long BuddhistCalendarRequests { get; }
    public long CulturalAppropriatenessRequests { get; }
    public long DiasporaAnalyticsRequests { get; }
    public decimal AverageComplexityScore { get; }

    public UsageMetrics(long totalAPIRequests, long buddhistCalendarRequests, long culturalAppropriatenessRequests, long diasporaAnalyticsRequests, decimal averageComplexityScore)
    {
        TotalAPIRequests = totalAPIRequests;
        BuddhistCalendarRequests = buddhistCalendarRequests;
        CulturalAppropriatenessRequests = culturalAppropriatenessRequests;
        DiasporaAnalyticsRequests = diasporaAnalyticsRequests;
        AverageComplexityScore = averageComplexityScore;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalAPIRequests;
        yield return BuddhistCalendarRequests;
        yield return CulturalAppropriatenessRequests;
        yield return DiasporaAnalyticsRequests;
        yield return AverageComplexityScore;
    }
}

public class CustomerMetrics : ValueObject
{
    public int CommunityTierUsers { get; }
    public int ProfessionalTierUsers { get; }
    public int EnterpriseTierUsers { get; }
    public int CustomTierUsers { get; }
    public decimal ChurnRate { get; }

    public CustomerMetrics(int communityTierUsers, int professionalTierUsers, int enterpriseTierUsers, int customTierUsers, decimal churnRate)
    {
        CommunityTierUsers = communityTierUsers;
        ProfessionalTierUsers = professionalTierUsers;
        EnterpriseTierUsers = enterpriseTierUsers;
        CustomTierUsers = customTierUsers;
        ChurnRate = churnRate;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CommunityTierUsers;
        yield return ProfessionalTierUsers;
        yield return EnterpriseTierUsers;
        yield return CustomTierUsers;
        yield return ChurnRate;
    }
}

public class CulturalFeatureMetrics : ValueObject
{
    public Dictionary<string, long> FeatureUsage { get; }
    public Dictionary<string, decimal> FeatureRevenue { get; }
    public Dictionary<string, decimal> PopularityScores { get; }

    public CulturalFeatureMetrics(Dictionary<string, long> featureUsage, Dictionary<string, decimal> featureRevenue, Dictionary<string, decimal> popularityScores)
    {
        FeatureUsage = featureUsage ?? new Dictionary<string, long>();
        FeatureRevenue = featureRevenue ?? new Dictionary<string, decimal>();
        PopularityScores = popularityScores ?? new Dictionary<string, decimal>();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var usage in FeatureUsage)
        {
            yield return usage.Key;
            yield return usage.Value;
        }
        foreach (var revenue in FeatureRevenue)
        {
            yield return revenue.Key;
            yield return revenue.Value;
        }
    }
}

// Supporting enums

public enum ModerationLevel
{
    Basic,
    Standard,
    Strict,
    Custom
}

// Additional types for contracts
public class CulturalService : ValueObject
{
    public string Name { get; }
    public decimal Price { get; }
    public string Description { get; }

    public CulturalService(string name, decimal price, string description)
    {
        Name = name;
        Price = price;
        Description = description;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Price;
        yield return Description;
    }
}

public class ContractValue : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public ContractValue(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

public class PaymentSchedule : ValueObject
{
    public PaymentFrequency Frequency { get; }
    public DateTime FirstPaymentDate { get; }
    public int NumberOfPayments { get; }

    public PaymentSchedule(PaymentFrequency frequency, DateTime firstPaymentDate, int numberOfPayments)
    {
        Frequency = frequency;
        FirstPaymentDate = firstPaymentDate;
        NumberOfPayments = numberOfPayments;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Frequency;
        yield return FirstPaymentDate;
        yield return NumberOfPayments;
    }
}

public class CulturalConsultingHours : ValueObject
{
    public int IncludedHours { get; }
    public decimal HourlyRate { get; }

    public CulturalConsultingHours(int includedHours, decimal hourlyRate)
    {
        IncludedHours = includedHours;
        HourlyRate = hourlyRate;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IncludedHours;
        yield return HourlyRate;
    }
}

public class WhiteLabelLicensing : ValueObject
{
    public bool IsIncluded { get; }
    public decimal SetupFee { get; }
    public decimal MonthlyFee { get; }

    public WhiteLabelLicensing(bool isIncluded, decimal setupFee, decimal monthlyFee)
    {
        IsIncluded = isIncluded;
        SetupFee = setupFee;
        MonthlyFee = monthlyFee;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsIncluded;
        yield return SetupFee;
        yield return MonthlyFee;
    }
}

public enum PaymentFrequency
{
    Monthly,
    Quarterly,
    Annually,
    OneTime
}

// Additional types for API endpoints
public class APIEndpoint : ValueObject
{
    public string Path { get; }
    public HttpMethod Method { get; }
    public EndpointCategory Category { get; }

    public APIEndpoint(string path, HttpMethod method, EndpointCategory category)
    {
        Path = path;
        Method = method;
        Category = category;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Path;
        yield return Method;
        yield return Category;
    }
}

public class RequestMetadata : ValueObject
{
    public Dictionary<string, object> Data { get; }
    public Guid RequestId { get; }
    public Guid? ClientId { get; }

    public RequestMetadata(Dictionary<string, object> data, Guid? requestId = null, Guid? clientId = null)
    {
        Data = data ?? new Dictionary<string, object>();
        RequestId = requestId ?? Guid.NewGuid();
        ClientId = clientId;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return RequestId;
        if (ClientId.HasValue) yield return ClientId.Value;
        foreach (var item in Data)
        {
            yield return item.Key;
            yield return item.Value;
        }
    }
}

public class TimeRange : ValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public TimeRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date");

        StartDate = startDate;
        EndDate = endDate;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}

/// <summary>
/// Strongly typed IDs
/// </summary>
public class EnterpriseClientId : ValueObject
{
    public Guid Value { get; }

    public EnterpriseClientId() : this(Guid.NewGuid()) { }
    public EnterpriseClientId(Guid value) => Value = value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator Guid(EnterpriseClientId id) => id.Value;
    public static implicit operator EnterpriseClientId(Guid value) => new(value);
}

public class ContractId : ValueObject
{
    public Guid Value { get; }

    public ContractId() : this(Guid.NewGuid()) { }
    public ContractId(Guid value) => Value = value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator Guid(ContractId id) => id.Value;
    public static implicit operator ContractId(Guid value) => new(value);
}

public class PartnershipId : ValueObject
{
    public Guid Value { get; }

    public PartnershipId() : this(Guid.NewGuid()) { }
    public PartnershipId(Guid value) => Value = value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator Guid(PartnershipId id) => id.Value;
    public static implicit operator PartnershipId(Guid value) => new(value);
}