using Hangfire;
using LankaConnect.Application.Common;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

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

        _logger.LogInformation(
            "[Phase 6A.64] EventCancelledEventHandler INVOKED - Event {EventId}, Cancelled At {CancelledAt}",
            domainEvent.EventId, domainEvent.CancelledAt);

        try
        {
            // Phase 6A.64: Queue background job instead of sending emails synchronously
            // This returns immediately (< 1ms), and Hangfire executes the job asynchronously
            var jobId = _backgroundJobClient.Enqueue<EventCancellationEmailJob>(
                job => job.ExecuteAsync(domainEvent.EventId, domainEvent.Reason));

            _logger.LogInformation(
                "[Phase 6A.64] Queued EventCancellationEmailJob for event {EventId} with job ID {JobId}. " +
                "Emails will be sent asynchronously in background.",
                domainEvent.EventId, jobId);

            // Return immediately - email sending happens in background
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            // The event status change will still persist even if job queueing fails
            _logger.LogError(ex,
                "[Phase 6A.64] ERROR queueing EventCancellationEmailJob for Event {EventId}. " +
                "Emails will NOT be sent.",
                domainEvent.EventId);

            return Task.CompletedTask;
        }
    }
}
