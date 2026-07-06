using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain;

public record UserAccountLockedEvent(Guid UserId, string Email, DateTime LockedUntil) : DomainEvent;