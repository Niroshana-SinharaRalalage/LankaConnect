using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value Object representing a user's position on an event's waiting list
/// Immutable to preserve waiting list integrity
/// </summary>
public class WaitingListEntry : ValueObject
{
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public int Position { get; private set; }

    // EF Core constructor
    private WaitingListEntry()
    {
    }

    private WaitingListEntry(Guid userId, DateTime joinedAt, int position)
    {
        UserId = userId;
        JoinedAt = joinedAt;
        Position = position;
    }

    /// <summary>
    /// Factory method to create a new waiting list entry
    /// </summary>
    public static WaitingListEntry Create(Guid userId, int position)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (position <= 0)
            throw new ArgumentException("Position must be greater than 0", nameof(position));

        return new WaitingListEntry(userId, DateTime.UtcNow, position);
    }

    /// <summary>
    /// Updates the position in the waiting list
    /// Used when resequencing after removals
    /// </summary>
    internal void UpdatePosition(int newPosition)
    {
        if (newPosition <= 0)
            throw new ArgumentException("Position must be greater than 0", nameof(newPosition));

        Position = newPosition;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserId;
        yield return JoinedAt;
        yield return Position;
    }
}
