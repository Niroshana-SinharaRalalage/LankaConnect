using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Phase 6A.24: Repository interface for ticket operations
/// </summary>
public interface ITicketRepository : IRepository<Ticket>
{
    /// <summary>
    /// Gets a ticket by its unique ticket code
    /// </summary>
    Task<Ticket?> GetByTicketCodeAsync(string ticketCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a ticket by registration ID
    /// </summary>
    Task<Ticket?> GetByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets for an event
    /// </summary>
    Task<IReadOnlyList<Ticket>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets for a user
    /// </summary>
    Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a ticket code already exists
    /// </summary>
    Task<bool> TicketCodeExistsAsync(string ticketCode, CancellationToken cancellationToken = default);
}
