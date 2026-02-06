using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when a newsletter subscription is confirmed
/// </summary>
public sealed record NewsletterSubscriptionConfirmedEvent(
    Guid SubscriberId,
    string Email,
    Guid MetroAreaId) : DomainEvent;
