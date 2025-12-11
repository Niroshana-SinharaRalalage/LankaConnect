using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.MetroAreas.Common;

namespace LankaConnect.Application.Users.Queries.GetUserPreferredMetroAreas;

/// <summary>
/// Query to get user's preferred metro areas with full metro area details
/// Phase 5A: User Preferred Metro Areas
/// </summary>
public record GetUserPreferredMetroAreasQuery : IQuery<IReadOnlyList<MetroAreaDto>>
{
    public Guid UserId { get; init; }

    public GetUserPreferredMetroAreasQuery(Guid userId)
    {
        UserId = userId;
    }
}
