using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Domain.Badges.Enums;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.UpdateBadge;

/// <summary>
/// Command to update an existing badge
/// Phase 6A.25: Badge Management System
/// Phase 6A.27: Added ExpiresAt for badge expiry feature
/// </summary>
public record UpdateBadgeCommand : IRequest<Result<BadgeDto>>
{
    public Guid BadgeId { get; init; }
    public string? Name { get; init; }
    public BadgePosition? Position { get; init; }
    public bool? IsActive { get; init; }
    public int? DisplayOrder { get; init; }

    /// <summary>
    /// Phase 6A.27: Optional expiry date (null = never expires, use ClearExpiry to explicitly remove)
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Phase 6A.27: Set to true to explicitly clear/remove the expiry date
    /// </summary>
    public bool ClearExpiry { get; init; } = false;
}
