using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Application.Interfaces;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers.RefundRequests;

/// <summary>
/// Phase 6A.148.c (D4b + D6 fix): Sends the "your refund has been reviewed" email
/// to the attendee when the organizer approves a refund request. The email body
/// includes a per-line decision breakdown — operator UAT showed that without this,
/// attendees who had (say) 2 sponsors approved + 4 add-ons declined received only
/// a single legacy-shape email with no per-line clarity.
///
/// Lifecycle: this email fires at the moment of organizer Approve. The legacy
/// `RefundCompletedEvent` email STILL fires later when Stripe confirms the money
/// movement (per product decision Q4 — keep both). The two emails describe
/// different facts: "organizer decided" vs "money landed".
///
/// Reuses template-refund-requested with the decision-summary in the refundReason
/// field. A bespoke decision template can replace this in Wave 3.
/// </summary>
public class RefundRequestApprovedEventHandler
    : INotificationHandler<DomainEventNotification<RefundRequestApprovedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRefundRequestRepository _refundRequestRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RefundRequestApprovedEventHandler> _logger;

    public RefundRequestApprovedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRefundRequestRepository refundRequestRepository,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RefundRequestApprovedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _refundRequestRepository = refundRequestRepository;
        _registrationRepository = registrationRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<RefundRequestApprovedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RefundRequestApproved"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RefundRequestId", domainEvent.RefundRequestId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148.c EMAIL] RefundRequestApproved START: RrId={RrId} EventId={EventId} OrganizerUserId={OrgId}",
                domainEvent.RefundRequestId, domainEvent.EventId, domainEvent.OrganizerUserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var refundRequest = await _refundRequestRepository.GetByIdAsync(
                    domainEvent.RefundRequestId, cancellationToken);
                if (refundRequest == null)
                {
                    _logger.LogWarning(
                        "[6A.148.c EMAIL] RefundRequestApproved: refund request not found RrId={RrId}",
                        domainEvent.RefundRequestId);
                    return;
                }

                // The attendee is the registration's UserId (NOT the organizer who approved).
                var registration = await _registrationRepository.GetByIdAsync(
                    refundRequest.RegistrationId, cancellationToken);
                if (registration == null || registration.UserId == null)
                {
                    _logger.LogWarning(
                        "[6A.148.c EMAIL] RefundRequestApproved: registration or attendee not found RegId={RegId}",
                        refundRequest.RegistrationId);
                    return;
                }

                var attendee = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                if (attendee == null) return;

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null) return;

                // Per-line decision breakdown — the architect-recommended single-summary
                // approach (Q2 decision) condensed into one email body string.
                var breakdownLines = refundRequest.LineItems.Select(li =>
                {
                    var decision = li.Status switch
                    {
                        RefundLineItemStatus.Approved => $"approved ${li.ApprovedAmount?.Amount:N2}",
                        RefundLineItemStatus.Processing => $"approved ${li.ApprovedAmount?.Amount:N2} (Stripe processing)",
                        RefundLineItemStatus.Refunded => $"refunded ${li.ApprovedAmount?.Amount:N2}",
                        RefundLineItemStatus.Rejected => $"declined (requested ${li.RequestedAmount.Amount:N2})",
                        RefundLineItemStatus.Failed => $"failed (requested ${li.RequestedAmount.Amount:N2}) — operator will retry",
                        _ => $"{li.Status}"
                    };
                    return $"{li.Type}: {decision}";
                });
                var breakdown = string.Join("; ", breakdownLines);

                var approvedTotal = refundRequest.LineItems
                    .Where(li => li.ApprovedAmount is not null)
                    .Sum(li => li.ApprovedAmount!.Amount);
                var requestedTotal = refundRequest.LineItems.Sum(li => li.RequestedAmount.Amount);

                var reason = $"Organizer decision: {breakdown}. Approved total: ${approvedTotal:N2} of ${requestedTotal:N2} requested. " +
                             "Stripe is now processing the refund(s); you'll get another email when the money lands in your account (typically 5–10 business days).";

                var emailParams = RefundEmailParams.CreateRequest(
                    userId: attendee.Id,
                    userName: $"{attendee.FirstName} {attendee.LastName}",
                    userEmail: attendee.Email.Value,
                    registrationId: refundRequest.RegistrationId,
                    refundId: refundRequest.Id,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate.GetValueOrDefault(),
                    timeZoneId: @event.TimeZoneId,
                    refundAmount: approvedTotal,
                    originalAmount: requestedTotal,
                    refundReason: reason,
                    requestedAt: domainEvent.ApprovedAt,
                    paymentIntentId: null);
                emailParams.EventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);

                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);
                sw.Stop();

                if (!result.Success)
                    _logger.LogError(
                        "[6A.148.c EMAIL] RefundRequestApproved FAILED to send: RrId={RrId} Email={Email} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, string.Join(", ", result.Errors), sw.ElapsedMilliseconds);
                else
                    _logger.LogInformation(
                        "[6A.148.c EMAIL] RefundRequestApproved email sent: RrId={RrId} Email={Email} Approved=${Approved} of ${Requested} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, approvedTotal, requestedTotal, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning(
                    "[6A.148.c EMAIL] RefundRequestApproved CANCELED: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148.c EMAIL] RefundRequestApproved EXCEPTION: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
            }
        }
    }
}
