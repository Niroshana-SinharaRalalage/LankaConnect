using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for creating a new Badge
/// Phase 6A.25: Badge Management System
/// </summary>
public record CreateBadgeDto
{
    public string Name { get; init; } = string.Empty;
    public BadgePosition Position { get; init; } = BadgePosition.TopRight;
}
