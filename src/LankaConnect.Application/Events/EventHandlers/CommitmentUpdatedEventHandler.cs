using System.Diagnostics;
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
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.51+: Handles CommitmentUpdatedEvent to send update confirmation email to user
/// when they change their commitment quantity or details.
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class CommitmentUpdatedEventHandler : INotificationHandler<DomainEventNotification<CommitmentUpdatedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ITypedEmailService _typedEmailService;  // Phase 6A.87: Typed email service
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<CommitmentUpdatedEventHandler> _logger;

    private const string HandlerName = nameof(CommitmentUpdatedEventHandler);  // Phase 6A.87: For feature flag lookup

    public CommitmentUpdatedEventHandler(
        IEmailService emailService,
        ITypedEmailService typedEmailService,  // Phase 6A.87: Typed email service
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<CommitmentUpdatedEventHandler> logger)
    {
        _emailService = emailService;
        _typedEmailService = typedEmailService;  // Phase 6A.87: Typed email service
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<CommitmentUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "CommitmentUpdated"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("UserId", domainEvent.UserId))
        using (LogContext.PushProperty("SignUpItemId", domainEvent.SignUpItemId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CommitmentUpdated START: UserId={UserId}, ItemDescription={ItemDescription}, OldQuantity={OldQuantity}, NewQuantity={NewQuantity}",
                domainEvent.UserId, domainEvent.ItemDescription, domainEvent.OldQuantity, domainEvent.NewQuantity);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user details
                var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CommitmentUpdated: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.UserId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent: don't throw to prevent transaction rollback
                }

                // Get event details via repository navigation method
                var @event = await _eventRepository.GetEventBySignUpItemIdAsync(domainEvent.SignUpItemId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CommitmentUpdated: Event not found - SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                        domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent
                }

                // Phase 6A.87: Use typed email parameters for compile-time safety
                var emailParams = SignupCommitmentEmailParams.CreateUpdate(
                    userId: user.Id,
                    userName: user.FirstName,
                    userEmail: user.Email.Value,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Untitled Event",
                    signupItem: domainEvent.ItemDescription,
                    quantity: domainEvent.NewQuantity,
                    eventStartDate: @event.StartDate,
                    timeZoneId: @event.TimeZoneId,
                    eventLocation: @event.Location?.ToString() ?? "Location TBD",
                    eventDetailsUrl: $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups"
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

                // Phase 6A.87: Send via typed email service (feature flags handled internally)
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    HandlerName,
                    cancellationToken);

                stopwatch.Stop();

                if (typedResult.Success)
                {
                    _logger.LogInformation(
                        "CommitmentUpdated COMPLETE: Email sent successfully - Email={Email}, EventId={EventId}, UsedTyped={UsedTyped}, Duration={ElapsedMs}ms",
                        user.Email.Value, @event.Id, typedResult.UsedTypedParameters, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "CommitmentUpdated FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        user.Email.Value, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "CommitmentUpdated CANCELED: Operation was canceled - UserId={UserId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "CommitmentUpdated FAILED: Exception occurred - UserId={UserId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
