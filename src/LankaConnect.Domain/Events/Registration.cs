using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events;

public class Registration : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public int Quantity { get; private set; }
    public RegistrationStatus Status { get; private set; }

    // EF Core constructor
    private Registration() { }

    private Registration(Guid eventId, Guid userId, int quantity)
    {
        EventId = eventId;
        UserId = userId;
        Quantity = quantity;
        Status = RegistrationStatus.Confirmed;
    }

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