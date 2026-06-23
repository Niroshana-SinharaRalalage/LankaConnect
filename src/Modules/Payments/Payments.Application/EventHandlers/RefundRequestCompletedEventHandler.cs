using System.Diagnostics;
using LankaConnect.Modules.Forms.Contracts;
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

namespace LankaConnect.Modules.Payments.Application.EventHandlers;

/// <summary>
/// Phase 6A.148.W5.6.B G2 — handles <see cref="RefundRequestCompletedEvent"/> by sending
/// the refund-completion confirmation email to the attendee.
///
/// This is the WORKFLOW-PATH counterpart to <see cref="RefundCompletedEventHandler"/>:
/// the legacy handler stays in place for the pre-148 direct-Stripe CancelRsvp path
/// (no RefundRequest exists there), and this handler takes over whenever a refund
/// flows through the approval workflow.
///
/// Race fix: the new event is raised from <c>RefundRequest.MarkCompletedIfAllSettled</c>
/// at the EXACT moment Status flips to Completed, which is gated by
/// <c>_lineItems.All(...terminal)</c>. The payload's <see cref="RefundRequestCompletedEvent.TotalRefundedAmount"/>
/// is therefore the final settled amount by construction — no IRefundTotalCalculator
/// snapshot, no mid-race undercount (closes 4th-report regression RR 86d0a7dc
/// 2026-05-23 where the ticket-webhook-driven legacy event captured $94 while the
/// Sponsor line was still 831ms from committing the $110 charge).
/// </summary>
public class RefundRequestCompletedEventHandler
    : INotificationHandler<DomainEventNotification<RefundRequestCompletedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IFormQueries _formQueries;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RefundRequestCompletedEventHandler> _logger;

    public RefundRequestCompletedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IFormQueries formQueries,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RefundRequestCompletedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _formQueries = formQueries;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<RefundRequestCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RefundRequestCompleted"))
        using (LogContext.PushProperty("RefundRequestId", domainEvent.RefundRequestId))
        using (LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId))
        using (LogContext.PushProperty("StripeRefundId", domainEvent.PrimaryStripeRefundId))
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "[Phase 6A.148.W5.6.B] RefundRequestCompleted START: RrId={RrId}, RegId={RegId}, Sri={Sri}, Total={Total} {Currency}",
                domainEvent.RefundRequestId, domainEvent.RegistrationId,
                domainEvent.PrimaryStripeRefundId, domainEvent.TotalRefundedAmount, domainEvent.Currency);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var registration = await _registrationRepository.GetByIdAsync(
                    domainEvent.RegistrationId, cancellationToken);
                if (registration is null)
                {
                    _logger.LogWarning(
                        "[Phase 6A.148.W5.6.B] RefundRequestCompleted: registration not found — RegId={RegId}",
                        domainEvent.RegistrationId);
                    return;
                }

                var @event = await _eventRepository.GetByIdAsync(registration.EventId, cancellationToken);
                if (@event is null)
                {
                    _logger.LogWarning(
                        "[Phase 6A.148.W5.6.B] RefundRequestCompleted: event not found — EventId={EventId}",
                        registration.EventId);
                    return;
                }

                var userName = "Valued Customer";
                var userId = Guid.Empty;
                var userEmail = registration.Contact?.Email
                                ?? registration.AttendeeInfo?.Email?.Value
                                ?? string.Empty;
                if (registration.UserId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                    if (user is not null)
                    {
                        userName = $"{user.FirstName} {user.LastName}";
                        userId = user.Id;
                        if (string.IsNullOrWhiteSpace(userEmail))
                            userEmail = user.Email?.Value ?? string.Empty;
                    }
                }

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    _logger.LogWarning(
                        "[Phase 6A.148.W5.6.B] RefundRequestCompleted: recipient email missing — RegId={RegId}",
                        domainEvent.RegistrationId);
                    return;
                }

                var emailParams = RefundEmailParams.CreateCompleted(
                    userId: userId,
                    userName: userName,
                    userEmail: userEmail,
                    registrationId: domainEvent.RegistrationId,
                    refundId: domainEvent.RefundRequestId,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate.GetValueOrDefault(),
                    timeZoneId: @event.TimeZoneId,
                    refundAmount: domainEvent.TotalRefundedAmount,
                    originalAmount: domainEvent.TotalRefundedAmount,
                    completedAt: domainEvent.CompletedAt,
                    stripeRefundId: domainEvent.PrimaryStripeRefundId ?? string.Empty,
                    processingMethod: "Original Payment Method");
                emailParams.Currency = domainEvent.Currency;
                emailParams.EventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);

                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                if (@event.HasSignUpLists())
                {
                    emailParams.WithSignUpLists(
                        _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups");
                }

                var forms = await _formQueries.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, @event.Id, cancellationToken);
                if (forms.Any(f => f.Status == FormStatusDto.Active))
                {
                    emailParams.WithSignupForms(
                        $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms");
                }

                var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);
                stopwatch.Stop();

                if (!result.Success)
                {
                    _logger.LogError(
                        "[Phase 6A.148.W5.6.B] RefundRequestCompleted email FAILED — RrId={RrId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.RefundRequestId, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.148.W5.6.B] RefundRequestCompleted email SENT — RrId={RrId}, Recipient={Recipient}, Total={Total} {Currency}, Duration={ElapsedMs}ms",
                        domainEvent.RefundRequestId, userEmail,
                        domainEvent.TotalRefundedAmount, domainEvent.Currency, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.148.W5.6.B] RefundRequestCompleted CANCELED — RrId={RrId}, Duration={ElapsedMs}ms",
                    domainEvent.RefundRequestId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[Phase 6A.148.W5.6.B] RefundRequestCompleted EXCEPTION — RrId={RrId}, Duration={ElapsedMs}ms",
                    domainEvent.RefundRequestId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
