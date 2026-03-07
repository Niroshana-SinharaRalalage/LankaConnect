using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a pending photo is rejected by the organizer.
/// </summary>
public record PhotoRejectedDomainEvent(Guid AlbumId, Guid PhotoId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
