using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

public record UserEmailVerifiedEvent(Guid UserId, string Email) : DomainEvent;