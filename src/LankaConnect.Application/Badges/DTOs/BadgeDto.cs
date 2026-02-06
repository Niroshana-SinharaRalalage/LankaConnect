using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for Badge entity
/// Phase 6A.25: Badge Management System
/// Phase 6A.27: Added role-based filtering
/// Phase 6A.28: Changed ExpiresAt to DefaultDurationDays (duration-based expiration)
/// Phase 6A.31a: Added per-location badge configurations
/// </summary>
public record BadgeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// Phase 6A.31a: DEPRECATED - Use ListingConfig, FeaturedConfig, DetailConfig instead
    /// Kept for backward compatibility during two-phase migration
    /// </summary>
    [Obsolete("Use ListingConfig, FeaturedConfig, and DetailConfig instead")]
    public BadgePosition Position { get; init; }

    /// <summary>
    /// Phase 6A.31a: Badge configuration for Events Listing page
    /// </summary>
    public BadgeLocationConfigDto ListingConfig { get; init; } = null!;

    /// <summary>
    /// Phase 6A.31a: Badge configuration for Featured Banner (home page)
    /// </summary>
    public BadgeLocationConfigDto FeaturedConfig { get; init; } = null!;

    /// <summary>
    /// Phase 6A.31a: Badge configuration for Event Detail Hero
    /// </summary>
    public BadgeLocationConfigDto DetailConfig { get; init; } = null!;

    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Phase 6A.28: Default duration in days for badge assignments (null = never expires)
    /// Replaces ExpiresAt from Phase 6A.27
    /// </summary>
    public int? DefaultDurationDays { get; init; }

    /// <summary>
    /// Phase 6A.27: User ID of the creator (null for system badges)
    /// </summary>
    public Guid? CreatedByUserId { get; init; }

    /// <summary>
    /// Phase 6A.27: Display name of the creator (for Admin view)
    /// </summary>
    public string? CreatorName { get; init; }
}
