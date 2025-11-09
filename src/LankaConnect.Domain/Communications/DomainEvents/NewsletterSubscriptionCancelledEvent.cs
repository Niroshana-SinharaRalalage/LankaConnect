using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when a newsletter subscription is cancelled
/// </summary>
public record NewsletterSubscriptionCancelledEvent(
    Guid SubscriberId,
    string Email,
    Guid? MetroAreaId
) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
