using System.Diagnostics;
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
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.51: Handles UserCommittedToSignUpEvent to send confirmation email to user
/// when they commit to bringing an item to an event.
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class UserCommittedToSignUpEventHandler : INotificationHandler<DomainEventNotification<UserCommittedToSignUpEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<UserCommittedToSignUpEventHandler> _logger;

    public UserCommittedToSignUpEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<UserCommittedToSignUpEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<UserCommittedToSignUpEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "UserCommittedToSignUp"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("UserId", domainEvent.UserId))
        using (LogContext.PushProperty("SignUpListId", domainEvent.SignUpListId))
        {
            var stopwatch = Stopwatch.StartNew();

            // Phase 6A.121: Support dual nullable fields (PhysicalQuantity or SlotsClaimed)
            var quantity = domainEvent.PhysicalQuantity ?? domainEvent.SlotsClaimed ?? 0;
            var quantityType = domainEvent.PhysicalQuantity.HasValue ? "units" : "slots";

            _logger.LogInformation(
                "UserCommittedToSignUp START: UserId={UserId}, Quantity={Quantity} {QuantityType}, ItemDescription={ItemDescription}, SignUpListId={SignUpListId}",
                domainEvent.UserId, quantity, quantityType, domainEvent.ItemDescription, domainEvent.SignUpListId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user details
                var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UserCommittedToSignUp: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.UserId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent: don't throw to prevent transaction rollback
                }

                // Get event details via repository navigation method
                var @event = await _eventRepository.GetEventBySignUpListIdAsync(domainEvent.SignUpListId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UserCommittedToSignUp: Event not found - SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent
                }

                // Phase 6A.87: Use typed email parameters for compile-time safety
                // Phase 6A.121: Use whichever quantity field is populated (PhysicalQuantity or SlotsClaimed)
                var emailParams = SignupCommitmentEmailParams.CreateConfirmation(
                    userId: user.Id,
                    userName: user.FirstName,
                    userEmail: user.Email.Value,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
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
                stopwatch.Stop();
                _logger.LogInformation(
                    "UserCommittedToSignUp COMPLETE: Signup confirmed, dispatching email async - UserId={UserId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, @event.Id, stopwatch.ElapsedMilliseconds);

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
                                "UserCommittedToSignUp EMAIL SENT: Email={Email}, EventId={EventId}",
                                capturedEmail, capturedEventId);
                        }
                        else
                        {
                            _logger.LogError(
                                "UserCommittedToSignUp EMAIL FAILED: Email={Email}, Errors={Errors}",
                                capturedEmail, string.Join(", ", emailResult.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "UserCommittedToSignUp EMAIL EXCEPTION: Email={Email}, EventId={EventId}",
                            capturedEmail, capturedEventId);
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "UserCommittedToSignUp CANCELED: Operation was canceled - UserId={UserId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "UserCommittedToSignUp FAILED: Exception occurred - UserId={UserId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
