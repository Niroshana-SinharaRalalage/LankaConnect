using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for Badge entity
/// Phase 6A.25: Badge Management System
/// Phase 6A.27: Added role-based filtering
/// Phase 6A.28: Changed ExpiresAt to DefaultDurationDays (duration-based expiration)
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
