using System.Diagnostics;
using System.Globalization;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Configuration;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A Event Notifications: Handles EventPublishedEvent to send notification emails.
/// This handler is triggered when an event is published (status changes from Draft to Published).
/// Sends email to consolidated list of event email groups and location-matched newsletter subscribers.
/// Phase 6A.39: Refactored to use IEmailService.SendTemplatedEmailAsync (database-based templates)
/// instead of IEmailTemplateService (filesystem-based) for consistency with other handlers.
/// Phase 6A.82: Added feature flag to disable automatic email sending on publish.
/// </summary>
public class EventPublishedEventHandler : INotificationHandler<DomainEventNotification<EventPublishedEvent>>
{
    private readonly IEventNotificationRecipientService _recipientService;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly EmailNotificationSettings _emailNotificationSettings;
    private readonly ILogger<EventPublishedEventHandler> _logger;

    public EventPublishedEventHandler(
        IEventNotificationRecipientService recipientService,
        IEventRepository eventRepository,
        IEmailService emailService,
        IEmailUrlHelper emailUrlHelper,
        IOptions<EmailNotificationSettings> emailNotificationSettings,
        ILogger<EventPublishedEventHandler> logger)
    {
        _recipientService = recipientService;
        _eventRepository = eventRepository;
        _emailService = emailService;
        _emailUrlHelper = emailUrlHelper;
        _emailNotificationSettings = emailNotificationSettings.Value;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventPublishedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "EventPublished"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "EventPublished START: EventId={EventId}, PublishedBy={PublishedBy}, PublishedAt={PublishedAt}",
                domainEvent.EventId, domainEvent.PublishedBy, domainEvent.PublishedAt);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Phase 6A.82: Check if automatic email sending is enabled
                if (!_emailNotificationSettings.SendOnEventPublish)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "EventPublished SKIPPED: Automatic email notifications disabled - EventId={EventId}, Duration={ElapsedMs}ms. " +
                        "Organizers can manually send notifications using 'Send Notification' button.",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Resolve email recipients using domain service
                var recipients = await _recipientService.ResolveRecipientsAsync(domainEvent.EventId, cancellationToken);

                if (!recipients.EmailAddresses.Any())
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "EventPublished: No recipients found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

            _logger.LogInformation(
                "Resolved {RecipientCount} unique email recipients for event {EventId}. " +
                "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
                recipients.EmailAddresses.Count, domainEvent.EventId,
                recipients.Breakdown.EmailGroupCount,
                recipients.Breakdown.MetroAreaSubscribers,
                recipients.Breakdown.StateLevelSubscribers,
                recipients.Breakdown.AllLocationsSubscribers);

            // Retrieve event details for email template
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for EventPublishedEvent", domainEvent.EventId);
                return;
            }

            // Prepare template parameters
            var isFree = @event.IsFree();
            // Phase 6A.56: Explicitly use en-US culture to ensure $ symbol instead of generic ¤
            var ticketPriceText = isFree ? "Free" : @event.TicketPrice?.Amount.ToString("C", CultureInfo.GetCultureInfo("en-US")) ?? "TBA";

            var parameters = new Dictionary<string, object>
            {
                ["EventTitle"] = @event.Title.Value,
                ["EventDescription"] = @event.Description.Value,
                ["EventStartDate"] = @event.StartDate.ToString("MMMM dd, yyyy"),
                ["EventStartTime"] = @event.StartDate.ToString("h:mm tt"),
                ["EventLocation"] = GetEventLocationString(@event),
                ["EventCity"] = @event.Location?.Address.City ?? "TBA",
                ["EventState"] = @event.Location?.Address.State ?? "TBA",
                ["IsFree"] = isFree,
                ["IsPaid"] = !isFree,
                ["TicketPrice"] = ticketPriceText,
                // Phase 6A.70: Use EmailUrlHelper instead of hardcoded URL
                ["EventUrl"] = _emailUrlHelper.BuildEventDetailsUrl(@event.Id)
            };

            // Phase 6A.82: Add organizer contact if opted in (matches EventNotificationEmailJob pattern)
            if (@event.HasOrganizerContact())
            {
                parameters["HasOrganizerContact"] = true;
                parameters["OrganizerName"] = @event.OrganizerContactName ?? "Event Organizer";

                if (!string.IsNullOrWhiteSpace(@event.OrganizerContactEmail))
                    parameters["OrganizerEmail"] = @event.OrganizerContactEmail;

                if (!string.IsNullOrWhiteSpace(@event.OrganizerContactPhone))
                    parameters["OrganizerPhone"] = @event.OrganizerContactPhone;
            }
            else
            {
                parameters["HasOrganizerContact"] = false;
            }

            // Phase 6A.39: Send email to each recipient using database-based template
            // Using the same pattern as RegistrationConfirmedEventHandler for consistency
            var successCount = 0;
            var failCount = 0;

            foreach (var email in recipients.EmailAddresses)
            {
                var result = await _emailService.SendTemplatedEmailAsync(
                    EmailTemplateNames.EventPublished,
                    email,
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
                        "Failed to send event notification email to {Email} for event {EventId}: {Errors}",
                        email, domainEvent.EventId, string.Join(", ", result.Errors));
                }
            }

                stopwatch.Stop();

                _logger.LogInformation(
                    "EventPublished COMPLETE: Emails sent - EventId={EventId}, Success={SuccessCount}, Failed={FailCount}, Total={Total}, Duration={ElapsedMs}ms, " +
                    "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
                    domainEvent.EventId, successCount, failCount, recipients.EmailAddresses.Count, stopwatch.ElapsedMilliseconds,
                    recipients.Breakdown.EmailGroupCount,
                    recipients.Breakdown.MetroAreaSubscribers,
                    recipients.Breakdown.StateLevelSubscribers,
                    recipients.Breakdown.AllLocationsSubscribers);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "EventPublished CANCELED: Operation was canceled - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "EventPublished FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);
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
        var state = @event.Location.Address.State;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);

        return string.Join(", ", parts);
    }
}
