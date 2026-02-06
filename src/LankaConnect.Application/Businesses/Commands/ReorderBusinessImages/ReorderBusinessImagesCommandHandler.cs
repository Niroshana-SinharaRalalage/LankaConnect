using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Businesses.Commands.ReorderBusinessImages;

/// <summary>
/// Handler for reordering business images
/// </summary>
public sealed class ReorderBusinessImagesCommandHandler : IRequestHandler<ReorderBusinessImagesCommand, Result>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReorderBusinessImagesCommandHandler> _logger;

    public ReorderBusinessImagesCommandHandler(
        IBusinessRepository businessRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReorderBusinessImagesCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ReorderBusinessImagesCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ReorderBusinessImages"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", request.BusinessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ReorderBusinessImages START: BusinessId={BusinessId}, ImageCount={ImageCount}",
                request.BusinessId, request.ImageIds?.Count ?? 0);

            try
            {
                if (request.BusinessId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ReorderBusinessImages FAILED: Invalid BusinessId - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business ID cannot be empty");
                }

                if (request.ImageIds == null || !request.ImageIds.Any())
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ReorderBusinessImages FAILED: No image IDs provided - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("At least one image ID must be provided");
                }

                // Get business entity
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ReorderBusinessImages FAILED: Business not found - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business not found");
                }

                _logger.LogInformation(
                    "ReorderBusinessImages: Reordering images - BusinessId={BusinessId}, CurrentImageCount={CurrentImageCount}, NewOrder={NewOrder}",
                    request.BusinessId, business.Images.Count, string.Join(",", request.ImageIds));

                // Reorder images in the business domain model
                var reorderResult = business.ReorderImages(request.ImageIds);

                if (!reorderResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ReorderBusinessImages FAILED: Reorder operation failed - BusinessId={BusinessId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.BusinessId, string.Join(", ", reorderResult.Errors), stopwatch.ElapsedMilliseconds);

                    return reorderResult;
                }

                // Update business in repository
                _businessRepository.Update(business);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "ReorderBusinessImages COMPLETE: BusinessId={BusinessId}, ImageCount={ImageCount}, Duration={ElapsedMs}ms",
                    request.BusinessId, request.ImageIds.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "ReorderBusinessImages FAILED: Exception occurred - BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.BusinessId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure("An unexpected error occurred during image reordering");
            }
        }
    }
}