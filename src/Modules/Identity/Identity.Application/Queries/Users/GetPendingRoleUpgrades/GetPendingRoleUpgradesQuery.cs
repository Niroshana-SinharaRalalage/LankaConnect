using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Identity.Application.DTOs;

namespace LankaConnect.Modules.Identity.Application.Queries.Users.GetPendingRoleUpgrades;

/// <summary>
/// Query to get all users with pending role upgrade requests
/// Phase 6A.5: Admin Approval Workflow
/// </summary>
public record GetPendingRoleUpgradesQuery : IQuery<IReadOnlyList<PendingRoleUpgradeDto>>
{
}
