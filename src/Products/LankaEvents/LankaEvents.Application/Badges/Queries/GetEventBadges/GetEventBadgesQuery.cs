using LankaConnect.Products.LankaEvents.Application.Badges.DTOs;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Badges.Queries.GetEventBadges;

/// <summary>
/// Query to get all badges assigned to an event
/// Phase 6A.25: Badge Management System
/// </summary>
public record GetEventBadgesQuery : IQuery<IReadOnlyList<EventBadgeDto>>
{
    public Guid EventId { get; init; }
}
