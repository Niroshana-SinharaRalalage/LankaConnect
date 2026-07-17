using LankaConnect.BuildingBlocks.Domain;
using MediatR;

namespace LankaConnect.Host.AllInOne.Dashboard.Queries.GetCommunityStats;

// Wave 8.5.a (2026-07-17, Consult #26 Q1 hint "fold into Host"). Relocated from
// legacy src/LankaConnect.Application/Dashboard/Queries/GetCommunityStats/ as
// part of LankaConnect.Application csproj dismantle. The query aggregates
// cross-module read-side counts (Users via IIdentityQueries + Events via
// IEventRepository) and is only consumed by the API host's PublicController;
// keeping it inside the host assembly avoids a new Dashboard.Contracts +
// Dashboard.Application capability pair for a single query. MediatR discovery
// still works because AddApplication() scans the executing assembly (which IS
// LankaConnect.API for this DI extension method).

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
