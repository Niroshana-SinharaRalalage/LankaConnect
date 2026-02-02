using System.Diagnostics;
using System.Globalization;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.X: Handles AttendeesAddedEvent to send confirmation email and regenerate ticket PDF.
/// This handler is triggered after additional attendees are successfully added to a paid registration.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public class AttendeesAddedEventHandler : INotificationHandler<DomainEventNotification<AttendeesAddedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ITicketService _ticketService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<AttendeesAddedEventHandler> _logger;

    private const string HandlerName = nameof(AttendeesAddedEventHandler);

    public AttendeesAddedEventHandler(
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ITicketService ticketService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<AttendeesAddedEventHandler> logger)
    {
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _ticketService = ticketService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<AttendeesAddedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Guid.NewGuid();

        using (LogContext.PushProperty("Operation", "AttendeesAdded"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Phase 6A.X] AttendeesAdded START: CorrelationId={CorrelationId}, EventId={EventId}, RegistrationId={RegistrationId}, " +
                "PreviousCount={PreviousCount}, AddedCount={AddedCount}, NewTotal={NewTotal}, AdditionalAmount={AdditionalAmount}",
                correlationId, domainEvent.EventId, domainEvent.RegistrationId,
                domainEvent.PreviousAttendeeCount, domainEvent.AddedAttendeeCount,
                domainEvent.NewTotalAttendeeCount, domainEvent.AdditionalAmountPaid);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Step 1: Load event data
                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-1] Loading event - CorrelationId={CorrelationId}, EventId={EventId}",
                    correlationId, domainEvent.EventId);

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "[Phase 6A.X] [AttendeesAdded-ERROR] Event not found - CorrelationId={CorrelationId}, EventId={EventId}",
                        correlationId, domainEvent.EventId);
                    return;
                }

                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-2] Event loaded - CorrelationId={CorrelationId}, EventTitle={EventTitle}",
                    correlationId, @event.Title.Value);

                // Step 2: Load registration with updated attendees
                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-3] Loading registration - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}",
                    correlationId, domainEvent.RegistrationId);

                var registration = await _registrationRepository.GetByIdAsync(domainEvent.RegistrationId, cancellationToken);
                if (registration == null)
                {
                    _logger.LogWarning(
                        "[Phase 6A.X] [AttendeesAdded-ERROR] Registration not found - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}",
                        correlationId, domainEvent.RegistrationId);
                    return;
                }

                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-4] Registration loaded - CorrelationId={CorrelationId}, AttendeeCount={AttendeeCount}",
                    correlationId, registration.Attendees.Count);

                // Step 3: Determine recipient
                string recipientName;
                string recipientEmail = domainEvent.ContactEmail;

                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-5] Determining recipient - CorrelationId={CorrelationId}, HasUserId={HasUserId}",
                    correlationId, domainEvent.UserId.HasValue);

                if (domainEvent.UserId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
                    if (user != null)
                    {
                        recipientName = $"{user.FirstName} {user.LastName}";
                        recipientEmail = user.Email.Value;
                        _logger.LogInformation(
                            "[Phase 6A.X] [AttendeesAdded-6] User found - CorrelationId={CorrelationId}, Name={Name}",
                            correlationId, recipientName);
                    }
                    else
                    {
                        recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                            ? registration.Attendees.First().Name
                            : "Guest";
                        _logger.LogWarning(
                            "[Phase 6A.X] [AttendeesAdded-WARN] User not found, using fallback - CorrelationId={CorrelationId}",
                            correlationId);
                    }
                }
                else
                {
                    recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                        ? registration.Attendees.First().Name
                        : "Guest";
                    _logger.LogInformation(
                        "[Phase 6A.X] [AttendeesAdded-7] Anonymous user - CorrelationId={CorrelationId}, Name={Name}",
                        correlationId, recipientName);
                }

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    _logger.LogError(
                        "[Phase 6A.X] [AttendeesAdded-ERROR] No email address - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}",
                        correlationId, domainEvent.RegistrationId);
                    return;
                }

                // Step 4: Regenerate ticket PDF with updated attendees
                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-8] Regenerating ticket PDF - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}",
                    correlationId, domainEvent.RegistrationId);

                var ticketResult = await _ticketService.RegenerateTicketPdfForRegistrationAsync(
                    domainEvent.RegistrationId,
                    cancellationToken);

                byte[]? pdfAttachment = null;
                string ticketCode = "";
                Guid? ticketId = null;

                if (ticketResult.IsSuccess)
                {
                    ticketCode = ticketResult.Value.TicketCode;
                    ticketId = ticketResult.Value.TicketId;

                    _logger.LogInformation(
                        "[Phase 6A.X] [AttendeesAdded-9] Ticket PDF regenerated - CorrelationId={CorrelationId}, TicketCode={TicketCode}",
                        correlationId, ticketCode);

                    // Get PDF bytes for email attachment
                    var pdfResult = await _ticketService.GetTicketPdfAsync(ticketResult.Value.TicketId, cancellationToken);
                    if (pdfResult.IsSuccess)
                    {
                        pdfAttachment = pdfResult.Value;
                        _logger.LogInformation(
                            "[Phase 6A.X] [AttendeesAdded-10] PDF retrieved - CorrelationId={CorrelationId}, Size={Size} bytes",
                            correlationId, pdfAttachment.Length);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Phase 6A.X] [AttendeesAdded-WARN] PDF retrieval failed - CorrelationId={CorrelationId}, Errors={Errors}",
                            correlationId, string.Join(", ", pdfResult.Errors));
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "[Phase 6A.X] [AttendeesAdded-WARN] Ticket regeneration failed - CorrelationId={CorrelationId}, Errors={Errors}",
                        correlationId, string.Join(", ", ticketResult.Errors));
                }

                // Step 5: Build email parameters
                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-11] Building email parameters - CorrelationId={CorrelationId}",
                    correlationId);

                // Format new attendees HTML
                var newAttendeesHtml = new System.Text.StringBuilder();
                var newAttendeesText = new System.Text.StringBuilder();

                // Get the NEW attendees (last N attendees where N = AddedAttendeeCount)
                var allAttendees = registration.Attendees.ToList();
                var newAttendees = allAttendees.TakeLast(domainEvent.AddedAttendeeCount).ToList();

                foreach (var attendee in newAttendees)
                {
                    newAttendeesHtml.AppendLine($@"<div class=""attendee-item"">
                        <div class=""attendee-icon new"">&#10003;</div>
                        <span class=""attendee-name"">{attendee.Name}</span>
                        <span class=""attendee-badge"">NEW</span>
                    </div>");
                    newAttendeesText.AppendLine($"- {attendee.Name} ({attendee.AgeCategory})");
                }

                // Format all attendees HTML
                var allAttendeesHtml = new System.Text.StringBuilder();
                var allAttendeesText = new System.Text.StringBuilder();
                int index = 1;
                foreach (var attendee in allAttendees)
                {
                    var isNew = newAttendees.Contains(attendee);
                    var iconClass = isNew ? "new" : "";
                    var badge = isNew ? @"<span class=""attendee-badge"">NEW</span>" : "";
                    var initial = attendee.Name.Length > 0 ? attendee.Name[0].ToString().ToUpper() : index.ToString();

                    allAttendeesHtml.AppendLine($@"<div class=""attendee-item"">
                        <div class=""attendee-icon {iconClass}"">{(isNew ? "&#10003;" : initial)}</div>
                        <span class=""attendee-name"">{attendee.Name}</span>
                        {badge}
                    </div>");
                    allAttendeesText.AppendLine($"- {attendee.Name} ({attendee.AgeCategory})");
                    index++;
                }

                var parameters = new Dictionary<string, object>
                {
                    { "UserName", recipientName },
                    { "EventTitle", @event.Title.Value },
                    { "EventStartDate", EmailDateTimeHelper.FormatEventDate(@event.StartDate, @event.TimeZoneId) },  // Phase 6A.97: Uses event's timezone
                    { "EventStartTime", EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId) },  // Phase 6A.97: Uses event's timezone
                    { "EventLocation", GetEventLocationString(@event) },
                    { "PreviousCount", domainEvent.PreviousAttendeeCount },
                    { "AddedCount", domainEvent.AddedAttendeeCount },
                    { "NewTotalCount", domainEvent.NewTotalAttendeeCount },
                    { "AdditionalAmount", domainEvent.AdditionalAmountPaid.ToString("F2", CultureInfo.InvariantCulture) },
                    { "TotalPaid", domainEvent.TotalAmountPaid.ToString("F2", CultureInfo.InvariantCulture) },
                    { "NewAttendees", newAttendeesText.ToString().TrimEnd() },
                    { "NewAttendeesHtml", newAttendeesHtml.ToString() },
                    { "AllAttendees", allAttendeesText.ToString().TrimEnd() },
                    { "AllAttendeesHtml", allAttendeesHtml.ToString() },
                    { "EventDetailsUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) },
                    { "Year", DateTime.UtcNow.Year }
                };

                if (ticketId.HasValue)
                {
                    parameters["TicketUrl"] = _emailUrlHelper.BuildTicketViewUrl(ticketId.Value);
                    parameters["TicketCode"] = ticketCode;
                    parameters["HasTicket"] = true;
                }
                else
                {
                    parameters["HasTicket"] = false;
                }

                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-12] Parameters built - CorrelationId={CorrelationId}, ParameterCount={Count}",
                    correlationId, parameters.Count);

                // Step 6: Render email template
                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-13] Rendering template - CorrelationId={CorrelationId}, Template={Template}",
                    correlationId, EmailTemplateNames.AttendeesAddedConfirmation);

                var renderResult = await _emailTemplateService.RenderTemplateAsync(
                    EmailTemplateNames.AttendeesAddedConfirmation,
                    parameters,
                    cancellationToken);

                if (renderResult.IsFailure)
                {
                    _logger.LogError(
                        "[Phase 6A.X] [AttendeesAdded-ERROR] Template rendering failed - CorrelationId={CorrelationId}, Error={Error}",
                        correlationId, renderResult.Error);
                    return;
                }

                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-14] Template rendered - CorrelationId={CorrelationId}, Subject={Subject}",
                    correlationId, renderResult.Value.Subject);

                // Step 7: Build and send email
                var emailMessage = new EmailMessageDto
                {
                    ToEmail = recipientEmail,
                    ToName = recipientName,
                    Subject = renderResult.Value.Subject ?? "Your Registration Has Been Updated",
                    HtmlBody = renderResult.Value.HtmlBody ?? string.Empty,
                    PlainTextBody = renderResult.Value.PlainTextBody,
                    Attachments = pdfAttachment != null
                        ? new List<EmailAttachment>
                        {
                            new EmailAttachment
                            {
                                FileName = $"ticket-{ticketCode}.pdf",
                                Content = pdfAttachment,
                                ContentType = "application/pdf"
                            }
                        }
                        : null
                };

                _logger.LogInformation(
                    "[Phase 6A.X] [AttendeesAdded-15] Sending email - CorrelationId={CorrelationId}, To={Email}, HasAttachment={HasAttachment}",
                    correlationId, recipientEmail, pdfAttachment != null);

                var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

                stopwatch.Stop();

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "[Phase 6A.X] AttendeesAdded FAILED: Email sending failed - CorrelationId={CorrelationId}, Email={Email}, " +
                        "Errors={Errors}, Duration={ElapsedMs}ms",
                        correlationId, recipientEmail, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.X] AttendeesAdded COMPLETE: Email sent successfully - CorrelationId={CorrelationId}, " +
                        "Email={Email}, RegistrationId={RegistrationId}, AddedCount={AddedCount}, HasTicket={HasTicket}, " +
                        "Duration={ElapsedMs}ms",
                        correlationId, recipientEmail, domainEvent.RegistrationId, domainEvent.AddedAttendeeCount,
                        parameters["HasTicket"], stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.X] AttendeesAdded CANCELED - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[Phase 6A.X] AttendeesAdded FAILED: Unhandled exception - CorrelationId={CorrelationId}, " +
                    "EventId={EventId}, RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                    correlationId, domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
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
}
