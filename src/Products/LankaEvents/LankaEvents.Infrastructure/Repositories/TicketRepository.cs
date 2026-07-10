using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using LankaConnect.Products.LankaEvents.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using System.Diagnostics;
using Serilog.Context;

using LankaConnect.BuildingBlocks.Abstractions;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Repositories;

/// <summary>
/// Phase 6A.24: Repository implementation for ticket operations
/// </summary>
public class TicketRepository : ProductRepositoryBase<Ticket>, ITicketRepository
{
    private readonly ILogger<TicketRepository> _repoLogger;

    public TicketRepository(
        LankaEventsDbContext context,
        ILogger<TicketRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<Ticket?> GetByTicketCodeAsync(string ticketCode, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByTicketCode"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("TicketCode", ticketCode))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByTicketCodeAsync START: TicketCode={TicketCode}", ticketCode);

            try
            {
                var ticket = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TicketCode == ticketCode, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByTicketCodeAsync COMPLETE: TicketCode={TicketCode}, Found={Found}, Duration={ElapsedMs}ms",
                    ticketCode,
                    ticket != null,
                    stopwatch.ElapsedMilliseconds);

                return ticket;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByTicketCodeAsync FAILED: TicketCode={TicketCode}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ticketCode,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<Ticket?> GetByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByRegistrationId"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByRegistrationIdAsync START: RegistrationId={RegistrationId}", registrationId);

            try
            {
                var ticket = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.RegistrationId == registrationId && t.IsValid, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByRegistrationIdAsync COMPLETE: RegistrationId={RegistrationId}, Found={Found}, Duration={ElapsedMs}ms",
                    registrationId,
                    ticket != null,
                    stopwatch.ElapsedMilliseconds);

                return ticket;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByRegistrationIdAsync FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    registrationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ticket>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEventIdAsync START: EventId={EventId}", eventId);

            try
            {
                var tickets = await _dbSet
                    .AsNoTracking()
                    .Where(t => t.EventId == eventId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    tickets.Count,
                    stopwatch.ElapsedMilliseconds);

                return tickets;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserId"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByUserIdAsync START: UserId={UserId}", userId);

            try
            {
                var tickets = await _dbSet
                    .AsNoTracking()
                    .Where(t => t.UserId == userId && t.IsValid)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserIdAsync COMPLETE: UserId={UserId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    tickets.Count,
                    stopwatch.ElapsedMilliseconds);

                return tickets;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByUserIdAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> TicketCodeExistsAsync(string ticketCode, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "TicketCodeExists"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("TicketCode", ticketCode))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("TicketCodeExistsAsync START: TicketCode={TicketCode}", ticketCode);

            try
            {
                var exists = await _dbSet.AnyAsync(t => t.TicketCode == ticketCode, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "TicketCodeExistsAsync COMPLETE: TicketCode={TicketCode}, Exists={Exists}, Duration={ElapsedMs}ms",
                    ticketCode,
                    exists,
                    stopwatch.ElapsedMilliseconds);

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "TicketCodeExistsAsync FAILED: TicketCode={TicketCode}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ticketCode,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> AnyValidatedTicketForRegistrationAsync(
        Guid registrationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Untracked existence check — cheap, doesn't load full ticket payloads.
            return await _context.Tickets
                .AsNoTracking()
                .AnyAsync(
                    t => t.RegistrationId == registrationId && t.ValidatedAt != null,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _repoLogger.LogError(ex,
                "AnyValidatedTicketForRegistrationAsync FAILED: RegistrationId={RegistrationId}, Error={ErrorMessage}",
                registrationId, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> TryMarkScannedAsync(
        Guid ticketId,
        DateTime scannedAtUtc,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "TryMarkScanned"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("TicketId", ticketId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogInformation(
                "TryMarkScannedAsync START: TicketId={TicketId}, ScannedAtUtc={ScannedAtUtc:o}",
                ticketId, scannedAtUtc);

            try
            {
                // F1: atomic UPDATE with WHERE clause that ensures only one parallel
                // scan can succeed. ExecuteUpdateAsync bypasses the EF change tracker
                // and emits a single SQL statement; row count returned by the driver
                // is the authoritative race winner.
                var rowsAffected = await _context.Tickets
                    .Where(t => t.Id == ticketId && t.ValidatedAt == null)
                    .ExecuteUpdateAsync(
                        s => s
                            .SetProperty(t => t.ValidatedAt, scannedAtUtc)
                            .SetProperty(t => t.UpdatedAt, scannedAtUtc),
                        cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "TryMarkScannedAsync COMPLETE: TicketId={TicketId}, RowsAffected={RowsAffected}, RaceWinner={RaceWinner}, Duration={ElapsedMs}ms",
                    ticketId, rowsAffected, rowsAffected == 1, stopwatch.ElapsedMilliseconds);

                return rowsAffected;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "TryMarkScannedAsync FAILED: TicketId={TicketId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ticketId, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }
}
