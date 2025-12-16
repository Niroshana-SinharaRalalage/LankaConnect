using LankaConnect.Domain.Badges;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// Extension methods for mapping Badge domain entity to DTOs
/// Phase 6A.27: Centralized mapping logic
/// Phase 6A.28: Changed to duration-based expiration model
/// Phase 6A.31a: Added per-location configuration mapping
/// </summary>
public static class BadgeMappingExtensions
{
    /// <summary>
    /// Maps a Badge domain entity to BadgeDto
    /// </summary>
    /// <param name="badge">The badge entity to map</param>
    /// <param name="creatorName">Optional creator name for display (populated by query)</param>
    public static BadgeDto ToBadgeDto(this Badge badge, string? creatorName = null)
    {
        return new BadgeDto
        {
            Id = badge.Id,
            Name = badge.Name,
            ImageUrl = badge.ImageUrl,

            // Phase 6A.31a: Suppress obsolete warning for backward compatibility during migration
#pragma warning disable CS0618
            Position = badge.Position,
#pragma warning restore CS0618

            // Phase 6A.31a: Map per-location configurations
            ListingConfig = badge.ListingConfig.ToDto(),
            FeaturedConfig = badge.FeaturedConfig.ToDto(),
            DetailConfig = badge.DetailConfig.ToDto(),

            IsActive = badge.IsActive,
            IsSystem = badge.IsSystem,
            DisplayOrder = badge.DisplayOrder,
            CreatedAt = badge.CreatedAt,
            DefaultDurationDays = badge.DefaultDurationDays,
            CreatedByUserId = badge.CreatedByUserId,
            CreatorName = creatorName
        };
    }

    /// <summary>
    /// Maps BadgeLocationConfig value object to DTO
    /// Phase 6A.31a: Per-location configuration mapping
    /// </summary>
    public static BadgeLocationConfigDto ToDto(this BadgeLocationConfig config)
    {
        return new BadgeLocationConfigDto
        {
            PositionX = config.PositionX,
            PositionY = config.PositionY,
            SizeWidth = config.SizeWidth,
            SizeHeight = config.SizeHeight,
            Rotation = config.Rotation
        };
    }
}
