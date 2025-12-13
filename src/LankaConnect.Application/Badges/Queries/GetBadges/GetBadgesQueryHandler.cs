using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Badges.Queries.GetBadges;

/// <summary>
/// Handler for GetBadgesQuery
/// Phase 6A.25: Returns list of badges for selection or management
/// Phase 6A.27: Added role-based filtering for ForManagement and ForAssignment modes
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
        var now = DateTime.UtcNow;

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
            // Always exclude expired badges for assignment
            badges = badges.Where(b => !b.ExpiresAt.HasValue || b.ExpiresAt > now);

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
        else
        {
            // Default behavior (no ForManagement/ForAssignment specified)
            // If ActiveOnly, also filter out expired badges
            if (request.ActiveOnly)
            {
                badges = badges.Where(b => !b.ExpiresAt.HasValue || b.ExpiresAt > now);
            }
        }

        // Map to DTOs using the extension method
        var dtos = badges
            .OrderBy(b => b.DisplayOrder)
            .ThenBy(b => b.Name)
            .Select(b => b.ToBadgeDto())
            .ToList();

        return Result<IReadOnlyList<BadgeDto>>.Success(dtos);
    }
}
