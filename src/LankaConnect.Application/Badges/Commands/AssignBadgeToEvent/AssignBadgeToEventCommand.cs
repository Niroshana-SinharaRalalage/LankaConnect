using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.AssignBadgeToEvent;

/// <summary>
/// Command to assign a badge to an event
/// Phase 6A.25: Badge Management System
/// </summary>
public record AssignBadgeToEventCommand : IRequest<Result<EventBadgeDto>>
{
    public Guid EventId { get; init; }
    public Guid BadgeId { get; init; }
}
