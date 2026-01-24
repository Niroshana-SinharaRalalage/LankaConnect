using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.BackgroundJobs;

/// <summary>
/// Background job that runs hourly to automatically update event statuses based on start/end dates
/// - Marks Published events as Active when start date arrives
/// - Marks Active events as Completed when end date passes
/// </summary>
public class EventStatusUpdateJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EventStatusUpdateJob> _logger;

    public EventStatusUpdateJob(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<EventStatusUpdateJob> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using (LogContext.PushProperty("Operation", "EventStatusUpdate"))
        using (LogContext.PushProperty("EntityType", "Event"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("EventStatusUpdateJob START: Beginning event status update");

            try
            {
                var now = DateTime.UtcNow;
                var updatedCount = 0;

                // 1. Mark Published events as Active if start date has arrived
                updatedCount += await MarkEventsAsActiveAsync(now);

                // 2. Mark Active events as Completed if end date has passed
                updatedCount += await MarkEventsAsCompletedAsync(now);

                stopwatch.Stop();
                _logger.LogInformation(
                    "EventStatusUpdateJob COMPLETE: Duration={ElapsedMs}ms, EventsUpdated={Count}",
                    stopwatch.ElapsedMilliseconds, updatedCount);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "EventStatusUpdateJob FAILED: Duration={ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw; // Re-throw for Hangfire retry
            }
        }
    }

    private async Task<int> MarkEventsAsActiveAsync(DateTime now)
    {
        var count = 0;
        try
        {
            // Get Published events that have started (start date <= now)
            var publishedEvents = await _eventRepository.GetEventsByStatusAsync(EventStatus.Published);
            var eventsToActivate = publishedEvents
                .Where(e => e.StartDate <= now)
                .ToList();

            _logger.LogInformation("EventStatusUpdateJob: Found {Count} Published events to mark as Active", eventsToActivate.Count);

            foreach (var @event in eventsToActivate)
            {
                try
                {
                    // Use domain method to activate event
                    var result = @event.ActivateEvent();
                    if (result.IsSuccess)
                    {
                        count++;
                        _logger.LogInformation("EventStatusUpdateJob: Marked event {EventId} ({Title}) as Active",
                            @event.Id, @event.Title.Value);
                    }
                    else
                    {
                        _logger.LogWarning("EventStatusUpdateJob: Failed to mark event {EventId} as Active: {Errors}",
                            @event.Id, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EventStatusUpdateJob: Error marking event {EventId} as Active", @event.Id);
                }
            }

            if (count > 0)
            {
                await _unitOfWork.CommitAsync();
                _logger.LogInformation("EventStatusUpdateJob: Committed {Count} events to Active status", count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventStatusUpdateJob: Error during MarkEventsAsActiveAsync");
        }
        return count;
    }

    private async Task<int> MarkEventsAsCompletedAsync(DateTime now)
    {
        var count = 0;
        try
        {
            // Get Active events that have ended (end date < now)
            var activeEvents = await _eventRepository.GetEventsByStatusAsync(EventStatus.Active);
            var eventsToComplete = activeEvents
                .Where(e => e.EndDate < now)
                .ToList();

            _logger.LogInformation("EventStatusUpdateJob: Found {Count} Active events to mark as Completed", eventsToComplete.Count);

            foreach (var @event in eventsToComplete)
            {
                try
                {
                    // Use existing Complete() method (void return)
                    @event.Complete();
                    count++;
                    _logger.LogInformation("EventStatusUpdateJob: Marked event {EventId} ({Title}) as Completed",
                        @event.Id, @event.Title.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EventStatusUpdateJob: Error marking event {EventId} as Completed", @event.Id);
                }
            }

            if (count > 0)
            {
                await _unitOfWork.CommitAsync();
                _logger.LogInformation("EventStatusUpdateJob: Committed {Count} events to Completed status", count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventStatusUpdateJob: Error during MarkEventsAsCompletedAsync");
        }
        return count;
    }
}
