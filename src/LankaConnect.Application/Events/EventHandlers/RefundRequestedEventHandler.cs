using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.92: Handles RefundRequestedEvent to send refund notification email to user.
/// Triggered when a refund is initiated (either by user cancellation or event cancellation).
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class RefundRequestedEventHandler : INotificationHandler<DomainEventNotification<RefundRequestedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RefundRequestedEventHandler> _logger;

    private const string SupportEmail = "lankaconnect.app@gmail.com";

    public RefundRequestedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEventFormRepository eventFormRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RefundRequestedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _eventFormRepository = eventFormRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RefundRequestedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RefundRequested"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Phase 6A.92] RefundRequested START: EventId={EventId}, RegistrationId={RegId}, UserId={UserId}, Amount=${Amount}",
                domainEvent.EventId, domainEvent.RegistrationId, domainEvent.UserId, domainEvent.RefundAmount);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get event details
                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[Phase 6A.92] RefundRequested: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Determine user name for email
                string userName = "Valued Customer";
                Guid userId = Guid.Empty;
                if (domainEvent.UserId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
                    if (user != null)
                    {
                        userName = $"{user.FirstName} {user.LastName}";
                        userId = user.Id;
                    }
                }

                // Phase 6A.87: Use typed email parameters for compile-time safety
                // Phase 6A.87++ Fix: Pass PaymentIntentId for reference number in email
                var emailParams = RefundEmailParams.CreateRequest(
                    userId: userId,
                    userName: userName,
                    userEmail: domainEvent.ContactEmail,
                    registrationId: domainEvent.RegistrationId,
                    refundId: Guid.NewGuid(),  // Refund ID not available in domain event yet
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate,
                    timeZoneId: @event.TimeZoneId,
                    refundAmount: domainEvent.RefundAmount,
                    originalAmount: domainEvent.RefundAmount,  // Same as refund for full refunds
                    refundReason: "Registration Cancellation",
                    requestedAt: DateTime.UtcNow,
                    paymentIntentId: domainEvent.PaymentIntentId  // Phase 6A.87++ Fix: Reference number
                );
                emailParams.SupportEmail = SupportEmail;
                emailParams.EventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);  // Phase 6A.97: For "View Event Details" button

                // Phase 6A.87+ Fix: Populate organizer contact if available
                if (!string.IsNullOrWhiteSpace(@event.OrganizerContactName))
                {
                    emailParams.WithOrganizerContact(
                        @event.OrganizerContactName,
                        @event.OrganizerContactEmail,
                        @event.OrganizerContactPhone);
                }

                // Phase 6A.100 Fix: Add signup lists URL if event has signup lists
                if (@event.HasSignUpLists())
                {
                    emailParams.WithSignUpLists(
                        _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups");
                }

                // Phase 6A.112: Check if event has active signup forms
                var forms = await _eventFormRepository.GetByEventIdAsync(@event.Id, cancellationToken);
                var hasActiveForms = forms.Any(f => f.Status == EventFormStatus.Active);

                if (hasActiveForms)
                {
                    emailParams.WithSignupForms($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms");
                }

                // Phase 6A.100: Send via typed email service
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    cancellationToken);

                stopwatch.Stop();

                if (!typedResult.Success)
                {
                    _logger.LogError(
                        "[Phase 6A.87] RefundRequested FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.ContactEmail, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.100] RefundRequested COMPLETE: Email sent - Email={Email}, Amount=${Amount}, Duration={ElapsedMs}ms",
                        domainEvent.ContactEmail, domainEvent.RefundAmount, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.92] RefundRequested CANCELED: Operation was canceled - EventId={EventId}, RegistrationId={RegId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "[Phase 6A.92] RefundRequested FAILED: Exception occurred - EventId={EventId}, RegistrationId={RegId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
