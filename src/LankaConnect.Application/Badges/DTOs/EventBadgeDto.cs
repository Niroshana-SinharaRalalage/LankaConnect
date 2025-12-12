namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for EventBadge entity (badge assigned to an event)
/// Phase 6A.25: Badge Management System
/// </summary>
public record EventBadgeDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid BadgeId { get; init; }
    public BadgeDto Badge { get; init; } = null!;
    public DateTime AssignedAt { get; init; }
    public Guid AssignedByUserId { get; init; }
}
