using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A Event Notifications: Handles EventPublishedEvent to send notification emails.
/// This handler is triggered when an event is published (status changes from Draft to Published).
/// Sends email to consolidated list of event email groups and location-matched newsletter subscribers.
/// </summary>
public class EventPublishedEventHandler : INotificationHandler<DomainEventNotification<EventPublishedEvent>>
{
    private readonly IEventNotificationRecipientService _recipientService;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<EventPublishedEventHandler> _logger;

    public EventPublishedEventHandler(
        IEventNotificationRecipientService recipientService,
        IEventRepository eventRepository,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<EventPublishedEventHandler> logger)
    {
        _recipientService = recipientService;
        _eventRepository = eventRepository;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventPublishedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "[Phase 6A] ✅ EventPublishedEventHandler INVOKED - Event {EventId}, Published By {PublishedBy} at {PublishedAt}",
            domainEvent.EventId, domainEvent.PublishedBy, domainEvent.PublishedAt);

        try
        {
            // Resolve email recipients using domain service
            var recipients = await _recipientService.ResolveRecipientsAsync(domainEvent.EventId, cancellationToken);

            if (!recipients.EmailAddresses.Any())
            {
                _logger.LogInformation(
                    "No email recipients found for event {EventId}. Skipping notification email.",
                    domainEvent.EventId);
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
            var ticketPriceText = isFree ? "Free" : @event.TicketPrice?.Amount.ToString("C") ?? "TBA";

            var parameters = new Dictionary<string, object>
            {
                ["EventTitle"] = @event.Title.Value,
                ["EventDescription"] = @event.Description.Value,
                ["EventStartDate"] = @event.StartDate.ToString("MMMM dd, yyyy"),
                ["EventStartTime"] = @event.StartDate.ToString("h:mm tt"),
                ["EventLocation"] = @event.Location?.ToString() ?? "Location TBA",
                ["EventCity"] = @event.Location?.Address.City ?? "TBA",
                ["EventState"] = @event.Location?.Address.State ?? "TBA",
                ["IsFree"] = isFree,
                ["TicketPrice"] = ticketPriceText,
                ["EventUrl"] = $"https://lankaconnect.com/events/{@event.Id}"
            };

            // Render email templates
            var subjectResult = await _emailTemplateService.RenderTemplateAsync(
                "event-published-subject",
                parameters,
                cancellationToken);
            var htmlResult = await _emailTemplateService.RenderTemplateAsync(
                "event-published-html",
                parameters,
                cancellationToken);
            var textResult = await _emailTemplateService.RenderTemplateAsync(
                "event-published-text",
                parameters,
                cancellationToken);

            if (subjectResult.IsFailure || htmlResult.IsFailure || textResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to render email template for event {EventId}. Subject: {SubjectError}, HTML: {HtmlError}, Text: {TextError}",
                    domainEvent.EventId,
                    subjectResult.IsFailure ? string.Join(", ", subjectResult.Errors) : "OK",
                    htmlResult.IsFailure ? string.Join(", ", htmlResult.Errors) : "OK",
                    textResult.IsFailure ? string.Join(", ", textResult.Errors) : "OK");
                return;
            }

            // Build email messages for bulk sending (one per recipient)
            var emailMessages = recipients.EmailAddresses.Select(email => new EmailMessageDto
            {
                ToEmail = email,
                ToName = string.Empty, // Generic name for newsletter recipients
                Subject = subjectResult.Value.Subject,
                HtmlBody = htmlResult.Value.HtmlBody,
                PlainTextBody = textResult.Value.PlainTextBody,
                Priority = 1 // High priority for event notifications
            }).ToList();

            var result = await _emailService.SendBulkEmailAsync(emailMessages, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "Failed to send event notification email for event {EventId}: {Errors}",
                    domainEvent.EventId, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "Event notification email sent successfully for event {EventId} to {RecipientCount} recipients. " +
                    "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
                    domainEvent.EventId, recipients.EmailAddresses.Count,
                    recipients.Breakdown.EmailGroupCount,
                    recipients.Breakdown.MetroAreaSubscribers,
                    recipients.Breakdown.StateLevelSubscribers,
                    recipients.Breakdown.AllLocationsSubscribers);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex,
                "Error handling EventPublishedEvent for Event {EventId}",
                domainEvent.EventId);
        }
    }
}
