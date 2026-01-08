using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Domain service implementation for resolving email recipients for event notifications
/// Implements 3-level location matching with parallel query execution for performance
/// Phase 6A.70: Enhanced with geo-spatial metro area matching for newsletter subscribers
/// </summary>
public class EventNotificationRecipientService : IEventNotificationRecipientService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly INewsletterSubscriberRepository _subscriberRepository;
    private readonly IMetroAreaRepository _metroAreaRepository;
    private readonly IGeoLocationService _geoLocationService;
    private readonly ILogger<EventNotificationRecipientService> _logger;

    public EventNotificationRecipientService(
        IEventRepository eventRepository,
        IEmailGroupRepository emailGroupRepository,
        INewsletterSubscriberRepository subscriberRepository,
        IMetroAreaRepository metroAreaRepository,
        IGeoLocationService geoLocationService,
        ILogger<EventNotificationRecipientService> logger)
    {
        _eventRepository = eventRepository;
        _emailGroupRepository = emailGroupRepository;
        _subscriberRepository = subscriberRepository;
        _metroAreaRepository = metroAreaRepository;
        _geoLocationService = geoLocationService;
        _logger = logger;
    }

    public async Task<EventNotificationRecipients> ResolveRecipientsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[RCA-1] ResolveRecipientsAsync START - EventId: {EventId}", eventId);

        // Fetch event details
        Event? @event;
        try
        {
            _logger.LogInformation("[RCA-2] Fetching event from repository...");
            @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            _logger.LogInformation("[RCA-3] Event fetch complete - Found: {Found}", @event != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-ERR] Failed to fetch event {EventId} from repository", eventId);
            throw;
        }

        if (@event == null)
        {
            _logger.LogWarning("[RCA-4] Event {EventId} not found", eventId);
            return CreateEmptyResult();
        }

        _logger.LogInformation("[RCA-5] Event details - Title: {Title}, Location: {HasLocation}, EmailGroupIds: {EmailGroupCount}",
            @event.Title?.Value ?? "N/A",
            @event.Location != null,
            @event.EmailGroupIds?.Count ?? 0);

        // Get email addresses from email groups
        List<string> emailGroupAddresses;
        try
        {
            _logger.LogInformation("[RCA-6] Getting email group addresses...");
            emailGroupAddresses = await GetEmailGroupAddressesAsync(@event, cancellationToken);
            _logger.LogInformation("[RCA-7] Email group addresses retrieved: {Count}", emailGroupAddresses.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-ERR] Failed to get email group addresses for event {EventId}", eventId);
            throw;
        }

        // Get newsletter subscriber emails (3-level location matching with parallel execution)
        NewsletterEmailsWithBreakdown newsletterAddresses;
        try
        {
            // Phase 6A.40: Check both Location AND Address for validity
            // EF Core may create a "shell" Location object with null Address for optional owned entities
            // This defensive check prevents NullReferenceException in GetNewsletterSubscriberEmailsAsync
            var hasValidLocation = @event.Location?.Address != null &&
                                   !string.IsNullOrWhiteSpace(@event.Location.Address.City) &&
                                   !string.IsNullOrWhiteSpace(@event.Location.Address.State);

            if (hasValidLocation)
            {
                _logger.LogInformation("[RCA-8] Getting newsletter subscribers for location: {City}, {State}",
                    @event.Location!.Address.City,
                    @event.Location.Address.State);
                newsletterAddresses = await GetNewsletterSubscriberEmailsAsync(@event.Location, cancellationToken);
            }
            else
            {
                _logger.LogInformation("[RCA-8] No valid location on event (Location: {HasLocation}, Address: {HasAddress}), skipping newsletter subscribers",
                    @event.Location != null,
                    @event.Location?.Address != null);
                newsletterAddresses = new NewsletterEmailsWithBreakdown(new HashSet<string>(), 0, 0, 0);
            }
            _logger.LogInformation("[RCA-9] Newsletter subscribers retrieved: {Count}", newsletterAddresses.Emails.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-ERR] Failed to get newsletter subscribers for event {EventId}", eventId);
            throw;
        }

        // Calculate breakdown before deduplication
        var breakdown = new RecipientBreakdown(
            EmailGroupCount: emailGroupAddresses.Count,
            MetroAreaSubscribers: newsletterAddresses.MetroCount,
            StateLevelSubscribers: newsletterAddresses.StateCount,
            AllLocationsSubscribers: newsletterAddresses.AllLocationsCount,
            TotalUnique: 0 // Will be updated after deduplication
        );

        // Consolidate and deduplicate (case-insensitive)
        var allEmails = new HashSet<string>(
            emailGroupAddresses.Concat(newsletterAddresses.Emails),
            StringComparer.OrdinalIgnoreCase);

        // Update breakdown with final unique count
        breakdown = breakdown with { TotalUnique = allEmails.Count };

        _logger.LogInformation(
            "Resolved {TotalUnique} unique email recipients for event {EventId}. " +
            "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
            allEmails.Count, eventId,
            breakdown.EmailGroupCount,
            breakdown.MetroAreaSubscribers,
            breakdown.StateLevelSubscribers,
            breakdown.AllLocationsSubscribers);

        return new EventNotificationRecipients(allEmails, breakdown);
    }

    private async Task<List<string>> GetEmailGroupAddressesAsync(
        Event @event,
        CancellationToken cancellationToken)
    {
        if (@event.EmailGroupIds == null || !@event.EmailGroupIds.Any())
        {
            _logger.LogInformation("[RCA-EG1] Event {EventId} has no email groups", @event.Id);
            return new List<string>();
        }

        _logger.LogInformation("[RCA-EG2] Fetching {Count} email groups: [{Ids}]",
            @event.EmailGroupIds.Count,
            string.Join(", ", @event.EmailGroupIds));

        IReadOnlyList<Domain.Communications.Entities.EmailGroup> emailGroups;
        try
        {
            emailGroups = await _emailGroupRepository.GetByIdsAsync(
                @event.EmailGroupIds,
                cancellationToken);
            _logger.LogInformation("[RCA-EG3] Email groups fetched: {Count}", emailGroups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-EG-ERR] Failed to fetch email groups for IDs: [{Ids}]",
                string.Join(", ", @event.EmailGroupIds));
            throw;
        }

        var emails = emailGroups
            .SelectMany(g => g.GetEmailList())
            .ToList();

        _logger.LogInformation("[RCA-EG4] Retrieved {Count} emails from {GroupCount} email groups",
            emails.Count, emailGroups.Count);

        return emails;
    }

    private async Task<NewsletterEmailsWithBreakdown> GetNewsletterSubscriberEmailsAsync(
        EventLocation location,
        CancellationToken cancellationToken)
    {
        var city = location.Address.City;
        var state = location.Address.State;

        _logger.LogInformation("[RCA-NL1] Querying newsletter subscribers for location: {City}, {State}", city, state);

        // Execute 3-level location matching queries SEQUENTIALLY
        // Note: Cannot run in parallel because DbContext is not thread-safe
        // Each query shares the same scoped DbContext instance
        IReadOnlyList<Domain.Communications.Entities.NewsletterSubscriber> metroSubscribers;
        IReadOnlyList<Domain.Communications.Entities.NewsletterSubscriber> stateSubscribers;
        IReadOnlyList<Domain.Communications.Entities.NewsletterSubscriber> allLocationsSubscribers;

        try
        {
            _logger.LogInformation("[RCA-NL2] Starting subscriber queries...");

            // Query 1: Metro area subscribers (Phase 6A.70: Now uses geo-spatial matching)
            metroSubscribers = await GetMetroAreaSubscribersAsync(city, state, location, cancellationToken);

            // Query 2: State-level subscribers
            stateSubscribers = await _subscriberRepository.GetConfirmedSubscribersByStateAsync(state, cancellationToken);

            // Query 3: All locations subscribers
            allLocationsSubscribers = await _subscriberRepository.GetConfirmedSubscribersForAllLocationsAsync(cancellationToken);

            _logger.LogInformation("[RCA-NL3] All subscriber queries completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-NL-ERR] Failed during subscriber queries for {City}, {State}", city, state);
            throw;
        }

        _logger.LogInformation(
            "[RCA-NL4] Retrieved newsletter subscribers: Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
            metroSubscribers.Count, stateSubscribers.Count, allLocationsSubscribers.Count);

        // Combine all subscriber emails (deduplication will happen at higher level)
        var allSubscribers = metroSubscribers
            .Concat(stateSubscribers)
            .Concat(allLocationsSubscribers)
            .Distinct() // Remove duplicates from overlapping queries
            .ToList();

        var emails = allSubscribers
            .Select(s => s.Email.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new NewsletterEmailsWithBreakdown(
            Emails: emails,
            MetroCount: metroSubscribers.Count,
            StateCount: stateSubscribers.Count,
            AllLocationsCount: allLocationsSubscribers.Count);
    }

    /// <summary>
    /// Phase 6A.70: Gets metro area subscribers using geo-spatial matching
    /// PRIORITY 1: If event has coordinates, use distance-based matching (Aurora → Cleveland)
    /// FALLBACK: Use exact city match if no coordinates (backward compatible)
    /// </summary>
    private async Task<IReadOnlyList<NewsletterSubscriber>> GetMetroAreaSubscribersAsync(
        string city,
        string state,
        EventLocation? location,
        CancellationToken cancellationToken)
    {
        // PRIORITY 1: Geo-spatial matching if coordinates available
        if (location?.Coordinates != null)
        {
            _logger.LogInformation(
                "[RCA-MA1-GEO] Using geo-spatial matching for event at {City}, {State} ({Lat}, {Lon})",
                city, state, location.Coordinates.Latitude, location.Coordinates.Longitude);

            return await GetSubscribersByGeoSpatialMatchingAsync(state, location.Coordinates, cancellationToken);
        }

        // FALLBACK: Exact city match (preserve existing behavior)
        _logger.LogInformation(
            "[RCA-MA1-EXACT] No coordinates available, using exact city match for {City}, {State}",
            city, state);

        Domain.Events.MetroArea? metroArea;
        try
        {
            metroArea = await _metroAreaRepository.FindByLocationAsync(city, state, cancellationToken);
            _logger.LogInformation("[RCA-MA2-EXACT] Metro area lookup result: {Found}, Id: {MetroAreaId}",
                metroArea != null, metroArea?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-MA-ERR] Failed to find metro area for {City}, {State}", city, state);
            throw;
        }

        if (metroArea == null)
        {
            _logger.LogInformation("[RCA-MA3-EXACT] No metro area found for {City}, {State}", city, state);
            return new List<NewsletterSubscriber>();
        }

        IReadOnlyList<NewsletterSubscriber> subscribers;
        try
        {
            _logger.LogInformation("[RCA-MA4-EXACT] Fetching subscribers for metro area {MetroAreaId}", metroArea.Id);
            subscribers = await _subscriberRepository.GetConfirmedSubscribersByMetroAreaAsync(
                metroArea.Id,
                cancellationToken);
            _logger.LogInformation("[RCA-MA5-EXACT] Found {Count} subscribers for metro area {MetroAreaId}",
                subscribers.Count, metroArea.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-MA-ERR] Failed to get subscribers for metro area {MetroAreaId}", metroArea.Id);
            throw;
        }

        return subscribers;
    }

    /// <summary>
    /// Phase 6A.70: NEW - Gets newsletter subscribers using geo-spatial distance matching
    /// Example: Event in Aurora, OH (41.3173°, -81.3460°) matches Cleveland metro subscribers
    /// Cleveland metro center: (41.4993°, -81.6944°), radius: 50 miles → Aurora is ~20 miles away
    /// </summary>
    private async Task<IReadOnlyList<NewsletterSubscriber>> GetSubscribersByGeoSpatialMatchingAsync(
        string state,
        GeoCoordinate coordinates,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[RCA-GEO1] Starting geo-spatial matching for state {State}, coordinates ({Lat}, {Lon})",
            state, coordinates.Latitude, coordinates.Longitude);

        // Step 1: Get all metro areas in the state
        IReadOnlyList<MetroArea> stateMetros;
        try
        {
            stateMetros = await _metroAreaRepository.GetMetroAreasInStateAsync(state, cancellationToken);
            _logger.LogInformation("[RCA-GEO2] Retrieved {Count} metro areas in state {State}",
                stateMetros.Count, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-GEO-ERR] Failed to get metro areas for state {State}", state);
            throw;
        }

        if (!stateMetros.Any())
        {
            _logger.LogInformation("[RCA-GEO3] No metro areas found for state {State}", state);
            return new List<NewsletterSubscriber>();
        }

        // Step 2: Find metro areas within radius of event location
        var matchingMetroIds = new List<Guid>();

        foreach (var metro in stateMetros)
        {
            var isWithinRadius = _geoLocationService.IsWithinMetroRadius(
                (decimal)coordinates.Latitude,
                (decimal)coordinates.Longitude,
                (decimal)metro.CenterLatitude,
                (decimal)metro.CenterLongitude,
                metro.RadiusMiles);

            if (isWithinRadius)
            {
                var distanceKm = _geoLocationService.CalculateDistanceKm(
                    (decimal)metro.CenterLatitude,
                    (decimal)metro.CenterLongitude,
                    (decimal)coordinates.Latitude,
                    (decimal)coordinates.Longitude);

                _logger.LogInformation(
                    "[RCA-GEO4] Event within {MetroName} metro area radius: Distance={DistanceKm:F2}km, Radius={RadiusMiles}mi ({RadiusKm:F2}km)",
                    metro.Name, distanceKm, metro.RadiusMiles, metro.RadiusMiles * 1.60934);

                matchingMetroIds.Add(metro.Id);
            }
        }

        if (!matchingMetroIds.Any())
        {
            _logger.LogInformation(
                "[RCA-GEO5] Event location not within radius of any metro areas in state {State}",
                state);
            return new List<NewsletterSubscriber>();
        }

        _logger.LogInformation(
            "[RCA-GEO6] Event matches {Count} metro areas: [{MetroIds}]",
            matchingMetroIds.Count, string.Join(", ", matchingMetroIds));

        // Step 3: Get subscribers for all matching metro areas
        var allSubscribers = new List<NewsletterSubscriber>();

        foreach (var metroId in matchingMetroIds)
        {
            try
            {
                var subscribers = await _subscriberRepository.GetConfirmedSubscribersByMetroAreaAsync(
                    metroId,
                    cancellationToken);

                _logger.LogInformation(
                    "[RCA-GEO7] Found {Count} subscribers for metro area {MetroId}",
                    subscribers.Count, metroId);

                allSubscribers.AddRange(subscribers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RCA-GEO-ERR] Failed to get subscribers for metro area {MetroId}", metroId);
                // Continue with other metros instead of failing completely
            }
        }

        // Deduplicate subscribers who may be subscribed to multiple matching metros
        var uniqueSubscribers = allSubscribers
            .DistinctBy(s => s.Id)
            .ToList();

        _logger.LogInformation(
            "[RCA-GEO8] Geo-spatial matching complete: {TotalSubscribers} total, {UniqueSubscribers} unique",
            allSubscribers.Count, uniqueSubscribers.Count);

        return uniqueSubscribers;
    }

    private static EventNotificationRecipients CreateEmptyResult()
    {
        return new EventNotificationRecipients(
            new HashSet<string>(),
            new RecipientBreakdown(0, 0, 0, 0, 0));
    }

    /// <summary>
    /// Internal helper record to track breakdown during newsletter subscriber resolution
    /// </summary>
    private sealed record NewsletterEmailsWithBreakdown(
        HashSet<string> Emails,
        int MetroCount,
        int StateCount,
        int AllLocationsCount);
}
