using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Badges.Queries.GetBadges;

/// <summary>
/// Handler for GetBadgesQuery
/// Phase 6A.25: Returns list of badges for selection or management
/// Phase 6A.27: Added role-based filtering for ForManagement and ForAssignment modes
/// Phase 6A.28: Removed ExpiresAt filtering (badges no longer expire, only EventBadge assignments do)
/// </summary>
public class GetBadgesQueryHandler : IQueryHandler<GetBadgesQuery, IReadOnlyList<BadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetBadgesQueryHandler(
        IBadgeRepository badgeRepository,
        ICurrentUserService currentUserService)
    {
        _badgeRepository = badgeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<BadgeDto>>> Handle(GetBadgesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsAdmin;

        // Get all badges - we'll filter in memory for flexibility
        IEnumerable<Badge> badges;
        if (request.ActiveOnly)
        {
            badges = await _badgeRepository.GetAllActiveAsync(cancellationToken);
        }
        else
        {
            badges = await _badgeRepository.GetAllAsync(cancellationToken);
        }

        // Phase 6A.27: Apply role-based filtering
        if (request.ForManagement)
        {
            // Badge Management UI filtering
            if (isAdmin)
            {
                // Admin sees ALL badges (system + custom) - no additional filtering
                // They can manage everything
            }
            else
            {
                // EventOrganizer sees only their own custom badges
                badges = badges.Where(b => !b.IsSystem && b.CreatedByUserId == userId);
            }
        }
        else if (request.ForAssignment)
        {
            // Badge Assignment UI filtering
            // Phase 6A.28: Badges no longer expire - only EventBadge assignments do
            // All active badges are available for assignment

            if (isAdmin)
            {
                // Admin sees system badges only (for their own events)
                badges = badges.Where(b => b.IsSystem);
            }
            else
            {
                // EventOrganizer sees: their own custom badges + all system badges
                badges = badges.Where(b => b.IsSystem || b.CreatedByUserId == userId);
            }
        }
        // Default behavior (no ForManagement/ForAssignment specified) - just return based on ActiveOnly

        // Map to DTOs using the extension method
        var dtos = badges
            .OrderBy(b => b.DisplayOrder)
            .ThenBy(b => b.Name)
            .Select(b => b.ToBadgeDto())
            .ToList();

        return Result<IReadOnlyList<BadgeDto>>.Success(dtos);
    }
}
