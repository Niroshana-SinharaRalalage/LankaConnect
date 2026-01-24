using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

public class Registration : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid? UserId { get; private set; }  // Nullable for anonymous registrations

    // Legacy fields (backward compatibility)
    public AttendeeInfo? AttendeeInfo { get; private set; }  // For anonymous registrations
    public int Quantity { get; private set; }

    // Session 21: Multi-attendee registration with detailed attendee info
    private readonly List<AttendeeDetails> _attendees = new();
    public IReadOnlyList<AttendeeDetails> Attendees => _attendees.AsReadOnly();
    public RegistrationContact? Contact { get; private set; }  // Shared contact info for all attendees
    public Money? TotalPrice { get; private set; }  // Calculated total based on attendee ages

    public RegistrationStatus Status { get; private set; }

    // Session 23: Payment integration for paid events
    public PaymentStatus PaymentStatus { get; private set; }
    public string? StripeCheckoutSessionId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }

    // Phase 6A.81: Payment lifecycle tracking
    /// <summary>
    /// Timestamp when Stripe checkout session expires (24 hours from creation).
    /// Set only for Preliminary registrations (paid events waiting for payment).
    /// </summary>
    public DateTime? CheckoutSessionExpiresAt { get; private set; }

    /// <summary>
    /// Timestamp when registration was marked as Abandoned (checkout expired or cancelled).
    /// Used for audit trail and soft delete after 30 days.
    /// </summary>
    public DateTime? AbandonedAt { get; private set; }

    // Phase 6A.X: Revenue breakdown components for reporting and reconciliation
    public Money? SalesTaxAmount { get; private set; }
    public Money? StripeFeeAmount { get; private set; }  // Estimated at registration, actual after payment
    public Money? PlatformCommissionAmount { get; private set; }
    public Money? OrganizerPayoutAmount { get; private set; }
    public decimal SalesTaxRate { get; private set; }  // Tax rate at time of registration

    // EF Core constructor
    private Registration() { }

    // Authenticated user registration
    private Registration(Guid eventId, Guid userId, int quantity)
    {
        EventId = eventId;
        UserId = userId;
        AttendeeInfo = null;
        Quantity = quantity;
        Status = RegistrationStatus.Confirmed;
        PaymentStatus = PaymentStatus.NotRequired; // Legacy format defaults to free
    }

    // Anonymous user registration
    private Registration(Guid eventId, AttendeeInfo attendeeInfo, int quantity)
    {
        EventId = eventId;
        UserId = null;
        AttendeeInfo = attendeeInfo;
        Quantity = quantity;
        Status = RegistrationStatus.Confirmed;
        PaymentStatus = PaymentStatus.NotRequired; // Legacy format defaults to free
    }

    // Factory method for authenticated users
    public static Result<Registration> Create(Guid eventId, Guid userId, int quantity)
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        if (userId == Guid.Empty)
            return Result<Registration>.Failure("User ID is required");

        if (quantity <= 0)
            return Result<Registration>.Failure("Quantity must be greater than 0");

        var registration = new Registration(eventId, userId, quantity);
        return Result<Registration>.Success(registration);
    }

    // Factory method for anonymous users (legacy - single attendee)
    public static Result<Registration> CreateAnonymous(Guid eventId, AttendeeInfo attendeeInfo, int quantity)
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        if (attendeeInfo == null)
            return Result<Registration>.Failure("Attendee information is required");

        if (quantity <= 0)
            return Result<Registration>.Failure("Quantity must be greater than 0");

        var registration = new Registration(eventId, attendeeInfo, quantity);
        return Result<Registration>.Success(registration);
    }

    // Session 21: Factory method for multi-attendee registration with contact info
    // Session 23: Updated to support payment status for paid events
    public static Result<Registration> CreateWithAttendees(
        Guid eventId,
        Guid? userId,
        IEnumerable<AttendeeDetails> attendees,
        RegistrationContact contact,
        Money totalPrice,
        bool isPaidEvent = false)
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        if (attendees == null || !attendees.Any())
            return Result<Registration>.Failure("At least one attendee is required");

        var attendeeList = attendees.ToList();

        // Validate max attendees (business rule from ADR)
        if (attendeeList.Count > 10)
            return Result<Registration>.Failure("Maximum 10 attendees per registration");

        if (contact == null)
            return Result<Registration>.Failure("Contact information is required");

        if (totalPrice == null)
            return Result<Registration>.Failure("Total price is required");

        var registration = new Registration
        {
            EventId = eventId,
            UserId = userId,
            AttendeeInfo = null,  // New format doesn't use legacy AttendeeInfo
            Quantity = attendeeList.Count,  // Maintain backward compatibility
            Contact = contact,
            TotalPrice = totalPrice,
            // Phase 6A.81: If paid event, start as Preliminary until payment completes (Three-State Lifecycle)
            Status = isPaidEvent ? RegistrationStatus.Preliminary : RegistrationStatus.Confirmed,
            PaymentStatus = isPaidEvent ? PaymentStatus.Pending : PaymentStatus.NotRequired,
            // Phase 6A.81: Set checkout expiration for paid events (Stripe expires at 24h, we check at 25h)
            CheckoutSessionExpiresAt = isPaidEvent ? DateTime.UtcNow.AddHours(24) : null
        };

        registration._attendees.AddRange(attendeeList);
        return Result<Registration>.Success(registration);
    }

    // Validation method to ensure XOR constraint (either UserId OR AttendeeInfo, not both)
    // Session 21: Updated to support new multi-attendee format
    public bool IsValid()
    {
        // Legacy format validation
        if (AttendeeInfo != null)
            return !UserId.HasValue;  // If legacy AttendeeInfo exists, UserId should be null

        // New format validation
        if (_attendees.Any())
            return Contact != null && TotalPrice != null;  // Multi-attendee must have contact and price

        // Authenticated user without attendee details (legacy format)
        return UserId.HasValue;
    }

    /// <summary>
    /// Session 21: Checks if registration uses new multi-attendee format
    /// </summary>
    public bool HasDetailedAttendees() => _attendees.Any();

    /// <summary>
    /// Session 21: Gets the number of attendees (works with both legacy and new format)
    /// </summary>
    public int GetAttendeeCount()
    {
        if (_attendees.Any())
            return _attendees.Count;

        return Quantity;  // Fallback to legacy quantity field
    }

    public void Cancel()
    {
        if (Status != RegistrationStatus.Cancelled)
        {
            Status = RegistrationStatus.Cancelled;
            MarkAsUpdated();
        }
    }

    public void Confirm()
    {
        if (Status != RegistrationStatus.Confirmed)
        {
            Status = RegistrationStatus.Confirmed;
            MarkAsUpdated();
        }
    }

    public Result CheckIn()
    {
        if (Status != RegistrationStatus.Confirmed)
            return Result.Failure("Only confirmed registrations can be checked in");

        Status = RegistrationStatus.CheckedIn;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result CompleteAttendance()
    {
        if (Status != RegistrationStatus.CheckedIn)
            return Result.Failure("Only checked-in registrations can be completed");

        // Phase 6A.81: Use Attended instead of deprecated Completed
        Status = RegistrationStatus.Attended;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result MoveTo(RegistrationStatus newStatus)
    {
        // Validate state transitions
        if (!IsValidTransition(Status, newStatus))
            return Result.Failure($"Invalid transition from {Status} to {newStatus}");

        Status = newStatus;
        MarkAsUpdated();
        return Result.Success();
    }

    // Session 23: Payment integration methods
    /// <summary>
    /// Sets the Stripe Checkout Session ID when payment session is created
    /// </summary>
    public Result SetStripeCheckoutSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Result.Failure("Session ID cannot be empty");

        if (PaymentStatus != PaymentStatus.Pending)
            return Result.Failure($"Cannot set checkout session for payment with status {PaymentStatus}");

        StripeCheckoutSessionId = sessionId;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Completes payment when Stripe webhook confirms successful payment.
    /// Phase 6A.24: Raises PaymentCompletedEvent for email and ticket generation.
    /// Phase 6A.81: Updated to enforce Three-State Lifecycle - only Preliminary registrations can complete payment.
    /// </summary>
    public Result CompletePayment(string paymentIntentId)
    {
        // Phase 6A.81: Validation - payment intent ID required
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID cannot be empty");

        // Phase 6A.81: Critical validation - registration must be in Preliminary state
        // This prevents double-payment and ensures proper state machine flow
        if (Status != RegistrationStatus.Preliminary)
        {
            return Result.Failure(
                $"Cannot complete payment for registration with status {Status}. " +
                $"Only Preliminary registrations can transition to Confirmed via payment completion. " +
                $"RegistrationId={Id}, EventId={EventId}");
        }

        // Phase 6A.81: Validate payment status is still Pending
        if (PaymentStatus != PaymentStatus.Pending)
        {
            return Result.Failure(
                $"Cannot complete payment with PaymentStatus {PaymentStatus}. " +
                $"Only Pending payments can be completed. RegistrationId={Id}");
        }

        // Phase 6A.81: State transition - Preliminary → Confirmed
        StripePaymentIntentId = paymentIntentId;
        PaymentStatus = PaymentStatus.Completed;
        Status = RegistrationStatus.Confirmed;
        CheckoutSessionExpiresAt = null;  // Clear expiration as payment is complete
        MarkAsUpdated();

        // Phase 6A.24: Raise PaymentCompletedEvent to trigger email and ticket generation
        var contactEmail = Contact?.Email ?? AttendeeInfo?.Email?.Value ?? string.Empty;
        var amountPaid = TotalPrice?.Amount ?? 0m;
        var attendeeCount = GetAttendeeCount();

        RaiseDomainEvent(new PaymentCompletedEvent(
            EventId,
            Id,
            UserId,
            contactEmail,
            paymentIntentId,
            amountPaid,
            attendeeCount,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Marks payment as failed when Stripe reports payment failure
    /// </summary>
    public Result FailPayment()
    {
        if (PaymentStatus != PaymentStatus.Pending)
            return Result.Failure($"Cannot fail payment with status {PaymentStatus}");

        PaymentStatus = PaymentStatus.Failed;
        Status = RegistrationStatus.Cancelled;  // Cancel registration if payment fails
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Marks payment as refunded when refund is processed
    /// </summary>
    public Result RefundPayment()
    {
        if (PaymentStatus != PaymentStatus.Completed)
            return Result.Failure($"Cannot refund payment with status {PaymentStatus}. Only Completed payments can be refunded.");

        PaymentStatus = PaymentStatus.Refunded;
        Status = RegistrationStatus.Refunded;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.81: Marks registration as Abandoned when Stripe checkout expires or user cancels.
    /// This is part of the Three-State Lifecycle to prevent payment bypass.
    /// Abandoned registrations:
    /// - Do NOT consume event capacity
    /// - Do NOT block email from re-registering
    /// - Are soft-deleted after 30 days for audit trail
    /// </summary>
    public Result MarkAbandoned()
    {
        // Phase 6A.81: Only Preliminary registrations can be abandoned
        // This prevents accidental abandonment of confirmed/paid registrations
        if (Status != RegistrationStatus.Preliminary)
        {
            return Result.Failure(
                $"Cannot abandon registration with status {Status}. " +
                $"Only Preliminary registrations can be marked as Abandoned. " +
                $"RegistrationId={Id}, EventId={EventId}");
        }

        // Phase 6A.81: Validate payment is still pending (defensive check)
        if (PaymentStatus != PaymentStatus.Pending)
        {
            return Result.Failure(
                $"Cannot abandon registration with PaymentStatus {PaymentStatus}. " +
                $"Expected Pending payment status. RegistrationId={Id}");
        }

        // Phase 6A.81: State transition - Preliminary → Abandoned
        Status = RegistrationStatus.Abandoned;
        PaymentStatus = PaymentStatus.Failed;  // Mark payment as failed since it was never completed
        AbandonedAt = DateTime.UtcNow;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.X: Sets the revenue breakdown components for this registration
    /// Should be called when registration is created for paid events
    /// </summary>
    public void SetRevenueBreakdown(ValueObjects.RevenueBreakdown breakdown)
    {
        if (breakdown == null)
            return;  // Free events don't have breakdown

        SalesTaxAmount = breakdown.SalesTaxAmount;
        StripeFeeAmount = breakdown.StripeFeeAmount;
        PlatformCommissionAmount = breakdown.PlatformCommission;
        OrganizerPayoutAmount = breakdown.OrganizerPayout;
        SalesTaxRate = breakdown.SalesTaxRate;
        MarkAsUpdated();
    }

    // Internal method for Event aggregate to update quantity
    internal void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(newQuantity));

        Quantity = newQuantity;
        MarkAsUpdated();
    }

    /// <summary>
    /// Phase 6A.14: Updates registration details (attendees and contact information)
    /// Business Rules:
    /// - Cannot update cancelled or refunded registrations
    /// - Cannot change attendee count on paid registrations (only names/ages allowed)
    /// - Maximum 10 attendees per registration
    /// - At least one attendee is required
    /// - Contact information is required
    /// </summary>
    /// <param name="newAttendees">Updated list of attendees</param>
    /// <param name="newContact">Updated contact information</param>
    /// <returns>Result indicating success or failure with error message</returns>
    public Result UpdateDetails(IEnumerable<AttendeeDetails> newAttendees, RegistrationContact newContact)
    {
        // Validation: Attendees list cannot be null or empty
        if (newAttendees == null || !newAttendees.Any())
            return Result.Failure("At least one attendee is required");

        var attendeeList = newAttendees.ToList();

        // Validation: Maximum 10 attendees
        if (attendeeList.Count > 10)
            return Result.Failure("Maximum 10 attendees per registration");

        // Validation: Contact is required
        if (newContact == null)
            return Result.Failure("Contact information is required");

        // Business Rule: Cannot update cancelled registrations
        if (Status == RegistrationStatus.Cancelled)
            return Result.Failure("Cannot update details for a cancelled registration");

        // Business Rule: Cannot update refunded registrations
        if (Status == RegistrationStatus.Refunded)
            return Result.Failure("Cannot update details for a refunded registration");

        // Business Rule: For paid registrations, cannot change attendee count
        // (changing count would affect pricing which requires new payment)
        if (PaymentStatus == PaymentStatus.Completed)
        {
            var currentCount = GetAttendeeCount();
            if (attendeeList.Count != currentCount)
            {
                return Result.Failure(
                    $"Cannot change attendee count on a paid registration. " +
                    $"Current: {currentCount}, Requested: {attendeeList.Count}. " +
                    $"Please cancel and create a new registration to change the number of attendees.");
            }
        }

        // Clear existing attendees and add new ones
        _attendees.Clear();
        _attendees.AddRange(attendeeList);

        // Update contact information
        Contact = newContact;

        // Update quantity to match attendee count (maintain backward compatibility)
        Quantity = attendeeList.Count;

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.81: Updated state machine to include Three-State Lifecycle transitions
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete (Pending and Completed are deprecated but supported for backward compatibility)
    private static bool IsValidTransition(RegistrationStatus from, RegistrationStatus to)
    {
        return (from, to) switch
        {
            // Phase 6A.81: Preliminary state transitions (NEW)
            (RegistrationStatus.Preliminary, RegistrationStatus.Confirmed) => true,  // Payment completed
            (RegistrationStatus.Preliminary, RegistrationStatus.Abandoned) => true,  // Checkout expired
            (RegistrationStatus.Preliminary, RegistrationStatus.Cancelled) => true,  // User cancels before payment

            // Backward compatibility: Pending (deprecated)
            (RegistrationStatus.Pending, RegistrationStatus.Confirmed) => true,
            (RegistrationStatus.Pending, RegistrationStatus.Cancelled) => true,

            // Confirmed state transitions
            (RegistrationStatus.Confirmed, RegistrationStatus.Waitlisted) => true,
            (RegistrationStatus.Confirmed, RegistrationStatus.CheckedIn) => true,
            (RegistrationStatus.Confirmed, RegistrationStatus.Cancelled) => true,

            // Waitlisted transitions
            (RegistrationStatus.Waitlisted, RegistrationStatus.Confirmed) => true,
            (RegistrationStatus.Waitlisted, RegistrationStatus.Cancelled) => true,

            // Check-in to completion
            // Note: Attended and Completed have same value (5), so only one pattern needed
            (RegistrationStatus.CheckedIn, RegistrationStatus.Attended) => true,

            // Cancelled to refunded
            (RegistrationStatus.Cancelled, RegistrationStatus.Refunded) => true,

            // Phase 6A.81: Abandoned is a terminal state (no transitions out)

            _ => false
        };
    }
#pragma warning restore CS0618
}