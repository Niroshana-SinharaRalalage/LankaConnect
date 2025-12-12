using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Badges.Queries.GetBadges;

/// <summary>
/// Query to get all badges (optionally filtered by active status)
/// Phase 6A.25: Badge Management System
/// </summary>
public record GetBadgesQuery : IQuery<IReadOnlyList<BadgeDto>>
{
    /// <summary>
    /// If true, only returns active badges
    /// If false, returns all badges (for admin management)
    /// </summary>
    public bool ActiveOnly { get; init; } = true;
}
