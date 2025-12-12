using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Badges.Queries.GetEventBadges;

/// <summary>
/// Handler for GetEventBadgesQuery
/// Returns all badges assigned to an event
/// </summary>
public class GetEventBadgesQueryHandler : IQueryHandler<GetEventBadgesQuery, IReadOnlyList<EventBadgeDto>>
{
    private readonly IEventRepository _eventRepository;

    public GetEventBadgesQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Result<IReadOnlyList<EventBadgeDto>>> Handle(GetEventBadgesQuery request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (@event == null)
            return Result<IReadOnlyList<EventBadgeDto>>.Failure($"Event with ID {request.EventId} not found");

        var dtos = @event.Badges
            .Where(eb => eb.Badge != null)
            .Select(eb => new EventBadgeDto
            {
                Id = eb.Id,
                EventId = eb.EventId,
                BadgeId = eb.BadgeId,
                Badge = new BadgeDto
                {
                    Id = eb.Badge!.Id,
                    Name = eb.Badge.Name,
                    ImageUrl = eb.Badge.ImageUrl,
                    Position = eb.Badge.Position,
                    IsActive = eb.Badge.IsActive,
                    IsSystem = eb.Badge.IsSystem,
                    DisplayOrder = eb.Badge.DisplayOrder,
                    CreatedAt = eb.Badge.CreatedAt
                },
                AssignedAt = eb.AssignedAt,
                AssignedByUserId = eb.AssignedByUserId
            })
            .ToList();

        return Result<IReadOnlyList<EventBadgeDto>>.Success(dtos);
    }
}
