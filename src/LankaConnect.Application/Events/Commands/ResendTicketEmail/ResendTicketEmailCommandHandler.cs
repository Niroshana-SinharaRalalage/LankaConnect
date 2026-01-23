using System.Diagnostics;
using System.Globalization;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "ResendTicketEmail"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ResendTicketEmail START: RegistrationId={RegistrationId}, UserId={UserId}",
                request.RegistrationId, request.UserId);

            try
            {
                // Check for cancellation at the start
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Get registration by ID (using base repository method)
                var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
                if (registration == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ResendTicketEmail FAILED: Registration not found - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Registration not found");
                }

                _logger.LogInformation(
                    "ResendTicketEmail: Registration loaded - RegistrationId={RegistrationId}, EventId={EventId}, PaymentStatus={PaymentStatus}",
                    registration.Id, registration.EventId, registration.PaymentStatus);

                // 2. Get event for details
                var @event = await _eventRepository.GetByIdAsync(registration.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ResendTicketEmail FAILED: Event not found - EventId={EventId}, RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        registration.EventId, request.RegistrationId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "ResendTicketEmail: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // 3. Authorization check: Only owner can resend
                if (registration.UserId != request.UserId)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ResendTicketEmail FAILED: Authorization failed - UserId={UserId}, RegistrationId={RegistrationId}, OwnerId={OwnerId}, Duration={ElapsedMs}ms",
                        request.UserId, request.RegistrationId, registration.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Not authorized to resend this ticket");
                }

                _logger.LogInformation(
                    "ResendTicketEmail: Authorization check passed - UserId={UserId}, RegistrationId={RegistrationId}",
                    request.UserId, request.RegistrationId);

                // 4. Verify payment status
                if (registration.PaymentStatus != PaymentStatus.Completed)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ResendTicketEmail FAILED: Payment not completed - RegistrationId={RegistrationId}, PaymentStatus={PaymentStatus}, Duration={ElapsedMs}ms",
                        request.RegistrationId, registration.PaymentStatus, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Payment not completed for this registration");
                }

                _logger.LogInformation(
                    "ResendTicketEmail: Payment status verified - RegistrationId={RegistrationId}, PaymentStatus={PaymentStatus}",
                    request.RegistrationId, registration.PaymentStatus);

                // 5. Phase 6A.24 FIX: Get or generate ticket
                // If ticket doesn't exist (webhook initially failed), generate it now
                var ticket = await _ticketService.GetTicketByRegistrationIdAsync(request.RegistrationId, cancellationToken);
                TicketResult? ticketResult = null;

                if (ticket == null)
                {
                    _logger.LogInformation(
                        "ResendTicketEmail: No ticket found, generating now - RegistrationId={RegistrationId}",
                        request.RegistrationId);

                    var generateResult = await _ticketService.GenerateTicketAsync(
                        request.RegistrationId,
                        registration.EventId,
                        cancellationToken);

                    if (generateResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            "ResendTicketEmail FAILED: Ticket generation failed - RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.RegistrationId, string.Join(", ", generateResult.Errors), stopwatch.ElapsedMilliseconds);

                        return Result.Failure("Failed to generate ticket");
                    }

                    ticketResult = generateResult.Value;
                    ticket = await _ticketService.GetTicketByIdAsync(ticketResult.TicketId, cancellationToken);

                    _logger.LogInformation(
                        "ResendTicketEmail: Ticket generated successfully - TicketCode={TicketCode}, TicketId={TicketId}",
                        ticketResult.TicketCode, ticketResult.TicketId);
                }
                else
                {
                    _logger.LogInformation(
                        "ResendTicketEmail: Existing ticket found - TicketCode={TicketCode}, TicketId={TicketId}",
                        ticket.TicketCode, ticket.Id);
                }

                if (ticket == null)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "ResendTicketEmail FAILED: Ticket not found after generation - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Ticket not found");
                }

                // 6. Get ticket PDF
                var pdfResult = await _ticketService.GetTicketPdfAsync(ticket.Id, cancellationToken);
                if (pdfResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "ResendTicketEmail FAILED: PDF retrieval failed - TicketId={TicketId}, Error={Error}, Duration={ElapsedMs}ms",
                        ticket.Id, string.Join(", ", pdfResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Failed to retrieve ticket PDF");
                }

                _logger.LogInformation(
                    "ResendTicketEmail: Ticket PDF retrieved - TicketId={TicketId}, Size={Size} bytes",
                    ticket.Id, pdfResult.Value.Length);

                // 7. Get user details
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                var recipientEmail = registration.Contact?.Email ?? user?.Email.Value ?? "";
                var recipientName = user != null
                    ? $"{user.FirstName} {user.LastName}"
                    : (registration.HasDetailedAttendees() && registration.Attendees.Any()
                        ? registration.Attendees.First().Name
                        : "Guest");

                _logger.LogInformation(
                    "ResendTicketEmail: Recipient details determined - Email={Email}, Name={Name}, RegistrationId={RegistrationId}",
                    recipientEmail, recipientName, request.RegistrationId);

                if (string.IsNullOrEmpty(recipientEmail))
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "ResendTicketEmail FAILED: No email address found - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("No email address found for this registration");
                }

                // 8. Phase 6A.44 FIX: Format attendee details - names only (no age) to match PaymentCompletedEventHandler
                var attendeeDetailsHtml = new System.Text.StringBuilder();
                if (registration.HasDetailedAttendees())
                {
                    foreach (var attendee in registration.Attendees)
                    {
                        // Phase 6A.44: Format attendee details - names only (no age displayed)
                        attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
                    }
                }
                else
                {
                    attendeeDetailsHtml.AppendLine($"<p>{registration.GetAttendeeCount()} attendee(s)</p>");
                }

                // Phase 6A.43 FIX: Align parameters with PaymentCompletedEventHandler
                // Get event's primary image URL (direct URL, no CID)
                var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
                var eventImageUrl = primaryImage?.ImageUrl ?? "";
                var hasEventImage = !string.IsNullOrEmpty(eventImageUrl);

                var parameters = new Dictionary<string, object>
                {
                    { "UserName", recipientName },
                    { "EventTitle", @event.Title.Value },
                    // Phase 6A.43: Use date range format matching PaymentCompletedEventHandler
                    { "EventDateTime", FormatEventDateTimeRange(@event.StartDate, @event.EndDate) },
                    { "EventLocation", GetEventLocationString(@event) },
                    { "RegistrationDate", registration.CreatedAt.ToString("MMMM dd, yyyy h:mm tt") },
                    // Attendee details - names only (no age)
                    { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
                    { "HasAttendeeDetails", registration.HasDetailedAttendees() },
                    // Event image
                    { "EventImageUrl", eventImageUrl },
                    { "HasEventImage", hasEventImage },
                    // Payment details
                    // Phase 6A.56: Explicitly use en-US culture to ensure $ symbol instead of generic ¤
                    { "AmountPaid", registration.TotalPrice?.Amount.ToString("C", CultureInfo.GetCultureInfo("en-US")) ?? "$0.00" },
                    { "PaymentIntentId", registration.StripePaymentIntentId ?? "" },
                    { "PaymentDate", registration.UpdatedAt?.ToString("MMMM dd, yyyy h:mm tt") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },
                    // Ticket details
                    { "HasTicket", true },
                    { "TicketCode", ticket.TicketCode },
                    { "TicketExpiryDate", @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy") },
                    // Contact information
                    { "ContactEmail", registration.Contact?.Email ?? "support@lankaconnect.com" },
                    { "ContactPhone", registration.Contact?.PhoneNumber ?? "" },
                    { "HasContactInfo", registration.Contact != null }
                };

                // 9. Render email template
                // Phase 6A.79: Use EmailTemplateNames constant instead of hardcoded string
                _logger.LogInformation(
                    "ResendTicketEmail: Rendering email template - Template={TemplateName}, RegistrationId={RegistrationId}",
                    EmailTemplateNames.PaidEventRegistration, request.RegistrationId);

                var renderResult = await _emailTemplateService.RenderTemplateAsync(
                    EmailTemplateNames.PaidEventRegistration,
                    parameters,
                    cancellationToken);

                if (renderResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "ResendTicketEmail FAILED: Template rendering failed - Template={TemplateName}, RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                        EmailTemplateNames.PaidEventRegistration, request.RegistrationId, renderResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Failed to render email template");
                }

                _logger.LogInformation(
                    "ResendTicketEmail: Email template rendered - Subject={Subject}, RegistrationId={RegistrationId}",
                    renderResult.Value.Subject, request.RegistrationId);

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

                _logger.LogInformation(
                    "ResendTicketEmail: Email message built - To={Email}, Subject={Subject}, AttachmentCount={AttachmentCount}",
                    recipientEmail, renderResult.Value.Subject, emailMessage.Attachments.Count);

                // 11. Send email
                var emailResult = await _emailService.SendEmailAsync(emailMessage, cancellationToken);
                if (emailResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "ResendTicketEmail FAILED: Email sending failed - Email={Email}, RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                        recipientEmail, request.RegistrationId, string.Join(", ", emailResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Failed to send ticket email");
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ResendTicketEmail COMPLETE: Email sent successfully - Email={Email}, RegistrationId={RegistrationId}, TicketCode={TicketCode}, Duration={ElapsedMs}ms",
                    recipientEmail, request.RegistrationId, ticket.TicketCode, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "ResendTicketEmail CANCELLED: Operation was cancelled - RegistrationId={RegistrationId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    request.RegistrationId, request.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "ResendTicketEmail FAILED: Exception occurred - RegistrationId={RegistrationId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.RegistrationId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }

    /// <summary>
    /// Phase 6A.43 FIX: Formats event date/time range matching PaymentCompletedEventHandler.
    /// Examples:
    /// - Same day: "December 24, 2025 from 5:00 PM to 10:00 PM"
    /// - Different days: "December 24, 2025 at 5:00 PM to December 25, 2025 at 10:00 PM"
    /// </summary>
    private static string FormatEventDateTimeRange(DateTime startDate, DateTime endDate)
    {
        if (startDate.Date == endDate.Date)
        {
            // Same day event - show date once with "from X to Y"
            return $"{startDate:MMMM dd, yyyy} from {startDate:h:mm tt} to {endDate:h:mm tt}";
        }
        else
        {
            // Multi-day event - show full date/time for both
            return $"{startDate:MMMM dd, yyyy} at {startDate:h:mm tt} to {endDate:MMMM dd, yyyy} at {endDate:h:mm tt}";
        }
    }

    /// <summary>
    /// Phase 6A.43 FIX: Gets event location string matching PaymentCompletedEventHandler.
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
}
