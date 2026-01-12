namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// Phase 6A.74 Part 3D: Request DTO for creating a newsletter
/// </summary>
public record CreateNewsletterRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<Guid> EmailGroupIds { get; init; } = new();
    public bool IncludeNewsletterSubscribers { get; init; }
    public Guid? EventId { get; init; }
    public List<Guid>? MetroAreaIds { get; init; }
    public bool TargetAllLocations { get; init; }
}
