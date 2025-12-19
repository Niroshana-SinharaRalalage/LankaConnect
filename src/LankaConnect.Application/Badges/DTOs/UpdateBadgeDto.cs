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

    // Phase 6A.32: Per-location badge positioning (Fixes Issue #2 - Badge positioning not saved/loaded)
    /// <summary>
    /// Badge configuration for Events Listing page (null = no change)
    /// Phase 6A.32: Fixes Issue #2 - Badge positioning not saved/loaded
    /// </summary>
    public BadgeLocationConfigDto? ListingConfig { get; init; }

    /// <summary>
    /// Badge configuration for Featured Banner (null = no change)
    /// Phase 6A.32: Fixes Issue #2 - Badge positioning not saved/loaded
    /// </summary>
    public BadgeLocationConfigDto? FeaturedConfig { get; init; }

    /// <summary>
    /// Badge configuration for Event Detail Hero (null = no change)
    /// Phase 6A.32: Fixes Issue #2 - Badge positioning not saved/loaded
    /// </summary>
    public BadgeLocationConfigDto? DetailConfig { get; init; }
}
