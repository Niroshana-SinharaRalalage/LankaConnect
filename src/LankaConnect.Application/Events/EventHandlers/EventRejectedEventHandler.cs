using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles EventRejectedEvent to send rejection notification email to event organizer
/// </summary>
public class EventRejectedEventHandler : INotificationHandler<DomainEventNotification<EventRejectedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventRejectedEventHandler> _logger;

    public EventRejectedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<EventRejectedEventHandler> logger)
    {
        _emailService = emailService;
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
                var parameters = new Dictionary<string, object>
                {
                    { "EventTitle", @event.Title.Value },
                    { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                    { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                    { "Reason", domainEvent.Reason },
                    { "RejectedAt", domainEvent.RejectedAt.ToString("MMMM dd, yyyy h:mm tt") },
                    { "OrganizerName", organizerName }
                };

                var emailMessage = new EmailMessageDto
                {
                    ToEmail = organizer.Email.Value,
                    ToName = organizerName,
                    Subject = $"Event Requires Changes: {@event.Title.Value}",
                    HtmlBody = GenerateEventRejectedHtml(parameters),
                    Priority = 1 // High priority
                };

                _logger.LogInformation(
                    "EventRejected: Sending rejection email - To={Email}",
                    organizer.Email.Value);

                var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

                stopwatch.Stop();

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "EventRejected FAILED: Email sending failed - EventId={EventId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "EventRejected COMPLETE: Email sent successfully - EventId={EventId}, To={Email}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, organizer.Email.Value, stopwatch.ElapsedMilliseconds);
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

    private string GenerateEventRejectedHtml(Dictionary<string, object> parameters)
    {
        var organizerName = parameters.TryGetValue("OrganizerName", out var name) ? name.ToString() : "Event Organizer";
        return $@"
            <html>
            <body>
                <h2>Event Requires Changes</h2>
                <p>Dear {organizerName},</p>
                <p>Your event submission has been reviewed and requires some changes before it can be approved:</p>
                <ul>
                    <li><strong>Event:</strong> {parameters["EventTitle"]}</li>
                    <li><strong>Date:</strong> {parameters["EventStartDate"]} at {parameters["EventStartTime"]}</li>
                    <li><strong>Reviewed:</strong> {parameters["RejectedAt"]}</li>
                </ul>
                <p><strong>Feedback from our team:</strong></p>
                <p>{parameters["Reason"]}</p>
                <p>Please update your event and resubmit for approval.</p>
                <p>Best regards,<br/>LankaConnect Team</p>
            </body>
            </html>";
    }
}
