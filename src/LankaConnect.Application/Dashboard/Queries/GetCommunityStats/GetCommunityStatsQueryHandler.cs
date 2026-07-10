using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Contracts;                   // 4C.e.3: IIdentityQueries
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using MediatR;
namespace LankaConnect.Host.AllInOne.Dashboard.Queries.GetCommunityStats;

/// <summary>
/// Phase 6A.69: Handler for GetCommunityStatsQuery
/// Queries real-time counts from database for landing page hero section
/// Public endpoint - no authentication required
/// Counts only active/published entities to show accurate community size
/// </summary>
public class GetCommunityStatsQueryHandler : IRequestHandler<GetCommunityStatsQuery, Result<CommunityStatsDto>>
{
    private readonly IIdentityQueries _identityQueries;
    private readonly IEventRepository _eventRepository;

    public GetCommunityStatsQueryHandler(
        IIdentityQueries identityQueries,
        IEventRepository eventRepository)
    {
        _identityQueries = identityQueries;
        _eventRepository = eventRepository;
    }

    public async Task<Result<CommunityStatsDto>> Handle(
        GetCommunityStatsQuery request,
        CancellationToken cancellationToken)
    {
        // 4C.e.3 (2026-07-08): route active-user count through the Identity
        // Contracts surface instead of IApplicationDbContext.Users. Per Consult
        // #14 PASS B — LC.Application must not touch Identity DbSets directly.
        var userCount = await _identityQueries.CountActiveUsersAsync(cancellationToken);

        // Count published and active events only (exclude drafts, cancelled, completed)
        // Use repository to get published events
        var publishedEvents = await _eventRepository.GetEventsByStatusAsync(EventStatus.Published, cancellationToken);
        var activeEvents = await _eventRepository.GetEventsByStatusAsync(EventStatus.Active, cancellationToken);
        var eventCount = publishedEvents.Count + activeEvents.Count;

        // Day 4 slot C sub-slice 4C.b (2026-07-06): Business aggregate deleted
        // per Consult #12 Option D. Business count returns 0 until LankaBusiness
        // product surface lands in Phase B.
        var businessCount = 0;

        var stats = new CommunityStatsDto
        {
            TotalUsers = userCount,
            TotalEvents = eventCount,
            TotalBusinesses = businessCount
        };

        return Result<CommunityStatsDto>.Success(stats);
    }
}
