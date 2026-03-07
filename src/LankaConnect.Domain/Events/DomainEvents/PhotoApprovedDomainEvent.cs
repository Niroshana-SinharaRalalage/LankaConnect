using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a pending photo is approved by the organizer.
/// </summary>
public record PhotoApprovedDomainEvent(Guid AlbumId, Guid PhotoId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
