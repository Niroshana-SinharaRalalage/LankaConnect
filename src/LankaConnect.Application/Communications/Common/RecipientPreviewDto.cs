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
/// </summary>
public record RecipientBreakdownDto
{
    /// <summary>
    /// Count of recipients from email groups
    /// </summary>
    public int EmailGroupCount { get; init; }

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
}
