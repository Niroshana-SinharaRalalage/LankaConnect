using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.63: Handles EventCancelledEvent to send cancellation notifications to ALL recipients.
/// Sends email to consolidated list of:
/// 1. Confirmed registrations
/// 2. Event email groups
/// 3. Location-matched newsletter subscribers (metro → state → all locations)
/// Uses database-based template: event-cancelled-notification
/// </summary>
public class EventCancelledEventHandler : INotificationHandler<DomainEventNotification<EventCancelledEvent>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventNotificationRecipientService _recipientService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly ILogger<EventCancelledEventHandler> _logger;

    public EventCancelledEventHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEventNotificationRecipientService recipientService,
        IUserRepository userRepository,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        ILogger<EventCancelledEventHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _recipientService = recipientService;
        _userRepository = userRepository;
        _emailService = emailService;
        _urlsService = urlsService;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("[Phase 6A.63] EventCancelledEventHandler INVOKED - Event {EventId}, Cancelled At {CancelledAt}",
            domainEvent.EventId, domainEvent.CancelledAt);

        try
        {
            // Retrieve event data
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for EventCancelledEvent", domainEvent.EventId);
                return;
            }

            // 1. Get confirmed registration emails
            var registrations = await _registrationRepository.GetByEventAsync(domainEvent.EventId, cancellationToken);
            var confirmedRegistrations = registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed)
                .ToList();

            var registrationEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var registration in confirmedRegistrations)
            {
                // Skip anonymous registrations
                if (!registration.UserId.HasValue)
                {
                    _logger.LogInformation("Skipping anonymous registration {RegistrationId} for cancelled event notification",
                        registration.Id);
                    continue;
                }

                var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                if (user != null)
                {
                    registrationEmails.Add(user.Email.Value);
                }
            }

            _logger.LogInformation("[Phase 6A.63] Found {Count} confirmed registrations for Event {EventId}",
                registrationEmails.Count, domainEvent.EventId);

            // 2. Get email groups + newsletter subscribers (reuse EventPublishedEventHandler pattern)
            var notificationRecipients = await _recipientService.ResolveRecipientsAsync(
                domainEvent.EventId,
                cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.63] Resolved {Count} notification recipients for Event {EventId}. " +
                "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
                notificationRecipients.EmailAddresses.Count, domainEvent.EventId,
                notificationRecipients.Breakdown.EmailGroupCount,
                notificationRecipients.Breakdown.MetroAreaSubscribers,
                notificationRecipients.Breakdown.StateLevelSubscribers,
                notificationRecipients.Breakdown.AllLocationsSubscribers);

            // 3. Consolidate all recipients (deduplicated, case-insensitive)
            _logger.LogInformation(
                "[Phase 6A.63 DEBUG] Before consolidation - Registration emails: [{RegEmails}], Notification emails: [{NotifEmails}]",
                string.Join(", ", registrationEmails),
                string.Join(", ", notificationRecipients.EmailAddresses));

            var allRecipients = registrationEmails
                .Concat(notificationRecipients.EmailAddresses)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation(
                "[Phase 6A.63 DEBUG] After consolidation - All unique recipients: [{AllEmails}]",
                string.Join(", ", allRecipients));

            if (!allRecipients.Any())
            {
                _logger.LogInformation("[Phase 6A.63] No recipients found for Event {EventId}, skipping cancellation emails",
                    domainEvent.EventId);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.63] Sending cancellation emails to {TotalCount} unique recipients for Event {EventId}. " +
                "Breakdown: Registrations={RegCount}, EmailGroups={EmailGroupCount}, Newsletter={NewsletterCount}",
                allRecipients.Count, domainEvent.EventId,
                registrationEmails.Count,
                notificationRecipients.Breakdown.EmailGroupCount,
                notificationRecipients.Breakdown.MetroAreaSubscribers +
                notificationRecipients.Breakdown.StateLevelSubscribers +
                notificationRecipients.Breakdown.AllLocationsSubscribers);

            // 4. Prepare template parameters
            var parameters = new Dictionary<string, object>
            {
                ["EventTitle"] = @event.Title.Value,
                ["EventDate"] = FormatEventDateTimeRange(@event.StartDate, @event.EndDate), // Phase 6A.63 FIX: Match template parameter name
                ["EventLocation"] = GetEventLocationString(@event),
                ["CancellationReason"] = domainEvent.Reason,
                ["DashboardUrl"] = _urlsService.FrontendBaseUrl // Phase 6A.63 FIX 7: Add DashboardUrl for "Browse Other Events" button
            };

            // 5. Send templated email to each recipient
            var successCount = 0;
            var failCount = 0;

            foreach (var email in allRecipients)
            {
                _logger.LogInformation("[Phase 6A.63 DEBUG] Attempting to send cancellation email to: {Email}", email);

                var result = await _emailService.SendTemplatedEmailAsync(
                    "event-cancelled-notification",
                    email,
                    parameters,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    successCount++;
                    _logger.LogInformation("[Phase 6A.63 DEBUG] Successfully sent cancellation email to: {Email}", email);
                }
                else
                {
                    failCount++;
                    _logger.LogWarning(
                        "[Phase 6A.63] Failed to send event cancellation email to {Email} for event {EventId}: {Errors}",
                        email, domainEvent.EventId, string.Join(", ", result.Errors));
                }
            }

            _logger.LogInformation(
                "[Phase 6A.63] Event cancellation emails completed for event {EventId}. Success: {SuccessCount}, Failed: {FailCount}",
                domainEvent.EventId, successCount, failCount);
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex, "[Phase 6A.63] Error handling EventCancelledEvent for Event {EventId}", domainEvent.EventId);
        }
    }

    /// <summary>
    /// Safely extracts event location string with defensive null handling.
    /// Copied from EventPublishedEventHandler for consistency.
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;
        var state = @event.Location.Address.State;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Phase 6A.63 FIX: Formats event date/time range for display.
    /// Copied from RegistrationConfirmedEventHandler for consistency.
    /// Examples:
    /// - Same day: "January 31, 2026 from 3:07 AM to 1:10 PM"
    /// - Different days: "January 31, 2026 at 3:07 AM to February 2, 2026 at 1:10 PM"
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
