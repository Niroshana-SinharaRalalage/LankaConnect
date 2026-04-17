using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for SeatReservation operations.
/// Reservations are created on payment completion and hard-deleted on cancellation (V1).
/// </summary>
public interface ISeatReservationRepository : IRepository<SeatReservation>
{
    /// <summary>
    /// Gets the reservation for a specific seat (at most one due to unique index).
    /// </summary>
    Task<SeatReservation?> GetBySeatIdAsync(Guid seatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all seat reservations for a registration.
    /// </summary>
    Task<IReadOnlyList<SeatReservation>> GetByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all seat reservations for an event.
    /// Used for seat availability display (combined with active holds).
    /// </summary>
    Task<IReadOnlyList<SeatReservation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the set of reserved seat IDs from the given list.
    /// Used for fast availability checking.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetReservedSeatIdsAsync(IEnumerable<Guid> seatIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all reservations for a registration (hard delete — V1 cancellation policy).
    /// </summary>
    Task DeleteByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default);
}
