using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
namespace LankaConnect.Products.LankaEvents.Domain.Repositories;

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

    /// <summary>
    /// Phase 6A.148 — fast scan-guard check for the refund approval workflow.
    /// Returns true if any ticket on this registration has been scanned
    /// (<c>ValidatedAt != null</c>). Used by <c>CreateRefundRequestCommandHandler</c>
    /// to compute the boolean passed to <c>Registration.CreateRefundRequest</c>.
    /// </summary>
    Task<bool> AnyValidatedTicketForRegistrationAsync(
        Guid registrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.141 F1 — race-safe atomic mark-as-scanned.
    ///
    /// Executes a single UPDATE statement of the form:
    ///   <c>UPDATE tickets SET validated_at = @now, updated_at = @now</c>
    ///   <c>WHERE id = @ticketId AND validated_at IS NULL</c>
    ///
    /// Returns the number of rows affected — exactly <c>1</c> if this caller won the
    /// race, exactly <c>0</c> if another scanner already marked the ticket (or the
    /// ticket doesn't exist).
    ///
    /// Uses EF Core 7+ <c>ExecuteUpdateAsync</c>, which bypasses the change tracker
    /// and SaveChanges entirely. Callers using this method as part of a multi-table
    /// audit-write MUST wrap the call in an explicit transaction
    /// (<c>IUnitOfWork.BeginTransactionAsync</c>) so that the resulting UPDATE and any
    /// subsequent <c>AddAsync</c> + <c>SaveChangesAsync</c> on the audit log stay
    /// atomic — see Plan-agent review F2.
    /// </summary>
    /// <returns>Number of rows affected: 1 = winner, 0 = race-loser or ticket missing.</returns>
    Task<int> TryMarkScannedAsync(Guid ticketId, DateTime scannedAtUtc, CancellationToken cancellationToken = default);
}
