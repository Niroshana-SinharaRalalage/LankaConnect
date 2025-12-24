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

            // Phase 6A.43: Format attendee details - names only (no age) to match free event template
            var attendeeDetailsHtml = new System.Text.StringBuilder();

            if (registration.HasDetailedAttendees() && registration.Attendees.Any())
            {
                foreach (var attendee in registration.Attendees)
                {
                    // HTML format - names only, matching free event template style
                    attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
                }
            }

            // Phase 6A.43: Prepare email parameters aligned with free event template
            var hasAttendeeDetails = registration.HasDetailedAttendees() && registration.Attendees.Any();

            // Get event's primary image URL (direct URL, no CID)
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? "";
            var hasEventImage = !string.IsNullOrEmpty(eventImageUrl);

            var parameters = new Dictionary<string, object>
            {
                { "UserName", recipientName },
                { "EventTitle", @event.Title.Value },
                // Phase 6A.43: Use date range format matching free event template
                { "EventDateTime", FormatEventDateTimeRange(@event.StartDate, @event.EndDate) },
                { "EventLocation", GetEventLocationString(@event) },
                { "RegistrationDate", domainEvent.PaymentCompletedAt.ToString("MMMM dd, yyyy h:mm tt") },
                // Attendee details - names only (no age)
                { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
                { "HasAttendeeDetails", hasAttendeeDetails },
                // Event image
                { "EventImageUrl", eventImageUrl },
                { "HasEventImage", hasEventImage },
                // Payment details
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
                parameters["ContactEmail"] = "";
                parameters["ContactPhone"] = "";
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

            // Phase 6A.24 FIX: Call RenderTemplateAsync with single template name "ticket-confirmation"
            // The service will automatically find: ticket-confirmation-subject.txt, ticket-confirmation-html.html, ticket-confirmation-text.txt
            // Previous code incorrectly passed "ticket-confirmation-subject" which looked for "ticket-confirmation-subject-subject.txt"
            var renderResult = await _emailTemplateService.RenderTemplateAsync(
                "ticket-confirmation",
                parameters,
                cancellationToken);

            if (renderResult.IsFailure)
            {
                _logger.LogError("Failed to render email template 'ticket-confirmation': {Error}", renderResult.Error);
                return;
            }

            // Build email message with attachment
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                ToName = recipientName,
                Subject = renderResult.Value.Subject,
                HtmlBody = renderResult.Value.HtmlBody,
                PlainTextBody = renderResult.Value.PlainTextBody,
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
                    recipientEmail, domainEvent.RegistrationId, registration.Attendees.Count, parameters["HasTicket"]);
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

    /// <summary>
    /// Phase 6A.35: Safely extracts event location string with defensive null handling.
    /// Handles data inconsistency where has_location=true but address fields are null.
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        // Check if Location or Address is null (defensive against data inconsistency)
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;

        // Handle case where address fields exist but are empty
        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        if (string.IsNullOrWhiteSpace(street))
            return city!;

        if (string.IsNullOrWhiteSpace(city))
            return street;

        return $"{street}, {city}";
    }

    /// <summary>
    /// Phase 6A.43: Formats event date/time range for display.
    /// Matches the format used in RegistrationConfirmedEventHandler.
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
