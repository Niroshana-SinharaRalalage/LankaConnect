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
    }

    // Anonymous user registration
    private Registration(Guid eventId, AttendeeInfo attendeeInfo, int quantity)
    {
        EventId = eventId;
        UserId = null;
        AttendeeInfo = attendeeInfo;
        Quantity = quantity;
        Status = RegistrationStatus.Confirmed;
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
    public static Result<Registration> CreateWithAttendees(
        Guid eventId,
        Guid? userId,
        IEnumerable<AttendeeDetails> attendees,
        RegistrationContact contact,
        Money totalPrice)
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
            Status = RegistrationStatus.Confirmed
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

    // Internal method for Event aggregate to update quantity
    internal void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(newQuantity));

        Quantity = newQuantity;
        MarkAsUpdated();
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