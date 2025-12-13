using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for updating a Badge
/// Phase 6A.25: Badge Management System
/// Phase 6A.28: Changed from ExpiresAt to DefaultDurationDays (duration-based expiration)
/// </summary>
public record UpdateBadgeDto
{
    public string? Name { get; init; }
    public BadgePosition? Position { get; init; }
    public bool? IsActive { get; init; }
    public int? DisplayOrder { get; init; }

    /// <summary>
    /// Phase 6A.28: Default duration in days for badge assignments (null = no change, set value = update duration)
    /// </summary>
    public int? DefaultDurationDays { get; init; }

    /// <summary>
    /// Phase 6A.28: Set to true to explicitly clear/remove the default duration (making badge never expire)
    /// </summary>
    public bool ClearDuration { get; init; } = false;
}
