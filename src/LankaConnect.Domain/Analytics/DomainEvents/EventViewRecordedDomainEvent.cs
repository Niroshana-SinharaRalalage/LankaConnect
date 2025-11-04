using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Analytics.DomainEvents;

/// <summary>
/// Domain event raised when an event view is recorded
/// Used for background processing of unique viewer calculations
/// </summary>
public record EventViewRecordedDomainEvent(Guid EventId, Guid? UserId, string IpAddress) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
