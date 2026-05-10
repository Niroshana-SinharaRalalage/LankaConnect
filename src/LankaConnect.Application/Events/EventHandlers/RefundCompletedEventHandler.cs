using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.92: Handles RefundCompletedEvent to send refund confirmation email to user.
/// Triggered when Stripe confirms the refund has been processed (via charge.refunded webhook).
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class RefundCompletedEventHandler : INotificationHandler<DomainEventNotification<RefundCompletedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RefundCompletedEventHandler> _logger;

    public RefundCompletedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEventFormRepository eventFormRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RefundCompletedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _eventFormRepository = eventFormRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RefundCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RefundCompleted"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId))
        using (LogContext.PushProperty("StripeRefundId", domainEvent.StripeRefundId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Phase 6A.92] RefundCompleted START: EventId={EventId}, RegistrationId={RegId}, RefundId={RefundId}, Amount=${Amount}",
                domainEvent.EventId, domainEvent.RegistrationId, domainEvent.StripeRefundId, domainEvent.RefundAmount);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get event details
                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[Phase 6A.92] RefundCompleted: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
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

                // Phase 6A.135: Calculate combined refund total (ticket + add-ons)
                var totalRefundAmount = domainEvent.RefundAmount + domainEvent.AddOnRefundAmount;

                _logger.LogInformation(
                    "[Phase 6A.135] RefundCompleted: TicketRefund=${TicketAmount}, AddOnRefund=${AddOnAmount}, TotalRefund=${TotalAmount}",
                    domainEvent.RefundAmount, domainEvent.AddOnRefundAmount, totalRefundAmount);

                // Phase 6A.87: Use typed email parameters for compile-time safety
                // Phase 6A.87 Fix: Added stripeRefundId parameter required by template
                var emailParams = RefundEmailParams.CreateCompleted(
                    userId: userId,
                    userName: userName,
                    userEmail: domainEvent.ContactEmail,
                    registrationId: domainEvent.RegistrationId,
                    refundId: Guid.NewGuid(),  // Internal refund ID
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: refund flow can't fire on TBD today (Register blocks)
                    timeZoneId: @event.TimeZoneId,
                    refundAmount: totalRefundAmount,  // Phase 6A.135: Combined ticket + add-on amount
                    originalAmount: totalRefundAmount,  // Same as refund for full refunds
                    completedAt: DateTime.UtcNow,
                    stripeRefundId: domainEvent.StripeRefundId,  // Phase 6A.87 Fix: Pass Stripe refund ID for template
                    processingMethod: "Original Payment Method"
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
                        "[Phase 6A.87] RefundCompleted FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.ContactEmail, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.100] RefundCompleted COMPLETE: Email sent - Email={Email}, RefundId={RefundId}, Amount=${Amount}, Duration={ElapsedMs}ms",
                        domainEvent.ContactEmail, domainEvent.StripeRefundId, domainEvent.RefundAmount, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.92] RefundCompleted CANCELED: Operation was canceled - EventId={EventId}, RegistrationId={RegId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "[Phase 6A.92] RefundCompleted FAILED: Exception occurred - EventId={EventId}, RegistrationId={RegId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
