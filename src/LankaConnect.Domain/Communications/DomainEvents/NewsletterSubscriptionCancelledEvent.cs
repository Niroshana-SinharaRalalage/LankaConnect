using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when a newsletter subscription is cancelled/unsubscribed
/// </summary>
public sealed record NewsletterSubscriptionCancelledEvent(
    Guid SubscriberId,
    string Email,
    Guid MetroAreaId) : DomainEvent;
