using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Dashboard.Queries.GetCommunityStats;

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

        // Count active businesses only (exclude inactive, suspended, pending approval)
        // Business entity uses BusinessStatus enum (Active = 1)
        var businessCount = await _context.Businesses
            .CountAsync(b => b.Status == Domain.Business.Enums.BusinessStatus.Active, cancellationToken);

        var stats = new CommunityStatsDto
        {
            TotalUsers = userCount,
            TotalEvents = eventCount,
            TotalBusinesses = businessCount
        };

        return Result<CommunityStatsDto>.Success(stats);
    }
}
