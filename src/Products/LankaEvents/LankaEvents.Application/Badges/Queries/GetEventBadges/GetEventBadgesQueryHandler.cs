using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
namespace LankaConnect.Products.LankaEvents.Application.Badges.Queries.GetEventBadges;

/// <summary>
/// Handler for GetEventBadgesQuery
/// Returns all badges assigned to an event.
///
/// Wave 6.5.f.5-hotfix2d (2026-07-04): repaired cross-module hydration per architect
/// ruling §6 (Rule 5j.3). The previous implementation dereferenced <c>eb.Badge</c>
/// via EF navigation, which worked when EventRepository routed through AppDbContext
/// (Badge mapped as principal). After Wave 6.5.f.5 cut EventRepository to
/// LankaEventsDbContext + hotfix1 §2.2 deleted the HasOne(eb =&gt; eb.Badge) block
/// from the moved config, LankaEventsDbContext maps EventBadge but not Badge —
/// <c>eb.Badge</c> materializes as null for every row. The <c>.Where(eb =&gt; eb.Badge != null)</c>
/// filter then dropped everything and the endpoint silently returned empty lists.
///
/// Fix: hydrate Badge details at the application layer via <see cref="IBadgeRepository"/>
/// (which routes through AppDbContext where Badge is mapped). Cross-module read via
/// Contracts-projection, not via EF navigation — Blueprint §7.8 pattern that hotfix1
/// intended for this path but the caller was never updated.
/// </summary>
public class GetEventBadgesQueryHandler : IQueryHandler<GetEventBadgesQuery, IReadOnlyList<EventBadgeDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IBadgeRepository _badgeRepository;

    public GetEventBadgesQueryHandler(
        IEventRepository eventRepository,
        IBadgeRepository badgeRepository)
    {
        _eventRepository = eventRepository;
        _badgeRepository = badgeRepository;
    }

    public async Task<Result<IReadOnlyList<EventBadgeDto>>> Handle(GetEventBadgesQuery request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (@event == null)
            return Result<IReadOnlyList<EventBadgeDto>>.Failure($"Event with ID {request.EventId} not found");

        var eventBadges = @event.Badges.ToList();
        if (eventBadges.Count == 0)
            return Result<IReadOnlyList<EventBadgeDto>>.Success((IReadOnlyList<EventBadgeDto>)new List<EventBadgeDto>());

        var badgeIds = eventBadges.Select(eb => eb.BadgeId).Distinct().ToList();
        var badges = await _badgeRepository.FindAsync(b => badgeIds.Contains(b.Id), cancellationToken);
        var badgeById = badges.ToDictionary(b => b.Id);

        var dtos = eventBadges
            .Where(eb => badgeById.ContainsKey(eb.BadgeId))
            .Select(eb => new EventBadgeDto
            {
                Id = eb.Id,
                EventId = eb.EventId,
                BadgeId = eb.BadgeId,
                Badge = badgeById[eb.BadgeId].ToBadgeDto(),
                AssignedAt = eb.AssignedAt,
                AssignedByUserId = eb.AssignedByUserId
            })
            .ToList();

        return Result<IReadOnlyList<EventBadgeDto>>.Success(dtos);
    }
}
