using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using System.Diagnostics.CodeAnalysis;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Standalone donation entity with its own lifecycle — completely separate from Registration.
/// Follows the RegistrationAddition pattern for Stripe payment lifecycle.
///
/// Two creation scenarios:
/// 1. Standalone: Donor donates from event details page at any time (RegistrationId = null)
/// 2. Bundled: Donor adds donation during registration (RegistrationId links to registration)
///    - For paid events: combined Stripe Checkout with ticket + donation as 2 line items
///    - For free events: standalone Stripe Checkout with donation-only line item
///
/// Lifecycle:
/// 1. Donor initiates donation -> Status = Pending, Stripe checkout created
/// 2. Donor completes payment -> Status = Completed (via webhook)
///
/// Alternative paths:
/// - Payment fails -> Status = Failed
/// - Checkout expires (24h) -> Status = Abandoned
/// - Payment refunded -> Status = Refunded
/// </summary>
public class Donation : LegacyBaseEntity
{
    // Event and registration linkage
    public Guid EventId { get; private set; }

    /// <summary>
    /// Links to registration when donation is bundled during registration (combined checkout).
    /// Null for standalone donations from event details page.
    /// </summary>
    public Guid? RegistrationId { get; private set; }

    // Donor information
    /// <summary>
    /// User ID of the donor. Null for anonymous donations.
    /// </summary>
    public Guid? DonorUserId { get; private set; }

    public string DonorName { get; private set; } = null!;
    public string DonorEmail { get; private set; } = null!;
    public string? DonorPhone { get; private set; }
    public string? DonorNotes { get; private set; }

    // Donation amount
    public Money Amount { get; private set; } = null!;

    // Status tracking
    public DonationStatus Status { get; private set; }

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
    [SetsRequiredMembers]
    private Donation()
    {
    }

    [SetsRequiredMembers]
    private Donation(
        Guid eventId,
        Guid? registrationId,
        Guid? donorUserId,
        string donorName,
        string donorEmail,
        string? donorPhone,
        string? donorNotes,
        Money amount)
    {
        EventId = eventId;
        RegistrationId = registrationId;
        DonorUserId = donorUserId;
        DonorName = donorName;
        DonorEmail = donorEmail;
        DonorPhone = donorPhone;
        DonorNotes = donorNotes;
        Amount = amount;
        Status = DonationStatus.Pending;
    }

    /// <summary>
    /// Creates a standalone donation (from event details page, any time).
    /// </summary>
    public static Result<Donation> Create(
        Guid eventId,
        Guid? donorUserId,
        string donorName,
        string donorEmail,
        string? donorPhone,
        string? donorNotes,
        Money amount)
    {
        var validationResult = ValidateCommon(eventId, donorName, donorEmail, amount);
        if (validationResult.IsFailure)
            return Result<Donation>.Failure(validationResult.Error);

        var donation = new Donation(
            eventId,
            registrationId: null,
            donorUserId,
            donorName.Trim(),
            donorEmail.Trim().ToLowerInvariant(),
            donorPhone?.Trim(),
            donorNotes?.Trim(),
            amount);

        return Result<Donation>.Success(donation);
    }

    /// <summary>
    /// Creates a donation bundled with a registration (combined checkout for paid events,
    /// or standalone checkout for free events).
    /// </summary>
    public static Result<Donation> CreateBundledWithRegistration(
        Guid eventId,
        Guid registrationId,
        Guid? donorUserId,
        string donorName,
        string donorEmail,
        string? donorPhone,
        string? donorNotes,
        Money amount)
    {
        if (registrationId == Guid.Empty)
            return Result<Donation>.Failure("Registration ID is required for bundled donations");

        var validationResult = ValidateCommon(eventId, donorName, donorEmail, amount);
        if (validationResult.IsFailure)
            return Result<Donation>.Failure(validationResult.Error);

        var donation = new Donation(
            eventId,
            registrationId,
            donorUserId,
            donorName.Trim(),
            donorEmail.Trim().ToLowerInvariant(),
            donorPhone?.Trim(),
            donorNotes?.Trim(),
            amount);

        return Result<Donation>.Success(donation);
    }

    /// <summary>
    /// Sets the Stripe checkout session ID and expiration time.
    /// </summary>
    public Result SetStripeCheckoutSession(string sessionId, DateTime expiresAt)
    {
        if (Status != DonationStatus.Pending)
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
    /// Marks the donation as completed after receiving Stripe webhook.
    /// Raises DonationCompletedEvent for email notifications.
    /// </summary>
    public Result CompletePayment(string paymentIntentId)
    {
        if (Status != DonationStatus.Pending)
            return Result.Failure($"Cannot complete payment when status is {Status}");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID is required");

        StripePaymentIntentId = paymentIntentId;
        Status = DonationStatus.Completed;
        PaymentCompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DonationCompletedEvent(
            EventId,
            Id,
            DonorUserId,
            DonorName,
            DonorEmail,
            paymentIntentId,
            Amount.Amount,
            Amount.Currency.ToString(),
            PaymentCompletedAt.Value));

        return Result.Success();
    }

    /// <summary>
    /// Marks the donation as failed due to payment failure.
    /// </summary>
    public Result MarkAsFailed()
    {
        if (Status != DonationStatus.Pending)
            return Result.Failure($"Cannot mark as failed when status is {Status}. Must be Pending.");

        Status = DonationStatus.Failed;
        FailedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the donation as abandoned due to checkout expiration.
    /// </summary>
    public Result MarkAsAbandoned()
    {
        if (Status != DonationStatus.Pending)
            return Result.Failure($"Cannot mark as abandoned when status is {Status}. Must be Pending.");

        Status = DonationStatus.Abandoned;
        AbandonedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the donation as refunded after a completed payment.
    /// </summary>
    public Result MarkAsRefunded()
    {
        if (Status != DonationStatus.Completed)
            return Result.Failure($"Cannot refund when status is {Status}. Must be Completed.");

        Status = DonationStatus.Refunded;
        RefundedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Sets the revenue breakdown (fees and payout) after payment.
    /// Can only be set on Pending (pre-payment estimate) or Completed donations.
    /// </summary>
    public Result SetRevenueBreakdown(Money stripeFee, Money platformCommission, Money organizerPayout)
    {
        if (Status != DonationStatus.Pending && Status != DonationStatus.Completed)
            return Result.Failure($"Cannot set revenue breakdown when status is {Status}");

        if (stripeFee == null || platformCommission == null || organizerPayout == null)
            return Result.Failure("All revenue breakdown components are required");

        StripeFeeAmount = stripeFee;
        PlatformCommissionAmount = platformCommission;
        OrganizerPayoutAmount = organizerPayout;

        return Result.Success();
    }

    /// <summary>
    /// Whether the donation is in a terminal state (cannot change anymore).
    /// </summary>
    public bool IsTerminal => Status == DonationStatus.Completed ||
                              Status == DonationStatus.Failed ||
                              Status == DonationStatus.Abandoned ||
                              Status == DonationStatus.Refunded;

    /// <summary>
    /// Whether the checkout session has expired.
    /// </summary>
    public bool IsCheckoutExpired => CheckoutExpiresAt.HasValue && DateTime.UtcNow > CheckoutExpiresAt.Value;

    /// <summary>
    /// Whether this donation is bundled with a registration.
    /// </summary>
    public bool IsBundled => RegistrationId.HasValue;

    private static Result ValidateCommon(Guid eventId, string donorName, string donorEmail, Money amount)
    {
        if (eventId == Guid.Empty)
            return Result.Failure("Event ID is required");

        if (string.IsNullOrWhiteSpace(donorName))
            return Result.Failure("Donor name is required");

        if (string.IsNullOrWhiteSpace(donorEmail))
            return Result.Failure("Donor email is required");

        if (amount == null)
            return Result.Failure("Donation amount is required");

        if (amount.Amount <= 0)
            return Result.Failure("Donation amount must be greater than zero");

        return Result.Success();
    }
}
