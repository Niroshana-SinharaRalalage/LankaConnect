using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.UpdateBadge;

/// <summary>
/// Handler for UpdateBadgeCommand
/// Phase 6A.25: Updates an existing badge
/// </summary>
public class UpdateBadgeCommandHandler : IRequestHandler<UpdateBadgeCommand, Result<BadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBadgeCommandHandler(
        IBadgeRepository badgeRepository,
        IUnitOfWork unitOfWork)
    {
        _badgeRepository = badgeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BadgeDto>> Handle(UpdateBadgeCommand request, CancellationToken cancellationToken)
    {
        // 1. Get existing badge
        var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);
        if (badge == null)
            return Result<BadgeDto>.Failure($"Badge with ID {request.BadgeId} not found");

        // 2. Check for duplicate name if name is being changed
        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != badge.Name)
        {
            var existingBadge = await _badgeRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existingBadge != null && existingBadge.Id != request.BadgeId)
                return Result<BadgeDto>.Failure($"A badge with the name '{request.Name}' already exists");
        }

        // 3. Update badge properties (name, position, displayOrder) - only if values provided
        if (request.Name != null || request.Position != null || request.DisplayOrder != null)
        {
            var updateResult = badge.Update(
                request.Name ?? badge.Name,
                request.Position ?? badge.Position,
                request.DisplayOrder ?? badge.DisplayOrder);

            if (!updateResult.IsSuccess)
                return Result<BadgeDto>.Failure(updateResult.Errors);
        }

        // 4. Handle IsActive separately if provided
        if (request.IsActive.HasValue && request.IsActive.Value != badge.IsActive)
        {
            var activationResult = request.IsActive.Value
                ? badge.Activate()
                : badge.Deactivate();

            if (!activationResult.IsSuccess)
                return Result<BadgeDto>.Failure(activationResult.Errors);
        }

        // 5. Save changes
        _badgeRepository.Update(badge);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 5. Return updated DTO
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
