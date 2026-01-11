using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when a new newsletter is created
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public record NewsletterCreatedEvent(
    Guid NewsletterId,
    Guid CreatedByUserId,
    string Title,
    Guid? EventId
) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
