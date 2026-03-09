using System.Diagnostics;
using System.Globalization;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Observability;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.ResendTicketEmail;

/// <summary>
/// Phase 6A.24: Handler for resending ticket emails
/// Verifies ownership and payment status before resending
/// Phase 6A.24 FIX: Now also generates tickets if payment is complete but ticket was never created
/// Phase 6A.100: Unified to use ITypedEmailService only
/// </summary>
public class ResendTicketEmailCommandHandler : ICommandHandler<ResendTicketEmailCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ITicketService _ticketService;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IEmailMetrics _emailMetrics; // Phase 6A.99: For recording pre-send failures
    private readonly ILogger<ResendTicketEmailCommandHandler> _logger;

    private const string HandlerName = "ResendTicketEmailCommandHandler";

    public ResendTicketEmailCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ITicketService ticketService,
        ITypedEmailService typedEmailService,
        IUserRepository userRepository,
        IEmailUrlHelper emailUrlHelper,
        IEmailMetrics emailMetrics, // Phase 6A.99
        ILogger<ResendTicketEmailCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _ticketService = ticketService;
        _typedEmailService = typedEmailService;
        _userRepository = userRepository;
        _emailUrlHelper = emailUrlHelper;
        _emailMetrics = emailMetrics;
        _logger = logger;
    }

    /// <summary>
    /// Phase 6A.99: Record pre-send failures in email metrics for dashboard visibility.
    /// These are failures that happen BEFORE the email send is attempted.
    /// </summary>
    private void RecordPreSendFailure(string errorMessage, string? recipientEmail = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        var maskedEmail = string.IsNullOrEmpty(recipientEmail)
            ? "unknown@unknown.com"
            : MaskEmail(recipientEmail);

        _emailMetrics.RecordFailedEmail(
            correlationId,
            "template-paid-event-registration-confirmation-with-ticket",
            maskedEmail,
            $"[Pre-send] {errorMessage}",
            HandlerName);

        // Also record as a failed send for template stats
        _emailMetrics.RecordEmailSent("template-paid-event-registration-confirmation-with-ticket", 0, success: false);
    }

    /// <summary>
    /// Masks email for privacy in metrics.
    /// </summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";

        var parts = email.Split('@');
        var localPart = parts[0];
        var domainParts = parts[1].Split('.');

        var maskedLocal = localPart.Length > 1
            ? localPart[0] + new string('*', Math.Min(localPart.Length - 1, 3))
            : "*";

        var maskedDomain = domainParts[0].Length > 1
            ? domainParts[0][0] + new string('*', Math.Min(domainParts[0].Length - 1, 3))
            : "*";

        var tld = domainParts.Length > 1 ? domainParts[^1] : "com";

        return $"{maskedLocal}@{maskedDomain}.{tld}";
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

                    RecordPreSendFailure("Registration not found");
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

                    RecordPreSendFailure("Event not found");
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

                    RecordPreSendFailure("Not authorized to resend this ticket");
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

                    RecordPreSendFailure("Payment not completed for this registration");
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

                        RecordPreSendFailure($"Failed to generate ticket: {string.Join(", ", generateResult.Errors)}");
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

                    RecordPreSendFailure("Ticket not found after generation");
                    return Result.Failure("Ticket not found");
                }

                // 5b. Phase 6A.X FIX: Check for stale PDF (attendee count mismatch)
                var currentAttendeeCount = registration.GetAttendeeCount();
                _logger.LogInformation(
                    "ResendTicketEmail: Checking PDF freshness - TicketId={TicketId}, CurrentAttendeeCount={CurrentCount}",
                    ticket.Id, currentAttendeeCount);

                // Regenerate PDF to ensure it has current attendee data
                var regenerateResult = await _ticketService.RegenerateTicketPdfForRegistrationAsync(
                    request.RegistrationId,
                    cancellationToken);

                if (regenerateResult.IsSuccess)
                {
                    _logger.LogInformation(
                        "ResendTicketEmail: PDF regenerated with current attendees - TicketCode={TicketCode}, AttendeeCount={Count}",
                        regenerateResult.Value.TicketCode, currentAttendeeCount);
                }
                else
                {
                    _logger.LogWarning(
                        "ResendTicketEmail: PDF regeneration failed, using cached PDF - Error={Error}",
                        string.Join(", ", regenerateResult.Errors));
                }

                // 6. Get ticket PDF (now with fresh data)
                var pdfResult = await _ticketService.GetTicketPdfAsync(ticket.Id, cancellationToken);
                if (pdfResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "ResendTicketEmail FAILED: PDF retrieval failed - TicketId={TicketId}, Error={Error}, Duration={ElapsedMs}ms",
                        ticket.Id, string.Join(", ", pdfResult.Errors), stopwatch.ElapsedMilliseconds);

                    RecordPreSendFailure($"Failed to retrieve ticket PDF: {string.Join(", ", pdfResult.Errors)}");
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

                    RecordPreSendFailure("No email address found for this registration");
                    return Result.Failure("No email address found for this registration");
                }

                // 8. Phase 6A.100: Format attendee details - names only (no age)
                var attendeeDetailsHtml = new System.Text.StringBuilder();
                if (registration.HasDetailedAttendees())
                {
                    foreach (var attendee in registration.Attendees)
                    {
                        attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
                    }
                }
                else
                {
                    attendeeDetailsHtml.AppendLine($"<p>{registration.GetAttendeeCount()} attendee(s)</p>");
                }

                // Get event's primary image URL
                var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
                var eventImageUrl = primaryImage?.ImageUrl ?? "";

                // Phase 6A.100: Use typed TicketConfirmationEmailParams
                var typedParams = TicketConfirmationEmailParams.Create(
                    eventId: @event.Id,
                    registrationId: registration.Id,
                    userName: recipientName,
                    contactEmail: recipientEmail,
                    eventTitle: @event.Title.Value,
                    eventStartDate: @event.StartDate,
                    eventStartTime: LankaConnect.Shared.Email.Helpers.EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId),
                    eventLocation: GetEventLocationString(@event),
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id),
                    amountPaid: registration.TotalPrice?.Amount ?? 0,
                    paymentIntentId: registration.StripePaymentIntentId ?? "",
                    paymentDate: registration.UpdatedAt ?? DateTime.UtcNow,
                    quantity: registration.GetAttendeeCount());

                // Set timezone
                typedParams.TimeZoneId = @event.TimeZoneId;

                // Set registration date
                typedParams.RegistrationDate = registration.CreatedAt;

                // Set attendee details
                if (registration.HasDetailedAttendees())
                {
                    typedParams.WithAttendees(attendeeDetailsHtml.ToString().TrimEnd());
                }

                // Set event image
                typedParams.WithEventImage(eventImageUrl);

                // Set ticket info
                typedParams.WithTicket(
                    ticket.TicketCode,
                    @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy"),
                    _emailUrlHelper.BuildTicketViewUrl(ticket.Id));

                // Set contact information
                if (registration.Contact != null)
                {
                    typedParams.WithContactInfo(registration.Contact.Email, registration.Contact.PhoneNumber);
                }

                // Phase 6A.133: Set organizer contacts (previously missing — caused empty organizer section)
                if (@event.HasOrganizerContact())
                {
                    typedParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                // Phase 6A.100: Validate parameters
                if (!typedParams.Validate(out var validationErrors))
                {
                    _logger.LogWarning(
                        "[Phase 6A.100] ResendTicketEmail: Parameter validation failed - Errors: {Errors}",
                        string.Join(", ", validationErrors));
                }

                // Phase 6A.100: Build attachment list
                var attachments = new List<EmailAttachmentDto>
                {
                    new EmailAttachmentDto
                    {
                        FileName = $"ticket-{ticket.TicketCode}.pdf",
                        Content = pdfResult.Value,
                        ContentType = "application/pdf"
                    }
                };

                // 9. Phase 6A.100: Send via ITypedEmailService with attachments
                _logger.LogInformation(
                    "ResendTicketEmail: Sending email via ITypedEmailService - Template={TemplateName}, RegistrationId={RegistrationId}",
                    typedParams.TemplateName, request.RegistrationId);

                var emailResult = await _typedEmailService.SendEmailWithAttachmentsAsync(
                    typedParams,
                    attachments,
                    cancellationToken);

                if (!emailResult.Success)
                {
                    stopwatch.Stop();

                    var errorMessage = emailResult.Errors.FirstOrDefault() ?? "Failed to send ticket email";
                    _logger.LogError(
                        "ResendTicketEmail FAILED: Email sending failed - Email={Email}, RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                        recipientEmail, request.RegistrationId, errorMessage, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(errorMessage);
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
