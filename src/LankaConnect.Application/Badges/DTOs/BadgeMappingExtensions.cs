using LankaConnect.Domain.Badges;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// Extension methods for mapping Badge domain entity to DTOs
/// Phase 6A.27: Centralized mapping logic
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
            Position = badge.Position,
            IsActive = badge.IsActive,
            IsSystem = badge.IsSystem,
            DisplayOrder = badge.DisplayOrder,
            CreatedAt = badge.CreatedAt,
            ExpiresAt = badge.ExpiresAt,
            IsExpired = badge.IsExpired(),
            CreatedByUserId = badge.CreatedByUserId,
            CreatorName = creatorName
        };
    }
}
