using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
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
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class CommitmentCancelledEmailHandler : INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ITypedEmailService _typedEmailService;  // Phase 6A.87: Typed email service
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<CommitmentCancelledEmailHandler> _logger;

    private const string HandlerName = nameof(CommitmentCancelledEmailHandler);  // Phase 6A.87: For feature flag lookup

    public CommitmentCancelledEmailHandler(
        IEmailService emailService,
        ITypedEmailService typedEmailService,  // Phase 6A.87: Typed email service
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<CommitmentCancelledEmailHandler> logger)
    {
        _emailService = emailService;
        _typedEmailService = typedEmailService;  // Phase 6A.87: Typed email service
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _emailUrlHelper = emailUrlHelper;
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

            // Phase 6A.87: Use typed email parameters for compile-time safety
            var emailParams = SignupCommitmentEmailParams.CreateCancellation(
                userId: user.Id,
                userName: user.FirstName,
                userEmail: user.Email.Value,
                eventId: @event.Id,
                eventTitle: @event.Title?.Value ?? "Untitled Event",
                signupItem: domainEvent.ItemDescription,
                quantity: domainEvent.Quantity,
                eventStartDate: @event.StartDate,
                timeZoneId: @event.TimeZoneId,
                eventLocation: @event.Location?.ToString() ?? "Location TBD",
                eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id)
            );

            // Phase 6A.87: Send via typed email service (feature flags handled internally)
            var typedResult = await _typedEmailService.SendEmailAsync(
                emailParams,
                HandlerName,
                cancellationToken);

            if (typedResult.Success)
            {
                _logger.LogInformation(
                    "[Phase 6A.87] Commitment cancellation email sent to {Email} for event {EventId}, UsedTyped={UsedTyped}",
                    user.Email.Value, @event.Id, typedResult.UsedTypedParameters);
            }
            else
            {
                _logger.LogError(
                    "[Phase 6A.87] Failed to send commitment cancellation email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", typedResult.Errors));
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
