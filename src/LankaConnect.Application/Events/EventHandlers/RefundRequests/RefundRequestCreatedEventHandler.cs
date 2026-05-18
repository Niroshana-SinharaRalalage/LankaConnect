using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
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
/// Phase 6A.148.c (D4b fix): Sends the "your refund request is pending organizer
/// approval" email to the attendee when they submit a refund request via the
/// approval workflow (typically by cancelling a paid registration).
///
/// This closes the gap the operator surfaced in UAT: cancellation email fired but
/// nothing told the attendee their refund was now in an organizer queue. Reuses
/// the existing template-refund-requested template; per-line breakdown is injected
/// into the refundReason field so it shows in the email body. A bespoke per-line
/// table template can replace this in a future cleanup (Wave 3).
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
                "[6A.148.c EMAIL] RefundRequestCreated START: RefundRequestId={RrId} EventId={EventId} UserId={UserId}",
                domainEvent.RefundRequestId, domainEvent.EventId, domainEvent.RequestedByUserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "[6A.148.c EMAIL] RefundRequestCreated: event not found EventId={EventId}",
                        domainEvent.EventId);
                    return;
                }

                var user = await _userRepository.GetByIdAsync(domainEvent.RequestedByUserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning(
                        "[6A.148.c EMAIL] RefundRequestCreated: user not found UserId={UserId}",
                        domainEvent.RequestedByUserId);
                    return;
                }

                var refundRequest = await _refundRequestRepository.GetByIdAsync(
                    domainEvent.RefundRequestId, cancellationToken);
                if (refundRequest == null)
                {
                    _logger.LogWarning(
                        "[6A.148.c EMAIL] RefundRequestCreated: refund request not found RrId={RrId}",
                        domainEvent.RefundRequestId);
                    return;
                }

                // Per-line summary text — injected into RefundReason which the existing
                // template-refund-requested template renders in the body.
                var lineBreakdown = string.Join("; ", refundRequest.LineItems
                    .Select(li => $"{li.Type} ${li.RequestedAmount.Amount:N2}"));
                var totalRequested = refundRequest.LineItems.Sum(li => li.RequestedAmount.Amount);
                var reasonWithBreakdown = string.IsNullOrWhiteSpace(domainEvent.RequesterReason)
                    ? $"Pending organizer review — items: {lineBreakdown}. You'll receive another email when the organizer approves or declines."
                    : $"Pending organizer review — items: {lineBreakdown}. Your reason: \"{domainEvent.RequesterReason}\". You'll receive another email when the organizer approves or declines.";

                var emailParams = RefundEmailParams.CreateRequest(
                    userId: user.Id,
                    userName: $"{user.FirstName} {user.LastName}",
                    userEmail: user.Email.Value,
                    registrationId: domainEvent.RegistrationId,
                    refundId: domainEvent.RefundRequestId,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate.GetValueOrDefault(),
                    timeZoneId: @event.TimeZoneId,
                    refundAmount: totalRequested,
                    originalAmount: totalRequested,
                    refundReason: reasonWithBreakdown,
                    requestedAt: domainEvent.RequestedAt,
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
                        "[6A.148.c EMAIL] RefundRequestCreated FAILED to send: RrId={RrId} Email={Email} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, user.Email.Value, string.Join(", ", result.Errors), sw.ElapsedMilliseconds);
                else
                    _logger.LogInformation(
                        "[6A.148.c EMAIL] RefundRequestCreated email sent: RrId={RrId} Email={Email} Total=${Total} Duration={Ms}ms",
                        domainEvent.RefundRequestId, user.Email.Value, totalRequested, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning(
                    "[6A.148.c EMAIL] RefundRequestCreated CANCELED: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                // Fail-silent: log but don't throw (matches RefundRequestedEventHandler pattern).
                _logger.LogError(ex,
                    "[6A.148.c EMAIL] RefundRequestCreated EXCEPTION: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
            }
        }
    }
}
