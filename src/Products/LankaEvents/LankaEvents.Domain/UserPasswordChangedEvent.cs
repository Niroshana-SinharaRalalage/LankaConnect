using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

public record UserPasswordChangedEvent(Guid UserId, string Email) : DomainEvent;