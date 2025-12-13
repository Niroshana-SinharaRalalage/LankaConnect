using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.DeleteBadge;

/// <summary>
/// Handler for DeleteBadgeCommand
/// Phase 6A.25: Deletes a badge (system badges are only deactivated)
/// Phase 6A.27: Added ownership validation - Admins can delete any badge, EventOrganizers only their own
/// </summary>
public class DeleteBadgeCommandHandler : IRequestHandler<DeleteBadgeCommand, Result>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBadgeCommandHandler(
        IBadgeRepository badgeRepository,
        ICurrentUserService currentUserService,
        IAzureBlobStorageService blobStorageService,
        IUnitOfWork unitOfWork)
    {
        _badgeRepository = badgeRepository;
        _currentUserService = currentUserService;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBadgeCommand request, CancellationToken cancellationToken)
    {
        // 1. Get existing badge
        var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);
        if (badge == null)
            return Result.Failure($"Badge with ID {request.BadgeId} not found");

        // 2. Phase 6A.27: Ownership validation
        // - Admins can delete ANY badge (system or custom)
        // - EventOrganizers can only delete their own custom badges
        if (!_currentUserService.IsAdmin)
        {
            if (badge.IsSystem)
                return Result.Failure("Only administrators can delete system badges");

            if (badge.CreatedByUserId != _currentUserService.UserId)
                return Result.Failure("You can only delete your own badges");
        }

        // 3. Check if badge can be deleted (system badges are deactivated instead)
        if (!badge.CanDelete())
        {
            // System badges can only be deactivated, not deleted
            badge.Deactivate();
            _badgeRepository.Update(badge);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Success();
        }

        // 4. Store blob name for deletion
        var blobName = badge.BlobName;

        // 5. Delete badge from repository
        _badgeRepository.Remove(badge);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 6. Delete image from blob storage (non-blocking, ignore errors)
        try
        {
            await _blobStorageService.DeleteFileAsync(blobName, "badges", cancellationToken);
        }
        catch
        {
            // Log but don't fail - blob cleanup is best effort
        }

        return Result.Success();
    }
}
