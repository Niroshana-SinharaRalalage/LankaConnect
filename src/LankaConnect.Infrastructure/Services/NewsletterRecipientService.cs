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
    private readonly EventMetroAreaMatcher _metroMatcher; // Phase 6A.74 Part 13: City-to-metro bucketing
    private readonly ILogger<NewsletterRecipientService> _logger;

    public NewsletterRecipientService(
        INewsletterRepository newsletterRepository,
        IEmailGroupRepository emailGroupRepository,
        INewsletterSubscriberRepository subscriberRepository,
        IEventRepository eventRepository,
        EventMetroAreaMatcher metroMatcher, // Phase 6A.74 Part 13: Inject EventMetroAreaMatcher
        ILogger<NewsletterRecipientService> logger)
    {
        _newsletterRepository = newsletterRepository;
        _emailGroupRepository = emailGroupRepository;
        _subscriberRepository = subscriberRepository;
        _eventRepository = eventRepository;
        _metroMatcher = metroMatcher; // Phase 6A.74 Part 13
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
            _logger.LogInformation("[Phase 6A.74] Fetching newsletter from repository...");
            newsletter = await _newsletterRepository.GetByIdAsync(newsletterId, cancellationToken);
            _logger.LogInformation("[Phase 6A.74] Newsletter fetch complete - Found: {Found}", newsletter != null);
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
            _logger.LogInformation("[Phase 6A.74] Getting newsletter email group addresses...");
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
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Getting event registered attendees for event {EventId}...", newsletter.EventId.Value);
                eventAttendeeEmails = await GetEventAttendeeEmailsAsync(newsletter.EventId.Value, cancellationToken);
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Event attendee emails retrieved: {Count}", eventAttendeeEmails.Count);
            }
            else
            {
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Newsletter not linked to event, no attendee emails");
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
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Getting event's email groups for event {EventId}...", newsletter.EventId.Value);
                eventEmailGroupAddresses = await GetEventEmailGroupAddressesAsync(newsletter.EventId.Value, cancellationToken);
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Event email group addresses retrieved: {Count}", eventEmailGroupAddresses.Count);
            }
            else
            {
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Newsletter not linked to event, no event email groups");
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
                _logger.LogInformation("[Phase 6A.74] Newsletter includes newsletter subscribers, resolving based on location targeting...");
                subscriberBreakdown = await GetNewsletterSubscriberEmailsAsync(newsletter, cancellationToken);
            }
            else
            {
                _logger.LogInformation("[Phase 6A.74] Newsletter does not include newsletter subscribers");
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

        // Phase 6A.74 Part 13+: Updated breakdown with all 4 sources separately
        var breakdownDto = new RecipientBreakdownDto
        {
            NewsletterEmailGroupCount = newsletterEmailGroupAddresses.Count,
            EventEmailGroupCount = eventEmailGroupAddresses.Count,
            SubscriberCount = subscriberBreakdown.Emails.Count,
            EventRegistrationCount = eventAttendeeEmails.Count,
            MetroAreaSubscribers = subscriberBreakdown.MetroCount,
            StateLevelSubscribers = subscriberBreakdown.StateCount,
            AllLocationsSubscribers = subscriberBreakdown.AllLocationsCount
        };

        _logger.LogInformation(
            "[Phase 6A.74 Part 13+] Resolved {TotalUnique} unique email recipients for newsletter {NewsletterId}. " +
            "Breakdown: NewsletterEmailGroups={NewsletterEmailGroupCount}, EventEmailGroups={EventEmailGroupCount}, " +
            "Subscribers={SubscriberCount}, EventRegistrations={EventRegistrationCount}",
            allEmails.Count, newsletterId,
            breakdownDto.NewsletterEmailGroupCount,
            breakdownDto.EventEmailGroupCount,
            breakdownDto.SubscriberCount,
            breakdownDto.EventRegistrationCount);

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
            _logger.LogInformation("[Phase 6A.74] Newsletter {NewsletterId} has no email groups", newsletter.Id);
            return new List<string>();
        }

        _logger.LogInformation("[Phase 6A.74] Fetching {Count} email groups: [{Ids}]",
            newsletter.EmailGroupIds.Count,
            string.Join(", ", newsletter.EmailGroupIds));

        IReadOnlyList<EmailGroup> emailGroups;
        try
        {
            emailGroups = await _emailGroupRepository.GetByIdsAsync(
                newsletter.EmailGroupIds,
                cancellationToken);
            _logger.LogInformation("[Phase 6A.74] Email groups fetched: {Count}", emailGroups.Count);
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
        // Location Targeting Logic (Phase 6A.85 Fix):
        // 1. If EventId is set: Use Event's MetroAreaId
        // 2. If MetroAreaIds are set: Get subscribers matching ANY metro area (includes "All Locations" case)
        // 3. If TargetAllLocations is true BUT no metros: Fallback to receive_all_locations subscribers
        //
        // CRITICAL: Phase 6A.85 fix populates ALL 84 metros when targetAllLocations=true
        // So we must check MetroAreaIds.Any() BEFORE checking TargetAllLocations boolean
        // Otherwise, we'll query for receive_all_locations=true (which has 0 subscribers)
        // instead of using metro intersection matching (which works correctly)

        // Case 1: Event-based newsletter
        if (newsletter.EventId.HasValue)
        {
            _logger.LogInformation("[Phase 6A.74] Event-based newsletter - EventId: {EventId}", newsletter.EventId.Value);
            return await GetSubscribersForEventAsync(newsletter.EventId.Value, cancellationToken);
        }

        // Case 2: Metro area targeting (includes "All Locations" when all 84 metros populated)
        // Phase 6A.85: This is now the PRIMARY path for targetAllLocations=true newsletters
        if (newsletter.MetroAreaIds.Any())
        {
            _logger.LogInformation(
                "[Phase 6A.85] Newsletter targets {Count} metro area(s). TargetAllLocations={TargetAllLocations}",
                newsletter.MetroAreaIds.Count,
                newsletter.TargetAllLocations);

            return await GetSubscribersByMetroAreasAsync(newsletter.MetroAreaIds, cancellationToken);
        }

        // Case 3: Fallback for "All Locations" if metros weren't populated (should not happen post-Phase 6A.85)
        if (newsletter.TargetAllLocations)
        {
            _logger.LogWarning(
                "[Phase 6A.85] Newsletter has TargetAllLocations=true but no MetroAreaIds. " +
                "This indicates a bug - Phase 6A.85 fix should populate all 84 metros. " +
                "Falling back to receive_all_locations subscribers (legacy behavior).");
            return await GetAllLocationSubscribersAsync(cancellationToken);
        }

        // No location targeting specified (should not happen due to domain validation)
        _logger.LogWarning("[Phase 6A.74] Newsletter {NewsletterId} has no location targeting specified", newsletter.Id);
        return new NewsletterSubscriberBreakdown(new HashSet<string>(), 0, 0, 0);
    }

    /// <summary>
    /// Phase 6A.74 Part 13: Gets subscribers for event-linked newsletters using geo-spatial metro area bucketing
    /// Example: Event in Aurora, OH → Matches Cleveland metro → Returns only Cleveland metro subscribers
    /// Replaces state-level matching (which would return ALL Ohio subscribers)
    /// </summary>
    private async Task<NewsletterSubscriberBreakdown> GetSubscribersForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        // Fetch event to get its location
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (@event == null || @event.Location?.Coordinates == null)
        {
            _logger.LogWarning(
                "[Phase 6A.74 Part 13] Event {EventId} not found or has no location/coordinates",
                eventId);
            return new NewsletterSubscriberBreakdown(new HashSet<string>(), 0, 0, 0);
        }

        _logger.LogInformation(
            "[Phase 6A.74 Part 13] Event location: {City}, {State}, Coordinates: ({Lat}, {Lng})",
            @event.Location.Address?.City,
            @event.Location.Address?.State,
            @event.Location.Coordinates.Latitude,
            @event.Location.Coordinates.Longitude);

        try
        {
            // NEW: Determine which metro areas this event belongs to based on geographic proximity
            var eventMetroIds = await _metroMatcher.GetMetroAreasForEventAsync(@event, cancellationToken);

            if (!eventMetroIds.Any())
            {
                // Fallback: Use state-level matching if no metro match (rural events)
                var state = @event.Location.Address?.State;
                if (string.IsNullOrWhiteSpace(state))
                {
                    _logger.LogWarning(
                        "[Phase 6A.74 Part 13] Event {EventId} has no state for fallback matching",
                        eventId);
                    return new NewsletterSubscriberBreakdown(new HashSet<string>(), 0, 0, 0);
                }

                _logger.LogWarning(
                    "[Phase 6A.74 Part 13] Event {EventId} at ({Lat}, {Lng}) does not fall within any metro area radius. Falling back to state-level matching for {State}.",
                    eventId,
                    @event.Location.Coordinates.Latitude,
                    @event.Location.Coordinates.Longitude,
                    state);

                var stateSubscribers = await _subscriberRepository.GetConfirmedSubscribersByStateAsync(
                    state,
                    cancellationToken);

                var stateEmails = stateSubscribers
                    .Select(s => s.Email.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                return new NewsletterSubscriberBreakdown(
                    Emails: stateEmails,
                    MetroCount: 0,
                    StateCount: stateSubscribers.Count,
                    AllLocationsCount: 0);
            }

            _logger.LogInformation(
                "[Phase 6A.74 Part 13] Event {EventId} belongs to {Count} metro area(s): [{MetroIds}]",
                eventId,
                eventMetroIds.Count,
                string.Join(", ", eventMetroIds));

            // Get subscribers for all matching metro areas
            return await GetSubscribersByMetroAreasAsync(eventMetroIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74 Part 13] Failed to get subscribers for event {EventId}",
                eventId);
            throw;
        }
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
                _logger.LogInformation("[Phase 6A.74] Fetching subscribers for metro area {MetroAreaId}", metroAreaId);

                var subscribers = await _subscriberRepository.GetConfirmedSubscribersByMetroAreaAsync(
                    metroAreaId,
                    cancellationToken);

                _logger.LogInformation("[Phase 6A.74] Found {Count} subscribers for metro area {MetroAreaId}",
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
                NewsletterEmailGroupCount = 0,
                EventEmailGroupCount = 0,
                SubscriberCount = 0,
                EventRegistrationCount = 0,
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
                _logger.LogInformation("[Phase 6A.74 HOTFIX] Event {EventId} has no email groups", eventId);
                return new List<string>();
            }

            _logger.LogInformation("[Phase 6A.74 HOTFIX] Fetching {Count} email groups for event {EventId}: [{Ids}]",
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
