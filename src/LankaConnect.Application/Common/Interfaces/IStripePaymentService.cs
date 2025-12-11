using LankaConnect.Domain.Billing;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Interface for Stripe payment integration with Cultural Intelligence billing
/// </summary>
public interface IStripePaymentService
{
    Task<Result> CreateSubscriptionAsync(CreateStripeSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Result> CreateEnterpriseSubscriptionAsync(CreateEnterpriseSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChargeUsageAsync(ChargeUsageRequest request, CancellationToken cancellationToken = default);
    Task<Result> CreatePartnerPayoutAsync(CreatePartnerPayoutRequest request, CancellationToken cancellationToken = default);
    Task<Result> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<Result> UpdateSubscriptionAsync(UpdateSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Result<StripeWebhookEvent>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);

    // Session 23: Event ticket payment integration
    Task<Result<string>> CreateEventCheckoutSessionAsync(CreateEventCheckoutSessionRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to create a Stripe subscription for Cultural Intelligence tiers
/// </summary>
public class CreateStripeSubscriptionRequest
{
    public required UserId UserId { get; init; }
    public decimal PriceAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string TierName { get; init; }
    public Dictionary<string, bool> Features { get; init; } = new();
    public string? PromoCode { get; init; }
    public int TrialDays { get; init; } = 0;
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request to create an enterprise subscription with custom billing
/// </summary>
public class CreateEnterpriseSubscriptionRequest
{
    public Guid ClientId { get; init; }
    public decimal ContractValue { get; init; }
    public string Currency { get; init; } = "USD";
    public required PaymentSchedule PaymentSchedule { get; init; }
    public CulturalService[] Services { get; init; } = Array.Empty<CulturalService>();
    public required CulturalConsultingHours ConsultingHours { get; init; }
    public WhiteLabelLicensing? WhiteLabelLicensing { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request to charge for API usage
/// </summary>
public class ChargeUsageRequest
{
    public required UserId UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string Description { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public bool IdempotencyEnabled { get; init; } = true;
    public string? IdempotencyKey { get; init; }
}

/// <summary>
/// Request to create a partner payout
/// </summary>
public class CreatePartnerPayoutRequest
{
    public Guid PartnershipId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string Description { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public string? DestinationAccount { get; init; }
}

/// <summary>
/// Request to update an existing subscription
/// </summary>
public class UpdateSubscriptionRequest
{
    public required string SubscriptionId { get; init; }
    public decimal? NewPriceAmount { get; init; }
    public Dictionary<string, bool>? UpdatedFeatures { get; init; }
    public string? PromoCode { get; init; }
    public bool ProrationBehavior { get; init; } = true;
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Stripe webhook event data
/// </summary>
public class StripeWebhookEvent
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public DateTime Created { get; init; }
    public required object Data { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public bool LiveMode { get; init; }
}

/// <summary>
/// Repository interface for billing operations
/// </summary>
public interface IBillingRepository
{
    Task<Result> SaveSubscriptionAsync(CulturalIntelligenceSubscription subscription, CancellationToken cancellationToken = default);
    Task<CulturalIntelligenceSubscription?> GetSubscriptionByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<Result> SaveAPIKeyAsync(APIKey apiKey, CancellationToken cancellationToken = default);
    Task<APIKey?> GetAPIKeyAsync(string apiKeyValue, CancellationToken cancellationToken = default);
    Task<Result> SaveEnterpriseContractAsync(CulturalServicesContract contract, CancellationToken cancellationToken = default);
    Task<Partnership?> GetPartnershipAsync(PartnershipId partnershipId, CancellationToken cancellationToken = default);
    Task<decimal> GetPartnershipRevenueAsync(PartnershipId partnershipId, CancellationToken cancellationToken = default);
    
    // Analytics methods
    Task<RevenueMetrics> GetRevenueMetricsAsync(TimeRange timeRange, CancellationToken cancellationToken = default);
    Task<UsageMetrics> GetUsageMetricsAsync(TimeRange timeRange, CancellationToken cancellationToken = default);
    Task<CustomerMetrics> GetCustomerMetricsAsync(TimeRange timeRange, CancellationToken cancellationToken = default);
    Task<CulturalFeatureMetrics> GetCulturalFeatureMetricsAsync(TimeRange timeRange, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interfaces for cultural intelligence features
/// </summary>
public interface ICulturalCalendarService
{
    Task<Result> ProcessBuddhistCalendarRequestAsync(BuddhistCalendarRequest request, CancellationToken cancellationToken = default);
    Task<Result> ProcessHinduCalendarRequestAsync(HinduCalendarRequest request, CancellationToken cancellationToken = default);
}

public interface ICulturalAppropriatenessService
{
    Task<Result> ProcessAppropriatenessRequestAsync(CulturalAppropriatenessRequest request, CancellationToken cancellationToken = default);
}

public interface IDiasporaAnalyticsService
{
    Task<Result> ProcessAnalyticsRequestAsync(DiasporaAnalyticsRequest request, CancellationToken cancellationToken = default);
}

public interface IUsageTrackingService
{
    Task<Result> TrackUsageAsync(CulturalAPIUsage usage, CancellationToken cancellationToken = default);
    Task<long> GetCurrentMonthlyUsageAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<UsageStatistics> GetUsageStatisticsAsync(UserId userId, TimeRange timeRange, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cultural Intelligence subscription entity
/// </summary>
public class CulturalIntelligenceSubscription : Entity<CulturalIntelligenceSubscriptionId>
{
    public UserId UserId { get; private set; }
    public CulturalIntelligenceTier Tier { get; private set; }
    public new DateTime CreatedAt { get; private set; }
    public DateTime NextBillingDate { get; private set; }
    public bool IsActive { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private CulturalIntelligenceSubscription(
        CulturalIntelligenceSubscriptionId id,
        UserId userId,
        CulturalIntelligenceTier tier,
        DateTime createdAt,
        DateTime nextBillingDate) : base(id)
    {
        UserId = userId;
        Tier = tier;
        CreatedAt = createdAt;
        NextBillingDate = nextBillingDate;
        IsActive = true;
        Metadata = new Dictionary<string, object>();
    }

    public static CulturalIntelligenceSubscription Create(
        CulturalIntelligenceSubscriptionId id,
        UserId userId,
        CulturalIntelligenceTier tier,
        DateTime createdAt,
        DateTime nextBillingDate)
    {
        return new CulturalIntelligenceSubscription(id, userId, tier, createdAt, nextBillingDate) { Id = id };
    }

    public void UpdateTier(CulturalIntelligenceTier newTier)
    {
        Tier = newTier ?? throw new ArgumentNullException(nameof(newTier));
    }

    public void SetStripeSubscriptionId(string stripeSubscriptionId)
    {
        StripeSubscriptionId = stripeSubscriptionId ?? throw new ArgumentNullException(nameof(stripeSubscriptionId));
    }

    public void Cancel()
    {
        IsActive = false;
    }

    public void UpdateNextBillingDate(DateTime nextBillingDate)
    {
        NextBillingDate = nextBillingDate;
    }
}

/// <summary>
/// Partnership entity for revenue sharing
/// </summary>
public class Partnership : Entity<PartnershipId>
{
    public string PartnerName { get; private set; }
    public ContactInfo ContactInfo { get; private set; }
    public RevenueShare RevenueShare { get; private set; }
    public DateTime PartnershipStartDate { get; private set; }
    public DateTime? PartnershipEndDate { get; private set; }
    public bool IsActive { get; private set; }

    private Partnership(
        PartnershipId id,
        string partnerName,
        ContactInfo contactInfo,
        RevenueShare revenueShare,
        DateTime partnershipStartDate) : base(id)
    {
        PartnerName = partnerName ?? throw new ArgumentNullException(nameof(partnerName));
        ContactInfo = contactInfo ?? throw new ArgumentNullException(nameof(contactInfo));
        RevenueShare = revenueShare ?? throw new ArgumentNullException(nameof(revenueShare));
        PartnershipStartDate = partnershipStartDate;
        IsActive = true;
    }

    public static Partnership Create(
        PartnershipId id,
        string partnerName,
        ContactInfo contactInfo,
        RevenueShare revenueShare,
        DateTime partnershipStartDate)
    {
        return new Partnership(id, partnerName, contactInfo, revenueShare, partnershipStartDate) { Id = id };
    }

    public void EndPartnership(DateTime endDate)
    {
        PartnershipEndDate = endDate;
        IsActive = false;
    }
}

/// <summary>
/// Usage statistics for tracking API consumption
/// </summary>
public class UsageStatistics
{
    public long TotalRequests { get; }
    public Dictionary<EndpointCategory, long> RequestsByCategory { get; }
    public decimal TotalCost { get; }
    public Dictionary<EndpointCategory, decimal> CostByCategory { get; }
    public DateTime PeriodStart { get; }
    public DateTime PeriodEnd { get; }

    public UsageStatistics(
        long totalRequests,
        Dictionary<EndpointCategory, long> requestsByCategory,
        decimal totalCost,
        Dictionary<EndpointCategory, decimal> costByCategory,
        DateTime periodStart,
        DateTime periodEnd)
    {
        TotalRequests = totalRequests;
        RequestsByCategory = requestsByCategory ?? new Dictionary<EndpointCategory, long>();
        TotalCost = totalCost;
        CostByCategory = costByCategory ?? new Dictionary<EndpointCategory, decimal>();
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }
}

/// <summary>
/// Strongly typed ID for Cultural Intelligence subscriptions
/// </summary>
public record CulturalIntelligenceSubscriptionId : StronglyTypedId
{
    public CulturalIntelligenceSubscriptionId() : base() { }
    public CulturalIntelligenceSubscriptionId(Guid value) : base(value) { }

    public static CulturalIntelligenceSubscriptionId New() => new();
}

/// <summary>
/// Hindu calendar request (similar to Buddhist calendar)
/// </summary>
public class HinduCalendarRequest : ValueObject
{
    public CalendarPrecisionLevel PrecisionLevel { get; }
    public HinduCalendarType CalendarType { get; }
    public CustomCalendarVariation[] Variations { get; }
    public DateTime RequestedDate { get; }

    public HinduCalendarRequest(
        CalendarPrecisionLevel precisionLevel,
        HinduCalendarType calendarType,
        CustomCalendarVariation[] variations,
        DateTime requestedDate)
    {
        PrecisionLevel = precisionLevel;
        CalendarType = calendarType;
        Variations = variations ?? Array.Empty<CustomCalendarVariation>();
        RequestedDate = requestedDate;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PrecisionLevel;
        yield return CalendarType;
        yield return RequestedDate;
        foreach (var variation in Variations)
            yield return variation;
    }
}

public enum HinduCalendarType
{
    Vikram,
    Shaka,
    Bengali,
    Tamil,
    Malayalam,
    Custom
}

/// <summary>
/// Session 23: Request to create a Stripe Checkout session for event ticket purchase
/// </summary>
public class CreateEventCheckoutSessionRequest
{
    public Guid EventId { get; init; }
    public Guid RegistrationId { get; init; }
    public required string EventTitle { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}