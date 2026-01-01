using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles RegistrationCancelledEvent to send cancellation confirmation email to attendee
/// </summary>
public class RegistrationCancelledEventHandler : INotificationHandler<DomainEventNotification<RegistrationCancelledEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<RegistrationCancelledEventHandler> _logger;

    public RegistrationCancelledEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<RegistrationCancelledEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RegistrationCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Handling RegistrationCancelledEvent for Event {EventId}, User {UserId}",
            domainEvent.EventId, domainEvent.AttendeeId);

        try
        {
            // Retrieve user and event data
            var user = await _userRepository.GetByIdAsync(domainEvent.AttendeeId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for RegistrationCancelledEvent", domainEvent.AttendeeId);
                return;
            }

            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for RegistrationCancelledEvent", domainEvent.EventId);
                return;
            }

            // Prepare email parameters
            var parameters = new Dictionary<string, object>
            {
                { "UserName", $"{user.FirstName} {user.LastName}" },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "CancellationDate", domainEvent.CancelledAt.ToString("MMMM dd, yyyy h:mm tt") },
                { "Reason", "User cancelled registration" }
            };

            // Send templated email
            var result = await _emailService.SendTemplatedEmailAsync(
                "registration-cancellation",
                user.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send RSVP cancellation email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("RSVP cancellation email sent successfully to {Email}", user.Email.Value);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex, "Error handling RegistrationCancelledEvent for Event {EventId}, User {UserId}",
                domainEvent.EventId, domainEvent.AttendeeId);
        }
    }
}
