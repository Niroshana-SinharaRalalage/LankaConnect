using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Phase 6A.141 — repository for the ticket-scan audit log.
///
/// Append-only by design: a scan attempt produces a new row, never updates an
/// existing one. The admin-unmark flow also writes a NEW row (reversing the
/// effect of a prior accept). This keeps the audit trail forensically complete.
/// </summary>
public interface ITicketScanLogRepository
{
    /// <summary>
    /// Queues the given scan log entry for insert. Caller is responsible for the
    /// surrounding transaction (Phase 6A.141 F2: the scan command handler wraps
    /// the mark-scanned UPDATE and this audit insert in a single transaction so
    /// the two stay atomic).
    /// </summary>
    Task AddAsync(TicketScanLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the last <paramref name="maxRows"/> scan-log rows for the given event,
    /// most-recent first. Used by the organizer dashboard to show real-time check-in
    /// activity at the gate.
    /// </summary>
    Task<IReadOnlyList<TicketScanLog>> GetRecentForEventAsync(
        Guid eventId,
        int maxRows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every scan attempt against the given ticket, ordered oldest first.
    /// Used for dispute resolution ("the attendee says they didn't get in" — pull
    /// the scan history for the ticket code and see what really happened).
    /// </summary>
    Task<IReadOnlyList<TicketScanLog>> GetForTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);
}
