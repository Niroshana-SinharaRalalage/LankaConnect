using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.DeleteBadge;

/// <summary>
/// Command to delete a badge
/// Phase 6A.25: Badge Management System
/// Note: System badges cannot be deleted, only deactivated
/// </summary>
public record DeleteBadgeCommand : IRequest<Result>
{
    public Guid BadgeId { get; init; }
}
