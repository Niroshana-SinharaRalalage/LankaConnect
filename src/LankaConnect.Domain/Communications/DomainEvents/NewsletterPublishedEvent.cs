using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when a newsletter is published (Draft → Active)
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public record NewsletterPublishedEvent(
    Guid NewsletterId,
    DateTime PublishedAt,
    DateTime ExpiresAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
