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
/// Phase 6A.24 FIX: Now also generates tickets if payment is complete but ticket was never created
/// (handles case where webhook initially failed)
/// </summary>
public class ResendTicketEmailCommandHandler : ICommandHandler<ResendTicketEmailCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ITicketService _ticketService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ResendTicketEmailCommandHandler> _logger;

    public ResendTicketEmailCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ITicketService ticketService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IUserRepository userRepository,
        ILogger<ResendTicketEmailCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
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
            _logger.LogInformation("Processing ticket email request for Registration {RegistrationId}, User {UserId}",
                request.RegistrationId, request.UserId);

            // 1. Get registration by ID (using base repository method)
            var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
            if (registration == null)
            {
                _logger.LogWarning("Registration {RegistrationId} not found", request.RegistrationId);
                return Result.Failure("Registration not found");
            }

            // 2. Get event for details
            var @event = await _eventRepository.GetByIdAsync(registration.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for Registration {RegistrationId}",
                    registration.EventId, request.RegistrationId);
                return Result.Failure("Event not found");
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

            // 5. Phase 6A.24 FIX: Get or generate ticket
            // If ticket doesn't exist (webhook initially failed), generate it now
            var ticket = await _ticketService.GetTicketByRegistrationIdAsync(request.RegistrationId, cancellationToken);
            TicketResult? ticketResult = null;

            if (ticket == null)
            {
                _logger.LogInformation("No ticket found for Registration {RegistrationId}, generating now...",
                    request.RegistrationId);

                var generateResult = await _ticketService.GenerateTicketAsync(
                    request.RegistrationId,
                    registration.EventId,
                    cancellationToken);

                if (generateResult.IsFailure)
                {
                    _logger.LogError("Failed to generate ticket for Registration {RegistrationId}: {Error}",
                        request.RegistrationId, string.Join(", ", generateResult.Errors));
                    return Result.Failure("Failed to generate ticket");
                }

                ticketResult = generateResult.Value;
                ticket = await _ticketService.GetTicketByIdAsync(ticketResult.TicketId, cancellationToken);
                _logger.LogInformation("Ticket generated successfully: {TicketCode}", ticketResult.TicketCode);
            }

            if (ticket == null)
            {
                _logger.LogError("Ticket still not found after generation attempt for Registration {RegistrationId}",
                    request.RegistrationId);
                return Result.Failure("Ticket not found");
            }

            // 6. Get ticket PDF
            var pdfResult = await _ticketService.GetTicketPdfAsync(ticket.Id, cancellationToken);
            if (pdfResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve ticket PDF for Ticket {TicketId}: {Error}",
                    ticket.Id, string.Join(", ", pdfResult.Errors));
                return Result.Failure("Failed to retrieve ticket PDF");
            }

            // 7. Get user details
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

            // 8. Phase 6A.24 FIX: Format attendee details as HTML string (not List<Dictionary>)
            var attendeeDetailsHtml = new System.Text.StringBuilder();
            if (registration.HasDetailedAttendees())
            {
                foreach (var attendee in registration.Attendees)
                {
                    attendeeDetailsHtml.AppendLine($"<p><strong>{attendee.Name}</strong> (Age: {attendee.Age})</p>");
                }
            }
            else
            {
                attendeeDetailsHtml.AppendLine($"<p>{registration.GetAttendeeCount()} attendee(s)</p>");
            }

            var parameters = new Dictionary<string, object>
            {
                { "UserName", recipientName },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventLocation", @event.Location != null ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}" : "Online Event" },
                { "AttendeeCount", registration.GetAttendeeCount() },
                { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
                { "HasAttendeeDetails", registration.HasDetailedAttendees() },
                { "IsPaidEvent", true },
                { "AmountPaid", registration.TotalPrice?.Amount.ToString("C") ?? "$0.00" },
                { "PaymentIntentId", registration.StripePaymentIntentId ?? "" },
                { "PaymentDate", DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },
                { "HasTicket", true },
                { "TicketCode", ticket.TicketCode },
                { "TicketExpiryDate", @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy") },
                { "ContactEmail", registration.Contact?.Email ?? "support@lankaconnect.com" }
            };

            // 9. Render email template
            // Phase 6A.24 FIX: Call RenderTemplateAsync with single template name "ticket-confirmation"
            // The service will automatically find: ticket-confirmation-subject.txt, ticket-confirmation-html.html, ticket-confirmation-text.txt
            var renderResult = await _emailTemplateService.RenderTemplateAsync(
                "ticket-confirmation",
                parameters,
                cancellationToken);

            if (renderResult.IsFailure)
            {
                _logger.LogError("Failed to render email template 'ticket-confirmation': {Error}", renderResult.Error);
                return Result.Failure("Failed to render email template");
            }

            // 10. Build email message with PDF attachment
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                ToName = recipientName,
                Subject = renderResult.Value.Subject,
                HtmlBody = renderResult.Value.HtmlBody,
                PlainTextBody = renderResult.Value.PlainTextBody,
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

            // 11. Send email
            var emailResult = await _emailService.SendEmailAsync(emailMessage, cancellationToken);
            if (emailResult.IsFailure)
            {
                _logger.LogError("Failed to send ticket email to {Email}: {Error}",
                    recipientEmail, string.Join(", ", emailResult.Errors));
                return Result.Failure("Failed to send ticket email");
            }

            _logger.LogInformation("Ticket email sent successfully to {Email} for Registration {RegistrationId} (Ticket: {TicketCode})",
                recipientEmail, request.RegistrationId, ticket.TicketCode);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ticket email for Registration {RegistrationId}",
                request.RegistrationId);
            return Result.Failure($"Error processing ticket email: {ex.Message}");
        }
    }
}
