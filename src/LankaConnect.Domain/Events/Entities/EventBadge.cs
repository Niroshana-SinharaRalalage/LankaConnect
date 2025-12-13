using LankaConnect.Domain.Common;
using LankaConnect.Domain.Badges;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Join entity representing a badge assigned to an event
/// Enables many-to-many relationship between Events and Badges
/// Phase 6A.28: Added duration-based expiration per assignment
/// </summary>
public class EventBadge : BaseEntity
{
    /// <summary>
    /// The event that this badge is assigned to
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// The badge that is assigned to the event
    /// </summary>
    public Guid BadgeId { get; private set; }

    /// <summary>
    /// When the badge was assigned to the event
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// User who assigned the badge to the event
    /// </summary>
    public Guid AssignedByUserId { get; private set; }

    /// <summary>
    /// Duration in days for this specific assignment (may differ from badge default)
    /// null = Never expires
    /// Phase 6A.28
    /// </summary>
    public int? DurationDays { get; private set; }

    /// <summary>
    /// Calculated expiration date: AssignedAt + DurationDays
    /// null = Never expires
    /// Phase 6A.28
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Navigation property to the Badge entity
    /// </summary>
    public Badge? Badge { get; private set; }

    // EF Core constructor
    private EventBadge()
    {
    }

    private EventBadge(Guid eventId, Guid badgeId, Guid assignedByUserId, int? durationDays)
    {
        EventId = eventId;
        BadgeId = badgeId;
        AssignedByUserId = assignedByUserId;
        AssignedAt = DateTime.UtcNow;
        DurationDays = durationDays;
        ExpiresAt = durationDays.HasValue
            ? DateTime.UtcNow.AddDays(durationDays.Value)
            : null;
    }

    /// <summary>
    /// Factory method to create a new EventBadge assignment
    /// Phase 6A.28: Added duration support with validation against max duration
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="badgeId">Badge ID</param>
    /// <param name="assignedByUserId">User who is assigning the badge</param>
    /// <param name="durationDays">Duration in days for this assignment (null = never expires)</param>
    /// <param name="maxDurationDays">Maximum allowed duration from badge's DefaultDurationDays</param>
    public static Result<EventBadge> Create(
        Guid eventId,
        Guid badgeId,
        Guid assignedByUserId,
        int? durationDays = null,
        int? maxDurationDays = null)
    {
        if (eventId == Guid.Empty)
            return Result<EventBadge>.Failure("Event ID is required");

        if (badgeId == Guid.Empty)
            return Result<EventBadge>.Failure("Badge ID is required");

        if (assignedByUserId == Guid.Empty)
            return Result<EventBadge>.Failure("Assigned by user ID is required");

        // Validate duration is positive if specified
        if (durationDays.HasValue && durationDays.Value <= 0)
            return Result<EventBadge>.Failure("Duration must be a positive number of days");

        // Validate duration doesn't exceed max (if max is set)
        // Note: If badge has DefaultDurationDays = null (never expires), no max constraint
        if (maxDurationDays.HasValue && durationDays.HasValue
            && durationDays.Value > maxDurationDays.Value)
        {
            return Result<EventBadge>.Failure(
                $"Duration cannot exceed {maxDurationDays.Value} days for this badge");
        }

        var eventBadge = new EventBadge(eventId, badgeId, assignedByUserId, durationDays);
        return Result<EventBadge>.Success(eventBadge);
    }

    /// <summary>
    /// Checks if this assignment has expired
    /// Returns false if ExpiresAt is null (never expires)
    /// Phase 6A.28
    /// </summary>
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}
