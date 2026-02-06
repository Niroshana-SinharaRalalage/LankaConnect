using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Dashboard.Queries.GetCommunityStats;

/// <summary>
/// Phase 6A.69: Query for public community statistics (hero numbers on landing page)
/// Returns real-time counts from database instead of hardcoded values
/// No authentication required - public endpoint
/// </summary>
public record GetCommunityStatsQuery : IRequest<Result<CommunityStatsDto>>;

/// <summary>
/// DTO for community statistics displayed on landing page
/// Only shows counts greater than zero to avoid showing "0" to users
/// </summary>
public record CommunityStatsDto
{
    public int TotalUsers { get; init; }
    public int TotalEvents { get; init; }
    public int TotalBusinesses { get; init; }
}
