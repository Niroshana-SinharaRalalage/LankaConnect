using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.24: Repository implementation for ticket operations
/// </summary>
public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    private readonly ILogger<TicketRepository> _repoLogger;

    public TicketRepository(
        AppDbContext context,
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
}
