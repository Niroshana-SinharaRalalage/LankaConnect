using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Badges.Queries.GetBadges;

/// <summary>
/// Query to get all badges (optionally filtered by active status)
/// Phase 6A.25: Badge Management System
/// Phase 6A.27: Added ForManagement and ForAssignment parameters for role-based filtering
/// </summary>
public record GetBadgesQuery : IQuery<IReadOnlyList<BadgeDto>>
{
    /// <summary>
    /// If true, only returns active badges
    /// If false, returns all badges (for admin management)
    /// </summary>
    public bool ActiveOnly { get; init; } = true;

    /// <summary>
    /// Phase 6A.27: For Badge Management UI
    /// When true:
    /// - Admin sees ALL badges (system + custom) with type indicators
    /// - EventOrganizer sees only their own custom badges
    /// </summary>
    public bool ForManagement { get; init; } = false;

    /// <summary>
    /// Phase 6A.27: For Badge Assignment UI (assigning badges to events)
    /// When true:
    /// - Admin sees all system badges only
    /// - EventOrganizer sees their own custom badges + all system badges
    /// Also excludes expired badges
    /// </summary>
    public bool ForAssignment { get; init; } = false;
}
