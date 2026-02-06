using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.100: Handles EventRejectedEvent to send rejection notification email to event organizer.
/// Uses ITypedEmailService with EventRejectedEmailParams for compile-time type safety.
/// Replaces inline HTML generation with database templates.
/// </summary>
public class EventRejectedEventHandler : INotificationHandler<DomainEventNotification<EventRejectedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventRejectedEventHandler> _logger;

    public EventRejectedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<EventRejectedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventRejectedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "EventRejected"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "EventRejected START: EventId={EventId}, RejectedAt={RejectedAt}, Reason={Reason}",
                domainEvent.EventId, domainEvent.RejectedAt, domainEvent.Reason);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retrieve event data
                _logger.LogInformation(
                    "EventRejected: Loading event - EventId={EventId}",
                    domainEvent.EventId);

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "EventRejected: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "EventRejected: Event loaded - EventTitle={EventTitle}, OrganizerId={OrganizerId}",
                    @event.Title.Value, @event.OrganizerId);

                // Retrieve organizer's user details
                var organizer = await _userRepository.GetByIdAsync(@event.OrganizerId, cancellationToken);
                if (organizer == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "EventRejected: Organizer not found - OrganizerId={OrganizerId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        @event.OrganizerId, domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "EventRejected: Organizer loaded - Email={Email}",
                    organizer.Email.Value);

                var organizerName = $"{organizer.FirstName} {organizer.LastName}";

                // Phase 6A.100: Create typed email parameters
                var emailParams = EventRejectedEmailParams.Create(
                    organizerId: organizer.Id,
                    organizerName: organizerName,
                    organizerEmail: organizer.Email.Value,
                    eventId: @event.Id,
                    eventTitle: @event.Title.Value,
                    eventStartDate: @event.StartDate,
                    timeZoneId: @event.TimeZoneId,
                    reason: domainEvent.Reason,
                    rejectedAt: domainEvent.RejectedAt);

                _logger.LogInformation(
                    "EventRejected: Sending rejection email - To={Email}",
                    organizer.Email.Value);

                // Phase 6A.100: Use typed email service
                var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                stopwatch.Stop();

                if (result.Success)
                {
                    _logger.LogInformation(
                        "EventRejected COMPLETE: Email sent successfully - EventId={EventId}, To={Email}, Duration={ElapsedMs}ms, CorrelationId={CorrelationId}",
                        domainEvent.EventId, organizer.Email.Value, stopwatch.ElapsedMilliseconds, result.CorrelationId);
                }
                else
                {
                    _logger.LogError(
                        "EventRejected FAILED: Email sending failed - EventId={EventId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "EventRejected CANCELED: Operation was canceled - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "EventRejected FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }
}
