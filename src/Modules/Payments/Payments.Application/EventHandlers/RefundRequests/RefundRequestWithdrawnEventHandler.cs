using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
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
/// Phase 6A.148.W4.D13 (G2 fix): Sends the "you withdrew your refund request" email
/// to the attendee when they use the in-app Withdraw button on the pending-review
/// status banner (<c>RefundRequestStatusBanner.tsx:106-114</c> → <c>page.tsx:1264</c>).
///
/// Before D13, <c>RefundRequestWithdrawnEvent</c> had ZERO subscribers — attendees
/// withdrew refunds and got no email confirmation, leaving them unsure whether the
/// action took effect. Architect's Wave 4 plan called this out as a silent gap (G2).
///
/// Per Q2 product decision (locked 2026-05-19): organizer is NOT notified — the
/// queue item simply disappears from their dashboard, no extra inbox noise.
///
/// Bind: <c>template-refund-withdrawn</c> (header "Refund Request Withdrawn"),
/// via <see cref="RefundWithdrawnEmailParams"/>. Includes the D10 Validate() pre-send
/// guard and the structured logging pattern that the other lifecycle handlers use.
///
/// Fail-silent — mirrors <see cref="RefundRequestRejectedEventHandler"/> pattern.
/// </summary>
public class RefundRequestWithdrawnEventHandler
    : INotificationHandler<DomainEventNotification<RefundRequestWithdrawnEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRefundRequestRepository _refundRequestRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RefundRequestWithdrawnEventHandler> _logger;

    public RefundRequestWithdrawnEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRefundRequestRepository refundRequestRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RefundRequestWithdrawnEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _refundRequestRepository = refundRequestRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<RefundRequestWithdrawnEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RefundRequestWithdrawn"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RefundRequestId", domainEvent.RefundRequestId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn START: RrId={RrId} EventId={EventId} WithdrawnByUserId={UserId}",
                domainEvent.RefundRequestId, domainEvent.EventId, domainEvent.WithdrawnByUserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var refundRequest = await _refundRequestRepository.GetByIdAsync(
                    domainEvent.RefundRequestId, cancellationToken);
                if (refundRequest == null)
                {
                    _logger.LogWarning(
                        "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn: refund request not found RrId={RrId}",
                        domainEvent.RefundRequestId);
                    return;
                }

                if (refundRequest.LineItems.Count == 0)
                {
                    // Defensive — domain CreateRefundRequest enforces line-items presence,
                    // but if we ever reached this state the email body would render empty.
                    _logger.LogWarning(
                        "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn: request has no line items, skipping email RrId={RrId}",
                        domainEvent.RefundRequestId);
                    return;
                }

                var user = await _userRepository.GetByIdAsync(domainEvent.WithdrawnByUserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning(
                        "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn: user not found UserId={UserId}",
                        domainEvent.WithdrawnByUserId);
                    return;
                }

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn: event not found EventId={EventId}",
                        domainEvent.EventId);
                    return;
                }

                var lineViews = refundRequest.LineItems.Select(li => li.ToView()).ToList();
                var currency = refundRequest.LineItems.FirstOrDefault()?.RequestedAmount.Currency.ToString() ?? "USD";

                var emailParams = RefundWithdrawnEmailParams.Create(
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
                    withdrawnAt: domainEvent.WithdrawnAt,
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id));

                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                // Phase 6A.148.D10: validate before send.
                if (!emailParams.Validate(out var validationErrors))
                {
                    sw.Stop();
                    _logger.LogError(
                        "[6A.148.D10 VALIDATE] RefundRequestWithdrawn: email params FAILED validation, NOT sending. RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, user.Email.Value, emailParams.TemplateName, string.Join("; ", validationErrors), sw.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "[6A.148.D10 EMAIL] RefundRequestWithdrawn invoking SendEmailAsync: RrId={RrId} Email={Email} Template={Template} Lines={LineCount}",
                    domainEvent.RefundRequestId, user.Email.Value, emailParams.TemplateName, lineViews.Count);

                var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);
                sw.Stop();

                if (!result.Success)
                    _logger.LogError(
                        "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn FAILED to send: RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, user.Email.Value, emailParams.TemplateName, string.Join(", ", result.Errors), sw.ElapsedMilliseconds);
                else
                    _logger.LogInformation(
                        "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn email sent: RrId={RrId} Email={Email} Lines={LineCount} Template={Template} Duration={Ms}ms",
                        domainEvent.RefundRequestId, user.Email.Value, lineViews.Count, emailParams.TemplateName, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning(
                    "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn CANCELED: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148.W4.D13 EMAIL] RefundRequestWithdrawn EXCEPTION: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
            }
        }
    }
}
