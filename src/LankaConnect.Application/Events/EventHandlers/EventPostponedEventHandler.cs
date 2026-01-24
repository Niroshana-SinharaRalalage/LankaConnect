using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles EventPostponedEvent to send bulk postponement notifications to all registered attendees
/// </summary>
public class EventPostponedEventHandler : INotificationHandler<DomainEventNotification<EventPostponedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<EventPostponedEventHandler> _logger;

    public EventPostponedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ILogger<EventPostponedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventPostponedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "EventPostponed"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "EventPostponed START: EventId={EventId}, PostponedAt={PostponedAt}, Reason={Reason}",
                domainEvent.EventId, domainEvent.PostponedAt, domainEvent.Reason);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retrieve event data
                _logger.LogInformation(
                    "EventPostponed: Loading event - EventId={EventId}",
                    domainEvent.EventId);

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "EventPostponed: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "EventPostponed: Event loaded - EventTitle={EventTitle}",
                    @event.Title.Value);

                // Get all confirmed registrations for this event
                var registrations = await _registrationRepository.GetByEventAsync(domainEvent.EventId, cancellationToken);
                var confirmedRegistrations = registrations
                    .Where(r => r.Status == RegistrationStatus.Confirmed)
                    .ToList();

                if (!confirmedRegistrations.Any())
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "EventPostponed: No confirmed registrations found, skipping notifications - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "EventPostponed: Found confirmed registrations - Count={Count}",
                    confirmedRegistrations.Count);

                // Prepare bulk email messages
                var emailMessages = new List<EmailMessageDto>();

                foreach (var registration in confirmedRegistrations)
                {
                    // Skip anonymous registrations - they don't have email in user repository
                    if (!registration.UserId.HasValue)
                    {
                        _logger.LogInformation(
                            "EventPostponed: Skipping anonymous registration - RegistrationId={RegistrationId}",
                            registration.Id);
                        continue;
                    }

                    var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                    if (user == null)
                    {
                        _logger.LogWarning(
                            "EventPostponed: User not found for registration - UserId={UserId}, RegistrationId={RegistrationId}",
                            registration.UserId.Value, registration.Id);
                        continue;
                    }

                    var parameters = new Dictionary<string, object>
                    {
                        { "UserName", $"{user.FirstName} {user.LastName}" },
                        { "EventTitle", @event.Title.Value },
                        { "OriginalStartDate", domainEvent.PostponedAt.ToString("MMMM dd, yyyy") },
                        { "OriginalStartTime", domainEvent.PostponedAt.ToString("h:mm tt") },
                        { "Reason", domainEvent.Reason },
                        { "PostponedAt", domainEvent.PostponedAt.ToString("MMMM dd, yyyy h:mm tt") }
                    };

                    var emailMessage = new EmailMessageDto
                    {
                        ToEmail = user.Email.Value,
                        ToName = $"{user.FirstName} {user.LastName}",
                        Subject = $"Event Postponed: {@event.Title.Value}",
                        HtmlBody = GenerateEventPostponedHtml(parameters),
                        Priority = 1 // High priority
                    };

                    emailMessages.Add(emailMessage);
                }

                if (!emailMessages.Any())
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "EventPostponed: No email messages prepared - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                _logger.LogInformation(
                    "EventPostponed: Sending bulk emails - EmailCount={EmailCount}",
                    emailMessages.Count);

                // Send bulk emails
                var result = await _emailService.SendBulkEmailAsync(emailMessages, cancellationToken);

                stopwatch.Stop();

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "EventPostponed FAILED: Bulk email sending failed - EventId={EventId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "EventPostponed COMPLETE: Emails sent successfully - EventId={EventId}, Successful={Successful}, Failed={Failed}, Total={Total}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, result.Value.SuccessfulSends, result.Value.FailedSends, result.Value.TotalEmails, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "EventPostponed CANCELED: Operation was canceled - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "EventPostponed FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }

    private string GenerateEventPostponedHtml(Dictionary<string, object> parameters)
    {
        // Simplified HTML generation - in production this would use Razor templates
        return $@"
            <html>
            <body>
                <h2>Event Postponed</h2>
                <p>Dear {parameters["UserName"]},</p>
                <p>We would like to inform you that the following event has been postponed:</p>
                <ul>
                    <li><strong>Event:</strong> {parameters["EventTitle"]}</li>
                    <li><strong>Original Date:</strong> {parameters["OriginalStartDate"]} at {parameters["OriginalStartTime"]}</li>
                    <li><strong>Reason:</strong> {parameters["Reason"]}</li>
                </ul>
                <p>We will notify you once a new date has been confirmed. Your registration remains active.</p>
                <p>We apologize for any inconvenience this may cause.</p>
                <p>Best regards,<br/>LankaConnect Team</p>
            </body>
            </html>";
    }
}
