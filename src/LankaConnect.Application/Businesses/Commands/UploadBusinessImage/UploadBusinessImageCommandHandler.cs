using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "UploadBusinessImage"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", request.BusinessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UploadBusinessImage START: BusinessId={BusinessId}, FileName={FileName}, FileSize={FileSize}",
                request.BusinessId, request.Image?.FileName, request.Image?.Length ?? 0);

            try
            {
                if (request.BusinessId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadBusinessImage FAILED: Invalid BusinessId - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result<UploadBusinessImageResponse>.Failure("Business ID cannot be empty");
                }

                if (request.Image == null || request.Image.Length == 0)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadBusinessImage FAILED: No image provided - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result<UploadBusinessImageResponse>.Failure("Image file is required");
                }

                // Get business entity
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadBusinessImage FAILED: Business not found - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result<UploadBusinessImageResponse>.Failure("Business not found");
                }

                _logger.LogInformation(
                    "UploadBusinessImage: Converting image to byte array - BusinessId={BusinessId}, FileName={FileName}, FileSize={FileSize}",
                    request.BusinessId, request.Image.FileName, request.Image.Length);

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
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadBusinessImage FAILED: Image resize/upload failed - BusinessId={BusinessId}, FileName={FileName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.Image.FileName, string.Join(", ", resizeResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result<UploadBusinessImageResponse>.Failure(resizeResult.Errors);
                }

                _logger.LogInformation(
                    "UploadBusinessImage: Images uploaded to Azure - BusinessId={BusinessId}, OriginalUrl={OriginalUrl}",
                    request.BusinessId, resizeResult.Value.OriginalUrl);

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
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadBusinessImage FAILED: BusinessImage creation failed - BusinessId={BusinessId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.BusinessId, string.Join(", ", businessImageResult.Errors), stopwatch.ElapsedMilliseconds);

                    // Clean up uploaded files if BusinessImage creation fails
                    try
                    {
                        _logger.LogInformation(
                            "UploadBusinessImage: Cleaning up uploaded blobs after BusinessImage creation failure - BusinessId={BusinessId}",
                            request.BusinessId);

                        await _imageService.DeleteImageAsync(resizeResult.Value.OriginalUrl, cancellationToken);
                        await _imageService.DeleteImageAsync(resizeResult.Value.ThumbnailUrl, cancellationToken);
                        await _imageService.DeleteImageAsync(resizeResult.Value.MediumUrl, cancellationToken);
                        await _imageService.DeleteImageAsync(resizeResult.Value.LargeUrl, cancellationToken);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx,
                            "UploadBusinessImage: Failed to clean up uploaded blobs - BusinessId={BusinessId}, Error={ErrorMessage}",
                            request.BusinessId, cleanupEx.Message);
                    }

                    return Result<UploadBusinessImageResponse>.Failure(businessImageResult.Errors);
                }

                using (LogContext.PushProperty("ImageId", businessImageResult.Value.Id))
                {
                    _logger.LogInformation(
                        "UploadBusinessImage: BusinessImage created - BusinessId={BusinessId}, ImageId={ImageId}, IsPrimary={IsPrimary}",
                        request.BusinessId, businessImageResult.Value.Id, request.IsPrimary);

                    // Add image to business
                    var addImageResult = business.AddImage(businessImageResult.Value);

                    if (!addImageResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UploadBusinessImage FAILED: Failed to add image to business - BusinessId={BusinessId}, ImageId={ImageId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.BusinessId, businessImageResult.Value.Id, string.Join(", ", addImageResult.Errors), stopwatch.ElapsedMilliseconds);

                        // Clean up uploaded files if adding to business fails
                        try
                        {
                            _logger.LogInformation(
                                "UploadBusinessImage: Cleaning up uploaded blobs after add-to-business failure - BusinessId={BusinessId}, ImageId={ImageId}",
                                request.BusinessId, businessImageResult.Value.Id);

                            await _imageService.DeleteImageAsync(resizeResult.Value.OriginalUrl, cancellationToken);
                            await _imageService.DeleteImageAsync(resizeResult.Value.ThumbnailUrl, cancellationToken);
                            await _imageService.DeleteImageAsync(resizeResult.Value.MediumUrl, cancellationToken);
                            await _imageService.DeleteImageAsync(resizeResult.Value.LargeUrl, cancellationToken);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx,
                                "UploadBusinessImage: Failed to clean up uploaded blobs - BusinessId={BusinessId}, ImageId={ImageId}, Error={ErrorMessage}",
                                request.BusinessId, businessImageResult.Value.Id, cleanupEx.Message);
                        }

                        return Result<UploadBusinessImageResponse>.Failure(addImageResult.Errors);
                    }

                    // Update business in repository
                    _businessRepository.Update(business);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "UploadBusinessImage: Database updated - BusinessId={BusinessId}, ImageId={ImageId}",
                        request.BusinessId, businessImageResult.Value.Id);

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

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "UploadBusinessImage COMPLETE: BusinessId={BusinessId}, ImageId={ImageId}, FileName={FileName}, FileSize={FileSize}, Duration={ElapsedMs}ms",
                        request.BusinessId, businessImageResult.Value.Id, request.Image.FileName, businessImageResult.Value.FileSizeBytes, stopwatch.ElapsedMilliseconds);

                    return Result<UploadBusinessImageResponse>.Success(response);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UploadBusinessImage FAILED: Exception occurred - BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.BusinessId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<UploadBusinessImageResponse>.Failure("An unexpected error occurred during image upload");
            }
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