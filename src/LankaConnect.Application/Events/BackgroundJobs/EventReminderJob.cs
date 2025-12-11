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
                            // Determine email recipient based on registration type
                            string? toEmail = null;
                            string toName = "Event Attendee";

                            if (registration.UserId.HasValue)
                            {
                                // Authenticated user registration - look up user email
                                var user = await _userRepository.GetByIdAsync(registration.UserId.Value, CancellationToken.None);
                                if (user != null)
                                {
                                    toEmail = user.Email.Value;
                                    toName = $"{user.FirstName} {user.LastName}";
                                }
                                else
                                {
                                    _logger.LogWarning("EventReminderJob: User {UserId} not found for registration, skipping reminder",
                                        registration.UserId);
                                    continue;
                                }
                            }
                            else if (registration.Contact != null)
                            {
                                // Anonymous registration with contact info (multi-attendee format)
                                toEmail = registration.Contact.Email;
                                // Try to get first attendee name if available
                                var firstAttendee = registration.Attendees.FirstOrDefault();
                                if (firstAttendee != null)
                                {
                                    toName = firstAttendee.Name;
                                }
                            }
                            else if (registration.AttendeeInfo != null)
                            {
                                // Legacy anonymous registration format
                                toEmail = registration.AttendeeInfo.Email.Value;
                                toName = registration.AttendeeInfo.Name;
                            }

                            if (string.IsNullOrWhiteSpace(toEmail))
                            {
                                _logger.LogWarning("EventReminderJob: No email found for registration {RegistrationId}, skipping reminder",
                                    registration.Id);
                                continue;
                            }

                            var parameters = new Dictionary<string, object>
                            {
                                { "EventTitle", @event.Title.Value },
                                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                                { "Location", @event.Location?.Address.ToString() ?? "Location TBD" },
                                { "Quantity", registration.Quantity },
                                { "HoursUntilEvent", Math.Round((@event.StartDate - now).TotalHours, 1) },
                                { "AttendeeName", toName }
                            };

                            var emailMessage = new EmailMessageDto
                            {
                                ToEmail = toEmail,
                                ToName = toName,
                                Subject = $"Reminder: {@event.Title.Value} starts in 24 hours",
                                HtmlBody = GenerateEventReminderHtml(parameters),
                                Priority = 1 // High priority
                            };

                            var result = await _emailService.SendEmailAsync(emailMessage, CancellationToken.None);

                            if (result.IsFailure)
                            {
                                _logger.LogWarning("EventReminderJob: Failed to send reminder for event {EventId} to {Email}: {Errors}",
                                    @event.Id, toEmail, string.Join(", ", result.Errors));
                            }
                            else
                            {
                                _logger.LogInformation("EventReminderJob: Reminder sent successfully for event {EventId} to {Email}",
                                    @event.Id, toEmail);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "EventReminderJob: Error sending reminder for event {EventId} to registration {RegistrationId}",
                                @event.Id, registration.Id);
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
        var attendeeName = parameters.TryGetValue("AttendeeName", out var name) ? name.ToString() : "Event Attendee";
        return $@"
            <html>
            <body>
                <h2>Event Reminder</h2>
                <p>Dear {attendeeName},</p>
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
