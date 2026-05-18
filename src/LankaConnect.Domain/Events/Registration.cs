using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Entities;
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

    // Phase 8 S8.2 — pending seat-assignment stash. Set during the RSVP handler
    // (status=Preliminary, before Stripe Checkout); read by the
    // checkout-completed webhook to drive ConfirmSeatAssignments + write
    // SeatReservation rows; cleared on either success or checkout-expired path.
    // Mutually exclusive with the seated steady-state on Attendees[*].SeatId
    // (which is populated AFTER ConfirmSeatAssignments succeeds in the same UoW).
    private readonly List<PendingSeatAssignment> _pendingSeatAssignments = new();
    public IReadOnlyList<PendingSeatAssignment> PendingSeatAssignments
        => _pendingSeatAssignments.AsReadOnly();
    public string? PendingSeatSessionId { get; private set; }

    // Phase 7E: Snapshot of the event's RegistrationMode at construction time.
    // Email re-renders for cancellation/reminder use this snapshot, NOT the live Event value,
    // so an organiser flipping the mode after the fact does not corrupt historical messaging.
    // Default = DetailedAttendees so all legacy rows (column DEFAULT 0) materialise correctly.
    public RegistrationMode RegistrationMode { get; private set; } = RegistrationMode.DetailedAttendees;

    // Phase 7E: Lead attendee name. Populated only for head-count modes (B1-B4).
    // Null for DetailedAttendees (use Attendees[0].Name) and NoRegistration (no Registration row at all).
    public string? LeadAttendeeName { get; private set; }

    // Phase 7E: Composite head-count breakdown (Total + Demographics? + TierCounts?).
    // Populated only for head-count modes (B1-B4). Mutually exclusive with the Attendees collection.
    // Persisted as a flat JSONB column via custom ValueConverter + deep-copy ValueComparer
    // (NOT OwnsOne.ToJson — Phase 6A.130 IReadOnlyList rehydration trap).
    public HeadCountBreakdown? HeadCount { get; private set; }

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

    // Phase 6A.91: Refund workflow tracking
    /// <summary>
    /// Timestamp when user requested a refund. Set when transitioning from Confirmed to RefundRequested.
    /// </summary>
    public DateTime? RefundRequestedAt { get; private set; }

    /// <summary>
    /// Timestamp when user withdrew their refund request. Set when transitioning from RefundRequested back to Confirmed.
    /// </summary>
    public DateTime? RefundWithdrawnAt { get; private set; }

    /// <summary>
    /// Timestamp when refund was completed by Stripe. Set when transitioning from RefundRequested to Refunded.
    /// </summary>
    public DateTime? RefundCompletedAt { get; private set; }

    /// <summary>
    /// Phase 6A.135: Add-on refund amount persisted during RequestRefund so that
    /// CompleteRefund (triggered by Stripe webhook) can include it in RefundCompletedEvent.
    /// Without persistence, the webhook handler has no way to know the add-on amount.
    /// </summary>
    public decimal? AddOnRefundAmount { get; private set; }

    /// <summary>
    /// Stripe Refund ID returned by Stripe when refund is processed.
    /// Used for reconciliation and customer support.
    /// </summary>
    public string? StripeRefundId { get; private set; }

    // Phase 6A.148: Refund approval workflow — collection of refund requests against this
    // registration. Aggregate invariants enforced in CreateRefundRequest(). Multiple requests
    // can co-exist (one active + multiple historical Rejected/Withdrawn for audit trail).
    private readonly List<RefundRequest> _refundRequests = new();
    public IReadOnlyList<RefundRequest> RefundRequests => _refundRequests.AsReadOnly();

    /// <summary>
    /// Phase 6A.148: True when this registration has a non-terminal refund request
    /// (Pending, Approved, or Processing). Used by the aggregate's single-active-request
    /// guard (architect F1) and by the application layer to prevent hard-deletion while
    /// money is in flight.
    /// </summary>
    public bool HasActiveRefundRequest => _refundRequests.Any(r => r.IsActive);

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
    // Issue #51: Added maxAttendeesPerRegistration parameter (configurable by event organizer)
    public static Result<Registration> CreateWithAttendees(
        Guid eventId,
        Guid? userId,
        IEnumerable<AttendeeDetails> attendees,
        RegistrationContact contact,
        Money totalPrice,
        bool isPaidEvent = false,
        int maxAttendeesPerRegistration = 10)  // Issue #51: Default 10 for backward compatibility
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        if (attendees == null || !attendees.Any())
            return Result<Registration>.Failure("At least one attendee is required");

        var attendeeList = attendees.ToList();

        // Issue #51: Validate max attendees using event's configured limit
        // Also enforce system maximum of 50 as safety net
        var effectiveMax = Math.Min(maxAttendeesPerRegistration, Event.SYSTEM_MAX_ATTENDEES_PER_REGISTRATION);
        if (attendeeList.Count > effectiveMax)
            return Result<Registration>.Failure($"Maximum {effectiveMax} attendees per registration");

        if (contact == null)
            return Result<Registration>.Failure("Contact information is required");

        if (totalPrice == null)
            return Result<Registration>.Failure("Total price is required");

        // Phase 6A.81: Determine status based on payment requirement
        var status = isPaidEvent ? RegistrationStatus.Preliminary : RegistrationStatus.Confirmed;
        var paymentStatus = isPaidEvent ? PaymentStatus.Pending : PaymentStatus.NotRequired;
        var expiresAt = isPaidEvent ? DateTime.UtcNow.AddHours(24) : (DateTime?)null;

        var registration = new Registration
        {
            EventId = eventId,
            UserId = userId,
            AttendeeInfo = null,  // New format doesn't use legacy AttendeeInfo
            Quantity = attendeeList.Count,  // Maintain backward compatibility
            Contact = contact,
            TotalPrice = totalPrice,
            // Phase 6A.81: If paid event, start as Preliminary until payment completes (Three-State Lifecycle)
            Status = status,
            PaymentStatus = paymentStatus,
            // Phase 6A.81: Set checkout expiration for paid events (Stripe expires at 24h, we check at 25h)
            CheckoutSessionExpiresAt = expiresAt
        };

        registration._attendees.AddRange(attendeeList);

        // Phase 6A.136: Removed Console.WriteLine diagnostic logging from domain entity.
        // Registration creation is traced via structured logging in the command handler.

        return Result<Registration>.Success(registration);
    }

    /// <summary>
    /// Phase 7E: Factory for head-count-mode registrations (B1-B4).
    ///
    /// Used when the event's <see cref="Event.RegistrationMode"/> is one of HeadCountOnly,
    /// HeadCountByAge, HeadCountByGender, or HeadCountByAgeAndGender. Captures a single
    /// <paramref name="leadAttendeeName"/> + a composite <paramref name="headCount"/>
    /// breakdown instead of per-attendee rows.
    ///
    /// The mode is snapshotted onto the registration so historical email re-renders survive
    /// later organiser mode changes (architect requirement).
    ///
    /// Mutual exclusion with <see cref="CreateWithAttendees"/> is structural: this factory
    /// does NOT populate the Attendees collection. The reverse factory does NOT populate
    /// HeadCount. Domain invariant: at most one of (Attendees, HeadCount) is populated.
    /// </summary>
    /// <param name="eventId">Event being registered for.</param>
    /// <param name="userId">Authenticated user ID, or null for anonymous registration.</param>
    /// <param name="mode">The event's registration mode (must be one of B1-B4).</param>
    /// <param name="leadAttendeeName">Name of the lead attendee — printed on emails.</param>
    /// <param name="headCount">Composite head-count breakdown built via one of the static factories on <see cref="HeadCountBreakdown"/>.</param>
    /// <param name="contact">Shared contact info (email, phone, etc.) — required for emails.</param>
    /// <param name="totalPrice">Total price for the registration. Use Money.Zero for free events.</param>
    /// <param name="isPaidEvent">True if Stripe checkout is required (sets Preliminary + Pending lifecycle).</param>
    /// <returns>Result wrapping the new Registration or a failure with error messages.</returns>
    public static Result<Registration> CreateWithHeadCount(
        Guid eventId,
        Guid? userId,
        RegistrationMode mode,
        string? leadAttendeeName,
        HeadCountBreakdown headCount,
        RegistrationContact contact,
        Money totalPrice,
        bool isPaidEvent = false)
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        // Mode must be a head-count variant. DetailedAttendees should use CreateWithAttendees;
        // NoRegistration should never produce a Registration row at all.
        if (mode != RegistrationMode.HeadCountOnly &&
            mode != RegistrationMode.HeadCountByAge &&
            mode != RegistrationMode.HeadCountByGender &&
            mode != RegistrationMode.HeadCountByAgeAndGender)
        {
            return Result<Registration>.Failure(
                $"CreateWithHeadCount is only valid for head-count modes (HeadCountOnly / HeadCountByAge / " +
                $"HeadCountByGender / HeadCountByAgeAndGender). Received: {mode}. " +
                $"Use CreateWithAttendees for DetailedAttendees mode; NoRegistration mode does not create Registration rows.");
        }

        if (string.IsNullOrWhiteSpace(leadAttendeeName))
            return Result<Registration>.Failure("Lead attendee name is required for head-count modes");

        if (headCount == null)
            return Result<Registration>.Failure("HeadCountBreakdown is required for head-count modes");

        if (contact == null)
            return Result<Registration>.Failure("Contact information is required");

        if (totalPrice == null)
            return Result<Registration>.Failure("Total price is required (use Money.Zero for free events)");

        // Phase 6A.81: Determine status based on payment requirement (mirrors CreateWithAttendees).
        var status = isPaidEvent ? RegistrationStatus.Preliminary : RegistrationStatus.Confirmed;
        var paymentStatus = isPaidEvent ? PaymentStatus.Pending : PaymentStatus.NotRequired;
        var expiresAt = isPaidEvent ? DateTime.UtcNow.AddHours(24) : (DateTime?)null;

        var registration = new Registration
        {
            EventId = eventId,
            UserId = userId,
            AttendeeInfo = null,
            Quantity = headCount.Total,  // Maintain backward compatibility for legacy capacity readers
            Contact = contact,
            TotalPrice = totalPrice,
            RegistrationMode = mode,           // Phase 7E: snapshot the mode
            LeadAttendeeName = leadAttendeeName.Trim(),
            HeadCount = headCount,
            Status = status,
            PaymentStatus = paymentStatus,
            CheckoutSessionExpiresAt = expiresAt
        };

        // Note: _attendees is intentionally NOT populated for head-count registrations.
        // Domain invariant: HeadCount != null XOR _attendees.Any().

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
    /// Session 21: Gets the number of attendees (works with both legacy and new format).
    /// Phase 7E: Now also handles head-count modes (B1-B4) — when HeadCount is populated, its
    /// Total is the canonical attendee count. This is the single mutation point that makes
    /// every consumer (Event.CurrentRegistrations, Event.ReservedCapacity, Event.SpotsLeft,
    /// every Sum(r.GetAttendeeCount()) aggregation) automatically Mode-B aware without
    /// touching every call-site.
    /// </summary>
    public int GetAttendeeCount()
    {
        // Phase 7E: Head-count modes (B1-B4) carry a HeadCountBreakdown.
        if (HeadCount != null)
            return HeadCount.Total;

        // Mode A (DetailedAttendees) — Session 21 multi-attendee format.
        if (_attendees.Any())
            return _attendees.Count;

        // Legacy single-attendee fallback (pre-Session-21).
        return Quantity;
    }

    public void Cancel()
    {
        if (Status != RegistrationStatus.Cancelled)
        {
            Status = RegistrationStatus.Cancelled;
            MarkAsUpdated();
            // Phase 8 S8.3: release seat reservations on cancel.
            RaiseDomainEvent(new DomainEvents.SeatReservationsReleasedEvent(
                EventId, Id, "registration_cancelled"));
        }
    }

    /// <summary>
    /// Force-cancels a registration that is stuck in <see cref="RegistrationStatus.RefundRequested"/>
    /// because the Stripe webhook never completed (or the refund was processed off-platform).
    ///
    /// Why this exists: <c>RefundRequested</c> rows consume capacity until Stripe confirms the
    /// refund. If Stripe never resolves them — common for very old events or when refunds were
    /// processed manually outside the system — the rows are permanently stuck. They block
    /// <see cref="Event.SetRegistrationMode"/> and clutter dashboards. Only an event organiser
    /// (verified at the application layer) can invoke this. Marks the row <c>Cancelled</c> —
    /// <c>Refunded</c> would be misleading because we're not actually issuing a refund here.
    /// </summary>
    /// <returns>Success if the row was force-cancelled, failure with a clear message otherwise.</returns>
    public Result ForceCancelStuckRefund()
    {
        if (Status != RegistrationStatus.RefundRequested)
        {
            return Result.Failure(
                $"Force-cancel is only valid for registrations in RefundRequested status. " +
                $"Current status: {Status}. RegistrationId={Id}");
        }

        Status = RegistrationStatus.Cancelled;
        MarkAsUpdated();
        // Phase 8 S8.3: release seat reservations on force-cancel.
        RaiseDomainEvent(new DomainEvents.SeatReservationsReleasedEvent(
            EventId, Id, "force_cancelled_stuck_refund"));
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.X FIX: Confirms a registration with payment status validation.
    ///
    /// CRITICAL GUARD: This method now validates that PaymentStatus is NOT Pending
    /// before allowing confirmation. This prevents the Confirmed+Pending inconsistent
    /// state that caused recurring bugs where registration details weren't displayed.
    ///
    /// For paid events, use CompletePayment() instead - it properly transitions
    /// both Status (Preliminary → Confirmed) and PaymentStatus (Pending → Completed).
    ///
    /// Valid use cases for Confirm():
    /// - Free events (PaymentStatus = NotRequired)
    /// - Re-confirming cancelled registrations with completed payment
    /// - Admin corrections with proper payment verification
    /// </summary>
    /// <returns>Result indicating success or failure with detailed error message</returns>
    public Result Confirm()
    {
        // Already confirmed - no action needed
        if (Status == RegistrationStatus.Confirmed)
        {
            return Result.Success();
        }

        // CRITICAL GUARD: Prevent Confirmed + Pending state (root cause of recurring bug)
        // This state is invalid because:
        // 1. It indicates payment was never completed for a paid event
        // 2. The frontend cannot properly display registration details
        // 3. It bypasses the Three-State Lifecycle (Preliminary → Confirmed via CompletePayment)
        if (PaymentStatus == PaymentStatus.Pending)
        {
            return Result.Failure(
                $"Cannot confirm registration with PaymentStatus=Pending. " +
                $"For paid events, use CompletePayment() after payment succeeds via Stripe webhook. " +
                $"RegistrationId={Id}, EventId={EventId}");
        }

        Status = RegistrationStatus.Confirmed;
        MarkAsUpdated();
        return Result.Success();
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
    public Result SetStripeCheckoutSession(string sessionId, DateTime? stripeExpiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Result.Failure("Session ID cannot be empty");

        if (PaymentStatus != PaymentStatus.Pending)
            return Result.Failure($"Cannot set checkout session for payment with status {PaymentStatus}");

        StripeCheckoutSessionId = sessionId;
        // Phase 6A.136F: Use Stripe's actual ExpiresAt instead of local calculation to prevent drift
        if (stripeExpiresAt.HasValue)
        {
            CheckoutSessionExpiresAt = stripeExpiresAt.Value;
        }
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Completes payment when Stripe webhook confirms successful payment.
    /// Phase 6A.24: Raises PaymentCompletedEvent for email and ticket generation.
    /// Phase 6A.81: Updated to enforce Three-State Lifecycle - only Preliminary registrations can complete payment.
    /// Issue #56 Fix: Added idempotency guard to prevent duplicate PaymentCompletedEvents from concurrent webhooks.
    /// </summary>
    public Result CompletePayment(string paymentIntentId)
    {
        // Phase 6A.81: Validation - payment intent ID required
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID cannot be empty");

        // Issue #56 FIX: Idempotency guard for duplicate webhook handling
        // If this exact payment intent was already processed, return success without raising events.
        // This prevents duplicate PaymentCompletedEvents when Stripe sends concurrent/retry webhooks.
        // Case-insensitive comparison per Stripe ID conventions.
        if (!string.IsNullOrEmpty(StripePaymentIntentId) &&
            StripePaymentIntentId.Equals(paymentIntentId, StringComparison.OrdinalIgnoreCase))
        {
            // Already completed with this payment intent - idempotent success (no domain event raised)
            return Result.Success();
        }

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
    /// Phase 8 S8.2 — stashes the buyer's intended seat assignments + seat-hold
    /// session id on the registration BEFORE Stripe Checkout. The webhook reads
    /// this list when <c>checkout.session.completed</c> fires and feeds it into
    /// <see cref="ConfirmSeatAssignments"/>.
    ///
    /// Invariants:
    /// <list type="bullet">
    ///   <item><see cref="Status"/> must be <see cref="RegistrationStatus.Preliminary"/>
    ///   (seat stash is meaningful only before payment).</item>
    ///   <item><paramref name="sessionId"/> non-empty.</item>
    ///   <item><paramref name="assignments"/>.Count must equal attendee count.</item>
    ///   <item>AttendeeIndex unique and within range; SeatId non-empty (already
    ///   guaranteed by <see cref="PendingSeatAssignment.Create"/>).</item>
    /// </list>
    /// Replacement-not-append: if a buyer re-issues RSVP with different seats
    /// (e.g., changes selection in another tab before redirect to Stripe), the
    /// second call fully replaces the stash.
    /// </summary>
    public Result SetPendingSeatAssignments(
        string sessionId,
        IReadOnlyList<PendingSeatAssignment> assignments)
    {
        if (assignments == null)
            return Result.Failure("Pending seat assignments are required");

        if (Status != RegistrationStatus.Preliminary)
            return Result.Failure(
                $"Cannot stash pending seat assignments while registration is {Status}. " +
                $"Status must be Preliminary (seat stash is for pre-payment registrations only). " +
                $"RegistrationId={Id}");

        if (string.IsNullOrWhiteSpace(sessionId))
            return Result.Failure(
                $"Seat-hold session id is required. RegistrationId={Id}");

        if (assignments.Count != _attendees.Count)
            return Result.Failure(
                $"Pending seat-assignment count {assignments.Count} does not match attendee " +
                $"count {_attendees.Count}. RegistrationId={Id}");

        var seenIndices = new HashSet<int>();
        foreach (var assignment in assignments)
        {
            if (assignment.AttendeeIndex < 0 || assignment.AttendeeIndex >= _attendees.Count)
                return Result.Failure(
                    $"Pending seat-assignment attendee index {assignment.AttendeeIndex} is out " +
                    $"of range [0, {_attendees.Count}). RegistrationId={Id}");

            if (!seenIndices.Add(assignment.AttendeeIndex))
                return Result.Failure(
                    $"Duplicate attendee index {assignment.AttendeeIndex} in pending seat " +
                    $"assignments. RegistrationId={Id}");
        }

        _pendingSeatAssignments.Clear();
        _pendingSeatAssignments.AddRange(assignments);
        PendingSeatSessionId = sessionId;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 8 S8.2 — clears the pending seat-assignment stash. Called by:
    /// <list type="bullet">
    ///   <item><see cref="ConfirmSeatAssignments"/> on the success path (after
    ///   the seats are bound to attendees and reservations are written).</item>
    ///   <item>The checkout-expired webhook (C5 guard) when Stripe reports the
    ///   buyer abandoned the session.</item>
    ///   <item><see cref="MarkAbandoned"/> on the abandoned-preliminary path.</item>
    /// </list>
    /// Idempotent: safe to call when no stash exists. Does NOT change
    /// <see cref="Status"/> — callers control state transitions separately.
    /// </summary>
    public void ClearPendingSeatAssignments()
    {
        _pendingSeatAssignments.Clear();
        PendingSeatSessionId = null;
        MarkAsUpdated();
    }

    /// <summary>
    /// Phase 8 S8.1 — binds the persisted seat assignments to the corresponding
    /// <see cref="AttendeeDetails"/> values and raises <see cref="SeatsReservedEvent"/>.
    /// Called from the Stripe webhook's checkout-completed path AFTER
    /// <see cref="CompletePayment"/> succeeds and BEFORE the unit-of-work commits.
    ///
    /// Invariants:
    /// <list type="bullet">
    ///   <item><see cref="Status"/> must be <see cref="RegistrationStatus.Confirmed"/>
    ///   (this method is the seat-binding step that comes after payment confirms).</item>
    ///   <item><paramref name="assignments"/> must have exactly one entry per attendee
    ///   (Mode-A only; Mode-B/C don't enter this path).</item>
    ///   <item>Each <c>AttendeeIndex</c> is unique and within range.</item>
    ///   <item>Each <c>SeatId</c> is non-empty.</item>
    /// </list>
    /// Idempotent on retry: when every attendee already carries the same seat
    /// assignment as the incoming list, returns Success without raising the
    /// domain event. This protects against webhook redelivery + reconciliation
    /// double-fire.
    /// </summary>
    public Result ConfirmSeatAssignments(
        IReadOnlyList<(int AttendeeIndex, Guid SeatId, string SeatLabel)> assignments)
    {
        if (assignments == null)
            return Result.Failure("Seat assignments are required");

        if (Status != RegistrationStatus.Confirmed)
            return Result.Failure(
                $"Cannot confirm seat assignments while registration is {Status}. " +
                $"Status must be Confirmed. RegistrationId={Id}");

        if (assignments.Count != _attendees.Count)
            return Result.Failure(
                $"Seat-assignment count {assignments.Count} does not match attendee count " +
                $"{_attendees.Count}. RegistrationId={Id}");

        // Reject duplicate or out-of-range attendee indices up-front so we don't
        // half-mutate the collection.
        var seenIndices = new HashSet<int>();
        foreach (var assignment in assignments)
        {
            if (assignment.AttendeeIndex < 0 || assignment.AttendeeIndex >= _attendees.Count)
                return Result.Failure(
                    $"Attendee index {assignment.AttendeeIndex} is out of range " +
                    $"[0, {_attendees.Count}). RegistrationId={Id}");

            if (!seenIndices.Add(assignment.AttendeeIndex))
                return Result.Failure(
                    $"Duplicate attendee index {assignment.AttendeeIndex} in seat assignments. " +
                    $"RegistrationId={Id}");

            if (assignment.SeatId == Guid.Empty)
                return Result.Failure(
                    $"Seat ID is required for attendee index {assignment.AttendeeIndex}. " +
                    $"RegistrationId={Id}");
        }

        // Idempotency guard: if every attendee already carries the same seat assignment,
        // return Success without raising the event. Webhook retries / reconciliation
        // re-runs hit this path.
        var allAlreadyBound = assignments.All(a =>
            _attendees[a.AttendeeIndex].SeatId == a.SeatId
            && _attendees[a.AttendeeIndex].SeatLabel == a.SeatLabel.Trim());
        if (allAlreadyBound)
            return Result.Success();

        // Apply assignments. Build the new list in one pass — if any WithSeat call
        // fails, we abort BEFORE mutating the backing field (no half-state).
        var rebound = new AttendeeDetails[_attendees.Count];
        for (var i = 0; i < _attendees.Count; i++)
            rebound[i] = _attendees[i];

        foreach (var assignment in assignments)
        {
            var withSeatResult = _attendees[assignment.AttendeeIndex]
                .WithSeat(assignment.SeatId, assignment.SeatLabel);
            if (withSeatResult.IsFailure)
                return Result.Failure(
                    $"Cannot bind seat to attendee index {assignment.AttendeeIndex}: " +
                    $"{withSeatResult.Error}");
            rebound[assignment.AttendeeIndex] = withSeatResult.Value;
        }

        _attendees.Clear();
        _attendees.AddRange(rebound);
        MarkAsUpdated();

        // Raise the domain event so downstream handlers (S8.4 metric emission;
        // future ticket-PDF regeneration) get notified.
        var seatTuples = assignments
            .Select(a => (a.SeatId, a.AttendeeIndex, a.SeatLabel))
            .ToList();
        RaiseDomainEvent(new SeatsReservedEvent(EventId, Id, seatTuples));

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
        // Phase 8 S8.3: release seat reservations on payment failure.
        RaiseDomainEvent(new DomainEvents.SeatReservationsReleasedEvent(
            EventId, Id, "payment_failed"));
        return Result.Success();
    }

    /// <summary>
    /// Marks payment as refunded when refund is processed (LEGACY - direct refund without intermediate state)
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
    /// Phase 6A.91: Initiates a refund request for a confirmed paid registration.
    /// This transitions the registration from Confirmed to RefundRequested.
    /// The actual Stripe refund is processed asynchronously, and CompleteRefund() is called
    /// when the charge.refunded webhook is received.
    ///
    /// Business Rules:
    /// - Only Confirmed registrations with Completed payment can request refund
    /// - Cannot request refund after event has started (validated in command handler)
    /// - Raises RefundRequestedEvent for email notification
    /// </summary>
    public Result RequestRefund(decimal additionalRefundAmount = 0m, string? stripeRefundId = null)
    {
        // Validation: Must be Confirmed status
        if (Status != RegistrationStatus.Confirmed)
        {
            return Result.Failure(
                $"Cannot request refund for registration with status {Status}. " +
                $"Only Confirmed registrations can request refunds. RegistrationId={Id}");
        }

        // Validation: Must have completed payment (paid event)
        if (PaymentStatus != PaymentStatus.Completed)
        {
            return Result.Failure(
                $"Cannot request refund for registration with PaymentStatus {PaymentStatus}. " +
                $"Only registrations with Completed payment can request refunds. RegistrationId={Id}");
        }

        // Validation: Must have PaymentIntentId for Stripe refund
        if (string.IsNullOrWhiteSpace(StripePaymentIntentId))
        {
            return Result.Failure(
                $"Cannot request refund without StripePaymentIntentId. " +
                $"This registration may be from before payment tracking was implemented. RegistrationId={Id}");
        }

        // State transition: Confirmed → RefundRequested
        Status = RegistrationStatus.RefundRequested;
        RefundRequestedAt = DateTime.UtcNow;
        // Phase 6A.135: Persist add-on refund amount so CompleteRefund (webhook) can include it
        AddOnRefundAmount = additionalRefundAmount > 0 ? additionalRefundAmount : null;
        // Phase 6A.136C: Store StripeRefundId immediately when refund is initiated at Stripe.
        // This prevents the race condition where a user withdraws after Stripe has already processed the refund.
        if (!string.IsNullOrWhiteSpace(stripeRefundId))
        {
            StripeRefundId = stripeRefundId;
        }
        MarkAsUpdated();

        // Raise domain event for email notification
        var contactEmail = Contact?.Email ?? AttendeeInfo?.Email?.Value ?? string.Empty;
        RaiseDomainEvent(new RefundRequestedEvent(
            EventId,
            Id,
            UserId,
            contactEmail,
            StripePaymentIntentId,
            TotalPrice?.Amount ?? 0m,
            DateTime.UtcNow,
            additionalRefundAmount));

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.91: Withdraws a pending refund request.
    /// This transitions the registration from RefundRequested back to Confirmed.
    /// The user keeps their registration and the Stripe refund is cancelled (if not yet processed).
    ///
    /// Business Rules:
    /// - Only RefundRequested registrations can be withdrawn
    /// - Cannot withdraw after event has started (validated in command handler)
    /// - Raises RefundWithdrawnEvent for audit trail
    /// </summary>
    public Result WithdrawRefundRequest()
    {
        // Validation: Must be in RefundRequested status
        if (Status != RegistrationStatus.RefundRequested)
        {
            return Result.Failure(
                $"Cannot withdraw refund request for registration with status {Status}. " +
                $"Only RefundRequested registrations can be withdrawn. RegistrationId={Id}");
        }

        // Phase 6A.136C: Guard against withdrawal after Stripe has already processed the refund.
        // StripeRefundId is set in RequestRefund when the refund is initiated at Stripe.
        // Once Stripe has the refund, it cannot be cancelled — withdrawal would leave the domain
        // in Confirmed state while the money is already refunded at Stripe.
        if (!string.IsNullOrWhiteSpace(StripeRefundId))
        {
            return Result.Failure(
                $"Cannot withdraw refund request — the refund has already been submitted to Stripe " +
                $"(RefundId: {StripeRefundId}). The refund will complete automatically. RegistrationId={Id}");
        }

        // State transition: RefundRequested → Confirmed
        Status = RegistrationStatus.Confirmed;
        RefundWithdrawnAt = DateTime.UtcNow;
        MarkAsUpdated();

        // Raise domain event for audit trail
        var contactEmail = Contact?.Email ?? AttendeeInfo?.Email?.Value ?? string.Empty;
        RaiseDomainEvent(new RefundWithdrawnEvent(
            EventId,
            Id,
            UserId,
            contactEmail,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.91: Completes a refund after Stripe processes it.
    /// Called when the charge.refunded webhook is received.
    /// This transitions the registration from RefundRequested to Refunded.
    ///
    /// Business Rules:
    /// - Only RefundRequested registrations can complete refund
    /// - Requires valid Stripe Refund ID
    /// - Raises RefundCompletedEvent for email notification
    /// </summary>
    public Result CompleteRefund(string stripeRefundId)
    {
        // Validation: Refund ID required
        if (string.IsNullOrWhiteSpace(stripeRefundId))
        {
            return Result.Failure("Stripe Refund ID is required to complete refund");
        }

        // Validation: Must be in RefundRequested status
        if (Status != RegistrationStatus.RefundRequested)
        {
            return Result.Failure(
                $"Cannot complete refund for registration with status {Status}. " +
                $"Only RefundRequested registrations can complete refund. RegistrationId={Id}");
        }

        // State transition: RefundRequested → Refunded
        Status = RegistrationStatus.Refunded;
        PaymentStatus = PaymentStatus.Refunded;
        StripeRefundId = stripeRefundId;
        RefundCompletedAt = DateTime.UtcNow;
        MarkAsUpdated();

        // Raise domain event for email notification
        // Phase 6A.135: Include persisted AddOnRefundAmount so completion email shows combined total
        var contactEmail = Contact?.Email ?? AttendeeInfo?.Email?.Value ?? string.Empty;
        RaiseDomainEvent(new RefundCompletedEvent(
            EventId,
            Id,
            UserId,
            contactEmail,
            stripeRefundId,
            TotalPrice?.Amount ?? 0m,
            DateTime.UtcNow,
            AddOnRefundAmount ?? 0m));

        // Phase 8 S8.3: release seat reservations on refund completion.
        RaiseDomainEvent(new DomainEvents.SeatReservationsReleasedEvent(
            EventId, Id, "refund_completed"));

        return Result.Success();
    }

    // =====================================================================================
    // Phase 6A.148 — Refund Approval Workflow entry points
    //
    // NEW MODEL (post operator-feedback rework): cancellation and refund are DECOUPLED.
    // The registration's lifecycle is owned by Cancel / Confirm / Refund methods; the
    // refund money lifecycle is owned by the RefundRequest aggregate. CreateRefundRequest
    // no longer mutates Registration.Status — it only attaches a Pending RefundRequest.
    // Callers (CancelRsvpCommandHandler.PaidBranch, organizer-initiated handler, standalone
    // refund-request handler) decide independently whether to also cancel the registration.
    // =====================================================================================

    /// <summary>
    /// Phase 6A.148: Creates a refund request against this registration. Two paths:
    ///
    /// - <b>Attendee path</b> (<paramref name="isOrganizerInitiated"/> = false): request is
    ///   created in <see cref="RefundRequestStatus.Pending"/>; an organizer must approve.
    ///   Accepts both Confirmed (standalone-refund use case) and Cancelled (cancel+refund
    ///   compound use case — registration is already Cancelled before this is called).
    /// - <b>Organizer path</b> (<paramref name="isOrganizerInitiated"/> = true): request is
    ///   created directly in <see cref="RefundRequestStatus.Approved"/>; RefundExecutionService
    ///   queues Stripe dispatch. Always operates on Confirmed registrations.
    ///
    /// Validation (architect F1, F7, F9):
    /// - No other active refund request (Pending|Approved|Processing).
    /// - Registration must be <see cref="RegistrationStatus.Confirmed"/> OR (attendee path
    ///   only) <see cref="RegistrationStatus.Cancelled"/>; PaymentStatus must be Completed;
    ///   StripePaymentIntentId must be present.
    /// - Scan guard: if <paramref name="anyTicketsScanned"/> is true, the attendee path is
    ///   blocked; organizer path is blocked unless <paramref name="overrideScanGuard"/> is
    ///   true (and <paramref name="organizerNotes"/> is non-empty — enforced by the entity).
    /// - Line items must be non-empty and unique per (Type, ReferenceId).
    ///
    /// On success: a Pending RefundRequest is appended to <see cref="RefundRequests"/>.
    /// Registration.Status is NOT mutated by this method (decoupled lifecycles).
    /// </summary>
    public Result<RefundRequest> CreateRefundRequest(
        Guid requestedByUserId,
        bool isOrganizerInitiated,
        string? requesterReason,
        string? organizerNotes,
        bool overrideScanGuard,
        bool anyTicketsScanned,
        IReadOnlyList<RefundRequestLineItemInput> lineItems)
    {
        // Architect F1 ordering: check active-request guard BEFORE status so the more
        // specific user-facing message fires when both would trip.
        if (HasActiveRefundRequest)
            return Result<RefundRequest>.Failure(
                "There is already an active refund request for this registration. " +
                "Wait for it to be resolved or withdraw it first.");

        // Allowed registration states:
        //   - Confirmed: standalone refund (attendee or organizer) OR organizer-initiated
        //   - Cancelled: only attendee compound cancel+refund path
        // Anything else (Preliminary, Abandoned, Refunded, etc.) is rejected.
        var allowedFromCancelled = !isOrganizerInitiated && Status == RegistrationStatus.Cancelled;
        var allowedFromConfirmed = Status == RegistrationStatus.Confirmed;
        if (!allowedFromConfirmed && !allowedFromCancelled)
            return Result<RefundRequest>.Failure(
                $"Cannot create refund request: Registration status {Status} is not eligible. " +
                $"Allowed: Confirmed (or Cancelled for attendee-initiated cancel-and-refund). " +
                $"RegistrationId={Id}");

        if (PaymentStatus != PaymentStatus.Completed)
            return Result<RefundRequest>.Failure(
                $"Cannot create refund request: PaymentStatus must be Completed (current: {PaymentStatus}). " +
                $"RegistrationId={Id}");

        if (string.IsNullOrWhiteSpace(StripePaymentIntentId))
            return Result<RefundRequest>.Failure(
                $"Cannot create refund request: StripePaymentIntentId is missing. " +
                $"RegistrationId={Id}");

        // Scan guard. Attendee path: always blocked when any ticket scanned.
        // Organizer path: blocked unless explicitly overridden.
        if (anyTicketsScanned)
        {
            if (!isOrganizerInitiated)
                return Result<RefundRequest>.Failure(
                    "Cannot request refund: one or more tickets have been scanned and used. " +
                    "Contact the event organizer if you believe this is in error.");

            if (!overrideScanGuard)
                return Result<RefundRequest>.Failure(
                    "Cannot initiate refund: one or more tickets have been scanned. " +
                    "Use the override option with a justification note to proceed.");
        }

        if (lineItems is null || lineItems.Count == 0)
            return Result<RefundRequest>.Failure("Refund request must include at least one line item");

        // Architect F9: uniqueness per (Type, ReferenceId).
        var duplicateKey = lineItems
            .GroupBy(li => new { li.Type, li.ReferenceId })
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateKey is not null)
            return Result<RefundRequest>.Failure(
                $"Refund request contains duplicate line item for " +
                $"(Type={duplicateKey.Key.Type}, ReferenceId={duplicateKey.Key.ReferenceId}). " +
                $"Each charge can be referenced at most once.");

        // Delegate to entity factories. The entity owns line-item construction + own validation.
        var requestResult = isOrganizerInitiated
            ? RefundRequest.CreateOrganizerInitiated(Id, requestedByUserId, organizerNotes,
                overrideScanGuard && anyTicketsScanned, lineItems)
            : RefundRequest.CreatePending(Id, requestedByUserId, requesterReason, lineItems);

        if (requestResult.IsFailure)
            return Result<RefundRequest>.Failure(requestResult.Errors);

        var req = requestResult.Value;
        _refundRequests.Add(req);
        MarkAsUpdated();

        // The entity raised its own creation event with EventId=Empty (it doesn't know it).
        // Re-raise from the aggregate with the real EventId so handlers can route by event.
        ReplaceLastRequestEventWithEventId(req);

        return Result<RefundRequest>.Success(req);
    }

    /// <summary>
    /// Phase 6A.148: Application-layer hook called by ApproveRefundRequestCommandHandler
    /// or by RefundExecutionService when dispatching Stripe begins. Transitions
    /// Registration.Status: PendingRefundApproval → RefundRequested so the legacy webhook
    /// completion path + ForceCancelStuckRefund recovery operate exactly as today.
    /// </summary>
    public Result MoveToRefundRequestedFromApproval()
    {
        if (Status != RegistrationStatus.PendingRefundApproval)
            return Result.Failure(
                $"Cannot move to RefundRequested: current status is {Status} " +
                $"(expected PendingRefundApproval). RegistrationId={Id}");

        Status = RegistrationStatus.RefundRequested;
        RefundRequestedAt = DateTime.UtcNow;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.148: Application-layer hook called when a refund request is rejected by
    /// the organizer or withdrawn by the attendee. Returns Registration to Confirmed
    /// so the attendee retains their seat / capacity slot.
    /// </summary>
    public Result MoveToConfirmedFromApproval()
    {
        if (Status != RegistrationStatus.PendingRefundApproval)
            return Result.Failure(
                $"Cannot move to Confirmed: current status is {Status} " +
                $"(expected PendingRefundApproval). RegistrationId={Id}");

        Status = RegistrationStatus.Confirmed;
        RefundWithdrawnAt = DateTime.UtcNow;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Internal helper — the entity factory raised a creation event with EventId=Empty
    /// because it doesn't know the EventId. Re-raise from the aggregate with the populated
    /// EventId so downstream handlers (which listen on Registration.DomainEvents) can
    /// route by event.
    /// </summary>
    private void ReplaceLastRequestEventWithEventId(RefundRequest req)
    {
        if (req.IsOrganizerInitiated)
        {
            RaiseDomainEvent(new OrganizerInitiatedRefundCreatedEvent(
                EventId: EventId,
                RegistrationId: Id,
                RefundRequestId: req.Id,
                OrganizerUserId: req.RequestedByUserId,
                OrganizerNotes: req.OrganizerNotes,
                ScanGuardOverridden: req.ScanGuardOverridden,
                CreatedAt: req.RequestedAt));
        }
        else
        {
            RaiseDomainEvent(new RefundRequestCreatedEvent(
                EventId: EventId,
                RegistrationId: Id,
                RefundRequestId: req.Id,
                RequestedByUserId: req.RequestedByUserId,
                RequesterReason: req.RequesterReason,
                RequestedAt: req.RequestedAt));
        }
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

        // Phase 8 S8.3: release seat reservations on abandonment. Defensive: a
        // Preliminary registration shouldn't have seat_reservations rows yet
        // (those come from the webhook conversion in S8.2.C), but the handler
        // is idempotent — DeleteByRegistrationIdAsync is a no-op when no rows.
        RaiseDomainEvent(new DomainEvents.SeatReservationsReleasedEvent(
            EventId, Id, "checkout_abandoned"));

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
    /// <param name="maxAttendeesPerRegistration">Issue #51: Event's configured max attendees (default 10)</param>
    /// <returns>Result indicating success or failure with error message</returns>
    public Result UpdateDetails(IEnumerable<AttendeeDetails> newAttendees, RegistrationContact newContact, int maxAttendeesPerRegistration = 10)
    {
        // Validation: Attendees list cannot be null or empty
        if (newAttendees == null || !newAttendees.Any())
            return Result.Failure("At least one attendee is required");

        var attendeeList = newAttendees.ToList();

        // Issue #51: Validate max attendees using event's configured limit
        var effectiveMax = Math.Min(maxAttendeesPerRegistration, Event.SYSTEM_MAX_ATTENDEES_PER_REGISTRATION);
        if (attendeeList.Count > effectiveMax)
            return Result.Failure($"Maximum {effectiveMax} attendees per registration");

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

        // Raise domain event for email notification
        RaiseDomainEvent(new RegistrationDetailsUpdatedEvent(
            EventId,
            Id,
            UserId,
            GetAttendeeCount(),
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Adds additional attendees to an existing paid registration.
    /// Called after additional payment has been confirmed.
    ///
    /// Business Rules:
    /// - Only Confirmed registrations can add attendees
    /// - Only registrations with Completed payment can add attendees
    /// - Cannot exceed the event's max attendees per registration
    /// - At least one new attendee is required
    ///
    /// Part of the Add-Only Attendees with Delta Payment feature.
    /// </summary>
    /// <param name="additionalAttendees">New attendees to add.</param>
    /// <param name="newTotalPrice">Updated total price including new attendees.</param>
    /// <param name="additionalPayment">The RegistrationPayment for this addition.</param>
    /// <param name="registrationAdditionId">ID of the RegistrationAddition record being merged.</param>
    /// <param name="maxAttendeesPerRegistration">Event's configured max attendees.</param>
    /// <returns>Result indicating success or failure with error message.</returns>
    public Result AddAttendees(
        IEnumerable<AttendeeDetails> additionalAttendees,
        Money newTotalPrice,
        RegistrationPayment additionalPayment,
        Guid registrationAdditionId,
        int maxAttendeesPerRegistration = 10)
    {
        // Validation: Must be Confirmed registration
        if (Status != RegistrationStatus.Confirmed)
            return Result.Failure($"Can only add attendees to confirmed registrations. Current status: {Status}");

        // Validation: Must have completed payment
        if (PaymentStatus != PaymentStatus.Completed)
            return Result.Failure($"Can only add attendees to paid registrations. Current payment status: {PaymentStatus}");

        // Validation: Attendees list cannot be null or empty
        if (additionalAttendees == null || !additionalAttendees.Any())
            return Result.Failure("At least one new attendee is required");

        var additionalList = additionalAttendees.ToList();

        // Validation: New total price is required
        if (newTotalPrice == null)
            return Result.Failure("New total price is required");

        // Validation: Payment record is required
        if (additionalPayment == null)
            return Result.Failure("Additional payment record is required");

        // Validation: RegistrationAddition ID is required
        if (registrationAdditionId == Guid.Empty)
            return Result.Failure("Registration Addition ID is required");

        var currentCount = GetAttendeeCount();
        var newCount = currentCount + additionalList.Count;

        // Validation: Cannot exceed max attendees per registration
        var effectiveMax = Math.Min(maxAttendeesPerRegistration, Event.SYSTEM_MAX_ATTENDEES_PER_REGISTRATION);
        if (newCount > effectiveMax)
            return Result.Failure($"Cannot exceed {effectiveMax} attendees per registration. Current: {currentCount}, Adding: {additionalList.Count}");

        // Store previous values for the event
        var previousCount = currentCount;
        var previousTotal = TotalPrice?.Amount ?? 0m;
        var additionalAmount = additionalPayment.Amount.Amount;

        // Add new attendees
        _attendees.AddRange(additionalList);

        // Update quantity to match attendee count
        Quantity = _attendees.Count;

        // Update total price
        TotalPrice = newTotalPrice;

        MarkAsUpdated();

        // Raise domain event for email notification
        RaiseDomainEvent(new AttendeesAddedEvent(
            EventId,
            Id,
            UserId,
            Contact?.Email ?? string.Empty,
            previousCount,
            additionalList.Count,
            newCount,
            additionalAmount,
            newTotalPrice.Currency.ToString(),
            newTotalPrice.Amount,
            registrationAdditionId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.81: Updated state machine to include Three-State Lifecycle transitions
    /// Phase 6A.91: Added RefundRequested transitions for refund workflow
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
            (RegistrationStatus.Confirmed, RegistrationStatus.RefundRequested) => true,  // Phase 6A.91: User requests refund

            // Waitlisted transitions
            (RegistrationStatus.Waitlisted, RegistrationStatus.Confirmed) => true,
            (RegistrationStatus.Waitlisted, RegistrationStatus.Cancelled) => true,

            // Check-in to completion
            // Note: Attended and Completed have same value (5), so only one pattern needed
            (RegistrationStatus.CheckedIn, RegistrationStatus.Attended) => true,

            // Cancelled to refunded
            (RegistrationStatus.Cancelled, RegistrationStatus.Refunded) => true,

            // Phase 6A.91: RefundRequested state transitions
            (RegistrationStatus.RefundRequested, RegistrationStatus.Confirmed) => true,  // User withdraws refund request
            (RegistrationStatus.RefundRequested, RegistrationStatus.Refunded) => true,   // Stripe confirms refund

            // Phase 6A.81: Abandoned is a terminal state (no transitions out)
            // Phase 6A.91: Refunded is a terminal state (no transitions out)

            _ => false
        };
    }
#pragma warning restore CS0618

    /// <summary>
    /// Phase 7F-B internal: collapses a Mode-A registration into a Mode-B head-count shape.
    /// Called only via <see cref="Event.ConvertRegistrationMode"/> after the aggregate has
    /// validated and built the new shape. NOT a public API — this method is intentionally
    /// internal so cross-aggregate code can't call it without going through Event.
    ///
    /// Snapshot semantics: the live row's <see cref="RegistrationMode"/> flips to the new
    /// mode. Audit table preserves the pre-conversion <see cref="Attendees"/> shape via the
    /// <c>BeforeShape</c> jsonb (recorded by the handler).
    /// </summary>
    internal Result ApplyConvertToHeadCountMode(
        RegistrationMode targetMode,
        HeadCountBreakdown headCount,
        string? leadName)
    {
        if (!IsHeadCountTargetMode(targetMode))
            return Result.Failure($"ApplyConvertToHeadCountMode: target mode {targetMode} is not a head-count mode");
        if (headCount == null)
            return Result.Failure("ApplyConvertToHeadCountMode: headCount is required");

        _attendees.Clear();
        HeadCount = headCount;
        LeadAttendeeName = leadName;
        RegistrationMode = targetMode;
        return Result.Success();
    }

    /// <summary>
    /// Phase 7F-B internal: explodes a Mode-B registration into Mode-A placeholder rows.
    /// </summary>
    internal Result ApplyConvertToDetailedAttendees(IReadOnlyList<AttendeeDetails> placeholders)
    {
        if (placeholders == null || placeholders.Count == 0)
            return Result.Failure("ApplyConvertToDetailedAttendees: placeholders are required");

        _attendees.Clear();
        foreach (var p in placeholders)
            _attendees.Add(p);
        HeadCount = null;
        LeadAttendeeName = null;
        RegistrationMode = RegistrationMode.DetailedAttendees;
        return Result.Success();
    }

    private static bool IsHeadCountTargetMode(RegistrationMode mode) =>
        mode == RegistrationMode.HeadCountOnly
        || mode == RegistrationMode.HeadCountByAge
        || mode == RegistrationMode.HeadCountByGender
        || mode == RegistrationMode.HeadCountByAgeAndGender;

    /// <summary>
    /// Phase 7F-D (architect-approved 2026-04-30): merges a Mode-B head-count addition
    /// into this registration. Mirrors <see cref="AddAttendees"/> at the contract level
    /// (Confirmed + PaymentCompleted required, max-attendees cap enforced) but operates
    /// on the head-count axis instead of per-attendee rows.
    ///
    /// Mode-match invariant (architect §2.2): the addition's mode MUST equal the
    /// registration's <see cref="RegistrationMode"/>. Cross-mode merges (Mode-A
    /// registration + Mode-B addition, or B2 + B4) are rejected — defence in depth on top
    /// of the application-layer validator.
    ///
    /// Tier-counts merge by <c>TierId</c> (sum of counts; tier-name from the addition,
    /// architect "live name preferred over snapshot" pattern). Demographics accumulate
    /// leaf-by-leaf within the same family (B2+B2, B4+B4); cross-family already rejected
    /// by the mode-match check.
    ///
    /// LeadAttendeeName is intentionally preserved — additions don't change the lead.
    ///
    /// Atomic / replay-safe: this method only mutates state when all guards pass. The
    /// Stripe webhook handler is responsible for idempotency at the addition-row level
    /// (architect plan §3 7F-D.5).
    /// </summary>
    /// <param name="additionMode">Mode snapshot from the <c>RegistrationAddition</c> row.</param>
    /// <param name="headCountDelta">The delta head-count to merge in.</param>
    /// <param name="newTotalPrice">Post-merge total price (computed by the handler upstream).</param>
    /// <param name="maxAttendeesPerRegistration">Event-level cap applied to the merged total.</param>
    public Result MergeHeadCountAddition(
        RegistrationMode additionMode,
        HeadCountBreakdown headCountDelta,
        Money newTotalPrice,
        int maxAttendeesPerRegistration = 10)
    {
        if (Status != RegistrationStatus.Confirmed)
            return Result.Failure(
                $"MergeHeadCountAddition: registration must be Confirmed to merge an addition. " +
                $"Current status: {Status}.");

        if (PaymentStatus != PaymentStatus.Completed)
            return Result.Failure(
                $"MergeHeadCountAddition: payment must be completed before merging. " +
                $"Current payment status: {PaymentStatus}.");

        if (headCountDelta == null)
            return Result.Failure("Head-count delta is required");

        if (newTotalPrice == null)
            return Result.Failure("New total price is required");

        // Mode-match invariant (architect §2.2): addition mode == registration mode.
        if (additionMode != RegistrationMode)
            return Result.Failure(
                $"Cannot merge a {additionMode} addition into a {RegistrationMode} registration. " +
                "Addition mode must match the parent's RegistrationMode.");

        // Defence in depth — Mode A and Mode C should never reach here, but be explicit.
        if (RegistrationMode == RegistrationMode.DetailedAttendees
            || RegistrationMode == RegistrationMode.NoRegistration)
            return Result.Failure(
                $"MergeHeadCountAddition is for head-count modes only. RegistrationMode={RegistrationMode}.");

        if (HeadCount == null)
            return Result.Failure(
                "Registration has no HeadCount populated — cannot merge a head-count delta.");

        // Compute the merged shape.
        var mergeResult = MergeHeadCountBreakdowns(HeadCount, headCountDelta);
        if (mergeResult.IsFailure)
            return Result.Failure(mergeResult.Errors);
        var merged = mergeResult.Value;

        // Max-attendees cap.
        var effectiveMax = Math.Min(maxAttendeesPerRegistration, Event.SYSTEM_MAX_ATTENDEES_PER_REGISTRATION);
        if (merged.Total > effectiveMax)
            return Result.Failure(
                $"Maximum {effectiveMax} attendees per registration. " +
                $"Current: {HeadCount.Total}, delta: {headCountDelta.Total}, post-merge: {merged.Total}.");

        // All guards pass — apply.
        HeadCount = merged;
        TotalPrice = newTotalPrice;
        Quantity = merged.Total;
        return Result.Success();
    }

    /// <summary>
    /// Internal helper: builds a new <see cref="HeadCountBreakdown"/> by accumulating the
    /// delta into the existing one. Same-family modes only (mode-match invariant already
    /// enforced upstream).
    /// </summary>
    private static Result<HeadCountBreakdown> MergeHeadCountBreakdowns(
        HeadCountBreakdown existing, HeadCountBreakdown delta)
    {
        // Tier-counts merge by TierId. Live name from the delta (latest), falling back to
        // the existing snapshot if the delta doesn't carry that tier.
        IReadOnlyList<TierCount>? mergedTiers = null;
        if (existing.TierCounts != null || delta.TierCounts != null)
        {
            var byId = new Dictionary<Guid, TierCount>();
            foreach (var tc in existing.TierCounts ?? Array.Empty<TierCount>())
                byId[tc.TierId] = tc;
            foreach (var tc in delta.TierCounts ?? Array.Empty<TierCount>())
            {
                if (byId.TryGetValue(tc.TierId, out var prior))
                {
                    var sum = prior.Count + tc.Count;
                    int? adultSum = (prior.AdultCount.HasValue || tc.AdultCount.HasValue)
                        ? (prior.AdultCount ?? prior.Count) + (tc.AdultCount ?? tc.Count) -
                          // Subtract whichever side doesn't have age split (we're double-counting otherwise).
                          ((!prior.AdultCount.HasValue ? prior.Count : 0) + (!tc.AdultCount.HasValue ? tc.Count : 0))
                          + (!prior.AdultCount.HasValue && !tc.AdultCount.HasValue ? sum : 0)
                        : null;
                    int? childSum = (prior.ChildCount.HasValue || tc.ChildCount.HasValue)
                        ? (prior.ChildCount ?? 0) + (tc.ChildCount ?? 0)
                        : null;
                    // If neither side had age split, leave both null.
                    if (!prior.AdultCount.HasValue && !tc.AdultCount.HasValue)
                    {
                        adultSum = null;
                        childSum = null;
                    }
                    // Phase 7F-E.7: also merge the optional per-tier 4-leaf split.
                    // Symmetric to age-split: sum leaf-by-leaf when at least one side has it,
                    // null when neither did.
                    int? amSum = null, afSum = null, cmSum = null, cfSum = null;
                    if (prior.HasFourLeafSplit || tc.HasFourLeafSplit)
                    {
                        amSum = (prior.AdultMaleCount ?? 0) + (tc.AdultMaleCount ?? 0);
                        afSum = (prior.AdultFemaleCount ?? 0) + (tc.AdultFemaleCount ?? 0);
                        cmSum = (prior.ChildMaleCount ?? 0) + (tc.ChildMaleCount ?? 0);
                        cfSum = (prior.ChildFemaleCount ?? 0) + (tc.ChildFemaleCount ?? 0);
                    }
                    var rebuilt = TierCount.Create(
                        tc.TierId, tc.TierName, sum, adultSum, childSum,
                        amSum, afSum, cmSum, cfSum);
                    if (rebuilt.IsFailure)
                        return Result<HeadCountBreakdown>.Failure(rebuilt.Errors);
                    byId[tc.TierId] = rebuilt.Value;
                }
                else
                {
                    byId[tc.TierId] = tc;
                }
            }
            mergedTiers = byId.Values.ToList();
        }

        // Mode-specific demographic merge.
        // Sums leaf-by-leaf within the same family.
        Result<HeadCountBreakdown> rebuilt2;
        if (existing.Demographics == null && delta.Demographics == null)
        {
            // B1 + B1 — TotalOnly
            rebuilt2 = HeadCountBreakdown.ForTotalOnly(existing.Total + delta.Total, mergedTiers);
        }
        else if (existing.Demographics?.Adults != null
                 || existing.Demographics?.Children != null
                 || delta.Demographics?.Adults != null
                 || delta.Demographics?.Children != null)
        {
            // B2 family
            rebuilt2 = HeadCountBreakdown.ForByAge(
                adults: (existing.Demographics?.Adults ?? 0) + (delta.Demographics?.Adults ?? 0),
                children: (existing.Demographics?.Children ?? 0) + (delta.Demographics?.Children ?? 0),
                mergedTiers);
        }
        else if (existing.Demographics?.Males != null
                 || existing.Demographics?.Females != null
                 || delta.Demographics?.Males != null
                 || delta.Demographics?.Females != null)
        {
            // B3 family
            rebuilt2 = HeadCountBreakdown.ForByGender(
                males: (existing.Demographics?.Males ?? 0) + (delta.Demographics?.Males ?? 0),
                females: (existing.Demographics?.Females ?? 0) + (delta.Demographics?.Females ?? 0),
                mergedTiers);
        }
        else
        {
            // B4 — 4-leaf
            rebuilt2 = HeadCountBreakdown.ForByAgeAndGender(
                adultMales: (existing.Demographics?.AdultMales ?? 0) + (delta.Demographics?.AdultMales ?? 0),
                adultFemales: (existing.Demographics?.AdultFemales ?? 0) + (delta.Demographics?.AdultFemales ?? 0),
                childMales: (existing.Demographics?.ChildMales ?? 0) + (delta.Demographics?.ChildMales ?? 0),
                childFemales: (existing.Demographics?.ChildFemales ?? 0) + (delta.Demographics?.ChildFemales ?? 0),
                mergedTiers);
        }

        return rebuilt2;
    }
}