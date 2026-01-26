using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.24: Handles AnonymousRegistrationConfirmedEvent to send confirmation email to anonymous attendees.
/// Phase 6A.80: Updated to reuse member FreeEventRegistration template for consistency and maintainability.
/// For paid events, email is sent by PaymentCompletedEventHandler after payment.
/// </summary>
public class AnonymousRegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<AnonymousRegistrationConfirmedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<AnonymousRegistrationConfirmedEventHandler> _logger;

    public AnonymousRegistrationConfirmedEventHandler(
        IEmailService emailService,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<AnonymousRegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
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
                var hasEventImage = !string.IsNullOrEmpty(eventImageUrl);

                // Phase 6A.80: Format date/time range properly (same as member handler)
                var eventDateTimeRange = FormatEventDateTimeRange(@event.StartDate, @event.EndDate);

                // Phase 6A.80: Prepare email parameters - ALIGNED WITH MEMBER HANDLER
                // Using FreeEventRegistration template parameters
                // Phase 6A.83 Part 3: Split EventDateTime into separate date and time fields per user request
                var parameters = new Dictionary<string, object>
                {
                    { "UserName", contactName },
                    { "EventTitle", @event.Title.Value },
                    { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },  // Phase 6A.83: Split date
                    { "EventStartTime", @event.StartDate.ToString("h:mm tt") },  // Phase 6A.83: Split time
                    { "EventLocation", GetEventLocationString(@event) },
                    { "RegistrationDate", domainEvent.RegistrationDate.ToString("MMMM dd, yyyy h:mm tt") },
                    { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
                    { "HasAttendeeDetails", hasAttendeeDetails },
                    // Phase 6A.80: Add event image support
                    { "EventImageUrl", eventImageUrl },
                    { "HasEventImage", hasEventImage },
                    // Phase 6A.83 Part 3: Add required URLs per user request
                    { "EventDetailsUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) },
                    { "SignUpListsUrl", @event.HasSignUpLists() ? $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups" : "" }
                };

                // Add contact information if available
                if (registration.Contact != null)
                {
                    parameters["ContactEmail"] = registration.Contact.Email;
                    parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
                    parameters["HasContactInfo"] = true;
                }
                else
                {
                    parameters["ContactEmail"] = "";
                    parameters["ContactPhone"] = "";
                    parameters["HasContactInfo"] = false;
                }

                // Phase 6A.80: Add organizer contact details (same as member handler)
                parameters["HasOrganizerContact"] = @event.HasOrganizerContact();
                parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "";
                parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail ?? "";
                parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone ?? "";

                // Phase 6A.80: Reuse FreeEventRegistration template (same as member handler)
                var result = await _emailService.SendTemplatedEmailAsync(
                    EmailTemplateNames.FreeEventRegistration,
                    domainEvent.AttendeeEmail,
                    parameters,
                    cancellationToken);

                stopwatch.Stop();

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "AnonymousRegistrationConfirmed FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeEmail, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "AnonymousRegistrationConfirmed COMPLETE: Email sent successfully - Email={Email}, AttendeeCount={AttendeeCount}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeEmail, domainEvent.Quantity, stopwatch.ElapsedMilliseconds);
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
