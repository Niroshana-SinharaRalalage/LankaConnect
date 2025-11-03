using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.BackgroundJobs;

/// <summary>
/// Background job that runs hourly to send reminder emails for events starting in 24 hours
/// </summary>
public class EventReminderJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventReminderJob> _logger;

    public EventReminderJob(
        IEventRepository eventRepository,
        IEmailService emailService,
        ILogger<EventReminderJob> logger)
    {
        _eventRepository = eventRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("EventReminderJob: Starting event reminder job execution at {Time}", DateTime.UtcNow);

        try
        {
            // Find events starting in 24 hours (23-25 hour window to account for hourly execution)
            var now = DateTime.UtcNow;
            var reminderWindowStart = now.AddHours(23);
            var reminderWindowEnd = now.AddHours(25);

            _logger.LogInformation("EventReminderJob: Searching for events starting between {Start} and {End}",
                reminderWindowStart, reminderWindowEnd);

            // Get published/active events starting in the next 24 hours
            var upcomingEvents = await _eventRepository.GetEventsStartingInTimeWindowAsync(
                reminderWindowStart,
                reminderWindowEnd,
                new[] { EventStatus.Published, EventStatus.Active });

            _logger.LogInformation("EventReminderJob: Found {Count} events requiring reminders", upcomingEvents.Count);

            foreach (var @event in upcomingEvents)
            {
                try
                {
                    // Get all attendees (registrations) for this event
                    var registrations = @event.Registrations;

                    _logger.LogInformation("EventReminderJob: Sending reminders for event {EventId} ({Title}) to {Count} attendees",
                        @event.Id, @event.Title.Value, registrations.Count);

                    foreach (var registration in registrations)
                    {
                        try
                        {
                            var parameters = new Dictionary<string, object>
                            {
                                { "EventTitle", @event.Title.Value },
                                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                                { "Location", @event.Location?.Address.ToString() ?? "Location TBD" },
                                { "Quantity", registration.Quantity },
                                { "HoursUntilEvent", Math.Round((@event.StartDate - now).TotalHours, 1) }
                            };

                            var emailMessage = new EmailMessageDto
                            {
                                ToEmail = "attendee@example.com", // TODO: Get from registration.UserId
                                ToName = "Event Attendee",
                                Subject = $"Reminder: {@event.Title.Value} starts in 24 hours",
                                HtmlBody = GenerateEventReminderHtml(parameters),
                                Priority = 1 // High priority
                            };

                            var result = await _emailService.SendEmailAsync(emailMessage, CancellationToken.None);

                            if (result.IsFailure)
                            {
                                _logger.LogWarning("EventReminderJob: Failed to send reminder for event {EventId} to user {UserId}: {Errors}",
                                    @event.Id, registration.UserId, string.Join(", ", result.Errors));
                            }
                            else
                            {
                                _logger.LogInformation("EventReminderJob: Reminder sent successfully for event {EventId} to user {UserId}",
                                    @event.Id, registration.UserId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "EventReminderJob: Error sending reminder for event {EventId} to user {UserId}",
                                @event.Id, registration.UserId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EventReminderJob: Error processing reminders for event {EventId}", @event.Id);
                }
            }

            _logger.LogInformation("EventReminderJob: Completed event reminder job execution at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventReminderJob: Fatal error during event reminder job execution");
        }
    }

    private string GenerateEventReminderHtml(Dictionary<string, object> parameters)
    {
        return $@"
            <html>
            <body>
                <h2>Event Reminder</h2>
                <p>Dear Event Attendee,</p>
                <p>This is a friendly reminder that your event is starting soon!</p>
                <ul>
                    <li><strong>Event:</strong> {parameters["EventTitle"]}</li>
                    <li><strong>Date:</strong> {parameters["EventStartDate"]} at {parameters["EventStartTime"]}</li>
                    <li><strong>Location:</strong> {parameters["Location"]}</li>
                    <li><strong>Your Tickets:</strong> {parameters["Quantity"]}</li>
                    <li><strong>Starting In:</strong> {parameters["HoursUntilEvent"]} hours</li>
                </ul>
                <p>We look forward to seeing you there!</p>
                <p>Best regards,<br/>LankaConnect Team</p>
            </body>
            </html>";
    }
}
