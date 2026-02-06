namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// DTO for previewing newsletter recipients before sending
/// Phase 6A.74: Newsletter recipient resolution
/// </summary>
public record RecipientPreviewDto
{
    /// <summary>
    /// Total unique recipients (deduplicated)
    /// </summary>
    public int TotalRecipients { get; init; }

    /// <summary>
    /// All unique email addresses that will receive the newsletter
    /// </summary>
    public IReadOnlyList<string> EmailAddresses { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Breakdown of recipient sources
    /// </summary>
    public RecipientBreakdownDto Breakdown { get; init; } = new();
}

/// <summary>
/// Breakdown of newsletter recipient sources
/// Phase 6A.74 Part 13+: Updated to track all 4 recipient sources separately
/// </summary>
public record RecipientBreakdownDto
{
    /// <summary>
    /// Count of recipients from newsletter's email groups
    /// </summary>
    public int NewsletterEmailGroupCount { get; init; }

    /// <summary>
    /// Count of recipients from event's email groups (if newsletter linked to event)
    /// </summary>
    public int EventEmailGroupCount { get; init; }

    /// <summary>
    /// Count of newsletter subscribers (metro + state + all locations combined)
    /// </summary>
    public int SubscriberCount { get; init; }

    /// <summary>
    /// Count of event registered attendees (if newsletter linked to event)
    /// </summary>
    public int EventRegistrationCount { get; init; }

    /// <summary>
    /// Count of newsletter subscribers matched by metro area
    /// </summary>
    public int MetroAreaSubscribers { get; init; }

    /// <summary>
    /// Count of newsletter subscribers matched by state (fallback from metro)
    /// </summary>
    public int StateLevelSubscribers { get; init; }

    /// <summary>
    /// Count of newsletter subscribers matched by "all locations"
    /// </summary>
    public int AllLocationsSubscribers { get; init; }

    /// <summary>
    /// Total newsletter subscribers (sum of metro + state + all locations)
    /// </summary>
    public int TotalNewsletterSubscribers => MetroAreaSubscribers + StateLevelSubscribers + AllLocationsSubscribers;

    /// <summary>
    /// Legacy property for backwards compatibility - total from all email groups
    /// </summary>
    public int EmailGroupCount => NewsletterEmailGroupCount + EventEmailGroupCount;
}
