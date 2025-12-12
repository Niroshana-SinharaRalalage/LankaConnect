using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.RemoveBadgeFromEvent;

/// <summary>
/// Command to remove a badge from an event
/// Phase 6A.25: Badge Management System
/// </summary>
public record RemoveBadgeFromEventCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid BadgeId { get; init; }
}
