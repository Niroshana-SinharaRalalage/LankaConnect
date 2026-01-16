using LankaConnect.Application.Communications.Common;
using LankaConnect.Application.Communications.Services;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Service implementation for resolving newsletter recipients
/// Phase 6A.74 Part 3C: Newsletter recipient resolution with location targeting
/// Pattern follows EventNotificationRecipientService for consistency
/// </summary>
public class NewsletterRecipientService : INewsletterRecipientService
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly INewsletterSubscriberRepository _subscriberRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<NewsletterRecipientService> _logger;

    public NewsletterRecipientService(
        INewsletterRepository newsletterRepository,
        IEmailGroupRepository emailGroupRepository,
        INewsletterSubscriberRepository subscriberRepository,
        IEventRepository eventRepository,
        ILogger<NewsletterRecipientService> logger)
    {
        _newsletterRepository = newsletterRepository;
        _emailGroupRepository = emailGroupRepository;
        _subscriberRepository = subscriberRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<RecipientPreviewDto> ResolveRecipientsAsync(
        Guid newsletterId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Phase 6A.74] ResolveRecipientsAsync START - NewsletterId: {NewsletterId}", newsletterId);

        Newsletter? newsletter;
        try
        {
            _logger.LogDebug("[Phase 6A.74] Fetching newsletter from repository...");
            newsletter = await _newsletterRepository.GetByIdAsync(newsletterId, cancellationToken);
            _logger.LogDebug("[Phase 6A.74] Newsletter fetch complete - Found: {Found}", newsletter != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74] Failed to fetch newsletter {NewsletterId} from repository", newsletterId);
            throw;
        }

        if (newsletter == null)
        {
            _logger.LogWarning("[Phase 6A.74] Newsletter {NewsletterId} not found", newsletterId);
            return CreateEmptyResult();
        }

        _logger.LogInformation("[Phase 6A.74] Newsletter details - Title: {Title}, EventId: {EventId}, EmailGroupIds: {EmailGroupCount}, MetroAreaIds: {MetroAreaCount}, TargetAllLocations: {TargetAllLocations}, IncludeSubscribers: {IncludeSubscribers}",
            newsletter.Title?.Value ?? "N/A",
            newsletter.EventId,
            newsletter.EmailGroupIds.Count,
            newsletter.MetroAreaIds.Count,
            newsletter.TargetAllLocations,
            newsletter.IncludeNewsletterSubscribers);

        // Get email addresses from newsletter's email groups
        List<string> newsletterEmailGroupAddresses;
        try
        {
            _logger.LogDebug("[Phase 6A.74] Getting newsletter email group addresses...");
            newsletterEmailGroupAddresses = await GetEmailGroupAddressesAsync(newsletter, cancellationToken);
            _logger.LogInformation("[Phase 6A.74] Newsletter email group addresses retrieved: {Count}", newsletterEmailGroupAddresses.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74] Failed to get newsletter email group addresses for newsletter {NewsletterId}", newsletterId);
            throw;
        }

        // Phase 6A.74 HOTFIX: Get event registered attendees' emails
        List<string> eventAttendeeEmails;
        try
        {
            if (newsletter.EventId.HasValue)
            {
                _logger.LogDebug("[Phase 6A.74 HOTFIX] Getting event registered attendees for event {EventId}...", newsletter.EventId.Value);
                eventAttendeeEmails = await GetEventAttendeeEmailsAsync(newsletter.EventId.Value, cancellationToken);
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Event attendee emails retrieved: {Count}", eventAttendeeEmails.Count);
            }
            else
            {
                _logger.LogDebug("[Phase 6A.74 HOTFIX] Newsletter not linked to event, no attendee emails");
                eventAttendeeEmails = new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74 HOTFIX] Failed to get event attendee emails for newsletter {NewsletterId}", newsletterId);
            throw;
        }

        // Phase 6A.74 HOTFIX: Get event's email groups
        List<string> eventEmailGroupAddresses;
        try
        {
            if (newsletter.EventId.HasValue)
            {
                _logger.LogDebug("[Phase 6A.74 HOTFIX] Getting event's email groups for event {EventId}...", newsletter.EventId.Value);
                eventEmailGroupAddresses = await GetEventEmailGroupAddressesAsync(newsletter.EventId.Value, cancellationToken);
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Event email group addresses retrieved: {Count}", eventEmailGroupAddresses.Count);
            }
            else
            {
                _logger.LogDebug("[Phase 6A.74 HOTFIX] Newsletter not linked to event, no event email groups");
                eventEmailGroupAddresses = new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74 HOTFIX] Failed to get event email groups for newsletter {NewsletterId}", newsletterId);
            throw;
        }

        // Get newsletter subscriber emails based on location targeting
        NewsletterSubscriberBreakdown subscriberBreakdown;
        try
        {
            if (newsletter.IncludeNewsletterSubscribers)
            {
                _logger.LogDebug("[Phase 6A.74] Newsletter includes newsletter subscribers, resolving based on location targeting...");
                subscriberBreakdown = await GetNewsletterSubscriberEmailsAsync(newsletter, cancellationToken);
            }
            else
            {
                _logger.LogDebug("[Phase 6A.74] Newsletter does not include newsletter subscribers");
                subscriberBreakdown = new NewsletterSubscriberBreakdown(new HashSet<string>(), 0, 0, 0);
            }
            _logger.LogInformation("[Phase 6A.74] Newsletter subscribers retrieved: {Count}", subscriberBreakdown.Emails.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74] Failed to get newsletter subscribers for newsletter {NewsletterId}", newsletterId);
            throw;
        }

        // Phase 6A.74 HOTFIX: Consolidate ALL recipient sources and deduplicate (case-insensitive)
        // Sources: 1) Newsletter email groups, 2) Event attendees, 3) Event email groups, 4) Newsletter subscribers
        var allEmails = new HashSet<string>(
            newsletterEmailGroupAddresses
                .Concat(eventAttendeeEmails)
                .Concat(eventEmailGroupAddresses)
                .Concat(subscriberBreakdown.Emails),
            StringComparer.OrdinalIgnoreCase);

        var breakdownDto = new RecipientBreakdownDto
        {
            EmailGroupCount = newsletterEmailGroupAddresses.Count,
            MetroAreaSubscribers = subscriberBreakdown.MetroCount,
            StateLevelSubscribers = subscriberBreakdown.StateCount,
            AllLocationsSubscribers = subscriberBreakdown.AllLocationsCount
        };

        _logger.LogInformation(
            "[Phase 6A.74 HOTFIX] Resolved {TotalUnique} unique email recipients for newsletter {NewsletterId}. " +
            "Breakdown: NewsletterEmailGroups={NewsletterEmailGroupCount}, EventAttendees={EventAttendeeCount}, " +
            "EventEmailGroups={EventEmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
            allEmails.Count, newsletterId,
            newsletterEmailGroupAddresses.Count,
            eventAttendeeEmails.Count,
            eventEmailGroupAddresses.Count,
            breakdownDto.MetroAreaSubscribers,
            breakdownDto.StateLevelSubscribers,
            breakdownDto.AllLocationsSubscribers);

        return new RecipientPreviewDto
        {
            TotalRecipients = allEmails.Count,
            EmailAddresses = allEmails.OrderBy(e => e).ToList(),
            Breakdown = breakdownDto
        };
    }

    private async Task<List<string>> GetEmailGroupAddressesAsync(
        Newsletter newsletter,
        CancellationToken cancellationToken)
    {
        if (!newsletter.EmailGroupIds.Any())
        {
            _logger.LogDebug("[Phase 6A.74] Newsletter {NewsletterId} has no email groups", newsletter.Id);
            return new List<string>();
        }

        _logger.LogDebug("[Phase 6A.74] Fetching {Count} email groups: [{Ids}]",
            newsletter.EmailGroupIds.Count,
            string.Join(", ", newsletter.EmailGroupIds));

        IReadOnlyList<EmailGroup> emailGroups;
        try
        {
            emailGroups = await _emailGroupRepository.GetByIdsAsync(
                newsletter.EmailGroupIds,
                cancellationToken);
            _logger.LogDebug("[Phase 6A.74] Email groups fetched: {Count}", emailGroups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74] Failed to fetch email groups for IDs: [{Ids}]",
                string.Join(", ", newsletter.EmailGroupIds));
            throw;
        }

        var emails = emailGroups
            .SelectMany(g => g.GetEmailList())
            .ToList();

        _logger.LogInformation("[Phase 6A.74] Retrieved {Count} emails from {GroupCount} email groups",
            emails.Count, emailGroups.Count);

        return emails;
    }

    private async Task<NewsletterSubscriberBreakdown> GetNewsletterSubscriberEmailsAsync(
        Newsletter newsletter,
        CancellationToken cancellationToken)
    {
        // Location Targeting Logic:
        // 1. If EventId is set: Use Event's MetroAreaId
        // 2. If TargetAllLocations is true: Get ALL confirmed active subscribers
        // 3. If MetroAreaIds are set: Get subscribers matching ANY metro area

        // Case 1: Event-based newsletter
        if (newsletter.EventId.HasValue)
        {
            _logger.LogInformation("[Phase 6A.74] Event-based newsletter - EventId: {EventId}", newsletter.EventId.Value);
            return await GetSubscribersForEventAsync(newsletter.EventId.Value, cancellationToken);
        }

        // Case 2: Target all locations
        if (newsletter.TargetAllLocations)
        {
            _logger.LogInformation("[Phase 6A.74] Newsletter targets all locations");
            return await GetAllLocationSubscribersAsync(cancellationToken);
        }

        // Case 3: Specific metro areas
        if (newsletter.MetroAreaIds.Any())
        {
            _logger.LogInformation("[Phase 6A.74] Newsletter targets specific metro areas: [{MetroAreaIds}]",
                string.Join(", ", newsletter.MetroAreaIds));
            return await GetSubscribersByMetroAreasAsync(newsletter.MetroAreaIds, cancellationToken);
        }

        // No location targeting specified (should not happen due to domain validation)
        _logger.LogWarning("[Phase 6A.74] Newsletter {NewsletterId} has no location targeting specified", newsletter.Id);
        return new NewsletterSubscriberBreakdown(new HashSet<string>(), 0, 0, 0);
    }

    private async Task<NewsletterSubscriberBreakdown> GetSubscribersForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        // Fetch event to get its metro area
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (@event == null || @event.Location?.Address == null)
        {
            _logger.LogWarning("[Phase 6A.74] Event {EventId} not found or has no location", eventId);
            return new NewsletterSubscriberBreakdown(new HashSet<string>(), 0, 0, 0);
        }

        var state = @event.Location.Address.State;
        _logger.LogDebug("[Phase 6A.74] Event location: {City}, {State}",
            @event.Location.Address.City, state);

        // Get subscribers for the event's state (matches event notification pattern)
        IReadOnlyList<NewsletterSubscriber> subscribers;
        try
        {
            subscribers = await _subscriberRepository.GetConfirmedSubscribersByStateAsync(state, cancellationToken);
            _logger.LogInformation("[Phase 6A.74] Found {Count} subscribers for event's state {State}",
                subscribers.Count, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74] Failed to get subscribers for event state {State}", state);
            throw;
        }

        var emails = subscribers
            .Select(s => s.Email.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // For event-based newsletters, all subscribers come from state-level matching
        return new NewsletterSubscriberBreakdown(
            Emails: emails,
            MetroCount: 0,
            StateCount: subscribers.Count,
            AllLocationsCount: 0);
    }

    private async Task<NewsletterSubscriberBreakdown> GetAllLocationSubscribersAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<NewsletterSubscriber> subscribers;
        try
        {
            subscribers = await _subscriberRepository.GetConfirmedSubscribersForAllLocationsAsync(cancellationToken);
            _logger.LogInformation("[Phase 6A.74] Found {Count} subscribers for all locations", subscribers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74] Failed to get all-location subscribers");
            throw;
        }

        var emails = subscribers
            .Select(s => s.Email.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new NewsletterSubscriberBreakdown(
            Emails: emails,
            MetroCount: 0,
            StateCount: 0,
            AllLocationsCount: subscribers.Count);
    }

    private async Task<NewsletterSubscriberBreakdown> GetSubscribersByMetroAreasAsync(
        IReadOnlyList<Guid> metroAreaIds,
        CancellationToken cancellationToken)
    {
        var allSubscribers = new List<NewsletterSubscriber>();

        foreach (var metroAreaId in metroAreaIds)
        {
            try
            {
                _logger.LogDebug("[Phase 6A.74] Fetching subscribers for metro area {MetroAreaId}", metroAreaId);

                var subscribers = await _subscriberRepository.GetConfirmedSubscribersByMetroAreaAsync(
                    metroAreaId,
                    cancellationToken);

                _logger.LogDebug("[Phase 6A.74] Found {Count} subscribers for metro area {MetroAreaId}",
                    subscribers.Count, metroAreaId);

                allSubscribers.AddRange(subscribers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 6A.74] Failed to get subscribers for metro area {MetroAreaId}", metroAreaId);
                // Continue with other metro areas instead of failing completely
            }
        }

        // Deduplicate subscribers who may be subscribed to multiple metros
        var uniqueSubscribers = allSubscribers
            .DistinctBy(s => s.Id)
            .ToList();

        _logger.LogInformation("[Phase 6A.74] Found {TotalSubscribers} total, {UniqueSubscribers} unique subscribers for {MetroAreaCount} metro areas",
            allSubscribers.Count, uniqueSubscribers.Count, metroAreaIds.Count);

        var emails = uniqueSubscribers
            .Select(s => s.Email.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new NewsletterSubscriberBreakdown(
            Emails: emails,
            MetroCount: uniqueSubscribers.Count,
            StateCount: 0,
            AllLocationsCount: 0);
    }

    private static RecipientPreviewDto CreateEmptyResult()
    {
        return new RecipientPreviewDto
        {
            TotalRecipients = 0,
            EmailAddresses = Array.Empty<string>(),
            Breakdown = new RecipientBreakdownDto
            {
                EmailGroupCount = 0,
                MetroAreaSubscribers = 0,
                StateLevelSubscribers = 0,
                AllLocationsSubscribers = 0
            }
        };
    }

    /// <summary>
    /// Phase 6A.74 HOTFIX: Gets email addresses of all confirmed attendees for an event
    /// Handles both legacy (AttendeeInfo) and new (Contact) registration formats
    /// </summary>
    private async Task<List<string>> GetEventAttendeeEmailsAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var @event = await _eventRepository.GetWithRegistrationsAsync(eventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("[Phase 6A.74 HOTFIX] Event {EventId} not found", eventId);
                return new List<string>();
            }

            var emails = new List<string>();

            foreach (var registration in @event.Registrations)
            {
                // Only include confirmed registrations
                if (registration.Status != Domain.Events.Enums.RegistrationStatus.Confirmed)
                    continue;

                // New format: Registration with Contact
                if (registration.Contact != null)
                {
                    emails.Add(registration.Contact.Email);
                }
                // Legacy format: Anonymous registration with AttendeeInfo
                else if (registration.AttendeeInfo != null)
                {
                    emails.Add(registration.AttendeeInfo.Email.Value);
                }
            }

            _logger.LogInformation("[Phase 6A.74 HOTFIX] Extracted {Count} attendee emails from event {EventId}",
                emails.Count, eventId);

            return emails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74 HOTFIX] Failed to get event attendees for event {EventId}", eventId);
            throw;
        }
    }

    /// <summary>
    /// Phase 6A.74 HOTFIX: Gets all email addresses from email groups assigned to an event
    /// </summary>
    private async Task<List<string>> GetEventEmailGroupAddressesAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Load event with email group IDs
            var @event = await _eventRepository.GetByIdAsync(eventId, trackChanges: false, cancellationToken);
            if (@event == null || !@event.EmailGroupIds.Any())
            {
                _logger.LogDebug("[Phase 6A.74 HOTFIX] Event {EventId} has no email groups", eventId);
                return new List<string>();
            }

            _logger.LogDebug("[Phase 6A.74 HOTFIX] Fetching {Count} email groups for event {EventId}: [{Ids}]",
                @event.EmailGroupIds.Count,
                eventId,
                string.Join(", ", @event.EmailGroupIds));

            // Fetch email groups
            var emailGroups = await _emailGroupRepository.GetByIdsAsync(
                @event.EmailGroupIds,
                cancellationToken);

            var emails = emailGroups
                .SelectMany(g => g.GetEmailList())
                .ToList();

            _logger.LogInformation("[Phase 6A.74 HOTFIX] Retrieved {Count} emails from {GroupCount} event email groups",
                emails.Count, emailGroups.Count);

            return emails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74 HOTFIX] Failed to get event email groups for event {EventId}", eventId);
            throw;
        }
    }

    /// <summary>
    /// Internal helper record to track breakdown during newsletter subscriber resolution
    /// </summary>
    private sealed record NewsletterSubscriberBreakdown(
        HashSet<string> Emails,
        int MetroCount,
        int StateCount,
        int AllLocationsCount);
}
