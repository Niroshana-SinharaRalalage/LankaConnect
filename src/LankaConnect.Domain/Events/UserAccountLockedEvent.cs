using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

public record UserAccountLockedEvent(Guid UserId, string Email, DateTime LockedUntil) : DomainEvent;