using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.41: Domain event raised when an event is unpublished (returned to Draft status).
/// This allows organizers to make corrections after premature publication.
/// </summary>
public record EventUnpublishedEvent(
    Guid EventId,
    DateTime UnpublishedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
