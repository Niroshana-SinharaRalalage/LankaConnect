using Microsoft.EntityFrameworkCore;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Events.Entities;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.61: Repository implementation for EventNotificationHistory tracking
/// </summary>
public class EventNotificationHistoryRepository : IEventNotificationHistoryRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<EventNotificationHistory> _dbSet;
    private readonly ILogger _logger;

    public EventNotificationHistoryRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<EventNotificationHistory>();
        _logger = Log.ForContext<EventNotificationHistoryRepository>();
    }

    public async Task<EventNotificationHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetEventNotificationHistoryById"))
        using (LogContext.PushProperty("HistoryId", id))
        {
            _logger.Debug("[Phase 6A.61] Getting notification history by ID {HistoryId}", id);

            var result = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

            _logger.Debug("[Phase 6A.61] Notification history {HistoryId} {Result}", id, result != null ? "found" : "not found");
            return result;
        }
    }

    public async Task<List<EventNotificationHistory>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetEventNotificationHistoryByEventId"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            _logger.Debug("[Phase 6A.61] Getting notification history for event {EventId}", eventId);

            var result = await _dbSet
                .Where(h => h.EventId == eventId)
                .OrderByDescending(h => h.SentAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.Debug("[Phase 6A.61] Retrieved {Count} notification history records for event {EventId}", result.Count, eventId);
            return result;
        }
    }

    public async Task<EventNotificationHistory> AddAsync(EventNotificationHistory history, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "AddEventNotificationHistory"))
        using (LogContext.PushProperty("HistoryId", history.Id))
        using (LogContext.PushProperty("EventId", history.EventId))
        {
            _logger.Information("[Phase 6A.61] Adding notification history {HistoryId} for event {EventId}", history.Id, history.EventId);

            await _dbSet.AddAsync(history, cancellationToken);

            _logger.Information("[Phase 6A.61] Successfully added notification history {HistoryId}", history.Id);
            return history;
        }
    }

    public void Update(EventNotificationHistory history)
    {
        using (LogContext.PushProperty("Operation", "UpdateEventNotificationHistory"))
        using (LogContext.PushProperty("HistoryId", history.Id))
        using (LogContext.PushProperty("EventId", history.EventId))
        {
            _logger.Information("[Phase 6A.61] Updating notification history {HistoryId} for event {EventId}", history.Id, history.EventId);

            _dbSet.Update(history);

            _logger.Information("[Phase 6A.61] Successfully updated notification history {HistoryId}", history.Id);
        }
    }
}
