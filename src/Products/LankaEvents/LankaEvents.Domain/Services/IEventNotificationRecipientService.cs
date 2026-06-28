namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Domain service for resolving email recipients for event notifications
/// Encapsulates business logic for 3-level location matching and deduplication
/// </summary>
public interface IEventNotificationRecipientService
{
    /// <summary>
    /// Resolves all email recipients for an event notification
    /// Combines event email groups and location-matched newsletter subscribers
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deduplicated email recipients with breakdown statistics</returns>
    Task<EventNotificationRecipients> ResolveRecipientsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result containing deduplicated email addresses and breakdown statistics
/// </summary>
public sealed record EventNotificationRecipients(
    IReadOnlySet<string> EmailAddresses,
    RecipientBreakdown Breakdown);

/// <summary>
/// Breakdown statistics showing source of email recipients
/// </summary>
public sealed record RecipientBreakdown(
    int EmailGroupCount,
    int MetroAreaSubscribers,
    int StateLevelSubscribers,
    int AllLocationsSubscribers,
    int TotalUnique);
