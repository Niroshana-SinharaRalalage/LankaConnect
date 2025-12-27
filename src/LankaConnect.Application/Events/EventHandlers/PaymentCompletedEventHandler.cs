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
        var correlationId = Guid.NewGuid(); // Phase 6A.52: Correlation ID for end-to-end tracing

        _logger.LogInformation(
            "[Phase 6A.52] [PaymentEmail-START] PaymentCompletedEventHandler invoked - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, Amount: {Amount}, Email: {Email}, PaymentIntent: {PaymentIntent}",
            correlationId, domainEvent.EventId, domainEvent.RegistrationId, domainEvent.AmountPaid, domainEvent.ContactEmail, domainEvent.PaymentIntentId);

        try
        {
            // Phase 6A.52: Step 1 - Retrieve event data
            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-1] Loading event - CorrelationId: {CorrelationId}, EventId: {EventId}",
                correlationId, domainEvent.EventId);

            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.52] [PaymentEmail-ERROR] Event not found - CorrelationId: {CorrelationId}, EventId: {EventId}",
                    correlationId, domainEvent.EventId);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-2] Event loaded successfully - CorrelationId: {CorrelationId}, EventTitle: {EventTitle}, Registrations: {RegistrationCount}",
                correlationId, @event.Title.Value, @event.Registrations.Count);

            // Phase 6A.52: Step 2 - Find registration
            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-3] Looking for registration in event - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                correlationId, domainEvent.RegistrationId);

            var registration = @event.Registrations.FirstOrDefault(r => r.Id == domainEvent.RegistrationId);
            if (registration == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.52] [PaymentEmail-ERROR] Registration not found in event - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, EventId: {EventId}",
                    correlationId, domainEvent.RegistrationId, domainEvent.EventId);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-4] Registration found - CorrelationId: {CorrelationId}, AttendeeCount: {AttendeeCount}, HasDetailedAttendees: {HasDetailedAttendees}",
                correlationId, registration.Attendees.Count, registration.HasDetailedAttendees());

            // Determine recipient name and email
            string recipientName;
            string recipientEmail = domainEvent.ContactEmail;

            // Phase 6A.52: Step 3 - Determine recipient
            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-5] Determining recipient - CorrelationId: {CorrelationId}, HasUserId: {HasUserId}, ContactEmail: {ContactEmail}",
                correlationId, domainEvent.UserId.HasValue, domainEvent.ContactEmail);

            if (domainEvent.UserId.HasValue)
            {
                // Authenticated user - get user details
                var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
                if (user != null)
                {
                    recipientName = $"{user.FirstName} {user.LastName}";
                    recipientEmail = user.Email.Value;
                    _logger.LogInformation(
                        "[Phase 6A.52] [PaymentEmail-6] User found - CorrelationId: {CorrelationId}, RecipientName: {RecipientName}, RecipientEmail: {RecipientEmail}",
                        correlationId, recipientName, recipientEmail);
                }
                else
                {
                    recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                        ? registration.Attendees.First().Name
                        : "Guest";
                    _logger.LogWarning(
                        "[Phase 6A.52] [PaymentEmail-WARN] User not found, using fallback name - CorrelationId: {CorrelationId}, UserId: {UserId}, FallbackName: {RecipientName}",
                        correlationId, domainEvent.UserId.Value, recipientName);
                }
            }
            else
            {
                // Anonymous user - use first attendee name or fallback
                recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                    ? registration.Attendees.First().Name
                    : "Guest";
                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-7] Anonymous user - CorrelationId: {CorrelationId}, RecipientName: {RecipientName}",
                    correlationId, recipientName);
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

            // Phase 6A.52: Step 4 - Generate ticket with QR code
            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-8] Starting ticket generation - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, EventId: {EventId}",
                correlationId, registration.Id, @event.Id);

            var ticketResult = await _ticketService.GenerateTicketAsync(
                registration.Id,
                @event.Id,
                cancellationToken);

            byte[]? pdfAttachment = null;
            if (ticketResult.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-9] Ticket generated successfully - CorrelationId: {CorrelationId}, TicketCode: {TicketCode}, TicketId: {TicketId}",
                    correlationId, ticketResult.Value.TicketCode, ticketResult.Value.TicketId);

                parameters["HasTicket"] = true;
                parameters["TicketCode"] = ticketResult.Value.TicketCode;
                parameters["TicketExpiryDate"] = @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy");

                // Get PDF bytes for email attachment
                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-10] Retrieving ticket PDF - CorrelationId: {CorrelationId}, TicketId: {TicketId}",
                    correlationId, ticketResult.Value.TicketId);

                var pdfResult = await _ticketService.GetTicketPdfAsync(ticketResult.Value.TicketId, cancellationToken);
                if (pdfResult.IsSuccess)
                {
                    pdfAttachment = pdfResult.Value;
                    _logger.LogInformation(
                        "[Phase 6A.52] [PaymentEmail-11] Ticket PDF retrieved successfully - CorrelationId: {CorrelationId}, Size: {Size} bytes",
                        correlationId, pdfAttachment.Length);
                }
                else
                {
                    _logger.LogWarning(
                        "[Phase 6A.52] [PaymentEmail-WARN] Failed to retrieve ticket PDF - CorrelationId: {CorrelationId}, Errors: {Errors}",
                        correlationId, string.Join(", ", pdfResult.Errors));
                }
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.52] [PaymentEmail-WARN] Failed to generate ticket - CorrelationId: {CorrelationId}, Errors: {Errors}",
                    correlationId, string.Join(", ", ticketResult.Errors));
                parameters["HasTicket"] = false;
            }

            // Phase 6A.52: Step 5 - Render email template
            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-12] Starting template rendering - CorrelationId: {CorrelationId}, TemplateName: ticket-confirmation, ParameterCount: {ParameterCount}",
                correlationId, parameters.Count);
            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-13] Template parameters - CorrelationId: {CorrelationId}, Parameters: {Parameters}",
                correlationId, string.Join(", ", parameters.Keys));

            // Phase 6A.54: Diagnostic - Log parameter types to detect Result objects
            // Changed LogDebug to LogInformation for visibility in production logs
            foreach (var param in parameters)
            {
                var paramType = param.Value?.GetType().FullName ?? "null";
                var paramValue = param.Value?.ToString() ?? "null";
                var valuePreview = paramValue.Length > 100 ? paramValue.Substring(0, 100) + "..." : paramValue;

                _logger.LogInformation(
                    "[Phase 6A.54] [PaymentEmail-ParamType] Parameter inspection - CorrelationId: {CorrelationId}, Key: {Key}, Type: {Type}, Value: {Value}",
                    correlationId, param.Key, paramType, valuePreview);

                // Phase 6A.54: Detect Result<T> objects and log error
                if (param.Value != null && paramType.Contains("Result"))
                {
                    _logger.LogError(
                        "[Phase 6A.54] [PaymentEmail-CRITICAL] Result object found in template parameters - CorrelationId: {CorrelationId}, Key: {Key}, Type: {Type}",
                        correlationId, param.Key, paramType);
                }
            }

            var renderResult = await _emailTemplateService.RenderTemplateAsync(
                "ticket-confirmation",
                parameters,
                cancellationToken);

            if (renderResult.IsFailure)
            {
                _logger.LogError(
                    "[Phase 6A.52] [PaymentEmail-ERROR] Template rendering failed - CorrelationId: {CorrelationId}, TemplateName: ticket-confirmation, Error: {Error}",
                    correlationId, renderResult.Error);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-14] Template rendered successfully - CorrelationId: {CorrelationId}, SubjectLength: {SubjectLength}, HtmlBodyLength: {HtmlBodyLength}, PlainTextLength: {PlainTextLength}",
                correlationId, renderResult.Value.Subject?.Length ?? 0, renderResult.Value.HtmlBody?.Length ?? 0, renderResult.Value.PlainTextBody?.Length ?? 0);

            // Build email message with attachment
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                ToName = recipientName,
                Subject = renderResult.Value.Subject ?? "Event Registration Confirmation",
                HtmlBody = renderResult.Value.HtmlBody ?? string.Empty,
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

            // Phase 6A.52: Step 6 - Send email with attachment
            _logger.LogInformation(
                "[Phase 6A.52] [PaymentEmail-15] Sending email - CorrelationId: {CorrelationId}, To: {RecipientEmail}, Subject: {Subject}, HasAttachment: {HasAttachment}",
                correlationId, recipientEmail, emailMessage.Subject, pdfAttachment != null);

            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "[Phase 6A.52] [PaymentEmail-ERROR] Failed to send email - CorrelationId: {CorrelationId}, RecipientEmail: {Email}, Errors: {Errors}",
                    correlationId, recipientEmail, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-SUCCESS] Email sent successfully - CorrelationId: {CorrelationId}, RecipientEmail: {Email}, RegistrationId: {RegistrationId}, AttendeeCount: {AttendeeCount}, HasTicket: {HasTicket}",
                    correlationId, recipientEmail, domainEvent.RegistrationId, registration.Attendees.Count, parameters["HasTicket"]);
            }
        }
        catch (Exception ex)
        {
            // Phase 6A.52: Enhanced exception logging with full context
            _logger.LogError(ex,
                "[Phase 6A.52] [PaymentEmail-EXCEPTION] Unhandled exception in PaymentCompletedEventHandler - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, Email: {Email}, ExceptionType: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                correlationId, domainEvent.EventId, domainEvent.RegistrationId, domainEvent.ContactEmail, ex.GetType().FullName, ex.Message, ex.StackTrace);
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
