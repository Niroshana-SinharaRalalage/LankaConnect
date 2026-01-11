namespace LankaConnect.Application.Communications.DTOs;

/// <summary>
/// DTO for updating an existing Newsletter (Draft only)
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public record UpdateNewsletterRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<Guid> EmailGroupIds { get; init; } = new();
    public bool IncludeNewsletterSubscribers { get; init; } = true;
    public Guid? EventId { get; init; }
}
