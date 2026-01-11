using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when a new newsletter subscription is created
/// </summary>
public sealed record NewsletterSubscriptionCreatedEvent(
    Guid SubscriberId,
    string Email,
    Guid MetroAreaId,
    bool ReceiveAllLocations) : DomainEvent;
