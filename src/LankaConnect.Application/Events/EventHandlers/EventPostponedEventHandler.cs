using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles EventPostponedEvent to send bulk postponement notifications to all registered attendees
/// </summary>
public class EventPostponedEventHandler : INotificationHandler<DomainEventNotification<EventPostponedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<EventPostponedEventHandler> _logger;

    public EventPostponedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ILogger<EventPostponedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventPostponedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Handling EventPostponedEvent for Event {EventId}", domainEvent.EventId);

        try
        {
            // Retrieve event data
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for EventPostponedEvent", domainEvent.EventId);
                return;
            }

            // Get all confirmed registrations for this event
            var registrations = await _registrationRepository.GetByEventAsync(domainEvent.EventId, cancellationToken);
            var confirmedRegistrations = registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed)
                .ToList();

            if (!confirmedRegistrations.Any())
            {
                _logger.LogInformation("No confirmed registrations found for Event {EventId}, skipping email notifications",
                    domainEvent.EventId);
                return;
            }

            _logger.LogInformation("Found {Count} confirmed registrations for Event {EventId}, preparing bulk email",
                confirmedRegistrations.Count, domainEvent.EventId);

            // Prepare bulk email messages
            var emailMessages = new List<EmailMessageDto>();

            foreach (var registration in confirmedRegistrations)
            {
                var user = await _userRepository.GetByIdAsync(registration.UserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for registration {RegistrationId}",
                        registration.UserId, registration.Id);
                    continue;
                }

                var parameters = new Dictionary<string, object>
                {
                    { "UserName", $"{user.FirstName} {user.LastName}" },
                    { "EventTitle", @event.Title.Value },
                    { "OriginalStartDate", domainEvent.PostponedAt.ToString("MMMM dd, yyyy") },
                    { "OriginalStartTime", domainEvent.PostponedAt.ToString("h:mm tt") },
                    { "Reason", domainEvent.Reason },
                    { "PostponedAt", domainEvent.PostponedAt.ToString("MMMM dd, yyyy h:mm tt") }
                };

                var emailMessage = new EmailMessageDto
                {
                    ToEmail = user.Email.Value,
                    ToName = $"{user.FirstName} {user.LastName}",
                    Subject = $"Event Postponed: {@event.Title.Value}",
                    HtmlBody = GenerateEventPostponedHtml(parameters),
                    Priority = 1 // High priority
                };

                emailMessages.Add(emailMessage);
            }

            if (!emailMessages.Any())
            {
                _logger.LogWarning("No email messages prepared for Event {EventId}", domainEvent.EventId);
                return;
            }

            // Send bulk emails
            var result = await _emailService.SendBulkEmailAsync(emailMessages, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send event postponement bulk emails for Event {EventId}: {Errors}",
                    domainEvent.EventId, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "Event postponement bulk emails sent: {Successful} successful, {Failed} failed out of {Total} total",
                    result.Value.SuccessfulSends, result.Value.FailedSends, result.Value.TotalEmails);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex, "Error handling EventPostponedEvent for Event {EventId}", domainEvent.EventId);
        }
    }

    private string GenerateEventPostponedHtml(Dictionary<string, object> parameters)
    {
        // Simplified HTML generation - in production this would use Razor templates
        return $@"
            <html>
            <body>
                <h2>Event Postponed</h2>
                <p>Dear {parameters["UserName"]},</p>
                <p>We would like to inform you that the following event has been postponed:</p>
                <ul>
                    <li><strong>Event:</strong> {parameters["EventTitle"]}</li>
                    <li><strong>Original Date:</strong> {parameters["OriginalStartDate"]} at {parameters["OriginalStartTime"]}</li>
                    <li><strong>Reason:</strong> {parameters["Reason"]}</li>
                </ul>
                <p>We will notify you once a new date has been confirmed. Your registration remains active.</p>
                <p>We apologize for any inconvenience this may cause.</p>
                <p>Best regards,<br/>LankaConnect Team</p>
            </body>
            </html>";
    }
}
