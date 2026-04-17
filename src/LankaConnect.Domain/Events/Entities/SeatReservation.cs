using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a confirmed seat reservation linking a seat to a specific attendee in a registration.
/// A unique index on seat_id prevents double-booking.
/// On cancellation/refund, the reservation is hard-deleted (V1 — no soft delete).
/// </summary>
public class SeatReservation : BaseEntity
{
    public Guid SeatId { get; private set; }
    public Guid RegistrationId { get; private set; }
    public Guid EventId { get; private set; }
    public int AttendeeIndex { get; private set; }

    // EF Core parameterless constructor
    private SeatReservation() { }

    private SeatReservation(
        Guid seatId,
        Guid registrationId,
        Guid eventId,
        int attendeeIndex)
    {
        SeatId = seatId;
        RegistrationId = registrationId;
        EventId = eventId;
        AttendeeIndex = attendeeIndex;
    }

    /// <summary>
    /// Creates a new seat reservation for an attendee.
    /// </summary>
    public static Result<SeatReservation> Create(
        Guid seatId,
        Guid registrationId,
        Guid eventId,
        int attendeeIndex)
    {
        if (seatId == Guid.Empty)
            return Result<SeatReservation>.Failure("Seat ID is required");

        if (registrationId == Guid.Empty)
            return Result<SeatReservation>.Failure("Registration ID is required");

        if (eventId == Guid.Empty)
            return Result<SeatReservation>.Failure("Event ID is required");

        if (attendeeIndex < 0)
            return Result<SeatReservation>.Failure("Attendee index cannot be negative");

        return Result<SeatReservation>.Success(new SeatReservation(
            seatId,
            registrationId,
            eventId,
            attendeeIndex));
    }
}
