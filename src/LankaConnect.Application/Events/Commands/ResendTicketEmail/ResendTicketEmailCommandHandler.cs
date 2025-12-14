using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.ResendTicketEmail;

/// <summary>
/// Phase 6A.24: Handler for resending ticket emails
/// Verifies ownership and payment status before resending
/// </summary>
public class ResendTicketEmailCommandHandler : ICommandHandler<ResendTicketEmailCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly ITicketService _ticketService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ResendTicketEmailCommandHandler> _logger;

    public ResendTicketEmailCommandHandler(
        IEventRepository eventRepository,
        ITicketService ticketService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IUserRepository userRepository,
        ILogger<ResendTicketEmailCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _ticketService = ticketService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(ResendTicketEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Resending ticket email for Registration {RegistrationId}, User {UserId}",
                request.RegistrationId, request.UserId);

            // 1. Get ticket by registration ID
            var ticket = await _ticketService.GetTicketByRegistrationIdAsync(request.RegistrationId, cancellationToken);
            if (ticket == null)
            {
                _logger.LogWarning("Ticket not found for Registration {RegistrationId}", request.RegistrationId);
                return Result.Failure("Ticket not found");
            }

            // 2. Get event and registration
            var @event = await _eventRepository.GetByIdAsync(ticket.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found", ticket.EventId);
                return Result.Failure("Event not found");
            }

            var registration = @event.Registrations.FirstOrDefault(r => r.Id == request.RegistrationId);
            if (registration == null)
            {
                _logger.LogWarning("Registration {RegistrationId} not found in Event {EventId}",
                    request.RegistrationId, ticket.EventId);
                return Result.Failure("Registration not found");
            }

            // 3. Authorization check: Only owner can resend
            if (registration.UserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to resend ticket for Registration {RegistrationId} owned by {OwnerId}",
                    request.UserId, request.RegistrationId, registration.UserId);
                return Result.Failure("Not authorized to resend this ticket");
            }

            // 4. Verify payment status
            if (registration.PaymentStatus != PaymentStatus.Completed)
            {
                _logger.LogWarning("Payment not completed for Registration {RegistrationId}, Status: {PaymentStatus}",
                    request.RegistrationId, registration.PaymentStatus);
                return Result.Failure("Payment not completed for this registration");
            }

            // 5. Get ticket PDF
            var pdfResult = await _ticketService.GetTicketPdfAsync(ticket.Id, cancellationToken);
            if (pdfResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve ticket PDF for Ticket {TicketId}: {Error}",
                    ticket.Id, string.Join(", ", pdfResult.Errors));
                return Result.Failure("Failed to retrieve ticket PDF");
            }

            // 6. Get user details
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            var recipientEmail = registration.Contact?.Email ?? user?.Email.Value ?? "";
            var recipientName = user != null
                ? $"{user.FirstName} {user.LastName}"
                : (registration.HasDetailedAttendees() && registration.Attendees.Any()
                    ? registration.Attendees.First().Name
                    : "Guest");

            if (string.IsNullOrEmpty(recipientEmail))
            {
                _logger.LogError("No email address found for Registration {RegistrationId}", request.RegistrationId);
                return Result.Failure("No email address found for this registration");
            }

            // 7. Prepare email parameters
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

            var parameters = new Dictionary<string, object>
            {
                { "UserName", recipientName },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventLocation", @event.Location != null ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}" : "Online Event" },
                { "AttendeeCount", registration.GetAttendeeCount() },
                { "Attendees", attendeeDetails },
                { "HasAttendeeDetails", attendeeDetails.Any() },
                { "IsPaidEvent", true },
                { "AmountPaid", registration.TotalPrice?.Amount.ToString("C") ?? "$0.00" },
                { "PaymentIntentId", registration.StripePaymentIntentId ?? "" },
                { "PaymentDate", DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },
                { "HasTicket", true },
                { "TicketCode", ticket.TicketCode },
                { "TicketExpiryDate", @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy") },
                { "ContactEmail", registration.Contact?.Email ?? "support@lankaconnect.com" }
            };

            // 8. Render email templates
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
                return Result.Failure("Failed to render email template");
            }

            // 9. Build email message with PDF attachment
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                ToName = recipientName,
                Subject = subjectResult.Value.Subject,
                HtmlBody = htmlResult.Value.HtmlBody,
                PlainTextBody = textResult.Value.PlainTextBody,
                Attachments = new List<EmailAttachment>
                {
                    new EmailAttachment
                    {
                        FileName = $"ticket-{ticket.TicketCode}.pdf",
                        Content = pdfResult.Value,
                        ContentType = "application/pdf"
                    }
                }
            };

            // 10. Send email
            var emailResult = await _emailService.SendEmailAsync(emailMessage, cancellationToken);
            if (emailResult.IsFailure)
            {
                _logger.LogError("Failed to send ticket email to {Email}: {Error}",
                    recipientEmail, string.Join(", ", emailResult.Errors));
                return Result.Failure("Failed to send ticket email");
            }

            _logger.LogInformation("Ticket email resent successfully to {Email} for Registration {RegistrationId}",
                recipientEmail, request.RegistrationId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending ticket email for Registration {RegistrationId}",
                request.RegistrationId);
            return Result.Failure($"Error resending ticket email: {ex.Message}");
        }
    }
}
