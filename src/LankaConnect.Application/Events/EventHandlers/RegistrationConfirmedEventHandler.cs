using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles RegistrationConfirmedEvent to send confirmation email to attendee.
/// Phase 6A.24: Enhanced to include attendee details and skip paid events.
/// For paid events, email is sent by PaymentCompletedEventHandler after payment.
/// </summary>
public class RegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<RegistrationConfirmedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<RegistrationConfirmedEventHandler> _logger;

    public RegistrationConfirmedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ILogger<RegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RegistrationConfirmedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}",
            domainEvent.EventId, domainEvent.AttendeeId);

        try
        {
            // Retrieve user and event data
            var user = await _userRepository.GetByIdAsync(domainEvent.AttendeeId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for RegistrationConfirmedEvent", domainEvent.AttendeeId);
                return;
            }

            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for RegistrationConfirmedEvent", domainEvent.EventId);
                return;
            }

            // Phase 6A.24: Fetch registration to get attendee details and check if paid event
            var registration = await _registrationRepository.GetByEventAndUserAsync(
                domainEvent.EventId, domainEvent.AttendeeId, cancellationToken);

            if (registration == null)
            {
                _logger.LogWarning("Registration not found for Event {EventId}, User {UserId}",
                    domainEvent.EventId, domainEvent.AttendeeId);
                return;
            }

            // Phase 6A.24: Skip email for paid events - PaymentCompletedEventHandler will send it after payment
            if (registration.PaymentStatus == PaymentStatus.Pending)
            {
                _logger.LogInformation(
                    "Skipping email for paid event {EventId}, User {UserId} - waiting for payment completion",
                    domainEvent.EventId, domainEvent.AttendeeId);
                return;
            }

            // Phase 6A.24: Prepare attendee details for email
            var attendeeDetailsHtml = new System.Text.StringBuilder();
            var attendeeDetailsText = new System.Text.StringBuilder();

            if (registration.HasDetailedAttendees())
            {
                foreach (var attendee in registration.Attendees)
                {
                    // HTML format
                    attendeeDetailsHtml.AppendLine($"<p><strong>{attendee.Name}</strong> (Age: {attendee.Age})</p>");

                    // Plain text format
                    attendeeDetailsText.AppendLine($"- {attendee.Name} (Age: {attendee.Age})");
                }
            }
            else
            {
                // Fallback if no detailed attendees
                attendeeDetailsHtml.AppendLine($"<p>{domainEvent.Quantity} attendee(s)</p>");
                attendeeDetailsText.AppendLine($"{domainEvent.Quantity} attendee(s)");
            }

            // Phase 6A.34 Enhancement: Get event's primary image URL (if available)
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? "";

            // Prepare email parameters with enhanced attendee details
            var parameters = new Dictionary<string, object>
            {
                { "UserName", $"{user.FirstName} {user.LastName}" },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventEndDate", @event.EndDate.ToString("MMMM dd, yyyy") },
                { "EventLocation", @event.Location != null ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}" : "Online Event" },
                { "Quantity", domainEvent.Quantity },
                { "RegistrationDate", domainEvent.RegistrationDate.ToString("MMMM dd, yyyy h:mm tt") },
                // Phase 6A.34: Format attendees as HTML/text string for template rendering
                { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
                { "HasAttendeeDetails", registration.HasDetailedAttendees() },
                // Phase 6A.34 Enhancement: Event image for branded email
                { "EventImageUrl", eventImageUrl },
                { "HasEventImage", !string.IsNullOrEmpty(eventImageUrl) }
            };

            // Phase 6A.24: Add contact information if available
            if (registration.Contact != null)
            {
                parameters["ContactEmail"] = registration.Contact.Email;
                parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
                parameters["HasContactInfo"] = true;
            }
            else
            {
                parameters["HasContactInfo"] = false;
            }

            // Send templated email
            // Phase 6A.34 FIX: Use registration-confirmation template for free event registrations
            // (separate from ticket-confirmation which is for paid events with payment details)
            var result = await _emailService.SendTemplatedEmailAsync(
                "registration-confirmation",
                user.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send RSVP confirmation email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("RSVP confirmation email sent successfully to {Email} with {AttendeeCount} attendees",
                    user.Email.Value, domainEvent.Quantity);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex, "Error handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}",
                domainEvent.EventId, domainEvent.AttendeeId);
        }
    }
}
