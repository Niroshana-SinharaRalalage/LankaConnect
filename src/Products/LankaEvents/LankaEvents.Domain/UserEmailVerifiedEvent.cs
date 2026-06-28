using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain;

public record UserEmailVerifiedEvent(Guid UserId, string Email) : DomainEvent;