using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.51+: Handles CommitmentCancelledEvent to send cancellation confirmation email to user.
///
/// NOTE: This handler is separate from CommitmentCancelledEventHandler which handles EF Core deletion.
/// Multiple handlers can listen to the same domain event for different responsibilities:
/// - CommitmentCancelledEventHandler: Handles database deletion (Phase 6A.28)
/// - CommitmentCancelledEmailHandler: Sends confirmation email (Phase 6A.51)
///
/// Phase 6A.51+ Fix: Uses data from the domain event (ItemDescription, Quantity) instead of
/// querying the database, since entities may be deleted by the time this handler runs.
/// </summary>
public class CommitmentCancelledEmailHandler : INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<CommitmentCancelledEmailHandler> _logger;

    public CommitmentCancelledEmailHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<CommitmentCancelledEmailHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<CommitmentCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            _logger.LogInformation(
                "[Phase 6A.51+] Processing CommitmentCancelledEvent: User {UserId} cancelled commitment {CommitmentId} for SignUpItem {SignUpItemId}, Item='{ItemDescription}', Qty={Quantity}",
                domainEvent.UserId, domainEvent.CommitmentId, domainEvent.SignUpItemId, domainEvent.ItemDescription, domainEvent.Quantity);

            // Get user details
            var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51+] User {UserId} not found for commitment cancellation email",
                    domainEvent.UserId);
                return; // Fail-silent
            }

            // Phase 6A.51+ Fix: Use data from event directly (entities may be deleted by now)
            // The event now contains ItemDescription and Quantity captured before deletion
            _logger.LogInformation(
                "[Phase 6A.51+] Using event data for cancellation email: Item='{ItemDescription}', Qty={Quantity}",
                domainEvent.ItemDescription, domainEvent.Quantity);

            // Get event details via repository navigation method using SignUpListId
            var @event = await _eventRepository.GetEventBySignUpListIdAsync(domainEvent.SignUpListId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51+] Event not found for SignUpListId {SignUpListId}",
                    domainEvent.SignUpListId);
                return; // Fail-silent
            }

            // Build template parameters using event data (not database queries)
            var templateData = new Dictionary<string, object>
            {
                { "UserName", user.FirstName },
                { "EventTitle", @event.Title },
                { "ItemDescription", domainEvent.ItemDescription },  // From event, not DB
                { "Quantity", domainEvent.Quantity },                // From event, not DB
                { "EventDateTime", @event.StartDate.ToString("f") }, // Full date/time pattern
                { "EventLocation", @event.Location?.ToString() ?? "Location TBD" },
                { "PickupInstructions", "No pickup/delivery needed as this commitment has been cancelled." }
            };

            // Send templated email
            var result = await _emailService.SendTemplatedEmailAsync(
                EmailTemplateNames.SignupCommitmentCancellation,
                user.Email.Value,
                templateData,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.51+] Commitment cancellation email sent to {Email} for event {EventId}",
                    user.Email.Value, @event.Id);
            }
            else
            {
                _logger.LogError(
                    "[Phase 6A.51+] Failed to send commitment cancellation email to {Email}: {Error}",
                    user.Email.Value, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex,
                "[Phase 6A.51+] Error sending commitment cancellation email for User {UserId}, Commitment {CommitmentId}",
                domainEvent.UserId, domainEvent.CommitmentId);
        }
    }
}
