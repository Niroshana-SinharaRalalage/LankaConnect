namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for EventBadge entity (badge assigned to an event)
/// Phase 6A.25: Badge Management System
/// Phase 6A.28: Added duration and expiration fields
/// </summary>
public record EventBadgeDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid BadgeId { get; init; }
    public BadgeDto Badge { get; init; } = null!;
    public DateTime AssignedAt { get; init; }
    public Guid AssignedByUserId { get; init; }

    /// <summary>
    /// Phase 6A.28: Duration in days for this specific assignment (may differ from badge default)
    /// null = Never expires
    /// </summary>
    public int? DurationDays { get; init; }

    /// <summary>
    /// Phase 6A.28: Calculated expiration date (AssignedAt + DurationDays)
    /// null = Never expires
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Phase 6A.28: Whether this assignment has expired
    /// </summary>
    public bool IsExpired { get; init; }
}
