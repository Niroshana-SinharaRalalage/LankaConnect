using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when a newsletter subscription is confirmed
/// </summary>
public record NewsletterSubscriptionConfirmedEvent(
    Guid SubscriberId,
    string Email,
    Guid? MetroAreaId
) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
