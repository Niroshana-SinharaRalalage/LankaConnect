using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using OrganizerContactInfo = LankaConnect.Shared.Email.Helpers.OrganizerContactInfo;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles RegistrationCancelledEvent to send cancellation confirmation email to attendee
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class RegistrationCancelledEventHandler : INotificationHandler<DomainEventNotification<RegistrationCancelledEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository; // Phase 7E.4: Load reg for HeadCount
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RegistrationCancelledEventHandler> _logger;

    public RegistrationCancelledEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEventFormRepository eventFormRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RegistrationCancelledEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _eventFormRepository = eventFormRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RegistrationCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RegistrationCancelled"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("AttendeeId", domainEvent.AttendeeId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RegistrationCancelled START: Event={EventId}, User={UserId}",
                domainEvent.EventId, domainEvent.AttendeeId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retrieve user and event data
                var user = await _userRepository.GetByIdAsync(domainEvent.AttendeeId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RegistrationCancelled: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RegistrationCancelled: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Phase 6A.87: Use typed email parameters for compile-time safety
                // Note: RegistrationCancelledEvent doesn't include RegistrationId, using Guid.Empty as placeholder
                var emailParams = RegistrationCancellationEmailParams.Create(
                    userId: user.Id,
                    userName: $"{user.FirstName} {user.LastName}",
                    userEmail: user.Email.Value,
                    registrationId: Guid.Empty,  // Phase 6A.97: RegistrationId is optional - domain event doesn't include it
                    eventId: @event.Id,
                    eventTitle: @event.Title.Value,
                    eventStartDate: @event.StartDate,
                    timeZoneId: @event.TimeZoneId,
                    eventLocation: GetEventLocationString(@event),
                    cancellationReason: "User cancelled registration",
                    cancelledAt: domainEvent.CancelledAt,
                    refundStatus: "No Refund Required"  // Default, will be updated by refund handler if applicable
                );
                emailParams.EventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);

                // Phase 7C.2b: emit decomposed location keys for the template rewrite.
                emailParams.WithLocationDetails(@event.ProjectEmailLocation());

                // Phase 6A.97+ Fix: Populate organizer contact if available
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

                // Phase 7E.4: Populate flexible registration params from the cancelled registration.
                // The row stays in the DB after Cancel() (Status changes to Cancelled), so loading
                // it here surfaces the snapshotted RegistrationMode + HeadCount. If the registration
                // can't be loaded for any reason, the FlexibleRegistration fields stay at their
                // safe defaults (HasHeadCount=false) and the email template renders neutrally.
                var cancelledRegistration = await _registrationRepository.GetByEventAndUserAsync(
                    domainEvent.EventId, domainEvent.AttendeeId, cancellationToken);
                if (cancelledRegistration != null)
                {
                    var flex = HeadCountEmailFormatter.Compute(cancelledRegistration);
                    emailParams.HasDetailedAttendees = flex.hasDetailedAttendees;
                    emailParams.HasHeadCount = flex.hasHeadCount;
                    emailParams.HasHeadCountBreakdown = flex.hasHeadCountBreakdown;
                    emailParams.HasTierBreakdown = flex.hasTierBreakdown;
                    emailParams.HeadCountTotal = flex.headCountTotal;
                    emailParams.HeadCountBreakdownLine = flex.headCountBreakdownLine;
                    emailParams.TierBreakdownLine = flex.tierBreakdownLine;
                    emailParams.LeadAttendeeName = flex.leadAttendeeName;
                }

                // Phase 6A.100: Send via typed email service
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    cancellationToken);

                stopwatch.Stop();

                if (!typedResult.Success)
                {
                    _logger.LogError(
                        "RegistrationCancelled FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        user.Email.Value, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.100] RegistrationCancelled COMPLETE: Email sent - Email={Email}, Duration={ElapsedMs}ms",
                        user.Email.Value, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "RegistrationCancelled CANCELED: Operation was canceled - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "RegistrationCancelled FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Phase 6A.83: Helper method to safely extract event location string
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;
        var state = @event.Location.Address.State;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);

        return string.Join(", ", parts);
    }
}
