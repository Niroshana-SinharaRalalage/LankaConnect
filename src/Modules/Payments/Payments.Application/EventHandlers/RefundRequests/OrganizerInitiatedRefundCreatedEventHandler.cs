using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.SharedKernel.Money;
using LankaConnect.SharedKernel.Identity;
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
/// Phase 6A.148.D8b: Sends the "your organizer has initiated a refund on your behalf"
/// email to the attendee when an organizer creates a refund request on the attendee's
/// behalf. The organizer-initiated path skips Pending and lands directly in Approved,
/// so the attendee should NOT receive the pending-review email — only this decision
/// email.
///
/// Before D8b, this event had NO subscribers — organizer-initiated refunds sent zero
/// emails to attendees. Architect's Wave 3 plan called this out as a silent gap.
///
/// Uses the same <see cref="RefundDecisionEmailParams"/> + template-refund-decision
/// surface as <see cref="RefundRequestApprovedEventHandler"/>, but passes
/// <c>IsOrganizerInitiated=true</c> so the template renders the body copy variant
/// ("Your organizer has initiated a refund on your behalf" instead of "Your organizer
/// has decided on your refund request").
///
/// Lifecycle: this email fires once at organizer-initiated request creation. The
/// legacy <c>RefundCompletedEvent</c> email STILL fires later when Stripe confirms
/// the money movement (parity with the attendee-initiated path).
///
/// Fail-silent — mirrors <see cref="RefundRequestApprovedEventHandler"/> pattern.
/// </summary>
public class OrganizerInitiatedRefundCreatedEventHandler
    : INotificationHandler<DomainEventNotification<OrganizerInitiatedRefundCreatedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRefundRequestRepository _refundRequestRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<OrganizerInitiatedRefundCreatedEventHandler> _logger;

    public OrganizerInitiatedRefundCreatedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRefundRequestRepository refundRequestRepository,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<OrganizerInitiatedRefundCreatedEventHandler> logger)
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
        DomainEventNotification<OrganizerInitiatedRefundCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "OrganizerInitiatedRefundCreated"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RefundRequestId", domainEvent.RefundRequestId))
        using (LogContext.PushProperty("OrganizerUserId", domainEvent.OrganizerUserId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated START: RrId={RrId} EventId={EventId} OrganizerUserId={OrgId} ScanGuardOverridden={Override}",
                domainEvent.RefundRequestId, domainEvent.EventId, domainEvent.OrganizerUserId, domainEvent.ScanGuardOverridden);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var refundRequest = await _refundRequestRepository.GetByIdAsync(
                    domainEvent.RefundRequestId, cancellationToken);
                if (refundRequest == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated: refund request not found RrId={RrId}",
                        domainEvent.RefundRequestId);
                    return;
                }

                if (refundRequest.LineItems.Count == 0)
                {
                    // Defensive — shouldn't happen because domain CreateRefundRequest enforces line-items
                    // presence, but if it did the email would have no payload to render.
                    _logger.LogWarning(
                        "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated: refund request has no line items, skipping email RrId={RrId}",
                        domainEvent.RefundRequestId);
                    return;
                }

                // The attendee is the registration's UserId (NOT the organizer who initiated).
                var registration = await _registrationRepository.GetByIdAsync(
                    refundRequest.RegistrationId, cancellationToken);
                if (registration == null || registration.UserId == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated: registration or attendee not found RegId={RegId}",
                        refundRequest.RegistrationId);
                    return;
                }

                var attendee = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                if (attendee == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated: attendee user not found UserId={UserId}",
                        registration.UserId.Value);
                    return;
                }

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated: event not found EventId={EventId}",
                        domainEvent.EventId);
                    return;
                }

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
                    isOrganizerInitiated: true, // drives template body-copy variant
                    decidedAt: domainEvent.CreatedAt,
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
                        "[6A.148.D10 VALIDATE] OrganizerInitiatedRefundCreated: email params FAILED validation, NOT sending. RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, string.Join("; ", validationErrors), sw.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "[6A.148.D10 EMAIL] OrganizerInitiatedRefundCreated invoking SendEmailAsync: RrId={RrId} Email={Email} Template={Template} Lines={LineCount} Approved=${Approved}",
                    domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, lineViews.Count, emailParams.ApprovedTotal);

                var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);
                sw.Stop();

                if (!result.Success)
                    _logger.LogError(
                        "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated FAILED to send: RrId={RrId} Email={Email} Template={Template} Errors={Errors} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, emailParams.TemplateName, string.Join(", ", result.Errors), sw.ElapsedMilliseconds);
                else
                    _logger.LogInformation(
                        "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated email sent: RrId={RrId} Email={Email} Approved=${Approved} of ${Requested} Lines={LineCount} Template={Template} Duration={Ms}ms",
                        domainEvent.RefundRequestId, attendee.Email.Value, emailParams.ApprovedTotal, emailParams.RequestedTotal, lineViews.Count, emailParams.TemplateName, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning(
                    "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated CANCELED: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148.D8b EMAIL] OrganizerInitiatedRefundCreated EXCEPTION: RrId={RrId} Duration={Ms}ms",
                    domainEvent.RefundRequestId, sw.ElapsedMilliseconds);
            }
        }
    }
}
