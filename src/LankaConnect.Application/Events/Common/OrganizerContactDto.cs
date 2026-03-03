namespace LankaConnect.Application.Events.Common;

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
}
