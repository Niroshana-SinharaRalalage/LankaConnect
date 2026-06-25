using System.Diagnostics;
using LankaConnect.Modules.Forms.Contracts;
using System.Globalization;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Configuration;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.100: Handles EventPublishedEvent to send notification emails.
/// Uses ITypedEmailService with EventPublishedEmailParams for compile-time type safety.
/// This handler is triggered when an event is published (status changes from Draft to Published).
/// Sends email to consolidated list of event email groups and location-matched newsletter subscribers.
/// </summary>
public class EventPublishedEventHandler : INotificationHandler<DomainEventNotification<EventPublishedEvent>>
{
    private readonly IEventNotificationRecipientService _recipientService;
    private readonly IEventRepository _eventRepository;
    private readonly IFormQueries _formQueries;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly INewsletterSubscriberRepository _newsletterSubscriberRepository;
    private readonly EmailNotificationSettings _emailNotificationSettings;
    private readonly ILogger<EventPublishedEventHandler> _logger;

    public EventPublishedEventHandler(
        IEventNotificationRecipientService recipientService,
        IEventRepository eventRepository,
        IFormQueries formQueries,
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        INewsletterSubscriberRepository newsletterSubscriberRepository,
        IOptions<EmailNotificationSettings> emailNotificationSettings,
        ILogger<EventPublishedEventHandler> logger)
    {
        _recipientService = recipientService;
        _eventRepository = eventRepository;
        _formQueries = formQueries;
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _newsletterSubscriberRepository = newsletterSubscriberRepository;
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

                // Phase 8YA.2 (Q1=A): TBD events can be Published. Skip the published-
                // notification email until SetDates fills in the date pair — sending
                // an announcement with "Date TBD" defeats the point of the email
                // (recipients can't add the event to their calendar). Q4=A keeps the
                // SetDates → Draft transition silent, so the announcement only ever
                // goes out when an organiser explicitly publishes a dated event.
                if (!@event.StartDate.HasValue || !@event.EndDate.HasValue)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "EventPublished SKIPPED: TBD event has no confirmed dates - " +
                        "EventId={EventId}, Duration={ElapsedMs}ms. Announcement email will " +
                        "fire on the next Publish call after SetDates is invoked.",
                        @event.Id, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Prepare common template data
                var isFree = @event.IsFree();
                // Use en-US culture to ensure $ symbol instead of generic ¤
                // Phase 6A.100 Fix: Fall back to Pricing.AdultPrice for events using dual/group pricing
                var ticketPriceText = isFree
                    ? "Free"
                    : @event.TicketPrice?.Amount.ToString("C", CultureInfo.GetCultureInfo("en-US"))
                      ?? @event.Pricing?.AdultPrice.Amount.ToString("C", CultureInfo.GetCultureInfo("en-US"))
                      ?? "See Event Details";
                var eventUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);
                var eventLocation = GetEventLocationString(@event);
                var eventCity = @event.Location?.Address.City ?? string.Empty;
                var eventState = @event.Location?.Address.State ?? string.Empty;
                var hasLocation = !string.IsNullOrWhiteSpace(eventCity) && !string.IsNullOrWhiteSpace(eventState);

                // Organizer contact info
                var hasOrganizerContact = @event.HasOrganizerContact();

                // Phase 6A.103: Get event's primary image URL for email template
                var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
                var eventImageUrl = primaryImage?.ImageUrl ?? @event.Images.FirstOrDefault()?.ImageUrl ?? "";

                // Phase 6A.100: Send email to each recipient using typed email parameters
                var successCount = 0;
                var failCount = 0;

                foreach (var email in recipients.EmailAddresses)
                {
                    var emailParams = EventPublishedEmailParams.Create(
                        recipientEmail: email,
                        eventId: @event.Id,
                        eventTitle: @event.Title.Value,
                        eventDescription: @event.Description.Value,
                        // Phase 8YA.2 (Q1=A): we early-returned above when StartDate was null,
                        // so .Value is safe here without a sentinel.
                        eventStartDate: @event.StartDate.Value,
                        timeZoneId: @event.TimeZoneId,
                        eventLocation: eventLocation,
                        eventCity: eventCity,
                        eventState: eventState,
                        isFree: isFree,
                        ticketPrice: ticketPriceText,
                        eventUrl: eventUrl,
                        hasOrganizerContact: hasOrganizerContact,
                        organizerContactName: hasOrganizerContact ? @event.OrganizerContactName ?? "Event Organizer" : null,
                        organizerContactEmail: hasOrganizerContact ? @event.OrganizerContactEmail : null,
                        organizerContactPhone: hasOrganizerContact ? @event.OrganizerContactPhone : null);

                    // Set location flag for conditional subject rendering
                    emailParams.HasLocation = hasLocation;

                    // Set per-recipient unsubscribe URL for List-Unsubscribe header (RFC 2369/8058)
                    var subscriber = await _newsletterSubscriberRepository.GetByEmailAsync(email, cancellationToken);
                    if (subscriber?.UnsubscribeToken != null)
                    {
                        emailParams.UnsubscribeUrl = _emailUrlHelper.BuildNewsletterUnsubscribeUrl(subscriber.UnsubscribeToken);
                    }

                    // Phase 6A.133 Email: Set all organizer contacts with pre-formatted HTML
                    if (hasOrganizerContact)
                    {
                        emailParams.WithOrganizerContacts(
                            @event.OrganizerContacts
                                .OrderBy(c => c.SortOrder)
                                .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                                .ToList());
                    }

                    // Phase 6A.103: Add event image if available
                    emailParams.WithEventImage(eventImageUrl);

                    // Phase 6A.100 Fix: Add signup lists if event has them
                    if (@event.HasSignUpLists())
                    {
                        emailParams.HasSignUpLists = true;
                        emailParams.SignUpListsUrl = eventUrl + "#sign-ups";
                    }

                    // Phase 6A.112: Check if event has active signup forms
                    var forms = await _formQueries.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, @event.Id, cancellationToken);
                    var hasActiveForms = forms.Any(f => f.Status == FormStatusDto.Active);

                    if (hasActiveForms)
                    {
                        emailParams.WithSignupForms($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms");
                    }

                    var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                    if (result.Success)
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
