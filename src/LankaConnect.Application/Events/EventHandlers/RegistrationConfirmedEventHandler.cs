using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles RegistrationConfirmedEvent to send confirmation email to attendee
/// </summary>
public class RegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<RegistrationConfirmedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<RegistrationConfirmedEventHandler> _logger;

    public RegistrationConfirmedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<RegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RegistrationConfirmedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}",
            domainEvent.EventId, domainEvent.AttendeeId);

        try
        {
            // Retrieve user and event data
            var user = await _userRepository.GetByIdAsync(domainEvent.AttendeeId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for RegistrationConfirmedEvent", domainEvent.AttendeeId);
                return;
            }

            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for RegistrationConfirmedEvent", domainEvent.EventId);
                return;
            }

            // Prepare email parameters
            var parameters = new Dictionary<string, object>
            {
                { "UserName", $"{user.FirstName} {user.LastName}" },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventEndDate", @event.EndDate.ToString("MMMM dd, yyyy") },
                { "EventLocation", @event.Location != null ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}" : "Online Event" },
                { "Quantity", domainEvent.Quantity },
                { "RegistrationDate", domainEvent.RegistrationDate.ToString("MMMM dd, yyyy h:mm tt") }
            };

            // Send templated email
            var result = await _emailService.SendTemplatedEmailAsync(
                "RsvpConfirmation",
                user.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send RSVP confirmation email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("RSVP confirmation email sent successfully to {Email}", user.Email.Value);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex, "Error handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}",
                domainEvent.EventId, domainEvent.AttendeeId);
        }
    }
}
