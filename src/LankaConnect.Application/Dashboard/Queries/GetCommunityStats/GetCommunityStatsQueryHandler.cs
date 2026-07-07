using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace LankaConnect.Host.AllInOne.Dashboard.Queries.GetCommunityStats;

/// <summary>
/// Phase 6A.69: Handler for GetCommunityStatsQuery
/// Queries real-time counts from database for landing page hero section
/// Public endpoint - no authentication required
/// Counts only active/published entities to show accurate community size
/// </summary>
public class GetCommunityStatsQueryHandler : IRequestHandler<GetCommunityStatsQuery, Result<CommunityStatsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventRepository _eventRepository;

    public GetCommunityStatsQueryHandler(
        IApplicationDbContext context,
        IEventRepository eventRepository)
    {
        _context = context;
        _eventRepository = eventRepository;
    }

    public async Task<Result<CommunityStatsDto>> Handle(
        GetCommunityStatsQuery request,
        CancellationToken cancellationToken)
    {
        // Count active users only (exclude inactive accounts)
        var userCount = await _context.Users
            .CountAsync(u => u.IsActive, cancellationToken);

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
