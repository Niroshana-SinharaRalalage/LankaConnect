using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Tracks pending attendee additions to an existing paid registration.
/// Part of the Add-Only Attendees with Delta Payment feature.
///
/// Lifecycle:
/// 1. User initiates addition -> Status = Pending, Stripe checkout created
/// 2. User completes payment -> Status = PaymentCompleted (via webhook)
/// 3. System merges into registration -> Status = Merged
///
/// Alternative paths:
/// - Payment fails -> Status = Failed
/// - Checkout expires (24h) -> Status = Abandoned
/// - User cancels before payment -> Record deleted
/// </summary>
public class RegistrationAddition : LegacyBaseEntity
{
    public Guid RegistrationId { get; private set; }
    public Guid EventId { get; private set; }

    // Phase 7F-D (architect-approved 2026-04-30): registration mode snapshot at addition
    // creation. Drives the polymorphic discriminator + the mode-match invariant on merge.
    // Default DetailedAttendees so legacy rows materialise correctly post-migration.
    public RegistrationMode RegistrationMode { get; private set; } = RegistrationMode.DetailedAttendees;

    /// <summary>
    /// Phase 7F-D: Mode-B head-count delta. Mutually exclusive with <see cref="NewAttendees"/>:
    /// a Mode-A addition has attendees + null head-count; a Mode-B addition has head-count + empty
    /// attendees list. Enforced in factory + DB CHECK constraint (7F-D.2 migration).
    /// </summary>
    public HeadCountBreakdown? HeadCountDelta { get; private set; }

    /// <summary>
    /// Phase 7F-D (architect edit #1): polymorphic discriminator based on the snapshotted
    /// RegistrationMode, NOT on `_newAttendees.Count > 0`. The latter would give a false
    /// positive AFTER a Mode-A addition is merged (the list is moved to the registration
    /// but the row stays). Always read this property for Mode-A vs Mode-B routing.
    /// </summary>
    public bool IsModeBAddition => RegistrationMode != RegistrationMode.DetailedAttendees
                                   && RegistrationMode != RegistrationMode.NoRegistration;

    public bool IsModeAAddition => !IsModeBAddition;

    // New attendees to be added (Mode A only — empty in Mode B additions).
    private readonly List<AttendeeDetails> _newAttendees = new();
    public IReadOnlyList<AttendeeDetails> NewAttendees => _newAttendees.AsReadOnly();

    // Pricing information
    /// <summary>
    /// Total price of the registration BEFORE this addition.
    /// </summary>
    public Money PreviousTotalPrice { get; private set; } = null!;

    /// <summary>
    /// Total price of the registration AFTER this addition (all attendees).
    /// </summary>
    public Money NewTotalPrice { get; private set; } = null!;

    /// <summary>
    /// Additional amount to be charged for this addition.
    /// Calculated as: NewTotalPrice - PreviousTotalPrice
    /// </summary>
    public Money AdditionalAmount { get; private set; } = null!;

    // Payment tracking
    public string? StripeCheckoutSessionId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public RegistrationAdditionStatus Status { get; private set; }

    // Timestamps for lifecycle tracking
    /// <summary>
    /// When the Stripe checkout session expires (24 hours from creation).
    /// </summary>
    public DateTime? CheckoutExpiresAt { get; private set; }

    /// <summary>
    /// When payment was completed via Stripe webhook.
    /// </summary>
    public DateTime? PaymentCompletedAt { get; private set; }

    /// <summary>
    /// When the new attendees were merged into the registration.
    /// </summary>
    public DateTime? MergedAt { get; private set; }

    /// <summary>
    /// When the addition was marked as failed.
    /// </summary>
    public DateTime? FailedAt { get; private set; }

    /// <summary>
    /// When the addition was marked as abandoned (checkout expired).
    /// </summary>
    public DateTime? AbandonedAt { get; private set; }

    // EF Core constructor
    private RegistrationAddition()
    {
        // Required for EF Core
    }

    private RegistrationAddition(
        Guid registrationId,
        Guid eventId,
        IEnumerable<AttendeeDetails> newAttendees,
        Money previousTotalPrice,
        Money newTotalPrice,
        Money additionalAmount)
    {
        RegistrationId = registrationId;
        EventId = eventId;
        _newAttendees.AddRange(newAttendees);
        PreviousTotalPrice = previousTotalPrice;
        NewTotalPrice = newTotalPrice;
        AdditionalAmount = additionalAmount;
        Status = RegistrationAdditionStatus.Pending;
    }

    /// <summary>
    /// Creates a new RegistrationAddition for adding attendees to an existing registration.
    /// </summary>
    /// <param name="registrationId">ID of the existing registration to add attendees to.</param>
    /// <param name="eventId">ID of the event.</param>
    /// <param name="newAttendees">New attendees to add.</param>
    /// <param name="previousTotalPrice">Total price before this addition.</param>
    /// <param name="newTotalPrice">Total price after this addition.</param>
    /// <param name="additionalAmount">Additional amount to charge.</param>
    public static Result<RegistrationAddition> Create(
        Guid registrationId,
        Guid eventId,
        IEnumerable<AttendeeDetails> newAttendees,
        Money previousTotalPrice,
        Money newTotalPrice,
        Money additionalAmount)
    {
        if (registrationId == Guid.Empty)
            return Result<RegistrationAddition>.Failure("Registration ID is required");

        if (eventId == Guid.Empty)
            return Result<RegistrationAddition>.Failure("Event ID is required");

        if (newAttendees == null || !newAttendees.Any())
            return Result<RegistrationAddition>.Failure("At least one new attendee is required");

        if (previousTotalPrice == null)
            return Result<RegistrationAddition>.Failure("Previous total price is required");

        if (newTotalPrice == null)
            return Result<RegistrationAddition>.Failure("New total price is required");

        if (additionalAmount == null)
            return Result<RegistrationAddition>.Failure("Additional amount is required");

        // Validate currencies match
        if (previousTotalPrice.Currency != newTotalPrice.Currency ||
            previousTotalPrice.Currency != additionalAmount.Currency)
            return Result<RegistrationAddition>.Failure("All prices must have the same currency");

        // Validate additional amount is not negative
        if (additionalAmount.Amount < 0)
            return Result<RegistrationAddition>.Failure("Additional amount cannot be negative");

        // Validate price calculation
        var expectedAdditional = newTotalPrice.Amount - previousTotalPrice.Amount;
        if (Math.Abs(additionalAmount.Amount - expectedAdditional) > 0.01m) // Allow for rounding
            return Result<RegistrationAddition>.Failure(
                $"Additional amount ({additionalAmount.Amount}) does not match price difference ({expectedAdditional})");

        var attendeeList = newAttendees.ToList();
        var addition = new RegistrationAddition(
            registrationId,
            eventId,
            attendeeList,
            previousTotalPrice,
            newTotalPrice,
            additionalAmount);

        return Result<RegistrationAddition>.Success(addition);
    }

    /// <summary>
    /// Phase 7F-D (architect-approved 2026-04-30): factory for a Mode-B head-count delta
    /// addition. Mutually exclusive with <see cref="Create"/> (which builds a Mode-A
    /// per-attendee list addition). Required architect-mandated invariants:
    ///
    ///   - <paramref name="mode"/> must be a head-count mode (B1/B2/B3/B4); Mode A and
    ///     Mode C are rejected at the factory.
    ///   - <paramref name="headCountDelta"/> is required.
    ///   - All three Money values share the same currency.
    ///   - <paramref name="additionalAmount"/> matches <c>newTotal − previousTotal</c>
    ///     within 1 cent (mirrors Mode A's tolerance at line 148).
    ///   - For free-event additions, <paramref name="additionalAmount"/> = zero is allowed
    ///     (architect §2.5 — same code path as free Mode-A; no fork).
    /// </summary>
    public static Result<RegistrationAddition> CreateForHeadCountDelta(
        Guid registrationId,
        Guid eventId,
        RegistrationMode mode,
        HeadCountBreakdown headCountDelta,
        Money previousTotal,
        Money newTotal,
        Money additionalAmount)
    {
        if (registrationId == Guid.Empty)
            return Result<RegistrationAddition>.Failure("Registration ID is required");

        if (eventId == Guid.Empty)
            return Result<RegistrationAddition>.Failure("Event ID is required");

        if (headCountDelta == null)
            return Result<RegistrationAddition>.Failure("Head-count delta is required for a Mode-B addition");

        // Mode must be a head-count mode (architect §2.2 mode-match invariant).
        if (mode == RegistrationMode.DetailedAttendees
            || mode == RegistrationMode.NoRegistration)
            return Result<RegistrationAddition>.Failure(
                $"CreateForHeadCountDelta requires a head-count mode (B1/B2/B3/B4); got {mode}. " +
                "Use the per-attendee Create factory for Mode-A additions.");

        if (previousTotal == null || newTotal == null || additionalAmount == null)
            return Result<RegistrationAddition>.Failure("All Money values are required");

        if (previousTotal.Currency != newTotal.Currency
            || previousTotal.Currency != additionalAmount.Currency)
            return Result<RegistrationAddition>.Failure("All prices must have the same currency");

        if (additionalAmount.Amount < 0)
            return Result<RegistrationAddition>.Failure("Additional amount cannot be negative");

        var expectedAdditional = newTotal.Amount - previousTotal.Amount;
        if (Math.Abs(additionalAmount.Amount - expectedAdditional) > 0.01m)
            return Result<RegistrationAddition>.Failure(
                $"Additional amount ({additionalAmount.Amount}) does not match price difference ({expectedAdditional})");

        var addition = new RegistrationAddition(
            registrationId, eventId,
            newAttendees: new List<AttendeeDetails>(), // Mode B: empty attendee list
            previousTotal, newTotal, additionalAmount)
        {
            RegistrationMode = mode,
            HeadCountDelta = headCountDelta,
        };

        return Result<RegistrationAddition>.Success(addition);
    }

    /// <summary>
    /// Sets the Stripe checkout session ID and expiration time.
    /// Called after creating the Stripe checkout session.
    /// </summary>
    /// <param name="sessionId">Stripe checkout session ID.</param>
    /// <param name="expiresAt">When the checkout session expires (typically 24 hours).</param>
    public Result SetStripeCheckoutSession(string sessionId, DateTime expiresAt)
    {
        if (Status != RegistrationAdditionStatus.Pending)
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
    /// Marks the payment as completed after receiving Stripe webhook.
    /// Transitions from Pending to PaymentCompleted.
    /// </summary>
    /// <param name="paymentIntentId">Stripe payment intent ID from the webhook.</param>
    public Result CompletePayment(string paymentIntentId)
    {
        if (Status != RegistrationAdditionStatus.Pending)
            return Result.Failure($"Cannot complete payment when status is {Status}");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID is required");

        StripePaymentIntentId = paymentIntentId;
        Status = RegistrationAdditionStatus.PaymentCompleted;
        PaymentCompletedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the addition as merged after attendees have been added to the registration.
    /// Transitions from PaymentCompleted to Merged.
    /// </summary>
    public Result MarkAsMerged()
    {
        if (Status != RegistrationAdditionStatus.PaymentCompleted)
            return Result.Failure($"Cannot mark as merged when status is {Status}. Must be PaymentCompleted.");

        Status = RegistrationAdditionStatus.Merged;
        MergedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the addition as failed due to payment failure.
    /// Can transition from Pending or PaymentCompleted states.
    /// </summary>
    public Result MarkAsFailed()
    {
        if (Status == RegistrationAdditionStatus.Merged)
            return Result.Failure("Cannot mark as failed when already merged");

        if (Status == RegistrationAdditionStatus.Failed)
            return Result.Failure("Already marked as failed");

        if (Status == RegistrationAdditionStatus.Abandoned)
            return Result.Failure("Cannot mark as failed when abandoned");

        Status = RegistrationAdditionStatus.Failed;
        FailedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the addition as abandoned due to checkout expiration.
    /// Can only transition from Pending state.
    /// </summary>
    public Result MarkAsAbandoned()
    {
        if (Status != RegistrationAdditionStatus.Pending)
            return Result.Failure($"Cannot mark as abandoned when status is {Status}. Must be Pending.");

        Status = RegistrationAdditionStatus.Abandoned;
        AbandonedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Checks if the addition is in a terminal state (cannot change anymore).
    /// </summary>
    public bool IsTerminal => Status == RegistrationAdditionStatus.Merged ||
                              Status == RegistrationAdditionStatus.Failed ||
                              Status == RegistrationAdditionStatus.Abandoned;

    /// <summary>
    /// Checks if the checkout session has expired.
    /// </summary>
    public bool IsCheckoutExpired => CheckoutExpiresAt.HasValue && DateTime.UtcNow > CheckoutExpiresAt.Value;

    /// <summary>
    /// Gets the number of new attendees being added.
    /// </summary>
    public int NewAttendeesCount => _newAttendees.Count;
}
