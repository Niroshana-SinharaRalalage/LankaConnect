namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// DTO for organizer contact information
/// </summary>
public record OrganizerContactDto
{
    public Guid Id { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }

    /// <summary>
    /// Phase 6A.133: The linked user ID for co-organizer management.
    /// Null if no user is linked to this contact.
    /// </summary>
    public Guid? LinkedUserId { get; init; }
}
