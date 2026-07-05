using LankaConnect.BuildingBlocks.Domain;
using MediatR;
namespace LankaConnect.Products.LankaEvents.Application.Badges.Commands.DeleteBadge;

/// <summary>
/// Command to delete a badge
/// Phase 6A.25: Badge Management System
/// Note: System badges cannot be deleted, only deactivated
/// </summary>
public record DeleteBadgeCommand : IRequest<Result>
{
    public Guid BadgeId { get; init; }
}
