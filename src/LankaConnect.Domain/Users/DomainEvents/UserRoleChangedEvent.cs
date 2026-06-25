using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Users.DomainEvents;

public record UserRoleChangedEvent(Guid UserId, string Email, UserRole OldRole, UserRole NewRole) : DomainEvent;