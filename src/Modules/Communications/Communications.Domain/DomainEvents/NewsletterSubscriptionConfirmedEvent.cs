using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Communications.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a newsletter subscription is confirmed
/// </summary>
public sealed record NewsletterSubscriptionConfirmedEvent(
    Guid SubscriberId,
    string Email,
    Guid MetroAreaId) : DomainEvent;
