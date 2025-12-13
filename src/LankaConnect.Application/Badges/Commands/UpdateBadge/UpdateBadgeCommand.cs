using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Domain.Badges.Enums;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.UpdateBadge;

/// <summary>
/// Command to update an existing badge
/// Phase 6A.25: Badge Management System
/// Phase 6A.28: Changed from ExpiresAt to DefaultDurationDays (duration-based expiration)
/// </summary>
public record UpdateBadgeCommand : IRequest<Result<BadgeDto>>
{
    public Guid BadgeId { get; init; }
    public string? Name { get; init; }
    public BadgePosition? Position { get; init; }
    public bool? IsActive { get; init; }
    public int? DisplayOrder { get; init; }

    /// <summary>
    /// Phase 6A.28: Default duration in days for badge assignments (null = no change, use ClearDuration to remove)
    /// </summary>
    public int? DefaultDurationDays { get; init; }

    /// <summary>
    /// Phase 6A.28: Set to true to explicitly clear/remove the default duration (making badge never expire)
    /// </summary>
    public bool ClearDuration { get; init; } = false;
}
