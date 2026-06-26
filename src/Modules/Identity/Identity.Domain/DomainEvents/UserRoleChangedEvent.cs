using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Domain.Enums;

namespace LankaConnect.Modules.Identity.Domain.DomainEvents;

public record UserRoleChangedEvent(Guid UserId, string Email, UserRole OldRole, UserRole NewRole) : DomainEvent;