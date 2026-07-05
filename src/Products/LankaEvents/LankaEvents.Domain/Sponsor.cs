using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain;

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
public class Sponsor : LegacyBaseEntity
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
    /// Phase 6A.145 — optional public URL of the sponsor's <b>logo</b> image displayed
    /// on the event details page. Any sponsor can attach an image (no threshold gate as
    /// of Commit 6 per UAT). Always set together with <see cref="ImageBlobName"/>.
    ///
    /// Phase 6A.162 — semantically this is the LOGO slot. A separate optional BROCHURE
    /// slot lives on <see cref="BrochureUrl"/>/<see cref="BrochureBlobName"/>; the two
    /// slots are orthogonal — touching one does NOT mutate the other.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Phase 6A.145 — Azure blob name (not URL) used by the upload handler to delete
    /// the old blob when the image is replaced or cleared. Always set together with
    /// <see cref="ImageUrl"/>.
    /// </summary>
    public string? ImageBlobName { get; private set; }

    /// <summary>
    /// Phase 6A.162 — optional public URL of the sponsor's <b>brochure / flyer</b>
    /// image. Sibling slot to the logo (<see cref="ImageUrl"/>); the two slots are
    /// orthogonal. Click-to-popup on the public sponsor strip shows the brochure when
    /// set, falling back to the logo when null. Always set together with
    /// <see cref="BrochureBlobName"/>.
    /// </summary>
    public string? BrochureUrl { get; private set; }

    /// <summary>
    /// Phase 6A.162 — Azure blob name for the brochure slot. Used by the upload handler
    /// to delete the old blob on replace/clear. Always set together with
    /// <see cref="BrochureUrl"/>.
    /// </summary>
    public string? BrochureBlobName { get; private set; }

    /// <summary>
    /// Phase 6A.151 — timestamp of the last human-initiated content edit
    /// (UpdateContactFields / UpdateAmount / UpdateItemDetails / UpdateName / image
    /// changes via SetImage / ClearImage when invoked from the edit surface).
    ///
    /// Distinct from <see cref="BaseEntity.UpdatedAt"/> which also fires on lifecycle
    /// transitions (CompletePayment, MarkAsFailed, etc.). A consumer wanting to
    /// answer "what did a human change recently?" should read LastEditedAt, not
    /// UpdatedAt.
    /// </summary>
    public DateTime? LastEditedAt { get; private set; }

    /// <summary>
    /// Phase 6A.151 — user ID of the last human actor to edit content fields.
    /// Null when only lifecycle transitions have run (no content edit has happened).
    /// Set in tandem with <see cref="LastEditedAt"/>.
    /// </summary>
    public Guid? LastEditedBy { get; private set; }

    // =========================================================================
    // Phase 6A.156 — Sponsorship Package linkage (nullable, additive)
    //
    // Generic sponsorship (today's flow) = SponsorshipPackageId IS NULL.
    // Packaged sponsorship (6A.157+) sets SponsorshipPackageId + snapshots the
    // package fields at purchase time so that organizer edits to the package
    // don't retroactively alter historic sponsor receipts (mirrors the verified
    // AddOnPurchase.UnitPrice snapshot pattern).
    //
    // Wiring for these fields lands in 6A.157 (public purchase command via
    // a new Sponsor.CreatePackageSponsor factory). 6A.156 only persists the
    // schema + properties so the EF migration is one ship, not two.
    // =========================================================================

    /// <summary>
    /// Phase 6A.156 — FK to <see cref="SponsorshipPackage"/>. Null for the
    /// existing generic flow (free-form amount or item); non-null for packaged
    /// sponsorships introduced in 6A.157. Database constraint: when set, the
    /// snapshot fields below must also be populated (DB-level CHECK in the EF
    /// configuration).
    /// </summary>
    public Guid? SponsorshipPackageId { get; private set; }

    /// <summary>
    /// Phase 6A.156 — FK to <see cref="Registration"/> when this sponsor was
    /// purchased as part of a registration checkout (RSVP-bundled flow in
    /// 6A.159) or when the package's bundled ticket allocation created a
    /// head-count registration (6A.158). Null for standalone (non-bundled)
    /// sponsors.
    /// </summary>
    public Guid? RegistrationId { get; private set; }

    /// <summary>
    /// Phase 6A.156 — package name at purchase time, denormalized so that
    /// organizer edits to <see cref="SponsorshipPackage.Name"/> don't change
    /// historical receipts. Required when <see cref="SponsorshipPackageId"/>
    /// is set (DB-level CHECK).
    /// </summary>
    public string? PackageNameSnapshot { get; private set; }

    /// <summary>
    /// Phase 6A.156 — package tier label at purchase time. Nullable even when
    /// packaged because the source <see cref="SponsorshipPackage.Tier"/> is
    /// itself optional.
    /// </summary>
    public string? PackageTierSnapshot { get; private set; }

    /// <summary>
    /// Phase 6A.156 — package price at purchase time. Distinct from
    /// <see cref="Amount"/>: for package sponsors the two are equal at create
    /// time, but storing the snapshot separately keeps reporting clear and
    /// allows future top-up/discount semantics on Amount without disturbing
    /// historic price-on-day-of-purchase.
    /// </summary>
    public Money? PackagePriceSnapshot { get; private set; }

    /// <summary>
    /// Phase 6A.156 — included-ticket count at purchase time. Drives the
    /// 6A.158 bundled head-count Registration allocation. Frozen at purchase
    /// so a package edit (e.g., organizer drops Gold from 5 tix to 3) does not
    /// re-allocate already-bought sponsors.
    /// </summary>
    public int? IncludedTicketCountSnapshot { get; private set; }

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
    /// Phase 6A.157 — creates a packaged sponsorship from a chosen
    /// <see cref="SponsorshipPackage"/>. Writes all 6 snapshot fields
    /// (SponsorshipPackageId + 5 snapshot values) atomically so the Sponsor row
    /// reflects the package state at purchase time even if the organizer later
    /// edits the catalogue.
    ///
    /// Status starts at <see cref="SponsorStatus.Pending"/> — caller is expected
    /// to immediately call <see cref="SetStripeCheckoutSession"/> and then
    /// <see cref="CompletePackagePayment"/> on webhook (paid flow), OR to call
    /// <see cref="CompletePackagePayment"/> directly with a sentinel
    /// "free_package_no_payment_required" intent ID for free packages.
    ///
    /// The Money <paramref name="packagePrice"/> is used for BOTH
    /// <see cref="Amount"/> and <see cref="PackagePriceSnapshot"/>. Per EF Core
    /// owned-type rules, the same Money instance cannot be referenced twice —
    /// we materialize two distinct copies via the Money factory.
    ///
    /// Mirrors <see cref="CreateMoneySponsor"/> for non-package fields. Type is
    /// fixed to <see cref="SponsorType.Money"/> (item packages explicitly out of
    /// scope per 6A.156 decision #3). RegistrationId stays null (RSVP-bundled
    /// flow was 6A.159, CANCELLED 2026-05-31).
    /// </summary>
    public static Result<Sponsor> CreatePackageSponsor(
        Guid eventId,
        Guid? sponsorUserId,
        string sponsorName,
        string sponsorEmail,
        string? sponsorPhone,
        string? sponsorOrganization,
        string? sponsorNotes,
        Guid sponsorshipPackageId,
        string packageName,
        string? packageTier,
        Money packagePrice,
        int includedTicketCount)
    {
        var validationResult = ValidateCommon(eventId, sponsorName, sponsorEmail);
        if (validationResult.IsFailure)
            return Result<Sponsor>.Failure(validationResult.Error);

        if (sponsorshipPackageId == Guid.Empty)
            return Result<Sponsor>.Failure("SponsorshipPackageId is required for package sponsors");

        if (string.IsNullOrWhiteSpace(packageName))
            return Result<Sponsor>.Failure("PackageName is required for package sponsors");

        if (packagePrice == null)
            return Result<Sponsor>.Failure("PackagePrice is required for package sponsors");

        if (packagePrice.Amount < 0)
            return Result<Sponsor>.Failure("PackagePrice cannot be negative");

        if (includedTicketCount < 0)
            return Result<Sponsor>.Failure("IncludedTicketCount cannot be negative");

        // EF Core owned-type rule — the same Money instance cannot be shared
        // across two properties. Materialize a second copy for the snapshot.
        var snapshotPriceResult = Money.Create(packagePrice.Amount, packagePrice.Currency);
        if (snapshotPriceResult.IsFailure)
            return Result<Sponsor>.Failure(snapshotPriceResult.Error);

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
            Amount = packagePrice,
            Status = SponsorStatus.Pending,
            // Package snapshot fields (6A.156 schema, 6A.157 wiring)
            SponsorshipPackageId = sponsorshipPackageId,
            RegistrationId = null,
            PackageNameSnapshot = packageName.Trim(),
            PackageTierSnapshot = packageTier?.Trim(),
            PackagePriceSnapshot = snapshotPriceResult.Value,
            IncludedTicketCountSnapshot = includedTicketCount
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
        UpdatedAt = DateTime.UtcNow;

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

        // Phase 6A.157 mutual guard — package sponsors MUST use
        // CompletePackagePayment so the package-specific event fires (which
        // drives the forked email template). Calling generic CompletePayment
        // would raise the wrong event and the buyer would get the generic
        // money-sponsor confirmation instead of the package one.
        if (SponsorshipPackageId.HasValue)
            return Result.Failure("Package sponsors must use CompletePackagePayment, not CompletePayment");

        if (Status != SponsorStatus.Pending)
            return Result.Failure($"Cannot complete payment when status is {Status}");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID is required");

        StripePaymentIntentId = paymentIntentId;
        Status = SponsorStatus.Completed;
        PaymentCompletedAt = DateTime.UtcNow;

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
    /// Phase 6A.157 — completes a packaged sponsorship after Stripe webhook
    /// (or instantly for free packages with sentinel
    /// "free_package_no_payment_required" intent ID). Mirrors
    /// <see cref="CompletePayment"/> but raises the sibling
    /// <see cref="PackageSponsorCompletedEvent"/> instead of the generic one,
    /// so the forked email handler can subscribe cleanly without null-checks
    /// on the snapshot fields.
    ///
    /// Errors when called on a generic (non-package) sponsor — guards prevent
    /// silent misuse from a webhook handler that doesn't branch correctly.
    /// </summary>
    public Result CompletePackagePayment(string paymentIntentId)
    {
        if (Type != SponsorType.Money)
            return Result.Failure("Cannot complete payment for item-based sponsors");

        if (!SponsorshipPackageId.HasValue)
            return Result.Failure("CompletePackagePayment applies only to package sponsors; use CompletePayment for generic money sponsors");

        if (Status != SponsorStatus.Pending)
            return Result.Failure($"Cannot complete payment when status is {Status}");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID is required");

        StripePaymentIntentId = paymentIntentId;
        Status = SponsorStatus.Completed;
        PaymentCompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new PackageSponsorCompletedEvent(
            EventId,
            Id,
            SponsorUserId,
            SponsorName,
            SponsorEmail,
            SponsorOrganization,
            paymentIntentId,
            Amount!.Amount,
            Amount.Currency.ToString(),
            PaymentCompletedAt.Value,
            SponsorshipPackageId.Value,
            PackageNameSnapshot!,
            PackageTierSnapshot,
            IncludedTicketCountSnapshot ?? 0));

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

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.162 — set or replace the sponsor's brochure / flyer image. Both URL
    /// and blob name are required and set atomically. Mirrors <see cref="SetImage"/>
    /// line-for-line — the brochure slot is fully orthogonal to the logo slot.
    /// Handler is responsible for uploading the new blob first and deleting any prior
    /// brochure blob on replace.
    /// </summary>
    public Result SetBrochure(string url, string blobName)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure("Brochure URL is required");
        if (string.IsNullOrWhiteSpace(blobName))
            return Result.Failure("Brochure blob name is required");

        BrochureUrl = url.Trim();
        BrochureBlobName = blobName.Trim();

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.162 — clear the sponsor's brochure / flyer image. Idempotent.
    /// Handler is responsible for deleting the blob from storage. Mirrors
    /// <see cref="ClearImage"/>; the logo slot is untouched.
    /// </summary>
    public Result ClearBrochure()
    {
        BrochureUrl = null;
        BrochureBlobName = null;

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

        // Phase 6A.157 mutual guard — same reason as CompletePayment's guard:
        // off-platform completion of a package sponsor would raise the generic
        // event and send the wrong (non-package) confirmation email. Off-
        // platform package completion is not a supported v1 flow.
        if (SponsorshipPackageId.HasValue)
            return Result.Failure("CompleteAsOrganizerCash does not apply to package sponsors; package sponsors complete via Stripe (or sentinel for free packages)");

        if (Status != SponsorStatus.Pending)
            return Result.Failure($"Cannot complete off-platform when status is {Status}. Must be Pending.");

        Status = SponsorStatus.Completed;
        PaymentCompletedAt = DateTime.UtcNow;

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

    // =========================================================================
    // Phase 6A.151 — Content-edit mutators
    //
    // State-edit matrix (final, post-architect-stress-test):
    //                            | Notes | Org | Amount | Item* | Image | Name |
    //   Pending Money            |  ✅   |  ✅ |   🚫   |  n/a  |  ✅   |  ✅  |
    //   Completed Stripe         |  ✅   |  ✅ |   🚫   |  n/a  |  ✅   |  ✅  |
    //   Completed off-platform   |  ✅   |  ✅ |   👤   |  n/a  |  ✅   |  ✅  |
    //   RecordedItem             |  ✅   |  ✅ |  n/a   |  ✅   |  ✅   |  ✅  |
    //   Failed/Abandoned/Refunded|👤notes|  🚫 |   🚫   |  🚫   |  🚫   |  🚫  |
    //   (✅ both / 👤 organizer-only / 🚫 disallowed)
    //
    // Image edits flow through the existing SetImage/ClearImage methods plus the
    // POST/DELETE /sponsors/{id}/image endpoints whose authz is extended in
    // C3 to allow non-anonymous sponsor-self edits.
    // =========================================================================

    /// <summary>
    /// Phase 6A.151 — terminal lifecycle states where most content edits are
    /// disallowed. Failed / Abandoned / Refunded permit only organizer-driven
    /// notes annotations (e.g. "card declined, sponsor moved to off-platform").
    /// </summary>
    private bool IsTerminalState =>
        Status == SponsorStatus.Failed
        || Status == SponsorStatus.Abandoned
        || Status == SponsorStatus.Refunded;

    /// <summary>
    /// Phase 6A.151 — implicit discriminator: a Completed money sponsor with no
    /// Stripe checkout session ID was recorded via the off-platform organizer
    /// flow (CompleteAsOrganizerCash). Only off-platform money sponsors permit
    /// organizer-only amount edits — Stripe-completed sponsors are hard-blocked
    /// v1 because we cannot reach back into Stripe to top-up or partial-refund
    /// the existing PaymentIntent (deferred to v2).
    /// </summary>
    private bool IsOffPlatformCompleted =>
        Type == SponsorType.Money
        && Status == SponsorStatus.Completed
        && StripeCheckoutSessionId == null;

    /// <summary>
    /// Phase 6A.151 — updates the sponsor's notes and/or organization. PATCH
    /// semantics: null = leave unchanged. Allowed across non-terminal states
    /// regardless of actor (sponsor-self or organizer). On terminal states
    /// (Failed/Abandoned/Refunded), only an organizer can annotate notes;
    /// organization changes are 🚫 because the transaction is dead.
    /// </summary>
    public Result UpdateContactFields(
        string? notes,
        string? organization,
        Guid actorUserId,
        bool actorIsOrganizer)
    {
        if (IsTerminalState)
        {
            if (!actorIsOrganizer)
                return Result.Failure($"Cannot edit a {Status} sponsor as a non-organizer");

            if (organization != null)
                return Result.Failure($"Cannot change organization on a {Status} sponsor — notes-only edits allowed for organizers");
        }

        if (notes != null)
            SponsorNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (organization != null)
            SponsorOrganization = string.IsNullOrWhiteSpace(organization) ? null : organization.Trim();

        MarkEdited(actorUserId);
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.151 — updates the sponsor's display name. Allowed across
    /// non-terminal states regardless of actor. On terminal states the entry is
    /// frozen — name changes muddy historical receipts.
    /// </summary>
    public Result UpdateName(string newName, Guid actorUserId, bool actorIsOrganizer)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure("Sponsor name cannot be empty");

        if (IsTerminalState)
            return Result.Failure($"Cannot change name on a {Status} sponsor");

        SponsorName = newName.Trim();
        MarkEdited(actorUserId);
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.151 — updates the sponsor's contribution amount. Hard rules:
    ///   - Item sponsors have no amount field → always Failure
    ///   - Pending Money → Failure (Stripe checkout session would be stale)
    ///   - Completed Money via Stripe → Failure v1 (Stripe top-up/partial-refund
    ///     deferred to v2; we cannot reach back into the existing PaymentIntent)
    ///   - Completed Money off-platform → organizer-only (revenue impact)
    ///   - Failed/Abandoned/Refunded → Failure (terminal)
    /// New amount must be strictly positive.
    /// </summary>
    public Result UpdateAmount(
        Money newAmount,
        Guid actorUserId,
        bool actorIsOrganizer)
    {
        if (newAmount == null || newAmount.Amount <= 0)
            return Result.Failure("Sponsorship amount must be greater than zero");

        if (Type == SponsorType.Item)
            return Result.Failure("Cannot update amount on an item sponsor");

        if (Status == SponsorStatus.Pending)
            return Result.Failure("Cannot update amount on a Pending money sponsor — the Stripe checkout session would be stale");

        if (Status == SponsorStatus.Completed && !IsOffPlatformCompleted)
            return Result.Failure("Cannot update amount on a Stripe-completed sponsor — top-up / partial-refund flow is deferred to a future release");

        if (IsOffPlatformCompleted && !actorIsOrganizer)
            return Result.Failure("Only an organizer can change the amount on an off-platform sponsor (revenue impact)");

        if (IsTerminalState)
            return Result.Failure($"Cannot update amount on a {Status} sponsor");

        Amount = newAmount;
        MarkEdited(actorUserId);
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.151 — updates the item-sponsor fields. Item sponsors only.
    /// PATCH semantics: null = leave unchanged. itemName cannot be set to empty
    /// when provided; estimatedValue cannot be negative when provided.
    /// </summary>
    public Result UpdateItemDetails(
        string? itemName,
        string? itemDescription,
        decimal? estimatedValue,
        Guid actorUserId)
    {
        if (Type != SponsorType.Item)
            return Result.Failure("Cannot update item details on a money sponsor — this is for item sponsors only");

        if (IsTerminalState)
            return Result.Failure($"Cannot update item details on a {Status} sponsor");

        if (itemName != null && string.IsNullOrWhiteSpace(itemName))
            return Result.Failure("Item name cannot be empty when provided");

        if (estimatedValue.HasValue && estimatedValue.Value < 0)
            return Result.Failure("Estimated value cannot be negative");

        if (itemName != null)
            ItemName = itemName.Trim();

        if (itemDescription != null)
            ItemDescription = string.IsNullOrWhiteSpace(itemDescription) ? null : itemDescription.Trim();

        if (estimatedValue.HasValue)
            EstimatedValue = estimatedValue.Value;

        MarkEdited(actorUserId);
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.151 — sets the audit-trail fields LastEditedAt + LastEditedBy
    /// and bumps the generic UpdatedAt timestamp. Called from every successful
    /// content-edit mutator above. Lifecycle transitions (CompletePayment,
    /// MarkAsFailed, etc.) intentionally do NOT call this — they call only
    /// MarkAsUpdated() so consumers can distinguish system-driven changes from
    /// human edits.
    /// </summary>
    private void MarkEdited(Guid actorUserId)
    {
        LastEditedAt = DateTime.UtcNow;
        LastEditedBy = actorUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
