using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 8 S8.2 — value object representing a buyer's intended seat assignment
/// stashed on the <see cref="Registration"/> aggregate AT RSVP TIME (before
/// Stripe Checkout completes). The webhook reads this list when
/// <c>checkout.session.completed</c> fires and uses it to (a) write
/// <c>SeatReservation</c> rows, (b) bind <c>seat_id</c>/<c>seat_label</c>
/// onto each <see cref="AttendeeDetails"/> via
/// <see cref="Registration.ConfirmSeatAssignments"/>.
///
/// Why stashed on the registration instead of read from <c>seat_holds</c> at
/// webhook time: by the time the webhook fires (potentially 30+ minutes
/// later), the buyer's session may hold *different* seats (re-selected in a
/// different tab, on a different event). The seat assignment intended for
/// *this* registration must travel with the registration row.
/// </summary>
public class PendingSeatAssignment : ValueObject
{
    /// <summary>Index into the registration's <c>Attendees</c> list (0-based).</summary>
    public int AttendeeIndex { get; private set; }

    /// <summary>The seat the buyer selected for this attendee.</summary>
    public Guid SeatId { get; private set; }

    /// <summary>Denormalised seat label (e.g., "A1", "T3-S5") for display.</summary>
    public string SeatLabel { get; private set; } = string.Empty;

    // EF Core constructor
    private PendingSeatAssignment() { }

    private PendingSeatAssignment(int attendeeIndex, Guid seatId, string seatLabel)
    {
        AttendeeIndex = attendeeIndex;
        SeatId = seatId;
        SeatLabel = seatLabel;
    }

    public static Result<PendingSeatAssignment> Create(int attendeeIndex, Guid seatId, string? seatLabel)
    {
        if (attendeeIndex < 0)
            return Result<PendingSeatAssignment>.Failure(
                $"AttendeeIndex must be non-negative; got {attendeeIndex}");

        if (seatId == Guid.Empty)
            return Result<PendingSeatAssignment>.Failure("SeatId is required");

        if (string.IsNullOrWhiteSpace(seatLabel))
            return Result<PendingSeatAssignment>.Failure("SeatLabel is required");

        return Result<PendingSeatAssignment>.Success(
            new PendingSeatAssignment(attendeeIndex, seatId, seatLabel.Trim()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return AttendeeIndex;
        yield return SeatId;
        yield return SeatLabel;
    }
}
