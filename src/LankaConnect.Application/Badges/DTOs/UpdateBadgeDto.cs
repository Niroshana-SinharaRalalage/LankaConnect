using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for updating a Badge
/// Phase 6A.25: Badge Management System
/// Phase 6A.27: Added ExpiresAt and ClearExpiry for expiry feature
/// </summary>
public record UpdateBadgeDto
{
    public string? Name { get; init; }
    public BadgePosition? Position { get; init; }
    public bool? IsActive { get; init; }
    public int? DisplayOrder { get; init; }

    /// <summary>
    /// Phase 6A.27: Optional expiry date (null = no change, set value = update expiry)
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Phase 6A.27: Set to true to explicitly clear/remove the expiry date
    /// </summary>
    public bool ClearExpiry { get; init; } = false;
}
