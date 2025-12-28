using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.BackgroundJobs;

/// <summary>
/// Background job that runs hourly to send reminder emails for events starting in 24 hours
/// </summary>
public class EventReminderJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventReminderJob> _logger;

    public EventReminderJob(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<EventReminderJob> logger)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[Phase 6A.57] EventReminderJob: Starting execution at {Time}", DateTime.UtcNow);

        try
        {
            var now = DateTime.UtcNow;

            // Phase 6A.57: Send 3 types of reminders (7 days, 2 days, 1 day)
            // Use 2-hour windows to prevent duplicates while running hourly
            await SendRemindersForWindowAsync(now, 167, 169, "in 1 week", "Your event is coming up next week. Mark your calendar!", CancellationToken.None);
            await SendRemindersForWindowAsync(now, 47, 49, "in 2 days", "Your event is just 2 days away. Don't forget!", CancellationToken.None);
            await SendRemindersForWindowAsync(now, 23, 25, "tomorrow", "Your event is tomorrow! We look forward to seeing you there.", CancellationToken.None);

            _logger.LogInformation("[Phase 6A.57] EventReminderJob: Completed execution at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.57] EventReminderJob: Fatal error during execution");
        }
    }

    /// <summary>
    /// Phase 6A.57: Send reminders for events starting within a specific time window.
    /// Uses 2-hour windows to prevent duplicate reminders while job runs hourly.
    /// </summary>
    private async Task SendRemindersForWindowAsync(
        DateTime now,
        int startHours,
        int endHours,
        string reminderTimeframe,
        string reminderMessage,
        CancellationToken cancellationToken)
    {
        var windowStart = now.AddHours(startHours);
        var windowEnd = now.AddHours(endHours);

        _logger.LogInformation(
            "[Phase 6A.57] EventReminderJob: Checking {Timeframe} reminder window ({Start} to {End})",
            reminderTimeframe, windowStart, windowEnd);

        var upcomingEvents = await _eventRepository.GetEventsStartingInTimeWindowAsync(
            windowStart,
            windowEnd,
            new[] { EventStatus.Published, EventStatus.Active });

        if (upcomingEvents.Count == 0)
        {
            _logger.LogInformation("[Phase 6A.57] No events found in {Timeframe} reminder window", reminderTimeframe);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.57] Found {Count} events requiring {Timeframe} reminders",
            upcomingEvents.Count, reminderTimeframe);

        foreach (var @event in upcomingEvents)
        {
            try
            {
                var registrations = @event.Registrations;

                _logger.LogInformation(
                    "[Phase 6A.57] Sending {Timeframe} reminders for event {EventId} ({Title}) to {Count} attendees",
                    reminderTimeframe, @event.Id, @event.Title.Value, registrations.Count);

                var successCount = 0;
                var failCount = 0;

                foreach (var registration in registrations)
                {
                    try
                    {
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
                                    "[Phase 6A.57] User {UserId} not found for registration {RegistrationId}, skipping",
                                    registration.UserId, registration.Id);
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
                                "[Phase 6A.57] No email found for registration {RegistrationId}, skipping",
                                registration.Id);
                            continue;
                        }

                        // Phase 6A.57: Use database template instead of inline HTML
                        var hoursUntilEvent = Math.Round((@event.StartDate - now).TotalHours, 1);
                        var parameters = new Dictionary<string, object>
                        {
                            { "AttendeeName", toName },
                            { "EventTitle", @event.Title.Value },
                            { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                            { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                            { "Location", @event.Location?.Address.ToString() ?? "Location TBD" },
                            { "Quantity", registration.Quantity },
                            { "HoursUntilEvent", hoursUntilEvent },
                            { "ReminderTimeframe", reminderTimeframe },
                            { "ReminderMessage", reminderMessage },
                            { "EventDetailsUrl", $"https://lankaconnect.com/events/{@event.Id}" }
                        };

                        var result = await _emailService.SendTemplatedEmailAsync(
                            "event-reminder",
                            toEmail,
                            parameters,
                            cancellationToken);

                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            _logger.LogWarning(
                                "[Phase 6A.57] Failed to send {Timeframe} reminder to {Email} for event {EventId}: {Errors}",
                                reminderTimeframe, toEmail, @event.Id, string.Join(", ", result.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex,
                            "[Phase 6A.57] Error sending {Timeframe} reminder for event {EventId} to registration {RegistrationId}",
                            reminderTimeframe, @event.Id, registration.Id);
                    }
                }

                _logger.LogInformation(
                    "[Phase 6A.57] {Timeframe} reminders for event {EventId}: Success={Success}, Failed={Failed}",
                    reminderTimeframe, @event.Id, successCount, failCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 6A.57] Error processing {Timeframe} reminders for event {EventId}",
                    reminderTimeframe, @event.Id);
            }
        }
    }

}
