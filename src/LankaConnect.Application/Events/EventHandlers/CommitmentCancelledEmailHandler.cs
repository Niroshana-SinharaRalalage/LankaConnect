using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Helpers;
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
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<CommitmentCancelledEmailHandler> _logger;

    public CommitmentCancelledEmailHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<CommitmentCancelledEmailHandler> logger)
    {
        _typedEmailService = typedEmailService;
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
            // Phase 6A.121: Support dual nullable fields (PhysicalQuantity or SlotsClaimed)
            var quantity = domainEvent.CancelledPhysicalQuantity ?? domainEvent.CancelledSlotsClaimed ?? 0;
            var quantityType = domainEvent.CancelledPhysicalQuantity.HasValue ? "units" : "slots";

            _logger.LogInformation(
                "[Phase 6A.51+] Processing CommitmentCancelledEvent: User {UserId} cancelled commitment {CommitmentId} for SignUpItem {SignUpItemId}, Item='{ItemDescription}', Qty={Quantity} {QuantityType}",
                domainEvent.UserId, domainEvent.CommitmentId, domainEvent.SignUpItemId, domainEvent.ItemDescription, quantity, quantityType);

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
            // Phase 6A.121: Event contains ItemDescription and dual quantity fields (PhysicalQuantity or SlotsClaimed)
            _logger.LogInformation(
                "[Phase 6A.51+] Using event data for cancellation email: Item='{ItemDescription}', Qty={Quantity} {QuantityType}",
                domainEvent.ItemDescription, quantity, quantityType);

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
            // Phase 6A.121: Use whichever quantity field is populated (PhysicalQuantity or SlotsClaimed)
            var emailParams = SignupCommitmentEmailParams.CreateCancellation(
                userId: user.Id,
                userName: user.FirstName,
                userEmail: user.Email.Value,
                eventId: @event.Id,
                eventTitle: @event.Title?.Value ?? "Untitled Event",
                signupItem: domainEvent.ItemDescription,
                quantity: quantity,  // Phase 6A.121: Calculated from dual fields above
                eventStartDate: @event.StartDate,
                timeZoneId: @event.TimeZoneId,
                eventLocation: @event.Location?.ToString() ?? "Location TBD",
                eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id)
            );

            // Phase 6A.103: Add event image if available
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            emailParams.WithEventImage(primaryImage?.ImageUrl ?? @event.Images.FirstOrDefault()?.ImageUrl ?? "");

            // Phase 6A.87+ Fix: Populate organizer contact if available
            if (!string.IsNullOrWhiteSpace(@event.OrganizerContactName))
            {
                emailParams.WithOrganizerContact(
                    @event.OrganizerContactName,
                    @event.OrganizerContactEmail,
                    @event.OrganizerContactPhone);
            }

            // Phase 6A.87+ Fix: Populate signup lists URL if event has signup lists
            if (@event.SignUpLists?.Count > 0)
            {
                emailParams.WithSignUpLists($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups");
            }

            // Phase 6A.122: Fire-and-forget email - don't block HTTP response waiting for email
            // Root cause of slow signup operations: Azure Communication Services takes 2-16 seconds
            _logger.LogInformation(
                "CommitmentCancelled COMPLETE: Cancellation confirmed, dispatching email async - UserId={UserId}, EventId={EventId}",
                domainEvent.UserId, @event.Id);

            var capturedParams = emailParams;
            var capturedEmail = user.Email.Value;
            var capturedEventId = @event.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    var emailResult = await _typedEmailService.SendEmailAsync(capturedParams, CancellationToken.None);
                    if (emailResult.Success)
                    {
                        _logger.LogInformation(
                            "CommitmentCancelled EMAIL SENT: Email={Email}, EventId={EventId}",
                            capturedEmail, capturedEventId);
                    }
                    else
                    {
                        _logger.LogError(
                            "CommitmentCancelled EMAIL FAILED: Email={Email}, Errors={Errors}",
                            capturedEmail, string.Join(", ", emailResult.Errors));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "CommitmentCancelled EMAIL EXCEPTION: Email={Email}, EventId={EventId}",
                        capturedEmail, capturedEventId);
                }
            }, CancellationToken.None);
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
