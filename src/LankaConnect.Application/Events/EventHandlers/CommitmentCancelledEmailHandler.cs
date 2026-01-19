using LankaConnect.Application.Common;
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
/// </summary>
public class CommitmentCancelledEmailHandler : INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CommitmentCancelledEmailHandler> _logger;

    public CommitmentCancelledEmailHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IApplicationDbContext context,
        ILogger<CommitmentCancelledEmailHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _context = context;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<CommitmentCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            _logger.LogInformation(
                "[Phase 6A.51+] Processing CommitmentCancelledEvent: User {UserId} cancelled commitment {CommitmentId} for SignUpItem {SignUpItemId}",
                domainEvent.UserId, domainEvent.CommitmentId, domainEvent.SignUpItemId);

            // Get user details
            var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51+] User {UserId} not found for commitment cancellation email",
                    domainEvent.UserId);
                return; // Fail-silent
            }

            // Get commitment details BEFORE it's deleted (we're in the same transaction)
            var commitment = await _context.SignUpCommitments
                .FindAsync(new object[] { domainEvent.CommitmentId }, cancellationToken);

            if (commitment == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51+] Commitment {CommitmentId} not found (may have been deleted already)",
                    domainEvent.CommitmentId);
                return; // Fail-silent
            }

            // Get SignUpItem to retrieve ItemDescription
            var signUpItem = await _context.SignUpItems
                .FindAsync(new object[] { domainEvent.SignUpItemId }, cancellationToken);

            if (signUpItem == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.51+] SignUpItem {SignUpItemId} not found for commitment cancellation email",
                    domainEvent.SignUpItemId);
                return; // Fail-silent
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
                { "ItemDescription", signUpItem.ItemDescription },
                { "Quantity", commitment.Quantity },
                { "EventDateTime", @event.StartDate.ToString("f") }, // Full date/time pattern - fixed placeholder name
                { "EventLocation", @event.Location?.ToString() ?? "Location TBD" },
                { "PickupInstructions", "No pickup/delivery needed as this commitment has been cancelled." } // Appropriate for cancellation
            };

            // Send templated email
            var result = await _emailService.SendTemplatedEmailAsync(
                "signup-commitment-cancelled",
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
