using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.Enums;

namespace LankaConnect.Modules.Identity.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a user links an external social provider to their account
/// </summary>
public record ExternalProviderLinkedEvent(
    Guid UserId,
    FederatedProvider Provider,
    string ExternalProviderId,
    string ProviderEmail) : DomainEvent;
