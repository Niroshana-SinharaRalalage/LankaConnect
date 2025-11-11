using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.RejectRoleUpgrade;

/// <summary>
/// Command to reject a user's role upgrade request
/// Phase 6A.5: Admin Approval Workflow
/// </summary>
public record RejectRoleUpgradeCommand : ICommand
{
    public Guid UserId { get; init; }
    public string? Reason { get; init; }

    public RejectRoleUpgradeCommand(Guid userId, string? reason = null)
    {
        UserId = userId;
        Reason = reason;
    }
}
