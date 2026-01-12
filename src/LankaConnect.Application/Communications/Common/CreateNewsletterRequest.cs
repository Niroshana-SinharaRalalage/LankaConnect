namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// Request DTO for creating a newsletter
/// Phase 6A.74: Newsletter creation with location targeting
/// </summary>
public record CreateNewsletterRequest
{
    /// <summary>
    /// Newsletter title (max 200 characters)
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Newsletter description/content (max 5000 characters)
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Email groups to send newsletter to
    /// </summary>
    public List<Guid>? EmailGroupIds { get; init; }

    /// <summary>
    /// Include newsletter subscribers as recipients
    /// </summary>
    public bool IncludeNewsletterSubscribers { get; init; }

    /// <summary>
    /// Optional event ID if newsletter is event-related
    /// </summary>
    public Guid? EventId { get; init; }

    /// <summary>
    /// Target all locations (for non-event newsletters with subscribers)
    /// Phase 6A.74 Enhancement 1
    /// </summary>
    public bool TargetAllLocations { get; init; }

    /// <summary>
    /// Specific metro areas to target (for non-event newsletters with subscribers)
    /// Phase 6A.74 Enhancement 1
    /// </summary>
    public List<Guid>? MetroAreaIds { get; init; }
}
