using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Badges.Queries.GetBadgeById;

/// <summary>
/// Handler for GetBadgeByIdQuery
/// Phase 6A.25: Returns a single badge by ID
/// </summary>
public class GetBadgeByIdQueryHandler : IQueryHandler<GetBadgeByIdQuery, BadgeDto>
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgeByIdQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<Result<BadgeDto>> Handle(GetBadgeByIdQuery request, CancellationToken cancellationToken)
    {
        var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);

        if (badge == null)
            return Result<BadgeDto>.Failure($"Badge with ID {request.BadgeId} not found");

        var dto = new BadgeDto
        {
            Id = badge.Id,
            Name = badge.Name,
            ImageUrl = badge.ImageUrl,
            Position = badge.Position,
            IsActive = badge.IsActive,
            IsSystem = badge.IsSystem,
            DisplayOrder = badge.DisplayOrder,
            CreatedAt = badge.CreatedAt
        };

        return Result<BadgeDto>.Success(dto);
    }
}
