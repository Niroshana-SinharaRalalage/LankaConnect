using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Standalone sponsor entity with dual-mode support: money (Stripe) or item (no payment).
/// Follows the Donation pattern for money sponsors — separate from the Event aggregate.
///
/// Money Sponsor Lifecycle:
/// 1. Sponsor initiates money sponsorship -> Status = Pending, Stripe checkout created
/// 2. Sponsor completes payment -> Status = Completed (via webhook)
///
/// Item Sponsor Lifecycle:
/// 1. Sponsor records item sponsorship -> Status = RecordedItem (immediate, no Stripe)
///
/// Alternative paths (money sponsors only):
/// - Payment fails -> Status = Failed
/// - Checkout expires (24h) -> Status = Abandoned
/// - Payment refunded -> Status = Refunded
/// </summary>
public class Sponsor : BaseEntity
{
    // Event linkage
    public Guid EventId { get; private set; }

    // Sponsor type
    public SponsorType Type { get; private set; }

    // Sponsor contact information
    /// <summary>
    /// User ID of the sponsor. Null for anonymous sponsors.
    /// </summary>
    public Guid? SponsorUserId { get; private set; }

    public string SponsorName { get; private set; } = null!;
    public string SponsorEmail { get; private set; } = null!;
    public string? SponsorPhone { get; private set; }
    public string? SponsorOrganization { get; private set; }
    public string? SponsorNotes { get; private set; }

    // Money sponsor fields (nullable for item sponsors)
    public Money? Amount { get; private set; }

    // Status tracking
    public SponsorStatus Status { get; private set; }

    // Stripe payment fields (money sponsors only)
    public string? StripeCheckoutSessionId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public DateTime? CheckoutExpiresAt { get; private set; }

    // Revenue breakdown fields (money sponsors only, populated after payment)
    public Money? StripeFeeAmount { get; private set; }
    public Money? PlatformCommissionAmount { get; private set; }
    public Money? OrganizerPayoutAmount { get; private set; }

    // Lifecycle timestamps
    public DateTime? PaymentCompletedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? AbandonedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    // Item sponsor fields (nullable for money sponsors)
    public string? ItemName { get; private set; }
    public string? ItemDescription { get; private set; }
    public decimal? EstimatedValue { get; private set; }

    /// <summary>
    /// When the item sponsorship was recorded (item sponsors only).
    /// </summary>
    public DateTime? RecordedAt { get; private set; }

    /// <summary>
    /// Phase 6A.145 — optional public URL of the sponsor's logo/image displayed on the
    /// event details page. Any sponsor can attach an image (no threshold gate as of
    /// Commit 6 per UAT). Always set together with <see cref="ImageBlobName"/>.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Phase 6A.145 — Azure blob name (not URL) used by the upload handler to delete
    /// the old blob when the image is replaced or cleared. Always set together with
    /// <see cref="ImageUrl"/>.
    /// </summary>
    public string? ImageBlobName { get; private set; }

    // EF Core constructor
    private Sponsor()
    {
    }

    /// <summary>
    /// Creates a money-based sponsorship (via Stripe payment).
    /// </summary>
    public static Result<Sponsor> CreateMoneySponsor(
        Guid eventId,
        Guid? sponsorUserId,
        string sponsorName,
        string sponsorEmail,
        string? sponsorPhone,
        string? sponsorOrganization,
        string? sponsorNotes,
        Money amount)
    {
        var validationResult = ValidateCommon(eventId, sponsorName, sponsorEmail);
        if (validationResult.IsFailure)
            return Result<Sponsor>.Failure(validationResult.Error);

        if (amount == null)
            return Result<Sponsor>.Failure("Sponsorship amount is required for money sponsors");

        if (amount.Amount <= 0)
            return Result<Sponsor>.Failure("Sponsorship amount must be greater than zero");

        var sponsor = new Sponsor
        {
            EventId = eventId,
            Type = SponsorType.Money,
            SponsorUserId = sponsorUserId,
            SponsorName = sponsorName.Trim(),
            SponsorEmail = sponsorEmail.Trim().ToLowerInvariant(),
            SponsorPhone = sponsorPhone?.Trim(),
            SponsorOrganization = sponsorOrganization?.Trim(),
            SponsorNotes = sponsorNotes?.Trim(),
            Amount = amount,
            Status = SponsorStatus.Pending
        };

        return Result<Sponsor>.Success(sponsor);
    }

    /// <summary>
    /// Creates an item-based sponsorship (no payment, immediate recording).
    /// Raises ItemSponsorRecordedEvent for acknowledgment email.
    /// </summary>
    public static Result<Sponsor> CreateItemSponsor(
        Guid eventId,
        Guid? sponsorUserId,
        string sponsorName,
        string sponsorEmail,
        string? sponsorPhone,
        string? sponsorOrganization,
        string? sponsorNotes,
        string itemName,
        string? itemDescription,
        decimal? estimatedValue)
    {
        var validationResult = ValidateCommon(eventId, sponsorName, sponsorEmail);
        if (validationResult.IsFailure)
            return Result<Sponsor>.Failure(validationResult.Error);

        if (string.IsNullOrWhiteSpace(itemName))
            return Result<Sponsor>.Failure("Item name is required for item sponsors");

        if (estimatedValue.HasValue && estimatedValue.Value < 0)
            return Result<Sponsor>.Failure("Estimated value cannot be negative");

        var now = DateTime.UtcNow;
        var sponsor = new Sponsor
        {
            EventId = eventId,
            Type = SponsorType.Item,
            SponsorUserId = sponsorUserId,
            SponsorName = sponsorName.Trim(),
            SponsorEmail = sponsorEmail.Trim().ToLowerInvariant(),
            SponsorPhone = sponsorPhone?.Trim(),
            SponsorOrganization = sponsorOrganization?.Trim(),
            SponsorNotes = sponsorNotes?.Trim(),
            ItemName = itemName.Trim(),
            ItemDescription = itemDescription?.Trim(),
            EstimatedValue = estimatedValue,
            Status = SponsorStatus.RecordedItem,
            RecordedAt = now
        };

        sponsor.RaiseDomainEvent(new ItemSponsorRecordedEvent(
            eventId,
            sponsor.Id,
            sponsorUserId,
            sponsorName.Trim(),
            sponsorEmail.Trim().ToLowerInvariant(),
            sponsorOrganization?.Trim(),
            itemName.Trim(),
            itemDescription?.Trim(),
            estimatedValue,
            now));

        return Result<Sponsor>.Success(sponsor);
    }

    /// <summary>
    /// Sets the Stripe checkout session ID and expiration time.
    /// Only valid for money sponsors.
    /// </summary>
    public Result SetStripeCheckoutSession(string sessionId, DateTime expiresAt)
    {
        if (Type != SponsorType.Money)
            return Result.Failure("Cannot set checkout session for item-based sponsors");

        if (Status != SponsorStatus.Pending)
            return Result.Failure($"Cannot set checkout session when status is {Status}");

        if (string.IsNullOrWhiteSpace(sessionId))
            return Result.Failure("Checkout session ID is required");

        if (expiresAt <= DateTime.UtcNow)
            return Result.Failure("Expiration time must be in the future");

        StripeCheckoutSessionId = sessionId;
        CheckoutExpiresAt = expiresAt;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Marks the money sponsorship as completed after receiving Stripe webhook.
    /// Raises SponsorPaymentCompletedEvent for email notifications.
    /// Only valid for money sponsors.
    /// </summary>
    public Result CompletePayment(string paymentIntentId)
    {
        if (Type != SponsorType.Money)
            return Result.Failure("Cannot complete payment for item-based sponsors");

        if (Status != SponsorStatus.Pending)
            return Result.Failure($"Cannot complete payment when status is {Status}");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID is required");

        StripePaymentIntentId = paymentIntentId;
        Status = SponsorStatus.Completed;
        PaymentCompletedAt = DateTime.UtcNow;
        MarkAsUpdated();

        RaiseDomainEvent(new SponsorPaymentCompletedEvent(
            EventId,
            Id,
            SponsorUserId,
            SponsorName,
            SponsorEmail,
            SponsorOrganization,
            paymentIntentId,
            Amount!.Amount,
            Amount.Currency.ToString(),
            PaymentCompletedAt.Value));

        return Result.Success();
    }

    /// <summary>
    /// Marks the money sponsorship as failed due to payment failure.
    /// Only valid for money sponsors.
    /// </summary>
    public Result MarkAsFailed()
    {
        if (Type != SponsorType.Money)
            return Result.Failure("Cannot mark item-based sponsors as failed");

        if (Status != SponsorStatus.Pending)
            return Result.Failure($"Cannot mark as failed when status is {Status}. Must be Pending.");

        Status = SponsorStatus.Failed;
        FailedAt = DateTime.UtcNow;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Marks the money sponsorship as abandoned due to checkout expiration.
    /// Only valid for money sponsors.
    /// </summary>
    public Result MarkAsAbandoned()
    {
        if (Type != SponsorType.Money)
            return Result.Failure("Cannot mark item-based sponsors as abandoned");

        if (Status != SponsorStatus.Pending)
            return Result.Failure($"Cannot mark as abandoned when status is {Status}. Must be Pending.");

        Status = SponsorStatus.Abandoned;
        AbandonedAt = DateTime.UtcNow;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Marks the money sponsorship as refunded after a completed payment.
    /// Only valid for money sponsors.
    /// </summary>
    public Result MarkAsRefunded()
    {
        if (Type != SponsorType.Money)
            return Result.Failure("Cannot refund item-based sponsors");

        if (Status != SponsorStatus.Completed)
            return Result.Failure($"Cannot refund when status is {Status}. Must be Completed.");

        Status = SponsorStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Sets the revenue breakdown (fees and payout) after payment.
    /// Only valid for money sponsors.
    /// </summary>
    public Result SetRevenueBreakdown(Money stripeFee, Money platformCommission, Money organizerPayout)
    {
        if (Type != SponsorType.Money)
            return Result.Failure("Cannot set revenue breakdown for item-based sponsors");

        if (Status != SponsorStatus.Pending && Status != SponsorStatus.Completed)
            return Result.Failure($"Cannot set revenue breakdown when status is {Status}");

        if (stripeFee == null || platformCommission == null || organizerPayout == null)
            return Result.Failure("All revenue breakdown components are required");

        StripeFeeAmount = stripeFee;
        PlatformCommissionAmount = platformCommission;
        OrganizerPayoutAmount = organizerPayout;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Whether the sponsor is in a terminal state (cannot change anymore).
    /// </summary>
    public bool IsTerminal => Status == SponsorStatus.Completed ||
                              Status == SponsorStatus.Failed ||
                              Status == SponsorStatus.Abandoned ||
                              Status == SponsorStatus.Refunded ||
                              Status == SponsorStatus.RecordedItem;

    /// <summary>
    /// Whether the checkout session has expired (money sponsors only).
    /// </summary>
    public bool IsCheckoutExpired => CheckoutExpiresAt.HasValue && DateTime.UtcNow > CheckoutExpiresAt.Value;

    /// <summary>
    /// Whether this is a money-based sponsor.
    /// </summary>
    public bool IsMoneyBased => Type == SponsorType.Money;

    /// <summary>
    /// Whether this is an item-based sponsor.
    /// </summary>
    public bool IsItemBased => Type == SponsorType.Item;

    private static Result ValidateCommon(Guid eventId, string sponsorName, string sponsorEmail)
    {
        if (eventId == Guid.Empty)
            return Result.Failure("Event ID is required");

        if (string.IsNullOrWhiteSpace(sponsorName))
            return Result.Failure("Sponsor name is required");

        if (string.IsNullOrWhiteSpace(sponsorEmail))
            return Result.Failure("Sponsor email is required");

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.145 — set or replace the sponsor's image. Both URL and blob name are
    /// required and set atomically. Any sponsor can attach an image (Commit 6 removed
    /// the threshold gate). Handler is responsible for uploading the new blob first
    /// and deleting any prior blob on replace.
    /// </summary>
    public Result SetImage(string url, string blobName)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure("Image URL is required");
        if (string.IsNullOrWhiteSpace(blobName))
            return Result.Failure("Image blob name is required");

        ImageUrl = url.Trim();
        ImageBlobName = blobName.Trim();
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.145 — clear the sponsor's image. Idempotent — succeeds when no image
    /// is set today. Handler is responsible for deleting the blob from storage.
    /// </summary>
    public Result ClearImage()
    {
        ImageUrl = null;
        ImageBlobName = null;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.145 — completes a money sponsor as off-platform (cash collected by the
    /// organizer directly, bypassing Stripe). Used by the organizer-add-sponsor flow
    /// in <see cref="CreateOffPlatformSponsor"/>. Sets Status=Completed +
    /// PaymentCompletedAt=now. Skips StripeCheckoutSessionId / StripePaymentIntentId /
    /// revenue-breakdown fields (no Stripe payment to account for).
    ///
    /// Architect E-3: cash sponsors are excluded from Stripe-payout totals because
    /// money never went through Stripe; a separate "Off-platform collected" tile in
    /// SponsorsManagementTab surfaces them. Implicit discriminator: a Completed Money
    /// sponsor with StripeCheckoutSessionId == null is "off-platform".
    /// </summary>
    public Result CompleteAsOrganizerCash()
    {
        if (Type != SponsorType.Money)
            return Result.Failure("CompleteAsOrganizerCash applies only to money sponsors");

        if (Status != SponsorStatus.Pending)
            return Result.Failure($"Cannot complete off-platform when status is {Status}. Must be Pending.");

        Status = SponsorStatus.Completed;
        PaymentCompletedAt = DateTime.UtcNow;
        MarkAsUpdated();

        // Reuse SponsorPaymentCompletedEvent so existing email/notification pipelines
        // fire for off-platform sponsors the same way they do for Stripe ones. The
        // PaymentIntentId slot carries a sentinel "off-platform" marker so subscribers
        // can distinguish if needed.
        RaiseDomainEvent(new SponsorPaymentCompletedEvent(
            EventId,
            Id,
            SponsorUserId,
            SponsorName,
            SponsorEmail,
            SponsorOrganization,
            PaymentIntentId: "off-platform",
            Amount!.Amount,
            Amount.Currency.ToString(),
            PaymentCompletedAt.Value));

        return Result.Success();
    }
}
