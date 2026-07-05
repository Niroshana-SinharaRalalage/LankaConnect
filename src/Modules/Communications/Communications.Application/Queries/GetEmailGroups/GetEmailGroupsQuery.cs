using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Common;
using LankaConnect.Modules.Communications.Application.Common;
namespace LankaConnect.Modules.Communications.Application.Queries.GetEmailGroups;

/// <summary>
/// Query to get email groups
/// Phase 6A.25: Email Groups Management
/// - For regular users: returns only their own groups
/// - For admins: returns all groups across the platform
/// </summary>
public record GetEmailGroupsQuery : IQuery<IReadOnlyList<EmailGroupDto>>
{
    /// <summary>
    /// If true and user is admin, returns all groups across platform
    /// If false or user is not admin, returns only user's own groups
    /// </summary>
    public bool IncludeAll { get; init; } = false;
}
