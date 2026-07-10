using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Communications.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a newsletter subscription is cancelled/unsubscribed
/// </summary>
public sealed record NewsletterSubscriptionCancelledEvent(
    Guid SubscriberId,
    string Email,
    Guid MetroAreaId) : DomainEvent;
