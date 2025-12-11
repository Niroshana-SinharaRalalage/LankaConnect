using LankaConnect.Domain.Common;
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
            // Session 23: If paid event, start as Pending until payment completes
            Status = isPaidEvent ? RegistrationStatus.Pending : RegistrationStatus.Confirmed,
            PaymentStatus = isPaidEvent ? PaymentStatus.Pending : PaymentStatus.NotRequired
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

        Status = RegistrationStatus.Completed;
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
    /// Completes payment when Stripe webhook confirms successful payment
    /// </summary>
    public Result CompletePayment(string paymentIntentId)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID cannot be empty");

        if (PaymentStatus != PaymentStatus.Pending)
            return Result.Failure($"Cannot complete payment with status {PaymentStatus}. Only Pending payments can be completed.");

        StripePaymentIntentId = paymentIntentId;
        PaymentStatus = PaymentStatus.Completed;
        Status = RegistrationStatus.Confirmed;  // Confirm registration when payment succeeds
        MarkAsUpdated();
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

    private static bool IsValidTransition(RegistrationStatus from, RegistrationStatus to)
    {
        return (from, to) switch
        {
            (RegistrationStatus.Pending, RegistrationStatus.Confirmed) => true,
            (RegistrationStatus.Pending, RegistrationStatus.Cancelled) => true,
            (RegistrationStatus.Confirmed, RegistrationStatus.Waitlisted) => true,
            (RegistrationStatus.Confirmed, RegistrationStatus.CheckedIn) => true,
            (RegistrationStatus.Confirmed, RegistrationStatus.Cancelled) => true,
            (RegistrationStatus.Waitlisted, RegistrationStatus.Confirmed) => true,
            (RegistrationStatus.Waitlisted, RegistrationStatus.Cancelled) => true,
            (RegistrationStatus.CheckedIn, RegistrationStatus.Completed) => true,
            (RegistrationStatus.Cancelled, RegistrationStatus.Refunded) => true,
            _ => false
        };
    }
}