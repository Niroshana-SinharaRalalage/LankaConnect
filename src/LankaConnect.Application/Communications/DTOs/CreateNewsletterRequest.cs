namespace LankaConnect.Application.Communications.DTOs;

/// <summary>
/// DTO for creating a new Newsletter
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public record CreateNewsletterRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<Guid> EmailGroupIds { get; init; } = new();
    public bool IncludeNewsletterSubscribers { get; init; } = true;
    public Guid? EventId { get; init; }
}
