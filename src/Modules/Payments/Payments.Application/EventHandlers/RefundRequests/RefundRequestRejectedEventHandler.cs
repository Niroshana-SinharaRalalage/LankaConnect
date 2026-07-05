using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.BuildingBlocks.Application.Interfaces;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Payments.Application.EventHandlers.RefundRequests;

/// <summary>
/// Phase 6A.148.D8 (Wave 3 rewire): Sends the rejection email to the attendee when
/// the organizer declines a refund request.
///
/// Now binds to the dedicated <c>template-refund-rejected</c> via
/// <see cref="RefundRejectedEmailParams"/> — header "Refund Request Declined". The
/// customer-facing <see cref="RefundRejectedEmailParams.RejectionReason"/> is a
/// top-level mandatory field (no body-stuffing). Line items still render as a
/// reference table so the attendee can confirm what was originally requested.
///
/// Per product decision Q4, this is the end state — no "Contact Organizer" button
/// or escalation path. The organizer-contact block still renders so attendees who
/// want to dispute can reach out manually.
/// </summary>
public class RefundRequestRejectedEventHandler
    : INotificationHandler<DomainEventNotification<RefundRequestRejectedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRefundRequestRepository _refundRequestRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RefundRequestRejectedEventHandler> _logger;

    public RefundRequestRejectedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRefundRequestRepository refundRequestRepository,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RefundRequestRejectedEventHandler> logger)
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
        DomainEventNotification<RefundRequestRejectedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RefundRequestRejected"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RefundRequestId", domainEvent.RefundRequestId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148.D8 EMAIL] RefundRequestRejected START: RrId={RrId} EventId={EventId}",
                domainEvent.RefundRequestId, domainEvent.EventId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var refundRequest = await _refundRequestRepository.GetByIdAsync(
                    domainEvent.RefundRequestId, cancellationToken);
                if (refundRequest == null) return;

                var registration = await _registrationRepository.GetByIdAsync(
                    refundRequest.RegistrationId, cancellationToken);
                if (registration == null || registration.UserId == null) return;

                var attendee = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                if (attendee == null) return;

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null) return;

                // Wave 3 D8: structured line items + dedicated template-refund-rejected.
                // RejectionReason is a top-level mandatory field (Validate() rejects empty).
                var lineViews = refundRequest.LineItems.Select(li => li.ToView()).ToList();
                var currency = refundRequest.LineItems.FirstOrDefault()?.RequestedAmount.Currency.ToString() ?? "USD";

                var emailParams = RefundRejectedEmailParams.Create(
                    userId: attendee.Id,
                    userName: $"{attendee.FirstName} {attendee.LastName}",
                    userEmail: attendee.Email.Value,
                    registrationId: refundRequest.RegistrationId,
                    refundRequestId: refundRequest.Id,
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate.GetValueOrDefault(),
                    timeZoneId: @event.TimeZoneId,
                    lineItems: lineViews,
                    currency: currency,
                    rejectionReason: domainEvent.RejectionReason,
                    rejectedAt: domainEvent.RejectedAt,
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id));

                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                // Phase 6A.148.D10: validate before send (RejectionReason required, mandatory line items).
                if (!emailParams.Validate(out var validationErrors))
                {
                    sw.Stop();
                    _logger.LogError(
                        "[6A.148.D10 VALIDATE] RefundRequestRejected: email params FAILED validation, NOT sending. RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, string.Join("; ", validationErrors), sw.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "[6A.148.D10 EMAIL] RefundRequestRejected invoking SendEmailAsync: RrId={RrId} Email={Email} Template={Template} Lines={LineCount}",
                    domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, lineViews.Count);

                var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);
                sw.Stop();

                if (!result.Success)
                    _logger.LogError(
                        "[6A.148.D8 EMAIL] RefundRequestRejected FAILED to send: RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, string.Join(", ", result.Errors), sw.ElapsedMilliseconds);
                else
                    _logger.LogInformation(
                        "[6A.148.D8 EMAIL] RefundRequestRejected email sent: RrId={RrId} Email={Email} Lines={LineCount} Template={Template} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, lineViews.Count, emailParams.TemplateName, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148.D8 EMAIL] RefundRequestRejected EXCEPTION: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
            }
        }
    }
}
