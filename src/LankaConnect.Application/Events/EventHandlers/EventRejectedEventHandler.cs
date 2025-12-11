using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

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

        _logger.LogInformation("Handling EventRejectedEvent for Event {EventId}", domainEvent.EventId);

        try
        {
            // Retrieve event data
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for EventRejectedEvent", domainEvent.EventId);
                return;
            }

            // Retrieve organizer's user details
            var organizer = await _userRepository.GetByIdAsync(@event.OrganizerId, cancellationToken);
            if (organizer == null)
            {
                _logger.LogWarning("Organizer {OrganizerId} not found for EventRejectedEvent", @event.OrganizerId);
                return;
            }

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

            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send event rejection email for Event {EventId}: {Errors}",
                    domainEvent.EventId, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("Event rejection email sent successfully for Event {EventId}", domainEvent.EventId);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex, "Error handling EventRejectedEvent for Event {EventId}", domainEvent.EventId);
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
