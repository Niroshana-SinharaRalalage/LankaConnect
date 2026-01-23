using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Businesses.Commands.DeleteBusinessImage;

/// <summary>
/// Handler for deleting business images
/// </summary>
public sealed class DeleteBusinessImageCommandHandler : IRequestHandler<DeleteBusinessImageCommand, Result>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IImageService _imageService;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteBusinessImageCommandHandler> _logger;

    public DeleteBusinessImageCommandHandler(
        IBusinessRepository businessRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteBusinessImageCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteBusinessImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteBusinessImage"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", request.BusinessId))
        using (LogContext.PushProperty("ImageId", request.ImageId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteBusinessImage START: BusinessId={BusinessId}, ImageId={ImageId}",
                request.BusinessId, request.ImageId);

            try
            {
                if (request.BusinessId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBusinessImage FAILED: Invalid BusinessId - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business ID cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(request.ImageId))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBusinessImage FAILED: Invalid ImageId - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Image ID cannot be empty");
                }

                // Get business entity
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBusinessImage FAILED: Business not found - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business not found");
                }

                // Find the image to get URLs for deletion
                var imageToDelete = business.Images.FirstOrDefault(img => img.Id == request.ImageId);

                if (imageToDelete == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBusinessImage FAILED: Image not found - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                        request.ImageId, request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Image not found");
                }

                _logger.LogInformation(
                    "DeleteBusinessImage: Removing image from business - BusinessId={BusinessId}, ImageId={ImageId}, OriginalUrl={OriginalUrl}",
                    request.BusinessId, request.ImageId, imageToDelete.OriginalUrl);

                // Remove image from business domain model first
                var removeResult = business.RemoveImage(request.ImageId);

                if (!removeResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBusinessImage FAILED: Failed to remove image - BusinessId={BusinessId}, ImageId={ImageId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.ImageId, string.Join(", ", removeResult.Errors), stopwatch.ElapsedMilliseconds);

                    return removeResult;
                }

                // Update business in repository
                _businessRepository.Update(business);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "DeleteBusinessImage: Database updated - BusinessId={BusinessId}, ImageId={ImageId}",
                    request.BusinessId, request.ImageId);

                // Delete images from Azure Storage (do this after database update to ensure consistency)
                var imageDeletionTasks = new[]
                {
                    _imageService.DeleteImageAsync(imageToDelete.OriginalUrl, cancellationToken),
                    _imageService.DeleteImageAsync(imageToDelete.ThumbnailUrl, cancellationToken),
                    _imageService.DeleteImageAsync(imageToDelete.MediumUrl, cancellationToken),
                    _imageService.DeleteImageAsync(imageToDelete.LargeUrl, cancellationToken)
                };

                var deletionResults = await Task.WhenAll(imageDeletionTasks);

                // Log any storage deletion failures (but don't fail the entire operation)
                var failedDeletions = deletionResults.Where(r => !r.IsSuccess).ToList();
                if (failedDeletions.Any())
                {
                    var errors = failedDeletions.SelectMany(r => r.Errors).ToList();

                    _logger.LogWarning(
                        "DeleteBusinessImage: Some blobs failed to delete - BusinessId={BusinessId}, ImageId={ImageId}, FailedCount={FailedCount}, Errors={Errors}",
                        request.BusinessId, request.ImageId, failedDeletions.Count, string.Join(", ", errors));
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteBusinessImage COMPLETE: BusinessId={BusinessId}, ImageId={ImageId}, BlobsDeleted={BlobsDeleted}, Duration={ElapsedMs}ms",
                    request.BusinessId, request.ImageId, deletionResults.Count(r => r.IsSuccess), stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "DeleteBusinessImage FAILED: Exception occurred - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure("An unexpected error occurred during image deletion");
            }
        }
    }
}