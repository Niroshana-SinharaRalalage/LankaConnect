using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when a newsletter is reactivated (Inactive → Active)
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public record NewsletterReactivatedEvent(
    Guid NewsletterId,
    DateTime ReactivatedAt,
    DateTime NewExpiresAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
