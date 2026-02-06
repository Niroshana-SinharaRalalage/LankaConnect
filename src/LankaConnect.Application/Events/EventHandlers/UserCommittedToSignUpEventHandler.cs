using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;  // Phase 6A.83: Added for IEmailUrlHelper
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
    private readonly IEmailService _emailService;
    private readonly ITypedEmailService _typedEmailService;  // Phase 6A.87: Typed email service
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;  // Phase 6A.83: Added for EventDetailsUrl
    private readonly ILogger<UserCommittedToSignUpEventHandler> _logger;

    private const string HandlerName = nameof(UserCommittedToSignUpEventHandler);  // Phase 6A.87: For feature flag lookup

    public UserCommittedToSignUpEventHandler(
        IEmailService emailService,
        ITypedEmailService typedEmailService,  // Phase 6A.87: Typed email service
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEmailUrlHelper emailUrlHelper,  // Phase 6A.83: Inject URL helper
        ILogger<UserCommittedToSignUpEventHandler> logger)
    {
        _emailService = emailService;
        _typedEmailService = typedEmailService;  // Phase 6A.87: Typed email service
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _emailUrlHelper = emailUrlHelper;  // Phase 6A.83
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

            _logger.LogInformation(
                "UserCommittedToSignUp START: UserId={UserId}, Quantity={Quantity}, ItemDescription={ItemDescription}, SignUpListId={SignUpListId}",
                domainEvent.UserId, domainEvent.Quantity, domainEvent.ItemDescription, domainEvent.SignUpListId);

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
                var emailParams = SignupCommitmentEmailParams.CreateConfirmation(
                    userId: user.Id,
                    userName: user.FirstName,
                    userEmail: user.Email.Value,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    signupItem: domainEvent.ItemDescription,
                    quantity: domainEvent.Quantity,
                    eventStartDate: @event.StartDate,
                    timeZoneId: @event.TimeZoneId,
                    eventLocation: @event.Location?.ToString() ?? "Location TBD",
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id)
                );

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

                // Phase 6A.100: Send via typed email service
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    cancellationToken);

                stopwatch.Stop();

                if (typedResult.Success)
                {
                    _logger.LogInformation(
                        "[Phase 6A.100] UserCommittedToSignUp COMPLETE: Email sent - Email={Email}, EventId={EventId}, Duration={ElapsedMs}ms",
                        user.Email.Value, @event.Id, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "UserCommittedToSignUp FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        user.Email.Value, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
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
