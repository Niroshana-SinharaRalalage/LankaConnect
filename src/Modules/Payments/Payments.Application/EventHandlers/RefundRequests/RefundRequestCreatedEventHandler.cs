using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Application.Interfaces;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Payments.Application.EventHandlers.RefundRequests;

/// <summary>
/// Phase 6A.148.D8 (Wave 3 rewire): Sends the "your refund request is pending organizer
/// review" email to the attendee when they submit a refund request via the approval
/// workflow (typically by cancelling a paid registration).
///
/// Now binds to the dedicated <c>template-refund-pending-review</c> via
/// <see cref="RefundPendingReviewEmailParams"/> — header "Refund Request Received".
/// The legacy <c>template-refund-requested</c> reuse from 148.c (header "Refund In
/// Progress") was misleading operators (E1/E2 UAT) by suggesting Stripe was already
/// running money before organizer approval.
///
/// LineItems are passed as a structured list (not body-stuffed text) so the email
/// can render a per-item table with amount columns. Operator-supplied reason flows
/// through as a first-class field with a HasRequesterReason boolean for conditional
/// rendering.
///
/// Fail-silent — mirrors RefundRequestedEventHandler (Phase 6A.92) pattern.
/// </summary>
public class RefundRequestCreatedEventHandler
    : INotificationHandler<DomainEventNotification<RefundRequestCreatedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRefundRequestRepository _refundRequestRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RefundRequestCreatedEventHandler> _logger;

    public RefundRequestCreatedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRefundRequestRepository refundRequestRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RefundRequestCreatedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _refundRequestRepository = refundRequestRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<RefundRequestCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RefundRequestCreated"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RefundRequestId", domainEvent.RefundRequestId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148.D8 EMAIL] RefundRequestCreated START: RefundRequestId={RrId} EventId={EventId} UserId={UserId}",
                domainEvent.RefundRequestId, domainEvent.EventId, domainEvent.RequestedByUserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8 EMAIL] RefundRequestCreated: event not found EventId={EventId}",
                        domainEvent.EventId);
                    return;
                }

                var user = await _userRepository.GetByIdAsync(domainEvent.RequestedByUserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8 EMAIL] RefundRequestCreated: user not found UserId={UserId}",
                        domainEvent.RequestedByUserId);
                    return;
                }

                var refundRequest = await _refundRequestRepository.GetByIdAsync(
                    domainEvent.RefundRequestId, cancellationToken);
                if (refundRequest == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8 EMAIL] RefundRequestCreated: refund request not found RrId={RrId}",
                        domainEvent.RefundRequestId);
                    return;
                }

                // Wave 3 D8: structured line items + dedicated template — no more body-stuffing.
                var lineViews = refundRequest.LineItems.Select(li => li.ToView()).ToList();
                var currency = refundRequest.LineItems.FirstOrDefault()?.RequestedAmount.Currency.ToString() ?? "USD";

                var emailParams = RefundPendingReviewEmailParams.Create(
                    userId: user.Id,
                    userName: $"{user.FirstName} {user.LastName}",
                    userEmail: user.Email.Value,
                    registrationId: domainEvent.RegistrationId,
                    refundRequestId: domainEvent.RefundRequestId,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate.GetValueOrDefault(),
                    timeZoneId: @event.TimeZoneId,
                    lineItems: lineViews,
                    currency: currency,
                    requesterReason: domainEvent.RequesterReason,
                    requestedAt: domainEvent.RequestedAt,
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id));

                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                // Phase 6A.148.D10: validate before send (see RefundRequestApprovedEventHandler for rationale).
                if (!emailParams.Validate(out var validationErrors))
                {
                    sw.Stop();
                    _logger.LogError(
                        "[6A.148.D10 VALIDATE] RefundRequestCreated: email params FAILED validation, NOT sending. RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, user.Email.Value, emailParams.TemplateName, string.Join("; ", validationErrors), sw.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "[6A.148.D10 EMAIL] RefundRequestCreated invoking SendEmailAsync: RrId={RrId} Email={Email} Template={Template} Lines={LineCount}",
                    domainEvent.RefundRequestId, user.Email.Value, emailParams.TemplateName, lineViews.Count);

                var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);
                sw.Stop();

                if (!result.Success)
                    _logger.LogError(
                        "[6A.148.D8 EMAIL] RefundRequestCreated FAILED to send: RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, user.Email.Value, emailParams.TemplateName, string.Join(", ", result.Errors), sw.ElapsedMilliseconds);
                else
                    _logger.LogInformation(
                        "[6A.148.D8 EMAIL] RefundRequestCreated email sent: RrId={RrId} Email={Email} Lines={LineCount} Template={Template} Duration={Ms}ms",
                        domainEvent.RefundRequestId, user.Email.Value, lineViews.Count, emailParams.TemplateName, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning(
                    "[6A.148.D8 EMAIL] RefundRequestCreated CANCELED: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                // Fail-silent: log but don't throw (matches RefundRequestedEventHandler pattern).
                _logger.LogError(ex,
                    "[6A.148.D8 EMAIL] RefundRequestCreated EXCEPTION: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
            }
        }
    }
}
