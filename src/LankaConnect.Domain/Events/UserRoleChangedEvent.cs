using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

public record UserRoleChangedEvent(Guid UserId, string Email, UserRole OldRole, UserRole NewRole) : DomainEvent;