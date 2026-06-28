using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

public record UserLoggedInEvent(Guid UserId, string Email, DateTime LoginTime) : DomainEvent;