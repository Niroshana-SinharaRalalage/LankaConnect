using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
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
/// Handles RegistrationConfirmedEvent to send confirmation email to attendee.
/// Phase 6A.24: Enhanced to include attendee details and skip paid events.
/// Phase 6A.38: Simplified to use direct Azure Blob Storage URLs for images (removed CID complexity).
/// Phase 6A.100: Unified to use ITypedEmailService only.
/// For paid events, email is sent by PaymentCompletedEventHandler after payment.
/// </summary>
public class RegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<RegistrationConfirmedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RegistrationConfirmedEventHandler> _logger;

    public RegistrationConfirmedEventHandler(
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEventFormRepository eventFormRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RegistrationConfirmedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _eventFormRepository = eventFormRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RegistrationConfirmedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RegistrationConfirmed"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("AttendeeId", domainEvent.AttendeeId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RegistrationConfirmed START: Event={EventId}, User={UserId}, Quantity={Quantity}",
                domainEvent.EventId, domainEvent.AttendeeId, domainEvent.Quantity);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Retrieve user and event data
                var user = await _userRepository.GetByIdAsync(domainEvent.AttendeeId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RegistrationConfirmed: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RegistrationConfirmed: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Phase 6A.24: Fetch registration to get attendee details and check if paid event
                var registration = await _registrationRepository.GetByEventAndUserAsync(
                    domainEvent.EventId, domainEvent.AttendeeId, cancellationToken);

                if (registration == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RegistrationConfirmed: Registration not found - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Phase 6A.24: Skip email for paid events - PaymentCompletedEventHandler will send it after payment
                if (registration.PaymentStatus == PaymentStatus.Pending)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "RegistrationConfirmed: Skipping paid event - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
                    return;
                }

            // Phase 6A.40: Prepare attendee details for email - show actual attendee names
            var attendeeDetailsHtml = new System.Text.StringBuilder();
            var hasAttendeeDetailsLocal = registration.HasDetailedAttendees() && registration.Attendees.Any();

            if (hasAttendeeDetailsLocal)
            {
                // Phase 8: Include tier name when present
                // Slice 7 S7.7: Append seat label for assigned-seating events
                foreach (var attendee in registration.Attendees)
                {
                    var tierSuffix = !string.IsNullOrWhiteSpace(attendee.TicketTierName)
                        ? $" <span style=\"color: #8B1538; font-weight: 600;\">({attendee.TicketTierName})</span>"
                        : "";
                    var seatSuffix = !string.IsNullOrWhiteSpace(attendee.SeatLabel)
                        ? $" <span style=\"color: #2563EB; font-weight: 600;\">(Seat {attendee.SeatLabel})</span>"
                        : "";
                    attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}{tierSuffix}{seatSuffix}</p>");
                }
            }

            // Phase 6A.38: Get event's primary image URL (direct URL, no CID)
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? "";

            // Phase 7C.2: Project event's primary + optional secondary location into the
            // 8 decomposed email fields. LegacyFlatString keeps un-migrated templates stable.
            var locationProjection = @event.ProjectEmailLocation();

            // Phase 6A.87: Use typed FreeEventRegistrationEmailParams
            var typedParams = FreeEventRegistrationEmailParams.Create(
                eventId: @event.Id,
                registrationId: registration.Id,
                userName: $"{user.FirstName} {user.LastName}",
                userEmail: user.Email.Value,
                eventTitle: @event.Title.Value,
                eventStartDate: @event.StartDate,
                eventStartTime: EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId),  // Phase 6A.97: Uses event's timezone
                eventLocation: locationProjection.LegacyFlatString,
                eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id),
                registrationDate: domainEvent.RegistrationDate);

            // Phase 7C.2: Populate decomposed LocationName / LocationAddress / secondary block fields.
            typedParams.WithLocationDetails(locationProjection);

            // Phase 6A.97: Set event's timezone for consistent date/time display
            typedParams.TimeZoneId = @event.TimeZoneId;

            // Phase 6A.128: Fix - use WithSignUpLists() which sets BOTH URL and HasSignUpLists flag
            // Previously used WithSignUpListsUrl() which only set URL, leaving HasSignUpLists=false
            if (@event.HasSignUpLists())
            {
                typedParams.WithSignUpLists($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups");
            }

            // Phase 6A.112: Check if event has active signup forms
            var forms = await _eventFormRepository.GetByEventIdAsync(@event.Id, cancellationToken);
            var hasActiveForms = forms.Any(f => f.Status == EventFormStatus.Active);

            if (hasActiveForms)
            {
                typedParams.WithSignupForms($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms");
            }

            // Set attendee details
            if (hasAttendeeDetailsLocal)
            {
                typedParams.WithAttendees(attendeeDetailsHtml.ToString().TrimEnd());
            }

            // Phase 7E.4: Populate the mode-aware FlexibleRegistration params from the
            // registration's snapshotted RegistrationMode + HeadCount. The shared formatter
            // produces identical output across every email surface so users see the same
            // wording in confirmation, cancellation, reminder, etc.
            var flex = HeadCountEmailFormatter.Compute(registration);
            typedParams.HasDetailedAttendees = flex.hasDetailedAttendees;
            typedParams.HasHeadCount = flex.hasHeadCount;
            typedParams.HasHeadCountBreakdown = flex.hasHeadCountBreakdown;
            typedParams.HasTierBreakdown = flex.hasTierBreakdown;
            typedParams.HeadCountTotal = flex.headCountTotal;
            typedParams.HeadCountBreakdownLine = flex.headCountBreakdownLine;
            typedParams.TierBreakdownLine = flex.tierBreakdownLine;
            typedParams.LeadAttendeeName = flex.leadAttendeeName;

            // Set event image
            typedParams.WithEventImage(eventImageUrl);

            // Set contact information
            if (registration.Contact != null)
            {
                typedParams.WithContactInfo(registration.Contact.Email, registration.Contact.PhoneNumber);
            }

            // Set organizer contact
            if (@event.HasOrganizerContact())
            {
                typedParams.WithOrganizerContacts(
                    @event.OrganizerContacts
                        .OrderBy(c => c.SortOrder)
                        .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                        .ToList());
            }

            _logger.LogInformation(
                "[Phase 6A.87] Sending free event registration email to {Email}",
                user.Email.Value);

            // Phase 6A.100: Send via typed email service
            var typedResult = await _typedEmailService.SendEmailAsync(
                typedParams,
                cancellationToken);

            var result = typedResult.Success
                ? Domain.Common.Result.Success()
                : Domain.Common.Result.Failure(string.Join(", ", typedResult.Errors));

            stopwatch.Stop();

            if (typedResult.Success)
            {
                _logger.LogInformation(
                    "[Phase 6A.100] RegistrationConfirmed COMPLETE: Email sent to {Email}, AttendeeCount={AttendeeCount}, Duration={ElapsedMs}ms",
                    user.Email.Value, domainEvent.Quantity, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(
                    "[Phase 6A.87] RegistrationConfirmed FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                    user.Email.Value, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
            }
        }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "RegistrationConfirmed CANCELED: Operation was canceled - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "RegistrationConfirmed FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Phase 6A.40: Formats event date/time range for display.
    /// Examples:
    /// - Same day: "December 24, 2025 from 5:00 PM to 10:00 PM"
    /// - Different days: "December 24, 2025 at 5:00 PM to December 25, 2025 at 10:00 PM"
    /// </summary>
    private static string FormatEventDateTimeRange(DateTime startDate, DateTime endDate)
    {
        if (startDate.Date == endDate.Date)
        {
            // Same day event
            return $"{startDate:MMMM dd, yyyy} from {startDate:h:mm tt} to {endDate:h:mm tt}";
        }
        else
        {
            // Multi-day event
            return $"{startDate:MMMM dd, yyyy} at {startDate:h:mm tt} to {endDate:MMMM dd, yyyy} at {endDate:h:mm tt}";
        }
    }
}
