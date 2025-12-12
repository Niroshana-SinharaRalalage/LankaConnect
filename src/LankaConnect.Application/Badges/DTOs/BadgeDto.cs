using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for Badge entity
/// Phase 6A.25: Badge Management System
/// </summary>
public record BadgeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public BadgePosition Position { get; init; }
    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }
}
