using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Standalone collection (event fund) contribution entity with its own payment lifecycle.
/// Follows the Donation pattern exactly — separate from the Event aggregate.
///
/// Lifecycle:
/// 1. Contributor initiates contribution -> Status = Pending, Stripe checkout created
/// 2. Contributor completes payment -> Status = Completed (via webhook)
///
/// Alternative paths:
/// - Payment fails -> Status = Failed
/// - Checkout expires (24h) -> Status = Abandoned
/// - Payment refunded -> Status = Refunded
/// </summary>
public class Collection : LegacyBaseEntity
{
    // Event linkage
    public Guid EventId { get; private set; }

    // Contributor information
    /// <summary>
    /// User ID of the contributor. Null for anonymous contributions.
    /// </summary>
    public Guid? ContributorUserId { get; private set; }

    public string ContributorName { get; private set; } = null!;
    public string ContributorEmail { get; private set; } = null!;
    public string? ContributorPhone { get; private set; }
    public string? ContributorNotes { get; private set; }

    // Contribution amount
    public Money Amount { get; private set; } = null!;

    // Status tracking
    public CollectionStatus Status { get; private set; }

    // Stripe payment fields
    public string? StripeCheckoutSessionId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }

    /// <summary>
    /// When the Stripe checkout session expires (24 hours from creation).
    /// </summary>
    public DateTime? CheckoutExpiresAt { get; private set; }

    // Revenue breakdown fields (populated after payment completion)
    public Money? StripeFeeAmount { get; private set; }
    public Money? PlatformCommissionAmount { get; private set; }
    public Money? OrganizerPayoutAmount { get; private set; }

    // Lifecycle timestamps
    public DateTime? PaymentCompletedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? AbandonedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    // EF Core constructor
    private Collection()
    {
    }

    private Collection(
        Guid eventId,
        Guid? contributorUserId,
        string contributorName,
        string contributorEmail,
        string? contributorPhone,
        string? contributorNotes,
        Money amount)
    {
        EventId = eventId;
        ContributorUserId = contributorUserId;
        ContributorName = contributorName;
        ContributorEmail = contributorEmail;
        ContributorPhone = contributorPhone;
        ContributorNotes = contributorNotes;
        Amount = amount;
        Status = CollectionStatus.Pending;
    }

    /// <summary>
    /// Creates a new collection contribution.
    /// </summary>
    public static Result<Collection> Create(
        Guid eventId,
        Guid? contributorUserId,
        string contributorName,
        string contributorEmail,
        string? contributorPhone,
        string? contributorNotes,
        Money amount)
    {
        if (eventId == Guid.Empty)
            return Result<Collection>.Failure("Event ID is required");

        if (string.IsNullOrWhiteSpace(contributorName))
            return Result<Collection>.Failure("Contributor name is required");

        if (string.IsNullOrWhiteSpace(contributorEmail))
            return Result<Collection>.Failure("Contributor email is required");

        if (amount == null)
            return Result<Collection>.Failure("Contribution amount is required");

        if (amount.Amount <= 0)
            return Result<Collection>.Failure("Contribution amount must be greater than zero");

        var collection = new Collection(
            eventId,
            contributorUserId,
            contributorName.Trim(),
            contributorEmail.Trim().ToLowerInvariant(),
            contributorPhone?.Trim(),
            contributorNotes?.Trim(),
            amount);

        return Result<Collection>.Success(collection);
    }

    /// <summary>
    /// Sets the Stripe checkout session ID and expiration time.
    /// </summary>
    public Result SetStripeCheckoutSession(string sessionId, DateTime expiresAt)
    {
        if (Status != CollectionStatus.Pending)
            return Result.Failure($"Cannot set checkout session when status is {Status}");

        if (string.IsNullOrWhiteSpace(sessionId))
            return Result.Failure("Checkout session ID is required");

        if (expiresAt <= DateTime.UtcNow)
            return Result.Failure("Expiration time must be in the future");

        StripeCheckoutSessionId = sessionId;
        CheckoutExpiresAt = expiresAt;

        return Result.Success();
    }

    /// <summary>
    /// Marks the collection as completed after receiving Stripe webhook.
    /// Raises CollectionCompletedEvent for email notifications.
    /// </summary>
    public Result CompletePayment(string paymentIntentId)
    {
        if (Status != CollectionStatus.Pending)
            return Result.Failure($"Cannot complete payment when status is {Status}");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID is required");

        StripePaymentIntentId = paymentIntentId;
        Status = CollectionStatus.Completed;
        PaymentCompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CollectionCompletedEvent(
            EventId,
            Id,
            ContributorUserId,
            ContributorName,
            ContributorEmail,
            paymentIntentId,
            Amount.Amount,
            Amount.Currency.ToString(),
            PaymentCompletedAt.Value));

        return Result.Success();
    }

    /// <summary>
    /// Marks the collection as failed due to payment failure.
    /// </summary>
    public Result MarkAsFailed()
    {
        if (Status != CollectionStatus.Pending)
            return Result.Failure($"Cannot mark as failed when status is {Status}. Must be Pending.");

        Status = CollectionStatus.Failed;
        FailedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the collection as abandoned due to checkout expiration.
    /// </summary>
    public Result MarkAsAbandoned()
    {
        if (Status != CollectionStatus.Pending)
            return Result.Failure($"Cannot mark as abandoned when status is {Status}. Must be Pending.");

        Status = CollectionStatus.Abandoned;
        AbandonedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the collection as refunded after a completed payment.
    /// </summary>
    public Result MarkAsRefunded()
    {
        if (Status != CollectionStatus.Completed)
            return Result.Failure($"Cannot refund when status is {Status}. Must be Completed.");

        Status = CollectionStatus.Refunded;
        RefundedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Sets the revenue breakdown (fees and payout) after payment.
    /// Can only be set on Pending (pre-payment estimate) or Completed collections.
    /// </summary>
    public Result SetRevenueBreakdown(Money stripeFee, Money platformCommission, Money organizerPayout)
    {
        if (Status != CollectionStatus.Pending && Status != CollectionStatus.Completed)
            return Result.Failure($"Cannot set revenue breakdown when status is {Status}");

        if (stripeFee == null || platformCommission == null || organizerPayout == null)
            return Result.Failure("All revenue breakdown components are required");

        StripeFeeAmount = stripeFee;
        PlatformCommissionAmount = platformCommission;
        OrganizerPayoutAmount = organizerPayout;

        return Result.Success();
    }

    /// <summary>
    /// Whether the collection is in a terminal state (cannot change anymore).
    /// </summary>
    public bool IsTerminal => Status == CollectionStatus.Completed ||
                              Status == CollectionStatus.Failed ||
                              Status == CollectionStatus.Abandoned ||
                              Status == CollectionStatus.Refunded;

    /// <summary>
    /// Whether the checkout session has expired.
    /// </summary>
    public bool IsCheckoutExpired => CheckoutExpiresAt.HasValue && DateTime.UtcNow > CheckoutExpiresAt.Value;
}
