using LankaConnect.Application.Common.Interfaces;
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
/// </summary>
public class EventNotificationRecipientService : IEventNotificationRecipientService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly INewsletterSubscriberRepository _subscriberRepository;
    private readonly IMetroAreaRepository _metroAreaRepository;
    private readonly ILogger<EventNotificationRecipientService> _logger;

    public EventNotificationRecipientService(
        IEventRepository eventRepository,
        IEmailGroupRepository emailGroupRepository,
        INewsletterSubscriberRepository subscriberRepository,
        IMetroAreaRepository metroAreaRepository,
        ILogger<EventNotificationRecipientService> logger)
    {
        _eventRepository = eventRepository;
        _emailGroupRepository = emailGroupRepository;
        _subscriberRepository = subscriberRepository;
        _metroAreaRepository = metroAreaRepository;
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

            // Query 1: Metro area subscribers
            metroSubscribers = await GetMetroAreaSubscribersAsync(city, state, cancellationToken);

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

    private async Task<IReadOnlyList<NewsletterSubscriber>> GetMetroAreaSubscribersAsync(
        string city,
        string state,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[RCA-MA1] Finding metro area for {City}, {State}", city, state);

        // Find metro area by city and state
        Domain.Events.MetroArea? metroArea;
        try
        {
            metroArea = await _metroAreaRepository.FindByLocationAsync(city, state, cancellationToken);
            _logger.LogInformation("[RCA-MA2] Metro area lookup result: {Found}, Id: {MetroAreaId}",
                metroArea != null, metroArea?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-MA-ERR] Failed to find metro area for {City}, {State}", city, state);
            throw;
        }

        if (metroArea == null)
        {
            _logger.LogInformation("[RCA-MA3] No metro area found for {City}, {State}", city, state);
            return new List<NewsletterSubscriber>();
        }

        // Get subscribers for this metro area
        IReadOnlyList<NewsletterSubscriber> subscribers;
        try
        {
            _logger.LogInformation("[RCA-MA4] Fetching subscribers for metro area {MetroAreaId}", metroArea.Id);
            subscribers = await _subscriberRepository.GetConfirmedSubscribersByMetroAreaAsync(
                metroArea.Id,
                cancellationToken);
            _logger.LogInformation("[RCA-MA5] Found {Count} subscribers for metro area {MetroAreaId}",
                subscribers.Count, metroArea.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RCA-MA-ERR] Failed to get subscribers for metro area {MetroAreaId}", metroArea.Id);
            throw;
        }

        return subscribers;
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
