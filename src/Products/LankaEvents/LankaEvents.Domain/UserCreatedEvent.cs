using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain;

public record UserCreatedEvent(Guid UserId, string Email, string FullName) : DomainEvent;