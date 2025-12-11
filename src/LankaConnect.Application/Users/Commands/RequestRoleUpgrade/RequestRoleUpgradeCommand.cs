using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Commands.RequestRoleUpgrade;

/// <summary>
/// Command to request a role upgrade to Event Organizer
/// Phase 6A.7: User Upgrade Workflow - User-initiated request
/// </summary>
public record RequestRoleUpgradeCommand : ICommand
{
    public UserRole TargetRole { get; init; }
    public string Reason { get; init; } = null!;

    public RequestRoleUpgradeCommand(UserRole targetRole, string reason)
    {
        TargetRole = targetRole;
        Reason = reason;
    }
}
