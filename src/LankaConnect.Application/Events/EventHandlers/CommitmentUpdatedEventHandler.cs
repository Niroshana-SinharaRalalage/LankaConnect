using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.51+: Handles CommitmentUpdatedEvent to send update confirmation email to user
/// when they change their commitment quantity or details.
/// </summary>
public class CommitmentUpdatedEventHandler : INotificationHandler<DomainEventNotification<CommitmentUpdatedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<CommitmentUpdatedEventHandler> _logger;

    public CommitmentUpdatedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<CommitmentUpdatedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<CommitmentUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            _logger.LogInformation(
                "[Phase 6A.51+] Processing CommitmentUpdatedEvent: User {UserId} updated commitment for '{ItemDescription}' from {OldQuantity} to {NewQuantity}",
                domainEvent.UserId, domainEvent.ItemDescription, domainEvent.OldQuantity, domainEvent.NewQuantity);

            // Get user details
            var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51+] User {UserId} not found for commitment update confirmation email",
                    domainEvent.UserId);
                return; // Fail-silent: don't throw to prevent transaction rollback
            }

            // Get event details via repository navigation method
            var @event = await _eventRepository.GetEventBySignUpItemIdAsync(domainEvent.SignUpItemId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51+] Event not found for SignUpItem {SignUpItemId}",
                    domainEvent.SignUpItemId);
                return; // Fail-silent
            }

            // Build template parameters
            var templateData = new Dictionary<string, object>
            {
                { "UserName", user.FirstName },
                { "EventTitle", @event.Title },
                { "ItemDescription", domainEvent.ItemDescription },
                { "OldQuantity", domainEvent.OldQuantity },
                { "NewQuantity", domainEvent.NewQuantity },
                { "EventDate", @event.StartDate.ToString("f") }, // Full date/time pattern
                { "EventLocation", @event.Location?.ToString() ?? "Location TBD" }
            };

            // Send templated email
            var result = await _emailService.SendTemplatedEmailAsync(
                "signup-commitment-updated",
                user.Email.Value,
                templateData,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.51+] Commitment update confirmation email sent to {Email} for event {EventId}",
                    user.Email.Value, @event.Id);
            }
            else
            {
                _logger.LogError(
                    "[Phase 6A.51+] Failed to send commitment update confirmation email to {Email}: {Error}",
                    user.Email.Value, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex,
                "[Phase 6A.51+] Error sending commitment update confirmation email for User {UserId}, SignUpItem {SignUpItemId}",
                domainEvent.UserId, domainEvent.SignUpItemId);
        }
    }
}
