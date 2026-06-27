using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain;

public record UserPasswordChangedEvent(Guid UserId, string Email) : DomainEvent;