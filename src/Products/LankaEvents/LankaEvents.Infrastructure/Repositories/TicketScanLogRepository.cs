using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Repositories;

/// <summary>
/// Phase 6A.141 — EF Core implementation of <see cref="ITicketScanLogRepository"/>.
///
/// Append-only repository; no Update / Delete methods. Inserts are queued via
/// <c>DbSet.AddAsync</c> and persisted by the caller's <c>SaveChangesAsync</c>
/// inside whatever transaction the caller has open.
/// </summary>
public class TicketScanLogRepository : ITicketScanLogRepository
{
    private readonly LankaEventsDbContext _context;
    private readonly ILogger<TicketScanLogRepository> _logger;

    public TicketScanLogRepository(LankaEventsDbContext context, ILogger<TicketScanLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddAsync(TicketScanLog log, CancellationToken cancellationToken = default)
    {
        if (log is null) throw new ArgumentNullException(nameof(log));
        try
        {
            await _context.TicketScanLogs.AddAsync(log, cancellationToken);
            _logger.LogDebug(
                "TicketScanLog queued — EventId={EventId} TicketId={TicketId} Result={Result} Reason={Reason} EntryMethod={EntryMethod}",
                log.EventId, log.TicketId, log.ScanResult, log.RejectionReason, log.EntryMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to queue TicketScanLog for EventId={EventId} TicketId={TicketId}",
                log.EventId, log.TicketId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TicketScanLog>> GetRecentForEventAsync(
        Guid eventId,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRows), "maxRows must be positive");

        return await _context.TicketScanLogs
            .AsNoTracking()
            .Where(l => l.EventId == eventId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(maxRows)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TicketScanLog>> GetForTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TicketScanLogs
            .AsNoTracking()
            .Where(l => l.TicketId == ticketId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(int AcceptedCount, string? LastScannerName, DateTime? LastAcceptedAt)>
        GetAcceptedSummaryForTicketAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default)
    {
        // Two filtered queries against the same index (ticket_id + scan_result + created_at).
        // Accepted rows per ticket are 1 in the steady state and very small (≤3) even with
        // admin-unmark cycles, so this is cheap and clear. A GroupBy projection would save
        // one round-trip but EF translation can be fragile across providers.
        var accepted = await _context.TicketScanLogs
            .AsNoTracking()
            .Where(l => l.TicketId == ticketId && l.ScanResult == TicketScanLog.ScanResultAccepted)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new { l.ScannerName, l.CreatedAt })
            .ToListAsync(cancellationToken);

        if (accepted.Count == 0)
        {
            return (0, null, null);
        }

        var mostRecent = accepted[0];
        return (accepted.Count, mostRecent.ScannerName, mostRecent.CreatedAt);
    }
}
