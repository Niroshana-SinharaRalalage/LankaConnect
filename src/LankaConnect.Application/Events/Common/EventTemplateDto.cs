using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6A.8: Event Template System
/// DTO for event templates that organizers can use to quickly create events
/// </summary>
public record EventTemplateDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public EventCategory Category { get; init; }
    public string ThumbnailSvg { get; init; } = string.Empty;
    public string TemplateDataJson { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
