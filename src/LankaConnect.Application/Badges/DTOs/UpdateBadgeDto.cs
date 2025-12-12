using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for updating a Badge
/// Phase 6A.25: Badge Management System
/// </summary>
public record UpdateBadgeDto
{
    public string? Name { get; init; }
    public BadgePosition? Position { get; init; }
    public bool? IsActive { get; init; }
    public int? DisplayOrder { get; init; }
}
