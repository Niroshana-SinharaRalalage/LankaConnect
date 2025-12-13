using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.UpdateBadge;

/// <summary>
/// Handler for UpdateBadgeCommand
/// Phase 6A.25: Updates an existing badge
/// Phase 6A.27: Added ownership validation and expiry update
/// </summary>
public class UpdateBadgeCommandHandler : IRequestHandler<UpdateBadgeCommand, Result<BadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBadgeCommandHandler(
        IBadgeRepository badgeRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _badgeRepository = badgeRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BadgeDto>> Handle(UpdateBadgeCommand request, CancellationToken cancellationToken)
    {
        // 1. Get existing badge
        var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);
        if (badge == null)
            return Result<BadgeDto>.Failure($"Badge with ID {request.BadgeId} not found");

        // 2. Phase 6A.27: Ownership validation
        // - Admins can edit ANY badge (system or custom)
        // - EventOrganizers can only edit their own custom badges
        if (!_currentUserService.IsAdmin)
        {
            if (badge.IsSystem)
                return Result<BadgeDto>.Failure("Only administrators can edit system badges");

            if (badge.CreatedByUserId != _currentUserService.UserId)
                return Result<BadgeDto>.Failure("You can only edit your own badges");
        }

        // 3. Check for duplicate name if name is being changed
        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != badge.Name)
        {
            var existingBadge = await _badgeRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existingBadge != null && existingBadge.Id != request.BadgeId)
                return Result<BadgeDto>.Failure($"A badge with the name '{request.Name}' already exists");
        }

        // 4. Update badge properties (name, position, displayOrder) - only if values provided
        if (request.Name != null || request.Position != null || request.DisplayOrder != null)
        {
            var updateResult = badge.Update(
                request.Name ?? badge.Name,
                request.Position ?? badge.Position,
                request.DisplayOrder ?? badge.DisplayOrder);

            if (!updateResult.IsSuccess)
                return Result<BadgeDto>.Failure(updateResult.Errors);
        }

        // 5. Handle IsActive separately if provided
        if (request.IsActive.HasValue && request.IsActive.Value != badge.IsActive)
        {
            var activationResult = request.IsActive.Value
                ? badge.Activate()
                : badge.Deactivate();

            if (!activationResult.IsSuccess)
                return Result<BadgeDto>.Failure(activationResult.Errors);
        }

        // 6. Phase 6A.28: Handle default duration update
        if (request.ClearDuration)
        {
            badge.UpdateDefaultDuration(null);
        }
        else if (request.DefaultDurationDays.HasValue)
        {
            var durationResult = badge.UpdateDefaultDuration(request.DefaultDurationDays.Value);
            if (!durationResult.IsSuccess)
                return Result<BadgeDto>.Failure(durationResult.Errors);
        }

        // 7. Save changes
        _badgeRepository.Update(badge);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 8. Return updated DTO using mapping extension
        return Result<BadgeDto>.Success(badge.ToBadgeDto());
    }
}
