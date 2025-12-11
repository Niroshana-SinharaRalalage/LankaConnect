using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles EventApprovedEvent to send approval notification email to event organizer
/// </summary>
public class EventApprovedEventHandler : INotificationHandler<DomainEventNotification<EventApprovedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventApprovedEventHandler> _logger;

    public EventApprovedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<EventApprovedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
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

            // Retrieve organizer's user details
            var organizer = await _userRepository.GetByIdAsync(@event.OrganizerId, cancellationToken);
            if (organizer == null)
            {
                _logger.LogWarning("Organizer {OrganizerId} not found for EventApprovedEvent", @event.OrganizerId);
                return;
            }

            var organizerName = $"{organizer.FirstName} {organizer.LastName}";
            var parameters = new Dictionary<string, object>
            {
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "ApprovedAt", domainEvent.ApprovedAt.ToString("MMMM dd, yyyy h:mm tt") },
                { "OrganizerName", organizerName }
            };

            var emailMessage = new EmailMessageDto
            {
                ToEmail = organizer.Email.Value,
                ToName = organizerName,
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
        var organizerName = parameters.TryGetValue("OrganizerName", out var name) ? name.ToString() : "Event Organizer";
        return $@"
            <html>
            <body>
                <h2>Event Approved!</h2>
                <p>Dear {organizerName},</p>
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
