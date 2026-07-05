using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
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
/// Phase 6A.148.D8 (Wave 3 rewire): Sends the "your refund has been reviewed" email
/// to the attendee when the organizer approves a refund request.
///
/// Now binds to the dedicated <c>template-refund-decision</c> via
/// <see cref="RefundDecisionEmailParams"/> — header "Refund Decision". The email
/// body renders a per-line decision table from the structured
/// <see cref="RefundLineItemView"/> list (approved $X / declined / processing
/// badges) instead of body-stuffing it as text into <c>RefundReason</c>.
///
/// IsOrganizerInitiated is false here — this handler fires for the attendee-initiated
/// path only. The parallel D8b handler (<c>OrganizerInitiatedRefundCreatedEventHandler</c>)
/// reuses <see cref="RefundDecisionEmailParams"/> with IsOrganizerInitiated=true
/// when the organizer creates a refund on behalf of an attendee.
///
/// Lifecycle: this email fires at the moment of organizer Approve. The legacy
/// <c>RefundCompletedEvent</c> email STILL fires later when Stripe confirms the
/// money movement (per product decision Q4 — keep both). The two emails describe
/// different facts: "organizer decided" vs "money landed".
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
                "[6A.148.D8 EMAIL] RefundRequestApproved START: RrId={RrId} EventId={EventId} OrganizerUserId={OrgId}",
                domainEvent.RefundRequestId, domainEvent.EventId, domainEvent.OrganizerUserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var refundRequest = await _refundRequestRepository.GetByIdAsync(
                    domainEvent.RefundRequestId, cancellationToken);
                if (refundRequest == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8 EMAIL] RefundRequestApproved: refund request not found RrId={RrId}",
                        domainEvent.RefundRequestId);
                    return;
                }

                // The attendee is the registration's UserId (NOT the organizer who approved).
                var registration = await _registrationRepository.GetByIdAsync(
                    refundRequest.RegistrationId, cancellationToken);
                if (registration == null || registration.UserId == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8 EMAIL] RefundRequestApproved: registration or attendee not found RegId={RegId}",
                        refundRequest.RegistrationId);
                    return;
                }

                var attendee = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                if (attendee == null) return;

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null) return;

                // Wave 3 D8: structured line items → per-line decision table rendered by
                // template-refund-decision (no more body-stuffed text). Status badges
                // (approved / declined / processing) come from the status string on
                // each RefundLineItemView via RefundLineItemsHtmlBuilder.BuildDecisionListHtml.
                var lineViews = refundRequest.LineItems.Select(li => li.ToView()).ToList();
                var currency = refundRequest.LineItems.FirstOrDefault()?.RequestedAmount.Currency.ToString() ?? "USD";

                var emailParams = RefundDecisionEmailParams.Create(
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
                    isOrganizerInitiated: false, // attendee-initiated path — D8b handles the organizer-initiated case
                    decidedAt: domainEvent.ApprovedAt,
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id));

                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                // Phase 6A.148.D10: validate before send so a silent template-binding gap
                // surfaces in logs instead of falling through to a no-op SendEmailAsync.
                if (!emailParams.Validate(out var validationErrors))
                {
                    sw.Stop();
                    _logger.LogError(
                        "[6A.148.D10 VALIDATE] RefundRequestApproved: email params FAILED validation, NOT sending. RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, string.Join("; ", validationErrors), sw.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "[6A.148.D10 EMAIL] RefundRequestApproved invoking SendEmailAsync: RrId={RrId} Email={Email} Template={Template} Lines={LineCount} Approved=${Approved}",
                    domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, lineViews.Count, emailParams.ApprovedTotal);

                var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);
                sw.Stop();

                if (!result.Success)
                    _logger.LogError(
                        "[6A.148.D8 EMAIL] RefundRequestApproved FAILED to send: RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, string.Join(", ", result.Errors), sw.ElapsedMilliseconds);
                else
                    _logger.LogInformation(
                        "[6A.148.D8 EMAIL] RefundRequestApproved email sent: RrId={RrId} Email={Email} Approved=${Approved} of ${Requested} Lines={LineCount} Template={Template} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, emailParams.ApprovedTotal, emailParams.RequestedTotal, lineViews.Count, emailParams.TemplateName, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning(
                    "[6A.148.D8 EMAIL] RefundRequestApproved CANCELED: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148.D8 EMAIL] RefundRequestApproved EXCEPTION: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
            }
        }
    }
}
