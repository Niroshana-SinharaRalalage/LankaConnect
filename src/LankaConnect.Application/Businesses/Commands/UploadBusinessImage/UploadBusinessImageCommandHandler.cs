using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Businesses.Commands.UploadBusinessImage;

/// <summary>
/// Handler for uploading business images
/// </summary>
public sealed class UploadBusinessImageCommandHandler 
    : IRequestHandler<UploadBusinessImageCommand, Result<UploadBusinessImageResponse>>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadBusinessImageCommandHandler> _logger;

    public UploadBusinessImageCommandHandler(
        IBusinessRepository businessRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<UploadBusinessImageCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UploadBusinessImageResponse>> Handle(
        UploadBusinessImageCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting image upload for business {BusinessId}", request.BusinessId);

            // Get business entity
            var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
            if (business == null)
            {
                _logger.LogWarning("Business not found: {BusinessId}", request.BusinessId);
                return Result<UploadBusinessImageResponse>.Failure("Business not found");
            }

            // Convert IFormFile to byte array
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await request.Image.CopyToAsync(memoryStream, cancellationToken);
                imageBytes = memoryStream.ToArray();
            }

            // Upload and resize image using the image service
            var resizeResult = await _imageService.ResizeAndUploadAsync(
                imageBytes,
                request.Image.FileName,
                request.BusinessId,
                cancellationToken);

            if (!resizeResult.IsSuccess)
            {
                _logger.LogError("Image resize and upload failed for business {BusinessId}: {Errors}",
                    request.BusinessId, string.Join(", ", resizeResult.Errors));
                return Result<UploadBusinessImageResponse>.Failure(resizeResult.Errors);
            }

            // Create BusinessImage value object
            var businessImageResult = BusinessImage.Create(
                resizeResult.Value.OriginalUrl,
                resizeResult.Value.ThumbnailUrl,
                resizeResult.Value.MediumUrl,
                resizeResult.Value.LargeUrl,
                request.AltText ?? string.Empty,
                request.Caption ?? string.Empty,
                request.DisplayOrder,
                request.IsPrimary,
                resizeResult.Value.SizesBytes.GetValueOrDefault("original", 0),
                GetContentType(request.Image.FileName),
                new Dictionary<string, string>
                {
                    ["OriginalFileName"] = request.Image.FileName,
                    ["UploadSource"] = "WebAPI",
                    ["UserId"] = "System" // TODO: Get from current user context
                });

            if (!businessImageResult.IsSuccess)
            {
                _logger.LogError("Failed to create BusinessImage for business {BusinessId}: {Errors}",
                    request.BusinessId, string.Join(", ", businessImageResult.Errors));

                // Clean up uploaded files if BusinessImage creation fails
                try
                {
                    await _imageService.DeleteImageAsync(resizeResult.Value.OriginalUrl, cancellationToken);
                    await _imageService.DeleteImageAsync(resizeResult.Value.ThumbnailUrl, cancellationToken);
                    await _imageService.DeleteImageAsync(resizeResult.Value.MediumUrl, cancellationToken);
                    await _imageService.DeleteImageAsync(resizeResult.Value.LargeUrl, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up uploaded images after BusinessImage creation failure");
                }

                return Result<UploadBusinessImageResponse>.Failure(businessImageResult.Errors);
            }

            // Add image to business
            var addImageResult = business.AddImage(businessImageResult.Value);
            if (!addImageResult.IsSuccess)
            {
                _logger.LogError("Failed to add image to business {BusinessId}: {Errors}",
                    request.BusinessId, string.Join(", ", addImageResult.Errors));

                // Clean up uploaded files if adding to business fails
                try
                {
                    await _imageService.DeleteImageAsync(resizeResult.Value.OriginalUrl, cancellationToken);
                    await _imageService.DeleteImageAsync(resizeResult.Value.ThumbnailUrl, cancellationToken);
                    await _imageService.DeleteImageAsync(resizeResult.Value.MediumUrl, cancellationToken);
                    await _imageService.DeleteImageAsync(resizeResult.Value.LargeUrl, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up uploaded images after adding to business failure");
                }

                return Result<UploadBusinessImageResponse>.Failure(addImageResult.Errors);
            }

            // Update business in repository
            _businessRepository.Update(business);
            await _unitOfWork.CommitAsync(cancellationToken);

            var response = new UploadBusinessImageResponse
            {
                ImageId = businessImageResult.Value.Id,
                OriginalUrl = businessImageResult.Value.OriginalUrl,
                ThumbnailUrl = businessImageResult.Value.ThumbnailUrl,
                MediumUrl = businessImageResult.Value.MediumUrl,
                LargeUrl = businessImageResult.Value.LargeUrl,
                FileSizeBytes = businessImageResult.Value.FileSizeBytes,
                UploadedAt = businessImageResult.Value.UploadedAt
            };

            _logger.LogInformation("Successfully uploaded image for business {BusinessId}. ImageId: {ImageId}",
                request.BusinessId, businessImageResult.Value.Id);

            return Result<UploadBusinessImageResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image upload for business {BusinessId}", request.BusinessId);
            return Result<UploadBusinessImageResponse>.Failure("An unexpected error occurred during image upload");
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
    }
}