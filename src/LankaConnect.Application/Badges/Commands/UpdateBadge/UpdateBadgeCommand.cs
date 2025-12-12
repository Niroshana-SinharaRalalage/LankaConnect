using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Domain.Badges.Enums;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.UpdateBadge;

/// <summary>
/// Command to update an existing badge
/// Phase 6A.25: Badge Management System
/// </summary>
public record UpdateBadgeCommand : IRequest<Result<BadgeDto>>
{
    public Guid BadgeId { get; init; }
    public string? Name { get; init; }
    public BadgePosition? Position { get; init; }
    public bool? IsActive { get; init; }
    public int? DisplayOrder { get; init; }
}
