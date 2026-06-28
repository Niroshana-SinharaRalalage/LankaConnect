using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for SeatHold operations.
/// Supports hold lifecycle: create → confirm/expire/release.
/// </summary>
public interface ISeatHoldRepository : IRepository<SeatHold>
{
    /// <summary>
    /// Gets the active hold on a specific seat (at most one due to partial unique index).
    /// </summary>
    Task<SeatHold?> GetActiveHoldForSeatAsync(Guid seatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active holds for a user's checkout session.
    /// </summary>
    Task<IReadOnlyList<SeatHold>> GetActiveHoldsBySessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active holds for a user across all events.
    /// </summary>
    Task<IReadOnlyList<SeatHold>> GetActiveHoldsByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expired but still Active-status holds (for cleanup service).
    /// These are holds where ExpiresAt &lt; utcNow AND Status == Active.
    /// </summary>
    Task<IReadOnlyList<SeatHold>> GetExpiredActiveHoldsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active holds for seats in a specific event (via seat → zone → layout → event chain).
    /// Used for seat availability queries.
    /// </summary>
    Task<IReadOnlyList<SeatHold>> GetActiveHoldsForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any of the given seat IDs have an active hold.
    /// Used for fast pre-check before creating new holds.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetHeldSeatIdsAsync(IEnumerable<Guid> seatIds, CancellationToken cancellationToken = default);
}
