using LankaConnect.Products.LankaEvents.Application.Badges.DTOs;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Badges.Queries.GetBadgeById;

/// <summary>
/// Query to get a badge by its ID
/// Phase 6A.25: Badge Management System
/// </summary>
public record GetBadgeByIdQuery : IQuery<BadgeDto>
{
    public Guid BadgeId { get; init; }
}
