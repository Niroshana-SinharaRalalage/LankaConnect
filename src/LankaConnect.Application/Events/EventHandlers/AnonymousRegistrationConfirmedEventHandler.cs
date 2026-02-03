using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.24: Handles AnonymousRegistrationConfirmedEvent to send confirmation email to anonymous attendees.
/// Phase 6A.80: Updated to reuse member FreeEventRegistration template for consistency and maintainability.
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// For paid events, email is sent by PaymentCompletedEventHandler after payment.
/// </summary>
public class AnonymousRegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<AnonymousRegistrationConfirmedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ITypedEmailService _typedEmailService;  // Phase 6A.87: Typed email service
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<AnonymousRegistrationConfirmedEventHandler> _logger;

    private const string HandlerName = nameof(AnonymousRegistrationConfirmedEventHandler);  // Phase 6A.87: For feature flag lookup

    public AnonymousRegistrationConfirmedEventHandler(
        IEmailService emailService,
        ITypedEmailService typedEmailService,  // Phase 6A.87: Typed email service
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<AnonymousRegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
        _typedEmailService = typedEmailService;  // Phase 6A.87: Typed email service
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<AnonymousRegistrationConfirmedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "AnonymousRegistrationConfirmed"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("AttendeeEmail", domainEvent.AttendeeEmail))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AnonymousRegistrationConfirmed START: Event={EventId}, Email={Email}, Quantity={Quantity}",
                domainEvent.EventId, domainEvent.AttendeeEmail, domainEvent.Quantity);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retrieve event data
                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "AnonymousRegistrationConfirmed: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Fetch registration to get attendee details and check if paid event
                var registration = await _registrationRepository.GetAnonymousByEventAndEmailAsync(
                    domainEvent.EventId, domainEvent.AttendeeEmail, cancellationToken);

                if (registration == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "AnonymousRegistrationConfirmed: Registration not found - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Skip email for paid events - PaymentCompletedEventHandler will send it after payment
                if (registration.PaymentStatus == PaymentStatus.Pending)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "AnonymousRegistrationConfirmed: Skipping paid event - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Phase 6A.80: Get contact name from first attendee or fallback to "Guest"
                var contactName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                    ? registration.Attendees.First().Name
                    : "Guest";

                // Phase 6A.80: Prepare attendee details HTML (same format as member handler)
                var attendeeDetailsHtml = new System.Text.StringBuilder();
                var hasAttendeeDetails = registration.HasDetailedAttendees() && registration.Attendees.Any();

                if (hasAttendeeDetails)
                {
                    foreach (var attendee in registration.Attendees)
                    {
                        attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
                    }
                }

                // Phase 6A.80: Get event's primary image URL (same as member handler)
                var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
                var eventImageUrl = primaryImage?.ImageUrl ?? "";

                // Phase 6A.87: Use typed FreeEventRegistrationEmailParams
                var emailParams = FreeEventRegistrationEmailParams.Create(
                    eventId: @event.Id,
                    registrationId: registration.Id,
                    userName: contactName,
                    userEmail: domainEvent.AttendeeEmail,
                    eventTitle: @event.Title.Value,
                    eventStartDate: @event.StartDate,
                    eventStartTime: EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId),
                    eventLocation: GetEventLocationString(@event),
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id),
                    registrationDate: domainEvent.RegistrationDate);

                // Phase 6A.97: Set event's timezone for consistent date/time display
                emailParams.TimeZoneId = @event.TimeZoneId;

                // Set signup lists URL if event has signup lists
                if (@event.HasSignUpLists())
                {
                    emailParams.WithSignUpListsUrl($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups");
                }

                // Set attendee details
                if (hasAttendeeDetails)
                {
                    emailParams.WithAttendees(attendeeDetailsHtml.ToString().TrimEnd());
                }

                // Set event image
                emailParams.WithEventImage(eventImageUrl);

                // Set contact information
                if (registration.Contact != null)
                {
                    emailParams.WithContactInfo(registration.Contact.Email, registration.Contact.PhoneNumber);
                }

                // Set organizer contact
                if (@event.HasOrganizerContact())
                {
                    emailParams.WithOrganizerContact(
                        @event.OrganizerContactName,
                        @event.OrganizerContactEmail,
                        @event.OrganizerContactPhone);
                }

                _logger.LogInformation(
                    "[Phase 6A.87] Sending anonymous registration email to {Email}",
                    domainEvent.AttendeeEmail);

                // Phase 6A.87: Send via typed email service (feature flags handled internally)
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    HandlerName,
                    cancellationToken);

                stopwatch.Stop();

                if (typedResult.Success)
                {
                    _logger.LogInformation(
                        "[Phase 6A.87] AnonymousRegistrationConfirmed COMPLETE: Email sent successfully to {Email}, UsedTyped={UsedTyped}, AttendeeCount={AttendeeCount}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeEmail, typedResult.UsedTypedParameters, domainEvent.Quantity, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "[Phase 6A.87] AnonymousRegistrationConfirmed FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeEmail, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AnonymousRegistrationConfirmed CANCELED: Operation was canceled - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "AnonymousRegistrationConfirmed FAILED: Exception occurred - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Phase 6A.80: Safely extracts event location string with defensive null handling.
    /// Copied from RegistrationConfirmedEventHandler for consistency.
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        if (string.IsNullOrWhiteSpace(street))
            return city!;

        if (string.IsNullOrWhiteSpace(city))
            return street;

        return $"{street}, {city}";
    }

    /// <summary>
    /// Phase 6A.80: Formats event date/time range for display.
    /// Copied from RegistrationConfirmedEventHandler for consistency.
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
