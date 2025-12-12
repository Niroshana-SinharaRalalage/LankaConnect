using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.24: Handles PaymentCompletedEvent to send confirmation email and generate tickets for paid events.
/// This handler is triggered after successful payment for paid event registrations.
/// </summary>
public class PaymentCompletedEventHandler : INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<PaymentCompletedEventHandler> _logger;

    public PaymentCompletedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ILogger<PaymentCompletedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "Handling PaymentCompletedEvent for Event {EventId}, Registration {RegistrationId}, Amount {Amount}",
            domainEvent.EventId, domainEvent.RegistrationId, domainEvent.AmountPaid);

        try
        {
            // Retrieve event data
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for PaymentCompletedEvent", domainEvent.EventId);
                return;
            }

            // Find the registration within the event
            var registration = @event.Registrations.FirstOrDefault(r => r.Id == domainEvent.RegistrationId);
            if (registration == null)
            {
                _logger.LogWarning("Registration {RegistrationId} not found in Event {EventId}",
                    domainEvent.RegistrationId, domainEvent.EventId);
                return;
            }

            // Determine recipient name and email
            string recipientName;
            string recipientEmail = domainEvent.ContactEmail;

            if (domainEvent.UserId.HasValue)
            {
                // Authenticated user - get user details
                var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
                if (user != null)
                {
                    recipientName = $"{user.FirstName} {user.LastName}";
                    recipientEmail = user.Email.Value;
                }
                else
                {
                    recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                        ? registration.Attendees.First().Name
                        : "Guest";
                }
            }
            else
            {
                // Anonymous user - use first attendee name or fallback
                recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                    ? registration.Attendees.First().Name
                    : "Guest";
            }

            // Prepare attendee details for email
            var attendeeDetails = new List<Dictionary<string, object>>();
            if (registration.HasDetailedAttendees())
            {
                foreach (var attendee in registration.Attendees)
                {
                    attendeeDetails.Add(new Dictionary<string, object>
                    {
                        { "Name", attendee.Name },
                        { "Age", attendee.Age }
                    });
                }
            }

            // Prepare email parameters with payment details
            var parameters = new Dictionary<string, object>
            {
                { "UserName", recipientName },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventEndDate", @event.EndDate.ToString("MMMM dd, yyyy") },
                { "EventLocation", @event.Location != null ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}" : "Online Event" },
                { "Quantity", domainEvent.AttendeeCount },
                { "RegistrationDate", domainEvent.PaymentCompletedAt.ToString("MMMM dd, yyyy h:mm tt") },
                // Attendee details
                { "Attendees", attendeeDetails },
                { "HasAttendeeDetails", attendeeDetails.Any() },
                // Payment details
                { "IsPaidEvent", true },
                { "AmountPaid", domainEvent.AmountPaid.ToString("C") },
                { "PaymentIntentId", domainEvent.PaymentIntentId },
                { "PaymentDate", domainEvent.PaymentCompletedAt.ToString("MMMM dd, yyyy h:mm tt") }
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
                parameters["HasContactInfo"] = false;
            }

            // TODO: Phase 6A.24 - Generate ticket with QR code here (Phase 5)
            // var ticketResult = await _ticketService.GenerateTicketAsync(registration.Id, @event.Id, cancellationToken);
            // if (ticketResult.IsSuccess)
            // {
            //     parameters["TicketCode"] = ticketResult.Value.TicketCode;
            //     parameters["TicketPdfUrl"] = ticketResult.Value.PdfBlobUrl;
            //     parameters["HasTicket"] = true;
            // }

            parameters["HasTicket"] = false; // Placeholder until ticket generation is implemented

            // Send templated email
            var result = await _emailService.SendTemplatedEmailAsync(
                "RsvpConfirmation",
                recipientEmail,
                parameters,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "Failed to send payment confirmation email to {Email}: {Errors}",
                    recipientEmail, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "Payment confirmation email sent successfully to {Email} for Registration {RegistrationId} with {AttendeeCount} attendees",
                    recipientEmail, domainEvent.RegistrationId, attendeeDetails.Count);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex,
                "Error handling PaymentCompletedEvent for Event {EventId}, Registration {RegistrationId}",
                domainEvent.EventId, domainEvent.RegistrationId);
        }
    }
}
