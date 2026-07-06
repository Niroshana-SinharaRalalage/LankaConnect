using LankaConnect.Modules.Payments.Domain.Billing;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Shared;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.BuildingBlocks.Domain.Models;
using LankaConnect.BuildingBlocks.Domain.Monitoring;
using LankaConnect.BuildingBlocks.Domain.Security;
using LankaConnect.BuildingBlocks.Domain.Recovery;
using LankaConnect.BuildingBlocks.Domain.Database;
using LankaConnect.BuildingBlocks.Domain.Enums;
using MultiLanguageModels = LankaConnect.BuildingBlocks.Domain.Database.MultiLanguageRoutingModels;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

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
    // Phase 6A.136D: Changed from Result<string> (URL) to Result<EventCheckoutResult> (ID + URL)
    Task<Result<EventCheckoutResult>> CreateEventCheckoutSessionAsync(CreateEventCheckoutSessionRequest request, CancellationToken cancellationToken = default);

    // Phase 6A.81 Part 3: Retrieve checkout URL from existing session
    Task<Result<string>> GetCheckoutSessionUrlAsync(string sessionId, CancellationToken cancellationToken = default);

    // Phase 6A.91: Create refund for a completed payment
    Task<Result<StripeRefundResult>> CreateRefundAsync(CreateRefundRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 7G — fetches the current status of a Stripe refund by id, used by the
    /// refund-reconciliation safety net to detect missed <c>charge.refunded</c>
    /// webhooks. Returns the same <see cref="StripeRefundResult"/> shape as
    /// <see cref="CreateRefundAsync"/> so callers can reuse <c>IsSucceeded</c>.
    /// Failure means the lookup itself faulted (Stripe API error, refund id not
    /// found) — caller should log and move on rather than escalate.
    /// </summary>
    Task<Result<StripeRefundResult>> GetRefundStatusAsync(string refundId, CancellationToken cancellationToken = default);

    // Add-Only Attendees Feature: Create checkout session for additional attendees
    Task<Result<AdditionCheckoutResult>> CreateAdditionCheckoutSessionAsync(
        CreateAdditionCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    // Donation Feature: Create standalone checkout session for donations
    Task<Result<DonationCheckoutResult>> CreateDonationCheckoutSessionAsync(
        CreateDonationCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    // Collection (Event Fund) Feature: Create checkout session for contributions
    Task<Result<CollectionCheckoutResult>> CreateCollectionCheckoutSessionAsync(
        CreateCollectionCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    // Sponsor Feature: Create checkout session for money sponsorships
    Task<Result<SponsorCheckoutResult>> CreateSponsorCheckoutSessionAsync(
        CreateSponsorCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    // Add-On Purchase Feature: Create checkout session for add-on purchases
    Task<Result<AddOnPurchaseCheckoutResult>> CreateAddOnPurchaseCheckoutSessionAsync(
        CreateAddOnPurchaseCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    // Add-On Cart Feature: Create checkout session with multiple add-on line items
    Task<Result<AddOnPurchaseCheckoutResult>> CreateAddOnCartCheckoutSessionAsync(
        CreateAddOnCartCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    // Phase 6A.157: Sponsorship Package Feature — create checkout session for
    // a packaged sponsorship purchase. Distinct from
    // CreateSponsorCheckoutSessionAsync (generic money sponsorship) because
    // the line-item description includes the package name + tier + (optional)
    // included-tickets appendix, and the metadata payment_type literal is
    // "package_sponsor" so the webhook dispatcher routes to
    // PackageSponsorWebhookHandler (which raises the new
    // PackageSponsorCompletedEvent rather than the generic one).
    Task<Result<PackageSponsorCheckoutResult>> CreatePackageSponsorCheckoutSessionAsync(
        CreatePackageSponsorCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);
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
    public decimal Amount { get; set; }
    public string Currency { get; init; } = "USD";
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Optional line items for multi-item checkout (e.g., ticket + donation).
    /// C1 Guard: When null or empty, existing single-item behavior is preserved.
    /// </summary>
    public List<CheckoutLineItem>? LineItems { get; set; }
}

/// <summary>
/// Represents a single line item in a Stripe Checkout session.
/// Used for combined ticket + donation checkout.
/// </summary>
public class CheckoutLineItem
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public int Quantity { get; init; } = 1;
}

/// <summary>
/// Phase 6A.91: Request to create a Stripe refund for a completed payment
/// </summary>
public class CreateRefundRequest
{
    /// <summary>
    /// The Stripe PaymentIntent ID to refund (stored in Registration.StripePaymentIntentId)
    /// </summary>
    public required string PaymentIntentId { get; init; }

    /// <summary>
    /// The registration ID for this refund (used for idempotency and audit trail)
    /// </summary>
    public Guid RegistrationId { get; init; }

    /// <summary>
    /// The amount to refund in the smallest currency unit (e.g., cents for USD)
    /// If null, refunds the full amount
    /// </summary>
    public long? AmountInCents { get; init; }

    /// <summary>
    /// The reason for the refund (for Stripe dashboard and reporting)
    /// </summary>
    public string Reason { get; init; } = "requested_by_customer";

    /// <summary>
    /// Optional metadata to attach to the refund
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Phase 6A.148.W5.D1: explicit Stripe IdempotencyKey for this refund call.
    ///
    /// When set, <see cref="IStripePaymentService.CreateRefundAsync"/> passes this value
    /// to Stripe verbatim — Stripe guarantees at-most-one successful refund per key for
    /// 24 hours. This is the foundation of safe re-dispatch from
    /// <c>RefundReconciliationService</c> AND retries from
    /// <c>RefundExecutionService.DispatchAsync</c> after a partial-success-then-rollback
    /// (the W5.D7 root cause).
    ///
    /// Callers in the 6A.148 workflow path should use
    /// <c>$"refund_line_{lineId:N}"</c> (or
    /// <c>$"refund_line_{lineId:N}_{attemptCounter}"</c> when the line has been
    /// re-attempted after a <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemStatus.Failed"/>
    /// state). Stable per-line key means reconciler re-dispatch is automatically safe.
    ///
    /// When null, <c>StripePaymentService</c> falls back to its legacy default
    /// <c>$"refund_{PaymentIntentId}_{AmountInCents}_{RegistrationId}"</c> for
    /// backward compatibility with legacy callers (CancelRsvp paid-refund branch,
    /// AddOnRefundService, etc.).
    /// </summary>
    public string? IdempotencyKey { get; init; }
}

/// <summary>
/// Phase 6A.91: Result of a Stripe refund operation
/// </summary>
public class StripeRefundResult
{
    /// <summary>
    /// The Stripe Refund ID (e.g., re_xxx)
    /// </summary>
    public required string RefundId { get; init; }

    /// <summary>
    /// The status of the refund (succeeded, pending, failed, canceled)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The amount refunded in cents
    /// </summary>
    public long AmountRefunded { get; init; }

    /// <summary>
    /// The currency of the refund
    /// </summary>
    public string Currency { get; init; } = "usd";

    /// <summary>
    /// When the refund was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Whether the refund succeeded
    /// </summary>
    public bool IsSucceeded => Status == "succeeded";
}

/// <summary>
/// Add-Only Attendees Feature: Request to create a Stripe Checkout session for adding attendees.
/// </summary>
public class CreateAdditionCheckoutSessionRequest
{
    /// <summary>
    /// The existing registration to add attendees to.
    /// </summary>
    public Guid RegistrationId { get; init; }

    /// <summary>
    /// The RegistrationAddition ID tracking this addition.
    /// </summary>
    public Guid RegistrationAdditionId { get; init; }

    /// <summary>
    /// The event ID.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Event title for display in checkout.
    /// </summary>
    public required string EventTitle { get; init; }

    /// <summary>
    /// Additional amount to charge (delta payment).
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency for the payment.
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Number of new attendees being added.
    /// </summary>
    public int NewAttendeesCount { get; init; }

    /// <summary>
    /// URL to redirect to after successful payment.
    /// </summary>
    public required string SuccessUrl { get; init; }

    /// <summary>
    /// URL to redirect to if user cancels.
    /// </summary>
    public required string CancelUrl { get; init; }

    /// <summary>
    /// Contact email for the registration (for anonymous receipts).
    /// </summary>
    public string? ContactEmail { get; init; }

    /// <summary>
    /// User ID if authenticated (null for anonymous).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Optional additional metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Phase 6A.136D: Result of creating an event registration checkout session.
/// Replaces the previous Result&lt;string&gt; that only returned the URL.
/// </summary>
public class EventCheckoutResult
{
    /// <summary>
    /// The Stripe Checkout Session ID (starts with cs_).
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The checkout URL to redirect the user to.
    /// </summary>
    public required string CheckoutUrl { get; init; }

    /// <summary>
    /// Phase 6A.136F: When the Stripe checkout session expires (from Stripe's response).
    /// Use this instead of local DateTime.UtcNow.AddHours(24) to prevent drift.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Add-Only Attendees Feature: Result of creating an addition checkout session.
/// </summary>
public class AdditionCheckoutResult
{
    /// <summary>
    /// The Stripe Checkout Session ID.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The checkout URL to redirect the user to.
    /// </summary>
    public required string CheckoutUrl { get; init; }

    /// <summary>
    /// When the checkout session expires (typically 24 hours).
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Request to create a standalone Stripe Checkout session for a donation.
/// </summary>
public class CreateDonationCheckoutSessionRequest
{
    public Guid EventId { get; init; }
    public Guid DonationId { get; init; }
    public required string EventTitle { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Result of creating a donation Stripe Checkout session.
/// </summary>
public class DonationCheckoutResult
{
    public required string SessionId { get; init; }
    public required string CheckoutUrl { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Request to create a Stripe Checkout session for an event fund collection contribution.
/// </summary>
public class CreateCollectionCheckoutSessionRequest
{
    public Guid EventId { get; init; }
    public Guid CollectionId { get; init; }
    public required string EventTitle { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Result of creating a collection Stripe Checkout session.
/// </summary>
public class CollectionCheckoutResult
{
    public required string SessionId { get; init; }
    public required string CheckoutUrl { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Request to create a Stripe Checkout session for a money sponsorship.
/// </summary>
public class CreateSponsorCheckoutSessionRequest
{
    public Guid EventId { get; init; }
    public Guid SponsorId { get; init; }
    public required string EventTitle { get; init; }
    public string? SponsorOrganization { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Result of creating a sponsor Stripe Checkout session.
/// </summary>
public class SponsorCheckoutResult
{
    public required string SessionId { get; init; }
    public required string CheckoutUrl { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Request to create a Stripe Checkout session for an add-on purchase.
/// Price is snapshotted from AddOnDefinition at checkout creation time (M3).
/// </summary>
public class CreateAddOnPurchaseCheckoutSessionRequest
{
    public Guid EventId { get; init; }
    public Guid AddOnPurchaseId { get; init; }
    public Guid AddOnDefinitionId { get; init; }
    public required string EventTitle { get; init; }
    public required string AddOnName { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Result of creating an add-on purchase Stripe Checkout session.
/// </summary>
public class AddOnPurchaseCheckoutResult
{
    public required string SessionId { get; init; }
    public required string CheckoutUrl { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Phase 6A.157 — request to create a Stripe Checkout session for a
/// packaged sponsorship purchase. Mirrors
/// <see cref="CreateSponsorCheckoutSessionRequest"/> but carries the
/// package-specific fields needed to render a clear Stripe line-item
/// description ("Gold Sponsor — Includes 3 tickets") and to populate the
/// webhook metadata for routing through PackageSponsorWebhookHandler.
/// </summary>
public class CreatePackageSponsorCheckoutSessionRequest
{
    public Guid EventId { get; init; }
    public Guid SponsorId { get; init; }
    public Guid SponsorshipPackageId { get; init; }
    public required string EventTitle { get; init; }
    public required string PackageName { get; init; }
    public string? PackageTier { get; init; }
    public string? SponsorOrganization { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Phase 6A.157 — included-ticket count drives a conditional appendix on
    /// the Stripe line-item description (per user pivot 2026-05-31, tickets
    /// are purely informational; organizer handles admission off-platform).
    /// Zero suppresses the appendix entirely.
    /// </summary>
    public int IncludedTicketCount { get; init; }

    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Phase 6A.157 — result of creating a packaged sponsorship Stripe Checkout
/// session. Same shape as <see cref="SponsorCheckoutResult"/>.
/// </summary>
public class PackageSponsorCheckoutResult
{
    public required string SessionId { get; init; }
    public required string CheckoutUrl { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Request to create a Stripe Checkout session for a multi-item add-on cart.
/// Creates N line items in a single Stripe session.
/// </summary>
public class CreateAddOnCartCheckoutSessionRequest
{
    public Guid EventId { get; init; }
    public required string EventTitle { get; init; }
    public required List<AddOnCartCheckoutLineItem> Items { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Single line item in an add-on cart checkout session.
/// </summary>
public class AddOnCartCheckoutLineItem
{
    public Guid AddOnPurchaseId { get; init; }
    public Guid AddOnDefinitionId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = "USD";
}