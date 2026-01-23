using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    private readonly ILogger<DeleteBadgeCommandHandler> _logger;

    public DeleteBadgeCommandHandler(
        IBadgeRepository badgeRepository,
        ICurrentUserService currentUserService,
        IAzureBlobStorageService blobStorageService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteBadgeCommandHandler> logger)
    {
        _badgeRepository = badgeRepository;
        _currentUserService = currentUserService;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteBadgeCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteBadge"))
        using (LogContext.PushProperty("EntityType", "Badge"))
        using (LogContext.PushProperty("BadgeId", request.BadgeId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteBadge START: BadgeId={BadgeId}, UserId={UserId}, IsAdmin={IsAdmin}",
                request.BadgeId, _currentUserService.UserId, _currentUserService.IsAdmin);

            try
            {
                if (request.BadgeId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBadge FAILED: Invalid BadgeId - BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Badge ID is required");
                }

                // 1. Get existing badge
                var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);
                if (badge == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBadge FAILED: Badge not found - BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Badge with ID {request.BadgeId} not found");
                }

                // 2. Phase 6A.27: Ownership validation
                // - Admins can delete ANY badge (system or custom)
                // - EventOrganizers can only delete their own custom badges
                if (!_currentUserService.IsAdmin)
                {
                    if (badge.IsSystem)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "DeleteBadge FAILED: Non-admin attempting to delete system badge - BadgeId={BadgeId}, UserId={UserId}, IsSystem={IsSystem}, Duration={ElapsedMs}ms",
                            request.BadgeId, _currentUserService.UserId, badge.IsSystem, stopwatch.ElapsedMilliseconds);

                        return Result.Failure("Only administrators can delete system badges");
                    }

                    if (badge.CreatedByUserId != _currentUserService.UserId)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "DeleteBadge FAILED: User attempting to delete another user's badge - BadgeId={BadgeId}, RequestingUserId={UserId}, BadgeCreatorId={CreatorId}, Duration={ElapsedMs}ms",
                            request.BadgeId, _currentUserService.UserId, badge.CreatedByUserId, stopwatch.ElapsedMilliseconds);

                        return Result.Failure("You can only delete your own badges");
                    }
                }

                // 3. Check if badge can be deleted (system badges are deactivated instead)
                if (!badge.CanDelete())
                {
                    // System badges can only be deactivated, not deleted
                    badge.Deactivate();
                    _badgeRepository.Update(badge);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "DeleteBadge COMPLETE: Badge deactivated - BadgeId={BadgeId}, IsSystem={IsSystem}, Duration={ElapsedMs}ms",
                        request.BadgeId, badge.IsSystem, stopwatch.ElapsedMilliseconds);

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

                    _logger.LogInformation(
                        "DeleteBadge: Blob deleted - BadgeId={BadgeId}, BlobName={BlobName}",
                        request.BadgeId, blobName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "DeleteBadge: Blob deletion failed (non-critical) - BadgeId={BadgeId}, BlobName={BlobName}, Error={ErrorMessage}",
                        request.BadgeId, blobName, ex.Message);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteBadge COMPLETE: Badge deleted - BadgeId={BadgeId}, BlobName={BlobName}, Duration={ElapsedMs}ms",
                    request.BadgeId, blobName, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "DeleteBadge FAILED: Exception occurred - BadgeId={BadgeId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.BadgeId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
