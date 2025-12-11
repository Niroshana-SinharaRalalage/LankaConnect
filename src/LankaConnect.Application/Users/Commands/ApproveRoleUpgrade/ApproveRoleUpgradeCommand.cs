using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.ApproveRoleUpgrade;

/// <summary>
/// Command to approve a user's role upgrade request
/// Phase 6A.5: Admin Approval Workflow
/// </summary>
public record ApproveRoleUpgradeCommand : ICommand
{
    public Guid UserId { get; init; }

    public ApproveRoleUpgradeCommand(Guid userId)
    {
        UserId = userId;
    }
}
