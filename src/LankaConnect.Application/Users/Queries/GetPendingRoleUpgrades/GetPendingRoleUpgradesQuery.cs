using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;

namespace LankaConnect.Application.Users.Queries.GetPendingRoleUpgrades;

/// <summary>
/// Query to get all users with pending role upgrade requests
/// Phase 6A.5: Admin Approval Workflow
/// </summary>
public record GetPendingRoleUpgradesQuery : IQuery<IReadOnlyList<PendingRoleUpgradeDto>>
{
}
