using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

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
public class RegistrationAddition : BaseEntity
{
    public Guid RegistrationId { get; private set; }
    public Guid EventId { get; private set; }

    // New attendees to be added
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
        MarkAsUpdated();

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
        MarkAsUpdated();

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
        MarkAsUpdated();

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
        MarkAsUpdated();

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
        MarkAsUpdated();

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
