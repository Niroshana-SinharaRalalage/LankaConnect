using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain;

public record UserEmailVerifiedEvent(Guid UserId, string Email) : DomainEvent;