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
/// Handles EventCancelledEvent to send bulk cancellation notifications to all registered attendees
/// </summary>
public class EventCancelledEventHandler : INotificationHandler<DomainEventNotification<EventCancelledEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<EventCancelledEventHandler> _logger;

    public EventCancelledEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ILogger<EventCancelledEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Handling EventCancelledEvent for Event {EventId}", domainEvent.EventId);

        try
        {
            // Retrieve event data
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for EventCancelledEvent", domainEvent.EventId);
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
                // Skip anonymous registrations - they don't have email in user repository
                if (!registration.UserId.HasValue)
                {
                    _logger.LogInformation("Skipping anonymous registration {RegistrationId} for cancelled event notification",
                        registration.Id);
                    continue;
                }

                var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for registration {RegistrationId}",
                        registration.UserId.Value, registration.Id);
                    continue;
                }

                var parameters = new Dictionary<string, object>
                {
                    { "UserName", $"{user.FirstName} {user.LastName}" },
                    { "EventTitle", @event.Title.Value },
                    { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                    { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                    { "Reason", domainEvent.Reason },
                    { "CancelledAt", domainEvent.CancelledAt.ToString("MMMM dd, yyyy h:mm tt") }
                };

                // Note: Using templated email approach with parameters
                // In production, you would generate HTML from template here
                var emailMessage = new EmailMessageDto
                {
                    ToEmail = user.Email.Value,
                    ToName = $"{user.FirstName} {user.LastName}",
                    Subject = $"Event Cancelled: {@event.Title.Value}",
                    HtmlBody = GenerateEventCancelledHtml(parameters),
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
                _logger.LogError("Failed to send event cancellation bulk emails for Event {EventId}: {Errors}",
                    domainEvent.EventId, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "Event cancellation bulk emails sent: {Successful} successful, {Failed} failed out of {Total} total",
                    result.Value.SuccessfulSends, result.Value.FailedSends, result.Value.TotalEmails);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex, "Error handling EventCancelledEvent for Event {EventId}", domainEvent.EventId);
        }
    }

    private string GenerateEventCancelledHtml(Dictionary<string, object> parameters)
    {
        // Simplified HTML generation - in production this would use Razor templates
        return $@"
            <html>
            <body>
                <h2>Event Cancelled</h2>
                <p>Dear {parameters["UserName"]},</p>
                <p>We regret to inform you that the following event has been cancelled:</p>
                <ul>
                    <li><strong>Event:</strong> {parameters["EventTitle"]}</li>
                    <li><strong>Original Date:</strong> {parameters["EventStartDate"]} at {parameters["EventStartTime"]}</li>
                    <li><strong>Cancellation Reason:</strong> {parameters["Reason"]}</li>
                </ul>
                <p>We apologize for any inconvenience this may cause.</p>
                <p>Best regards,<br/>LankaConnect Team</p>
            </body>
            </html>";
    }
}
