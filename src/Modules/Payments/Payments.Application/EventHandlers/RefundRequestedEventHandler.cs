using System.Diagnostics;
using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Interfaces;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Payments.Application.EventHandlers;

/// <summary>
/// Phase 6A.92: Handles RefundRequestedEvent to send refund notification email to user.
/// Triggered when a refund is initiated (either by user cancellation or event cancellation).
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support.
///
/// LEGACY HANDLER — DO NOT EXTEND.
/// Phase 6A.148 introduced an approval-workflow refund path with its own dedicated
/// lifecycle handlers (RefundRequestCreated / RefundRequestApproved / RefundRequestRejected /
/// OrganizerInitiatedRefundCreated under <c>Events.EventHandlers.RefundRequests</c>) and
/// dedicated lifecycle templates (Wave 3 D7). This handler now only fires on the
/// <c>Refund:ApprovalWorkflow:Enabled=false</c> code path, which exists for rollback
/// safety only. Remove this handler + the RefundRequestedEvent itself after the
/// feature flag has ramped to 100% in production AND the legacy paths in
/// <c>CancelRsvpCommandHandler</c> + <c>EventCancellationEmailJob</c> have been removed.
/// </summary>
public class RefundRequestedEventHandler : INotificationHandler<DomainEventNotification<RefundRequestedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IFormQueries _formQueries;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RefundRequestedEventHandler> _logger;

    public RefundRequestedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IFormQueries formQueries,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RefundRequestedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _formQueries = formQueries;
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
                // Cancellation enhancement: Include add-on refund amount in email total
                var totalRefundAmount = domainEvent.RefundAmount + domainEvent.AddOnRefundAmount;

                _logger.LogInformation(
                    "[RefundEmail] Composing refund email - RegistrationRefund=${RegistrationRefund}, AddOnRefund=${AddOnRefund}, TotalRefund=${TotalRefund}",
                    domainEvent.RefundAmount, domainEvent.AddOnRefundAmount, totalRefundAmount);

                var emailParams = RefundEmailParams.CreateRequest(
                    userId: userId,
                    userName: userName,
                    userEmail: domainEvent.ContactEmail,
                    registrationId: domainEvent.RegistrationId,
                    refundId: Guid.NewGuid(),  // Refund ID not available in domain event yet
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: refund flow can't fire on TBD today (Register blocks)
                    timeZoneId: @event.TimeZoneId,
                    refundAmount: totalRefundAmount,
                    originalAmount: totalRefundAmount,  // Same as refund for full refunds
                    refundReason: "Registration Cancellation",
                    requestedAt: DateTime.UtcNow,
                    paymentIntentId: domainEvent.PaymentIntentId  // Phase 6A.87++ Fix: Reference number
                );
                emailParams.EventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);  // Phase 6A.97: For "View Event Details" button

                // Phase 6A.87+ Fix: Populate organizer contact if available
                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                // Phase 6A.100 Fix: Add signup lists URL if event has signup lists
                if (@event.HasSignUpLists())
                {
                    emailParams.WithSignUpLists(
                        _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups");
                }

                // Phase 6A.112: Check if event has active signup forms
                var forms = await _formQueries.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, @event.Id, cancellationToken);
                var hasActiveForms = forms.Any(f => f.Status == FormStatusDto.Active);

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
