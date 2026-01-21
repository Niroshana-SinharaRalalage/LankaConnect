using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.BackgroundJobs;

/// <summary>
/// Phase 6A.71: Background job that runs hourly to send reminder emails for events
/// with idempotency tracking to prevent duplicate reminders
/// </summary>
public class EventReminderJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IEventReminderRepository _eventReminderRepository;
    private readonly ILogger<EventReminderJob> _logger;

    public EventReminderJob(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        IEmailUrlHelper emailUrlHelper,
        IEventReminderRepository eventReminderRepository,
        ILogger<EventReminderJob> logger)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _emailUrlHelper = emailUrlHelper;
        _eventReminderRepository = eventReminderRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation(
            "[Phase 6A.71] [{CorrelationId}] EventReminderJob: Starting execution at {Time}",
            correlationId, DateTime.UtcNow);

        try
        {
            var now = DateTime.UtcNow;

            // Phase 6A.71: Send 3 types of reminders (7 days, 2 days, 1 day)
            // Use 2-hour windows to prevent duplicates while running hourly
            await SendRemindersForWindowAsync(now, 167, 169, "7day", "in 1 week", "Your event is coming up next week. Mark your calendar!", correlationId, CancellationToken.None);
            await SendRemindersForWindowAsync(now, 47, 49, "2day", "in 2 days", "Your event is just 2 days away. Don't forget!", correlationId, CancellationToken.None);
            await SendRemindersForWindowAsync(now, 23, 25, "1day", "tomorrow", "Your event is tomorrow! We look forward to seeing you there.", correlationId, CancellationToken.None);

            _logger.LogInformation(
                "[Phase 6A.71] [{CorrelationId}] EventReminderJob: Completed execution at {Time}",
                correlationId, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.71] [{CorrelationId}] EventReminderJob: Fatal error during execution",
                correlationId);
            throw;  // Phase 6A.61+ Fix: Re-throw for Hangfire retry
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

                        // Phase 6A.71: Use database template with configuration-based URLs
                        var hoursUntilEvent = Math.Round((@event.StartDate - now).TotalHours, 1);
                        var parameters = new Dictionary<string, object>
                        {
                            { "AttendeeName", toName },
                            { "EventTitle", @event.Title?.Value ?? "Untitled Event" },
                            { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                            { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                            { "Location", @event.Location?.Address.ToString() ?? "Location TBD" },
                            { "Quantity", registration.Quantity },
                            { "HoursUntilEvent", hoursUntilEvent },
                            { "ReminderTimeframe", reminderTimeframe },
                            { "ReminderMessage", reminderMessage },
                            { "EventDetailsUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) },
                            // Phase 6A.X: Organizer Contact Details
                            { "HasOrganizerContact", @event.HasOrganizerContact() },
                            { "OrganizerContactName", @event.OrganizerContactName ?? "" },
                            { "OrganizerContactEmail", @event.OrganizerContactEmail ?? "" },
                            { "OrganizerContactPhone", @event.OrganizerContactPhone ?? "" }
                        };

                        var result = await _emailService.SendTemplatedEmailAsync(
                            "event-reminder",
                            toEmail,
                            parameters,
                            cancellationToken);

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

                    // Phase 6A.71: Use database template with configuration-based URLs
                    var hoursUntilEvent = Math.Round((@event.StartDate - now).TotalHours, 1);
                    var parameters = new Dictionary<string, object>
                    {
                        { "AttendeeName", toName },
                        { "EventTitle", @event.Title?.Value ?? "Untitled Event" },
                        { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                        { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                        { "Location", @event.Location?.Address.ToString() ?? "Location TBD" },
                        { "Quantity", registration.Quantity },
                        { "HoursUntilEvent", hoursUntilEvent },
                        { "ReminderTimeframe", reminderTimeframe },
                        { "ReminderMessage", reminderMessage },
                        { "EventDetailsUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) },
                        // Phase 6A.X: Organizer Contact Details
                        { "HasOrganizerContact", @event.HasOrganizerContact() },
                        { "OrganizerContactName", @event.OrganizerContactName ?? "" },
                        { "OrganizerContactEmail", @event.OrganizerContactEmail ?? "" },
                        { "OrganizerContactPhone", @event.OrganizerContactPhone ?? "" }
                    };

                    var result = await _emailService.SendTemplatedEmailAsync(
                        "event-reminder",
                        toEmail,
                        parameters,
                        cancellationToken);

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.75] [{CorrelationId}] Fatal error sending manual reminders for event {EventId}",
                correlationId, eventId);
            throw;  // Re-throw for Hangfire retry
        }
    }
}
