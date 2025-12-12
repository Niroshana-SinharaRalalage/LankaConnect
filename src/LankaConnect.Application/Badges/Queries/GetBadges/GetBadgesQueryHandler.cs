using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Badges.Queries.GetBadges;

/// <summary>
/// Handler for GetBadgesQuery
/// Phase 6A.25: Returns list of badges for selection or management
/// </summary>
public class GetBadgesQueryHandler : IQueryHandler<GetBadgesQuery, IReadOnlyList<BadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgesQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<Result<IReadOnlyList<BadgeDto>>> Handle(GetBadgesQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Badge> badges;

        if (request.ActiveOnly)
        {
            badges = await _badgeRepository.GetAllActiveAsync(cancellationToken);
        }
        else
        {
            badges = await _badgeRepository.GetAllAsync(cancellationToken);
        }

        var dtos = badges
            .OrderBy(b => b.DisplayOrder)
            .ThenBy(b => b.Name)
            .Select(b => new BadgeDto
            {
                Id = b.Id,
                Name = b.Name,
                ImageUrl = b.ImageUrl,
                Position = b.Position,
                IsActive = b.IsActive,
                IsSystem = b.IsSystem,
                DisplayOrder = b.DisplayOrder,
                CreatedAt = b.CreatedAt
            })
            .ToList();

        return Result<IReadOnlyList<BadgeDto>>.Success(dtos);
    }
}
