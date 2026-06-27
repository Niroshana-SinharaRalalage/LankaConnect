using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Events.Entities;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.61: Repository implementation for EventNotificationHistory tracking
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class EventNotificationHistoryRepository : IEventNotificationHistoryRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<EventNotificationHistory> _dbSet;
    private readonly ILogger<EventNotificationHistoryRepository> _repoLogger;

    public EventNotificationHistoryRepository(
        AppDbContext context,
        ILogger<EventNotificationHistoryRepository> logger)
    {
        _context = context;
        _dbSet = context.EventNotificationHistories;
        _repoLogger = logger;
    }

    public async Task<EventNotificationHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", "EventNotificationHistory"))
        using (LogContext.PushProperty("HistoryId", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByIdAsync START: HistoryId={HistoryId}", id);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByIdAsync COMPLETE: HistoryId={HistoryId}, Found={Found}, Duration={ElapsedMs}ms",
                    id,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByIdAsync FAILED: HistoryId={HistoryId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<List<EventNotificationHistory>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "EventNotificationHistory"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEventIdAsync START: EventId={EventId}", eventId);

            try
            {
                var result = await _dbSet
                    .Where(h => h.EventId == eventId)
                    .OrderByDescending(h => h.SentAt)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
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

    public async Task<EventNotificationHistory> AddAsync(EventNotificationHistory history, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", "EventNotificationHistory"))
        using (LogContext.PushProperty("HistoryId", history.Id))
        using (LogContext.PushProperty("EventId", history.EventId))
        using (LogContext.PushProperty("SentByUserId", history.SentByUserId))
        using (LogContext.PushProperty("RecipientCount", history.RecipientCount))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "AddAsync START: HistoryId={HistoryId}, EventId={EventId}, SentByUserId={SentByUserId}, RecipientCount={RecipientCount}",
                history.Id, history.EventId, history.SentByUserId, history.RecipientCount);

            try
            {
                await _dbSet.AddAsync(history, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "AddAsync COMPLETE: HistoryId={HistoryId}, EventId={EventId}, SentByUserId={SentByUserId}, RecipientCount={RecipientCount}, Duration={ElapsedMs}ms",
                    history.Id,
                    history.EventId,
                    history.SentByUserId,
                    history.RecipientCount,
                    stopwatch.ElapsedMilliseconds);

                return history;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "AddAsync FAILED: HistoryId={HistoryId}, EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    history.Id,
                    history.EventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public void Update(EventNotificationHistory history)
    {
        using (LogContext.PushProperty("Operation", "Update"))
        using (LogContext.PushProperty("EntityType", "EventNotificationHistory"))
        using (LogContext.PushProperty("HistoryId", history.Id))
        using (LogContext.PushProperty("EventId", history.EventId))
        using (LogContext.PushProperty("SuccessfulSends", history.SuccessfulSends))
        using (LogContext.PushProperty("FailedSends", history.FailedSends))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "Update START: HistoryId={HistoryId}, EventId={EventId}, SuccessfulSends={SuccessfulSends}, FailedSends={FailedSends}",
                history.Id, history.EventId, history.SuccessfulSends, history.FailedSends);

            try
            {
                _dbSet.Update(history);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "Update COMPLETE: HistoryId={HistoryId}, EventId={EventId}, SuccessfulSends={SuccessfulSends}, FailedSends={FailedSends}, Duration={ElapsedMs}ms",
                    history.Id,
                    history.EventId,
                    history.SuccessfulSends,
                    history.FailedSends,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "Update FAILED: HistoryId={HistoryId}, EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    history.Id,
                    history.EventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
