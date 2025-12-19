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
        _logger.LogDebug("Resolving email recipients for event {EventId}", eventId);

        // Fetch event details
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("Event {EventId} not found", eventId);
            return CreateEmptyResult();
        }

        // Get email addresses from email groups
        var emailGroupAddresses = await GetEmailGroupAddressesAsync(@event, cancellationToken);
        _logger.LogDebug("Found {Count} emails from email groups", emailGroupAddresses.Count);

        // Get newsletter subscriber emails (3-level location matching with parallel execution)
        var newsletterAddresses = @event.Location != null
            ? await GetNewsletterSubscriberEmailsAsync(@event.Location, cancellationToken)
            : new NewsletterEmailsWithBreakdown(new HashSet<string>(), 0, 0, 0);
        _logger.LogDebug("Found {Count} emails from newsletter subscribers", newsletterAddresses.Emails.Count);

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
            _logger.LogDebug("Event {@EventId} has no email groups", @event.Id);
            return new List<string>();
        }

        var emailGroups = await _emailGroupRepository.GetByIdsAsync(
            @event.EmailGroupIds,
            cancellationToken);

        var emails = emailGroups
            .SelectMany(g => g.GetEmailList())
            .ToList();

        _logger.LogDebug("Retrieved {Count} emails from {GroupCount} email groups",
            emails.Count, emailGroups.Count);

        return emails;
    }

    private async Task<NewsletterEmailsWithBreakdown> GetNewsletterSubscriberEmailsAsync(
        EventLocation location,
        CancellationToken cancellationToken)
    {
        var city = location.Address.City;
        var state = location.Address.State;

        _logger.LogDebug("Querying newsletter subscribers for location: {City}, {State}", city, state);

        // Execute 3-level location matching queries IN PARALLEL for performance
        var metroTask = GetMetroAreaSubscribersAsync(city, state, cancellationToken);
        var stateTask = _subscriberRepository.GetConfirmedSubscribersByStateAsync(state, cancellationToken);
        var allLocationsTask = _subscriberRepository.GetConfirmedSubscribersForAllLocationsAsync(cancellationToken);

        await Task.WhenAll(metroTask, stateTask, allLocationsTask);

        var metroSubscribers = await metroTask;
        var stateSubscribers = await stateTask;
        var allLocationsSubscribers = await allLocationsTask;

        _logger.LogDebug(
            "Retrieved newsletter subscribers: Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
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
        // Find metro area by city and state
        var metroArea = await _metroAreaRepository.FindByLocationAsync(city, state, cancellationToken);

        if (metroArea == null)
        {
            _logger.LogDebug("No metro area found for {City}, {State}", city, state);
            return new List<NewsletterSubscriber>();
        }

        // Get subscribers for this metro area
        var subscribers = await _subscriberRepository.GetConfirmedSubscribersByMetroAreaAsync(
            metroArea.Id,
            cancellationToken);

        _logger.LogDebug("Found {Count} subscribers for metro area {MetroAreaId}",
            subscribers.Count, metroArea.Id);

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
