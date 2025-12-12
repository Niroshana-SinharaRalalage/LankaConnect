using LankaConnect.Domain.Common;
using LankaConnect.Domain.Badges;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Join entity representing a badge assigned to an event
/// Enables many-to-many relationship between Events and Badges
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
    /// Navigation property to the Badge entity
    /// </summary>
    public Badge? Badge { get; private set; }

    // EF Core constructor
    private EventBadge()
    {
    }

    private EventBadge(Guid eventId, Guid badgeId, Guid assignedByUserId)
    {
        EventId = eventId;
        BadgeId = badgeId;
        AssignedByUserId = assignedByUserId;
        AssignedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to create a new EventBadge assignment
    /// </summary>
    public static Result<EventBadge> Create(Guid eventId, Guid badgeId, Guid assignedByUserId)
    {
        if (eventId == Guid.Empty)
            return Result<EventBadge>.Failure("Event ID is required");

        if (badgeId == Guid.Empty)
            return Result<EventBadge>.Failure("Badge ID is required");

        if (assignedByUserId == Guid.Empty)
            return Result<EventBadge>.Failure("Assigned by user ID is required");

        var eventBadge = new EventBadge(eventId, badgeId, assignedByUserId);
        return Result<EventBadge>.Success(eventBadge);
    }
}
