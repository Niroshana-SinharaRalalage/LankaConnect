using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

public record UserCreatedEvent(Guid UserId, string Email, string FullName) : DomainEvent;