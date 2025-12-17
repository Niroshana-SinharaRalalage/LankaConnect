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
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ITicketService _ticketService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<PaymentCompletedEventHandler> _logger;

    public PaymentCompletedEventHandler(
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ITicketService ticketService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ILogger<PaymentCompletedEventHandler> logger)
    {
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _ticketService = ticketService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "[Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED - Event {EventId}, Registration {RegistrationId}, Amount {Amount}, Email {Email}",
            domainEvent.EventId, domainEvent.RegistrationId, domainEvent.AmountPaid, domainEvent.ContactEmail);

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

            // Phase 6A.24: Generate ticket with QR code
            var ticketResult = await _ticketService.GenerateTicketAsync(
                registration.Id,
                @event.Id,
                cancellationToken);

            byte[]? pdfAttachment = null;
            if (ticketResult.IsSuccess)
            {
                _logger.LogInformation("Ticket generated successfully: {TicketCode}", ticketResult.Value.TicketCode);

                parameters["HasTicket"] = true;
                parameters["TicketCode"] = ticketResult.Value.TicketCode;
                parameters["TicketExpiryDate"] = @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy");

                // Get PDF bytes for email attachment
                var pdfResult = await _ticketService.GetTicketPdfAsync(ticketResult.Value.TicketId, cancellationToken);
                if (pdfResult.IsSuccess)
                {
                    pdfAttachment = pdfResult.Value;
                    _logger.LogInformation("Ticket PDF retrieved successfully, size: {Size} bytes", pdfAttachment.Length);
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve ticket PDF: {Error}", string.Join(", ", pdfResult.Errors));
                }
            }
            else
            {
                _logger.LogWarning("Failed to generate ticket: {Error}", string.Join(", ", ticketResult.Errors));
                parameters["HasTicket"] = false;
            }

            // Render email template
            var subjectResult = await _emailTemplateService.RenderTemplateAsync(
                "ticket-confirmation-subject",
                parameters,
                cancellationToken);
            var htmlResult = await _emailTemplateService.RenderTemplateAsync(
                "ticket-confirmation-html",
                parameters,
                cancellationToken);
            var textResult = await _emailTemplateService.RenderTemplateAsync(
                "ticket-confirmation-text",
                parameters,
                cancellationToken);

            if (subjectResult.IsFailure || htmlResult.IsFailure || textResult.IsFailure)
            {
                _logger.LogError("Failed to render email template");
                return;
            }

            // Build email message with attachment
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                ToName = recipientName,
                Subject = subjectResult.Value.Subject,
                HtmlBody = htmlResult.Value.HtmlBody,
                PlainTextBody = textResult.Value.PlainTextBody,
                Attachments = pdfAttachment != null
                    ? new List<EmailAttachment>
                    {
                        new EmailAttachment
                        {
                            FileName = $"ticket-{ticketResult.Value?.TicketCode ?? "event"}.pdf",
                            Content = pdfAttachment,
                            ContentType = "application/pdf"
                        }
                    }
                    : null
            };

            // Send email with attachment
            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "Failed to send payment confirmation email to {Email}: {Errors}",
                    recipientEmail, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "Payment confirmation email sent successfully to {Email} for Registration {RegistrationId} with {AttendeeCount} attendees, HasTicket: {HasTicket}",
                    recipientEmail, domainEvent.RegistrationId, attendeeDetails.Count, parameters["HasTicket"]);
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
