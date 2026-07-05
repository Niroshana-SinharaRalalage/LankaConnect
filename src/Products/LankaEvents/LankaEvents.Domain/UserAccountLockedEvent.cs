using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain;

public record UserAccountLockedEvent(Guid UserId, string Email, DateTime LockedUntil) : DomainEvent;