using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

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
        try
        {
            _logger.LogInformation("Deleting image {ImageId} for business {BusinessId}", 
                request.ImageId, request.BusinessId);

            // Get business entity
            var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
            if (business == null)
            {
                _logger.LogWarning("Business not found: {BusinessId}", request.BusinessId);
                return Result.Failure("Business not found");
            }

            // Find the image to get URLs for deletion
            var imageToDelete = business.Images.FirstOrDefault(img => img.Id == request.ImageId);
            if (imageToDelete == null)
            {
                _logger.LogWarning("Image not found: {ImageId} for business {BusinessId}", 
                    request.ImageId, request.BusinessId);
                return Result.Failure("Image not found");
            }

            // Remove image from business domain model first
            var removeResult = business.RemoveImage(request.ImageId);
            if (!removeResult.IsSuccess)
            {
                _logger.LogError("Failed to remove image from business {BusinessId}: {Errors}",
                    request.BusinessId, string.Join(", ", removeResult.Errors));
                return removeResult;
            }

            // Update business in repository
            _businessRepository.Update(business);
            await _unitOfWork.CommitAsync(cancellationToken);

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
                _logger.LogWarning("Some image files failed to delete from storage for business {BusinessId}, image {ImageId}: {Errors}",
                    request.BusinessId, request.ImageId, string.Join(", ", errors));
            }

            _logger.LogInformation("Successfully deleted image {ImageId} for business {BusinessId}", 
                request.ImageId, request.BusinessId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image deletion for business {BusinessId}, image {ImageId}",
                request.BusinessId, request.ImageId);
            return Result.Failure("An unexpected error occurred during image deletion");
        }
    }
}