using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain;

public record UserPasswordChangedEvent(Guid UserId, string Email) : DomainEvent;