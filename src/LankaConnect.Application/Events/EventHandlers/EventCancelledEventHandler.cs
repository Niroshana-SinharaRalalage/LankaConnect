using System.Diagnostics;
using Hangfire;
using LankaConnect.Application.Common;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.64: Handles EventCancelledEvent by queuing a background job to send cancellation notifications.
///
/// ARCHITECTURE CHANGE (Phase 6A.64):
/// - BEFORE: Sent emails synchronously within HTTP request (80-90 seconds, caused timeouts)
/// - AFTER: Queues Hangfire background job (instant API response, unlimited scalability)
///
/// Background job (EventCancellationEmailJob) sends email to:
/// 1. Confirmed registrations (user accounts only)
/// 2. Event email groups
/// 3. Location-matched newsletter subscribers (metro → state → all locations)
///
/// Performance: API response < 1 second, emails sent asynchronously in background
/// Retry: Hangfire automatically retries failed jobs (default: 10 attempts with exponential backoff)
/// Monitoring: View job status and failures in Hangfire Dashboard (/hangfire)
/// </summary>
public class EventCancelledEventHandler : INotificationHandler<DomainEventNotification<EventCancelledEvent>>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<EventCancelledEventHandler> _logger;

    public EventCancelledEventHandler(
        IBackgroundJobClient backgroundJobClient,
        ILogger<EventCancelledEventHandler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<EventCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "EventCancelled"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "EventCancelled START: EventId={EventId}, CancelledAt={CancelledAt}, Reason={Reason}",
                domainEvent.EventId, domainEvent.CancelledAt, domainEvent.Reason);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Phase 6A.64: Queue background job instead of sending emails synchronously
                // This returns immediately (< 1ms), and Hangfire executes the job asynchronously
                var jobId = _backgroundJobClient.Enqueue<EventCancellationEmailJob>(
                    job => job.ExecuteAsync(domainEvent.EventId, domainEvent.Reason));

                stopwatch.Stop();

                _logger.LogInformation(
                    "EventCancelled COMPLETE: Job queued - EventId={EventId}, JobId={JobId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, jobId, stopwatch.ElapsedMilliseconds);

                return Task.CompletedTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "EventCancelled CANCELED: Operation was canceled - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "EventCancelled FAILED: Job queueing failed - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);

                return Task.CompletedTask;
            }
        }
    }
}
