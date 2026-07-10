using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.CancelRoleUpgrade;

/// <summary>
/// Command to cancel a pending role upgrade request
/// Phase 6A.7: User Upgrade Workflow - User-initiated cancellation
/// </summary>
public record CancelRoleUpgradeCommand : ICommand
{
    public CancelRoleUpgradeCommand()
    {
    }
}
