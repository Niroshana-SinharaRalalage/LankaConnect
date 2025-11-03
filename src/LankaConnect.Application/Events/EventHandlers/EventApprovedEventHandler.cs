using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles EventApprovedEvent to send approval notification email to event organizer
/// </summary>
public class EventApprovedEventHandler : INotificationHandler<DomainEventNotification<EventApprovedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventApprovedEventHandler> _logger;

    public EventApprovedEventHandler(
        IEmailService emailService,
        IEventRepository eventRepository,
        ILogger<EventApprovedEventHandler> logger)
    {
        _emailService = emailService;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventApprovedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Handling EventApprovedEvent for Event {EventId}", domainEvent.EventId);

        try
        {
            // Retrieve event data
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for EventApprovedEvent", domainEvent.EventId);
                return;
            }

            // Note: In production, you would retrieve the organizer's user details
            // For now, using a simplified approach with event owner ID
            var parameters = new Dictionary<string, object>
            {
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "ApprovedAt", domainEvent.ApprovedAt.ToString("MMMM dd, yyyy h:mm tt") }
            };

            var emailMessage = new EmailMessageDto
            {
                ToEmail = "organizer@example.com", // TODO: Get from event.OrganizerId
                ToName = "Event Organizer",
                Subject = $"Event Approved: {@event.Title.Value}",
                HtmlBody = GenerateEventApprovedHtml(parameters),
                Priority = 1 // High priority
            };

            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send event approval email for Event {EventId}: {Errors}",
                    domainEvent.EventId, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("Event approval email sent successfully for Event {EventId}", domainEvent.EventId);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex, "Error handling EventApprovedEvent for Event {EventId}", domainEvent.EventId);
        }
    }

    private string GenerateEventApprovedHtml(Dictionary<string, object> parameters)
    {
        return $@"
            <html>
            <body>
                <h2>Event Approved!</h2>
                <p>Dear Event Organizer,</p>
                <p>Great news! Your event has been approved and is now published:</p>
                <ul>
                    <li><strong>Event:</strong> {parameters["EventTitle"]}</li>
                    <li><strong>Date:</strong> {parameters["EventStartDate"]} at {parameters["EventStartTime"]}</li>
                    <li><strong>Approved:</strong> {parameters["ApprovedAt"]}</li>
                </ul>
                <p>Your event is now visible to all users and attendees can register.</p>
                <p>Best regards,<br/>LankaConnect Team</p>
            </body>
            </html>";
    }
}
