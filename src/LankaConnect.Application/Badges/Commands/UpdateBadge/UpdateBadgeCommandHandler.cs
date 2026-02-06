using System.Diagnostics;
using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    private readonly ILogger<UpdateBadgeCommandHandler> _logger;

    public UpdateBadgeCommandHandler(
        IBadgeRepository badgeRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateBadgeCommandHandler> logger)
    {
        _badgeRepository = badgeRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BadgeDto>> Handle(UpdateBadgeCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateBadge"))
        using (LogContext.PushProperty("EntityType", "Badge"))
        using (LogContext.PushProperty("BadgeId", request.BadgeId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateBadge START: BadgeId={BadgeId}, Name={Name}, IsActive={IsActive}, DefaultDurationDays={DefaultDurationDays}, IsAdmin={IsAdmin}",
                request.BadgeId, request.Name, request.IsActive, request.DefaultDurationDays, _currentUserService.IsAdmin);

            try
            {
                if (request.BadgeId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBadge FAILED: Invalid BadgeId - BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result<BadgeDto>.Failure("Badge ID is required");
                }

                // 1. Get existing badge
                var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);
                if (badge == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBadge FAILED: Badge not found - BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result<BadgeDto>.Failure($"Badge with ID {request.BadgeId} not found");
                }

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
            // Phase 6A.31a: Suppress obsolete warning for backward compatibility during migration
#pragma warning disable CS0618
            var updateResult = badge.Update(
                request.Name ?? badge.Name,
                request.Position ?? badge.Position,
                request.DisplayOrder ?? badge.DisplayOrder);
#pragma warning restore CS0618

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

        // 7. Phase 6A.32: Handle location config updates (Fixes Issue #2 - Badge positioning not saved/loaded)
        if (request.ListingConfig != null)
        {
            badge.UpdateListingConfig(request.ListingConfig.FromDto());
        }

        if (request.FeaturedConfig != null)
        {
            badge.UpdateFeaturedConfig(request.FeaturedConfig.FromDto());
        }

        if (request.DetailConfig != null)
        {
            badge.UpdateDetailConfig(request.DetailConfig.FromDto());
        }

                // 8. Save changes
                _badgeRepository.Update(badge);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateBadge COMPLETE: BadgeId={BadgeId}, Name={Name}, IsActive={IsActive}, DefaultDurationDays={DefaultDurationDays}, Duration={ElapsedMs}ms",
                    request.BadgeId, badge.Name, badge.IsActive, badge.DefaultDurationDays, stopwatch.ElapsedMilliseconds);

                // 8. Return updated DTO using mapping extension
                return Result<BadgeDto>.Success(badge.ToBadgeDto());
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateBadge FAILED: Exception occurred - BadgeId={BadgeId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.BadgeId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
