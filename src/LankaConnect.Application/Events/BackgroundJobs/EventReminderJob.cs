using System.Diagnostics;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using OrganizerContactInfo = LankaConnect.Shared.Email.Helpers.OrganizerContactInfo;
using LankaConnect.Shared.Email.Services;
using LankaConnect.Shared.WhatsApp.Contracts;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.BackgroundJobs;

/// <summary>
/// Phase 6A.71: Background job that runs hourly to send reminder emails for events
/// with idempotency tracking to prevent duplicate reminders
/// </summary>
public class EventReminderJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IEventReminderRepository _eventReminderRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IWhatsAppService? _whatsAppService;
    private readonly ILogger<EventReminderJob> _logger;

    public EventReminderJob(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        IEventReminderRepository eventReminderRepository,
        ITicketRepository ticketRepository,
        ILogger<EventReminderJob> logger,
        IWhatsAppService? whatsAppService = null)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _eventReminderRepository = eventReminderRepository;
        _ticketRepository = ticketRepository;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using (LogContext.PushProperty("Operation", "EventReminder"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..8];

            _logger.LogInformation(
                "[Phase 6A.71] [{CorrelationId}] EventReminderJob START: Beginning event reminder processing",
                correlationId);

            try
            {
                var now = DateTime.UtcNow;

                // Phase 6A.71: Send 3 types of reminders (7 days, 2 days, 1 day)
                // Use 2-hour windows to prevent duplicates while running hourly
                await SendRemindersForWindowAsync(now, 167, 169, "7day", "in 1 week", "Your event is coming up next week. Mark your calendar!", correlationId, CancellationToken.None);
                await SendRemindersForWindowAsync(now, 47, 49, "2day", "in 2 days", "Your event is just 2 days away. Don't forget!", correlationId, CancellationToken.None);
                await SendRemindersForWindowAsync(now, 23, 25, "1day", "tomorrow", "Your event is tomorrow! We look forward to seeing you there.", correlationId, CancellationToken.None);

                stopwatch.Stop();
                _logger.LogInformation(
                    "[Phase 6A.71] [{CorrelationId}] EventReminderJob COMPLETE: Duration={ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (CancellationToken.None.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.71] [{CorrelationId}] EventReminderJob CANCELED: Duration={ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[Phase 6A.71] [{CorrelationId}] EventReminderJob FAILED: Duration={ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
                throw;  // Phase 6A.61+ Fix: Re-throw for Hangfire retry
            }
        }
    }

    /// <summary>
    /// Phase 6A.71: Send reminders for events starting within a specific time window.
    /// Uses 2-hour windows to prevent duplicate reminders while job runs hourly.
    /// Includes idempotency tracking via event_reminders_sent table.
    /// </summary>
    private async Task SendRemindersForWindowAsync(
        DateTime now,
        int startHours,
        int endHours,
        string reminderType,
        string reminderTimeframe,
        string reminderMessage,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var windowStart = now.AddHours(startHours);
        var windowEnd = now.AddHours(endHours);

        _logger.LogInformation(
            "[Phase 6A.71] [{CorrelationId}] Checking {Timeframe} reminder window ({Start} to {End})",
            correlationId, reminderTimeframe, windowStart, windowEnd);

        var upcomingEvents = await _eventRepository.GetEventsStartingInTimeWindowAsync(
            windowStart,
            windowEnd,
            new[] { EventStatus.Published, EventStatus.Active });

        if (upcomingEvents.Count == 0)
        {
            _logger.LogInformation(
                "[Phase 6A.71] [{CorrelationId}] No events found in {Timeframe} reminder window",
                correlationId, reminderTimeframe);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.71] [{CorrelationId}] Found {Count} events requiring {Timeframe} reminders",
            correlationId, upcomingEvents.Count, reminderTimeframe);

        foreach (var @event in upcomingEvents)
        {
            try
            {
                var registrations = @event.Registrations;

                // Phase 6A.103: Get event's primary image URL
                var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
                var eventImageUrl = primaryImage?.ImageUrl ?? @event.Images.FirstOrDefault()?.ImageUrl ?? "";

                _logger.LogInformation(
                    "[Phase 6A.71] [{CorrelationId}] Sending {Timeframe} reminders for event {EventId} ({Title}) to {Count} attendees",
                    correlationId, reminderTimeframe, @event.Id, @event.Title?.Value ?? "Untitled Event", registrations.Count);

                var successCount = 0;
                var failCount = 0;
                var skippedCount = 0;

                foreach (var registration in registrations)
                {
                    try
                    {
                        // Phase 6A.71: Check idempotency before processing
                        var alreadySent = await _eventReminderRepository.IsReminderAlreadySentAsync(
                            @event.Id, registration.Id, reminderType, cancellationToken);

                        if (alreadySent)
                        {
                            skippedCount++;
                            _logger.LogInformation(
                                "[Phase 6A.71] [{CorrelationId}] Skipping duplicate {ReminderType} reminder for registration {RegistrationId}",
                                correlationId, reminderType, registration.Id);
                            continue;
                        }

                        // Determine email recipient based on registration type
                        string? toEmail = null;
                        string toName = "Event Attendee";

                        if (registration.UserId.HasValue)
                        {
                            var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                            if (user != null)
                            {
                                toEmail = user.Email.Value;
                                toName = $"{user.FirstName} {user.LastName}";
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "[Phase 6A.71] [{CorrelationId}] User {UserId} not found for registration {RegistrationId}, skipping",
                                    correlationId, registration.UserId, registration.Id);
                                continue;
                            }
                        }
                        else if (registration.Contact != null)
                        {
                            toEmail = registration.Contact.Email;
                            var firstAttendee = registration.Attendees.FirstOrDefault();
                            if (firstAttendee != null)
                            {
                                toName = firstAttendee.Name;
                            }
                        }
                        else if (registration.AttendeeInfo != null)
                        {
                            toEmail = registration.AttendeeInfo.Email.Value;
                            toName = registration.AttendeeInfo.Name;
                        }

                        if (string.IsNullOrWhiteSpace(toEmail))
                        {
                            _logger.LogWarning(
                                "[Phase 6A.71] [{CorrelationId}] No email found for registration {RegistrationId}, skipping",
                                correlationId, registration.Id);
                            continue;
                        }

                        // Phase 6A.87: Use typed email parameters for compile-time safety
                        var hoursUntilEvent = Math.Round((@event.StartDate - now).TotalHours, 1);
                        var emailParams = EventReminderEmailParams.Create(
                            eventId: @event.Id,
                            registrationId: registration.Id,
                            attendeeName: toName,
                            attendeeEmail: toEmail,
                            eventTitle: @event.Title?.Value ?? "Untitled Event",
                            eventStartDate: @event.StartDate,
                            eventStartTime: EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId),  // Phase 6A.97: Uses event's timezone
                            eventLocation: @event.Location?.Address.ToString() ?? "Location TBD",
                            quantity: registration.Quantity,
                            hoursUntilEvent: hoursUntilEvent,
                            reminderTimeframe: reminderTimeframe,
                            reminderMessage: reminderMessage,
                            eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id)
                        );

                        // Phase 7C.2b: emit decomposed location keys for the template rewrite.
                        emailParams.WithLocationDetails(@event.ProjectEmailLocation());

                        // Phase 6A.97: Set event's timezone for consistent date/time display
                        emailParams.TimeZoneId = @event.TimeZoneId;

                        // Phase 6A.103: Add event image if available
                        emailParams.WithEventImage(eventImageUrl);

                        // Phase 6A.87: Add organizer contacts if available
                        emailParams.WithOrganizerContacts(
                            @event.OrganizerContacts
                                .OrderBy(c => c.SortOrder)
                                .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                                .ToList());

                        // Phase 6A.100 Fix: Add signup lists URL if event has signup lists
                        if (@event.HasSignUpLists())
                        {
                            emailParams.WithSignUpLists(
                                _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups");
                        }

                        // Phase 6A.87: Add ticket info if available (paid events only)
                        var ticket = await _ticketRepository.GetByRegistrationIdAsync(registration.Id, cancellationToken);
                        if (ticket != null)
                        {
                            emailParams.WithTicket(
                                ticketCode: ticket.TicketCode,
                                expiryDate: EmailDateTimeHelper.FormatEventDate(ticket.ExpiresAt, @event.TimeZoneId)  // Phase 6A.97: Uses event's timezone
                            );
                        }

                        // Phase 7F-A: populate FlexibleRegistration params via the shared
                        // formatter. Mode A leaves attendee table; Mode B renders Lead/Total/
                        // breakdown lines via the {{#if HasDetailedAttendees}} ... {{else}}
                        // block in the template.
                        try
                        {
                            var flex = LankaConnect.Application.Events.Common.HeadCountEmailFormatter.Compute(registration);
                            emailParams.HasDetailedAttendees = flex.hasDetailedAttendees;
                            emailParams.HasHeadCount = flex.hasHeadCount;
                            emailParams.HasHeadCountBreakdown = flex.hasHeadCountBreakdown;
                            emailParams.HasTierBreakdown = flex.hasTierBreakdown;
                            emailParams.HeadCountTotal = flex.headCountTotal;
                            emailParams.HeadCountBreakdownLine = flex.headCountBreakdownLine;
                            emailParams.TierBreakdownLine = flex.tierBreakdownLine;
                            emailParams.LeadAttendeeName = flex.leadAttendeeName;
                        }
                        catch (Exception flexEx)
                        {
                            _logger.LogWarning(flexEx,
                                "[Phase 7F-A] FlexibleRegistration params computation failed for reminder email " +
                                "to {Email} (registration {RegistrationId}) — continuing with default Mode-A fields.",
                                toEmail, registration.Id);
                        }

                        // Phase 6A.100: Send via typed email service
                        var typedResult = await _typedEmailService.SendEmailAsync(
                            emailParams,
                            cancellationToken);

                        // Convert to Result pattern for existing logic compatibility
                        var result = typedResult.Success
                            ? LankaConnect.Domain.Common.Result.Success()
                            : LankaConnect.Domain.Common.Result.Failure(typedResult.Errors.ToArray());

                        if (result.IsSuccess)
                        {
                            successCount++;

                            // Phase 6A.71: Record successful send for idempotency
                            await _eventReminderRepository.RecordReminderSentAsync(
                                @event.Id, registration.Id, reminderType, toEmail, cancellationToken);
                        }
                        else
                        {
                            failCount++;
                            _logger.LogWarning(
                                "[Phase 6A.71] [{CorrelationId}] Failed to send {Timeframe} reminder to {Email} for event {EventId}: {Errors}",
                                correlationId, reminderTimeframe, toEmail, @event.Id, string.Join(", ", result.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex,
                            "[Phase 6A.71] [{CorrelationId}] Error sending {Timeframe} reminder for event {EventId} to registration {RegistrationId}",
                            correlationId, reminderTimeframe, @event.Id, registration.Id);
                    }
                }

                _logger.LogInformation(
                    "[Phase 6A.71] [{CorrelationId}] {Timeframe} reminders for event {EventId}: Success={Success}, Failed={Failed}, Skipped={Skipped}",
                    correlationId, reminderTimeframe, @event.Id, successCount, failCount, skippedCount);

                // Phase 7B.3: Send WhatsApp reminder broadcast to all opted-in attendees
                await SendWhatsAppReminderAsync(@event.Id, @event.Title?.Value ?? "Untitled Event", reminderTimeframe, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 6A.71] [{CorrelationId}] Error processing {Timeframe} reminders for event {EventId}",
                    correlationId, reminderTimeframe, @event.Id);
            }
        }
    }

    /// <summary>
    /// Phase 6A.75: Send reminders for a specific event, bypassing the time window check.
    /// This is for manual testing/debugging purposes.
    /// </summary>
    public async Task SendRemindersForEventAsync(Guid eventId, string reminderType, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation(
            "[Phase 6A.75] [{CorrelationId}] Manual reminder trigger for event {EventId}, type={ReminderType}",
            correlationId, eventId, reminderType);

        try
        {
            // Fetch the event with registrations
            var @event = await _eventRepository.GetWithRegistrationsAsync(eventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.75] [{CorrelationId}] Event {EventId} not found",
                    correlationId, eventId);
                return;
            }

            var registrations = @event.Registrations;
            if (registrations.Count == 0)
            {
                _logger.LogInformation(
                    "[Phase 6A.75] [{CorrelationId}] Event {EventId} has no registrations",
                    correlationId, eventId);
                return;
            }

            // Phase 6A.103: Get event's primary image URL
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? @event.Images.FirstOrDefault()?.ImageUrl ?? "";

            // Determine reminder message based on type
            var (reminderTimeframe, reminderMessage) = reminderType switch
            {
                "7day" => ("in 1 week", "Your event is coming up next week. Mark your calendar!"),
                "2day" => ("in 2 days", "Your event is just 2 days away. Don't forget!"),
                "1day" => ("tomorrow", "Your event is tomorrow! We look forward to seeing you there."),
                _ => ("soon", "Your event is coming up soon!")
            };

            _logger.LogInformation(
                "[Phase 6A.75] [{CorrelationId}] Sending {ReminderType} reminders for event {EventId} ({Title}) to {Count} attendees",
                correlationId, reminderType, eventId, @event.Title?.Value ?? "Untitled Event", registrations.Count);

            var now = DateTime.UtcNow;
            var successCount = 0;
            var failCount = 0;
            var skippedCount = 0;

            foreach (var registration in registrations)
            {
                try
                {
                    // Phase 6A.71: Check idempotency before processing (skip if already sent)
                    var alreadySent = await _eventReminderRepository.IsReminderAlreadySentAsync(
                        eventId, registration.Id, reminderType, cancellationToken);

                    if (alreadySent)
                    {
                        skippedCount++;
                        _logger.LogInformation(
                            "[Phase 6A.75] [{CorrelationId}] Skipping duplicate {ReminderType} reminder for registration {RegistrationId}",
                            correlationId, reminderType, registration.Id);
                        continue;
                    }

                    // Determine email recipient based on registration type
                    string? toEmail = null;
                    string toName = "Event Attendee";

                    if (registration.UserId.HasValue)
                    {
                        var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                        if (user != null)
                        {
                            toEmail = user.Email.Value;
                            toName = $"{user.FirstName} {user.LastName}";
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[Phase 6A.75] [{CorrelationId}] User {UserId} not found for registration {RegistrationId}, skipping",
                                correlationId, registration.UserId, registration.Id);
                            continue;
                        }
                    }
                    else if (registration.Contact != null)
                    {
                        toEmail = registration.Contact.Email;
                        var firstAttendee = registration.Attendees.FirstOrDefault();
                        if (firstAttendee != null)
                        {
                            toName = firstAttendee.Name;
                        }
                    }
                    else if (registration.AttendeeInfo != null)
                    {
                        toEmail = registration.AttendeeInfo.Email.Value;
                        toName = registration.AttendeeInfo.Name;
                    }

                    if (string.IsNullOrWhiteSpace(toEmail))
                    {
                        _logger.LogWarning(
                            "[Phase 6A.75] [{CorrelationId}] No email found for registration {RegistrationId}, skipping",
                            correlationId, registration.Id);
                        continue;
                    }

                    // Phase 6A.87: Use typed email parameters for compile-time safety
                    var hoursUntilEvent = Math.Round((@event.StartDate - now).TotalHours, 1);
                    var emailParams = EventReminderEmailParams.Create(
                        eventId: @event.Id,
                        registrationId: registration.Id,
                        attendeeName: toName,
                        attendeeEmail: toEmail,
                        eventTitle: @event.Title?.Value ?? "Untitled Event",
                        eventStartDate: @event.StartDate,
                        eventStartTime: EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId),  // Phase 6A.97: Uses event's timezone
                        eventLocation: @event.Location?.Address.ToString() ?? "Location TBD",
                        quantity: registration.Quantity,
                        hoursUntilEvent: hoursUntilEvent,
                        reminderTimeframe: reminderTimeframe,
                        reminderMessage: reminderMessage,
                        eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id)
                    );

                    // Phase 7C.2b: emit decomposed location keys for the template rewrite.
                    emailParams.WithLocationDetails(@event.ProjectEmailLocation());

                    // Phase 6A.97: Set event's timezone for consistent date/time display
                    emailParams.TimeZoneId = @event.TimeZoneId;

                    // Phase 6A.103: Add event image if available
                    emailParams.WithEventImage(eventImageUrl);

                    // Phase 6A.87: Add organizer contacts if available
                    emailParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());

                    // Phase 6A.100 Fix: Add signup lists URL if event has signup lists
                    if (@event.HasSignUpLists())
                    {
                        emailParams.WithSignUpLists(
                            _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups");
                    }

                    // Phase 6A.87: Add ticket info if available (paid events only)
                    var ticket = await _ticketRepository.GetByRegistrationIdAsync(registration.Id, cancellationToken);
                    if (ticket != null)
                    {
                        emailParams.WithTicket(
                            ticketCode: ticket.TicketCode,
                            expiryDate: EmailDateTimeHelper.FormatEventDate(ticket.ExpiresAt, @event.TimeZoneId)  // Phase 6A.97: Uses event's timezone
                        );
                    }

                    // Phase 7F-A: populate FlexibleRegistration params via the shared formatter.
                    try
                    {
                        var flex = LankaConnect.Application.Events.Common.HeadCountEmailFormatter.Compute(registration);
                        emailParams.HasDetailedAttendees = flex.hasDetailedAttendees;
                        emailParams.HasHeadCount = flex.hasHeadCount;
                        emailParams.HasHeadCountBreakdown = flex.hasHeadCountBreakdown;
                        emailParams.HasTierBreakdown = flex.hasTierBreakdown;
                        emailParams.HeadCountTotal = flex.headCountTotal;
                        emailParams.HeadCountBreakdownLine = flex.headCountBreakdownLine;
                        emailParams.TierBreakdownLine = flex.tierBreakdownLine;
                        emailParams.LeadAttendeeName = flex.leadAttendeeName;
                    }
                    catch (Exception flexEx)
                    {
                        _logger.LogWarning(flexEx,
                            "[Phase 7F-A] FlexibleRegistration params computation failed for reminder email " +
                            "to {Email} (registration {RegistrationId}) — continuing with default Mode-A fields.",
                            toEmail, registration.Id);
                    }

                    // Phase 6A.100: Send via typed email service
                    var typedResult = await _typedEmailService.SendEmailAsync(
                        emailParams,
                        cancellationToken);

                    // Convert to Result pattern for existing logic compatibility
                    var result = typedResult.Success
                        ? LankaConnect.Domain.Common.Result.Success()
                        : LankaConnect.Domain.Common.Result.Failure(typedResult.Errors.ToArray());

                    if (result.IsSuccess)
                    {
                        successCount++;

                        // Phase 6A.71: Record successful send for idempotency
                        await _eventReminderRepository.RecordReminderSentAsync(
                            eventId, registration.Id, reminderType, toEmail, cancellationToken);
                    }
                    else
                    {
                        failCount++;
                        _logger.LogWarning(
                            "[Phase 6A.75] [{CorrelationId}] Failed to send {ReminderType} reminder to {Email} for event {EventId}: {Errors}",
                            correlationId, reminderType, toEmail, eventId, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex,
                        "[Phase 6A.75] [{CorrelationId}] Error sending {ReminderType} reminder for event {EventId} to registration {RegistrationId}",
                        correlationId, reminderType, eventId, registration.Id);
                }
            }

            _logger.LogInformation(
                "[Phase 6A.75] [{CorrelationId}] Manual {ReminderType} reminders for event {EventId}: Success={Success}, Failed={Failed}, Skipped={Skipped}",
                correlationId, reminderType, eventId, successCount, failCount, skippedCount);

            // Phase 7B.3: Send WhatsApp reminder broadcast to all opted-in attendees
            var (reminderTimeframeForWhatsApp, _) = reminderType switch
            {
                "7day" => ("in 1 week", ""),
                "2day" => ("in 2 days", ""),
                "1day" => ("tomorrow", ""),
                _ => ("soon", "")
            };
            await SendWhatsAppReminderAsync(eventId, @event.Title?.Value ?? "Untitled Event", reminderTimeframeForWhatsApp, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.75] [{CorrelationId}] Fatal error sending manual reminders for event {EventId}",
                correlationId, eventId);
            throw;  // Re-throw for Hangfire retry
        }
    }

    /// <summary>
    /// Phase 7B.3: Sends WhatsApp event reminder to all opted-in attendees via broadcast.
    /// Best-effort: failures are logged but don't block email reminders.
    /// </summary>
    private async Task SendWhatsAppReminderAsync(Guid eventId, string eventTitle, string reminderTimeframe, string correlationId)
    {
        if (_whatsAppService == null)
            return;

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { WhatsAppTemplateContract.Common.EventTitle, eventTitle },
                { WhatsAppTemplateContract.Reminder.ReminderTimeframe, reminderTimeframe },
                { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
            };

            var result = await _whatsAppService.BroadcastToEventAttendeesAsync(
                eventId,
                WhatsAppTemplateContract.TemplateNames.EventReminder,
                parameters,
                WhatsAppNotificationType.EventReminder,
                CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 7B.3] [{CorrelationId}] WhatsApp EventReminder BROADCAST: EventId={EventId}, SentCount={SentCount}",
                    correlationId, eventId, result.Value);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 7B.3] [{CorrelationId}] WhatsApp EventReminder FAILED: EventId={EventId}, {Errors}",
                    correlationId, eventId, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7B.3] [{CorrelationId}] WhatsApp EventReminder EXCEPTION: EventId={EventId}",
                correlationId, eventId);
        }
    }
}
