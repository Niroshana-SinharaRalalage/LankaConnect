using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.51: Handles UserCommittedToSignUpEvent to send confirmation email to user
/// when they commit to bringing an item to an event.
/// </summary>
public class UserCommittedToSignUpEventHandler : INotificationHandler<DomainEventNotification<UserCommittedToSignUpEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<UserCommittedToSignUpEventHandler> _logger;

    public UserCommittedToSignUpEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<UserCommittedToSignUpEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<UserCommittedToSignUpEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            _logger.LogInformation(
                "[Phase 6A.51] Processing UserCommittedToSignUpEvent: User {UserId} committed {Quantity}x '{ItemDescription}' to SignUpList {SignUpListId}",
                domainEvent.UserId, domainEvent.Quantity, domainEvent.ItemDescription, domainEvent.SignUpListId);

            // Get user details
            var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51] User {UserId} not found for commitment confirmation email",
                    domainEvent.UserId);
                return; // Fail-silent: don't throw to prevent transaction rollback
            }

            // Get event details via repository navigation method
            var @event = await _eventRepository.GetEventBySignUpListIdAsync(domainEvent.SignUpListId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51] Event not found for SignUpList {SignUpListId}",
                    domainEvent.SignUpListId);
                return; // Fail-silent
            }

            // Build template parameters
            var templateData = new Dictionary<string, object>
            {
                { "UserName", user.FirstName },
                { "EventTitle", @event.Title },
                { "ItemDescription", domainEvent.ItemDescription },
                { "Quantity", domainEvent.Quantity },
                { "EventDateTime", @event.StartDate.ToString("f") }, // Full date/time pattern - fixed placeholder name
                { "EventLocation", @event.Location?.ToString() ?? "Location TBD" },
                { "PickupInstructions", "Please coordinate pickup/delivery details with the event organizer." } // Default instruction
            };

            // Send templated email
            var result = await _emailService.SendTemplatedEmailAsync(
                "template-signup-list-commitment-confirmation",
                user.Email.Value,
                templateData,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.51] Commitment confirmation email sent to {Email} for event {EventId}",
                    user.Email.Value, @event.Id);
            }
            else
            {
                _logger.LogError(
                    "[Phase 6A.51] Failed to send commitment confirmation email to {Email}: {Error}",
                    user.Email.Value, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex,
                "[Phase 6A.51] Error sending commitment confirmation email for User {UserId}, SignUpList {SignUpListId}",
                domainEvent.UserId, domainEvent.SignUpListId);
        }
    }
}
