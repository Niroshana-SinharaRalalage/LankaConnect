using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain;

public record UserCreatedEvent(Guid UserId, string Email, string FullName) : DomainEvent;