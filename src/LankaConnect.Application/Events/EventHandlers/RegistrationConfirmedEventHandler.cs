using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles RegistrationConfirmedEvent to send confirmation email to attendee.
/// Phase 6A.24: Enhanced to include attendee details and skip paid events.
/// Phase 6A.38: Simplified to use direct Azure Blob Storage URLs for images (removed CID complexity).
/// For paid events, email is sent by PaymentCompletedEventHandler after payment.
/// </summary>
public class RegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<RegistrationConfirmedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<RegistrationConfirmedEventHandler> _logger;

    public RegistrationConfirmedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<RegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
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
            var hasAttendeeDetails = registration.HasDetailedAttendees() && registration.Attendees.Any();

            if (hasAttendeeDetails)
            {
                foreach (var attendee in registration.Attendees)
                {
                    attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
                }
            }

            // Phase 6A.38: Get event's primary image URL (direct URL, no CID)
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? "";
            var hasEventImage = !string.IsNullOrEmpty(eventImageUrl);

            // Phase 6A.40: Format date/time range properly
            var eventDateTimeRange = FormatEventDateTimeRange(@event.StartDate, @event.EndDate);

            // Prepare email parameters
            // Phase 6A.83 Part 3: Split EventDateTime into separate date and time fields per user request
            var parameters = new Dictionary<string, object>
            {
                { "UserName", $"{user.FirstName} {user.LastName}" },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },  // Phase 6A.83: Split date
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },  // Phase 6A.83: Split time
                { "EventLocation", GetEventLocationString(@event) },
                { "RegistrationDate", domainEvent.RegistrationDate.ToString("MMMM dd, yyyy h:mm tt") },
                { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
                { "HasAttendeeDetails", hasAttendeeDetails },
                // Phase 6A.38: Pass event image URL for direct embedding
                { "EventImageUrl", eventImageUrl },
                { "HasEventImage", hasEventImage },
                // Phase 6A.83 Part 3: Add required URLs per user request
                { "EventDetailsUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) },
                { "SignUpListsUrl", @event.HasSignUpLists() ? $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups" : "" }
            };

            // Phase 6A.40: Add registrant's contact information (what they provided during registration)
            if (registration.Contact != null)
            {
                parameters["ContactEmail"] = registration.Contact.Email;
                parameters["ContactPhone"] = registration.Contact.PhoneNumber;
                parameters["HasContactInfo"] = true;
            }
            else
            {
                parameters["ContactEmail"] = "";
                parameters["ContactPhone"] = "";
                parameters["HasContactInfo"] = false;
            }

            // Phase 6A.X: Organizer Contact Details - for event inquiries
            parameters["HasOrganizerContact"] = @event.HasOrganizerContact();
            parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "";
            parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail ?? "";
            parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone ?? "";

            // Phase 6A.38: Send templated email (no attachments - using direct URLs in template)
            // Phase 6A.79: Use EmailTemplateNames constant
            var result = await _emailService.SendTemplatedEmailAsync(
                EmailTemplateNames.FreeEventRegistration,
                user.Email.Value,
                parameters,
                cancellationToken);

                stopwatch.Stop();

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "RegistrationConfirmed FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        user.Email.Value, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "RegistrationConfirmed COMPLETE: Email sent successfully - Email={Email}, AttendeeCount={AttendeeCount}, Duration={ElapsedMs}ms",
                        user.Email.Value, domainEvent.Quantity, stopwatch.ElapsedMilliseconds);
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
    /// Safely extracts event location string with defensive null handling.
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
