using System.Diagnostics;
using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Badges.Commands.UpdateBadgeImage;

/// <summary>
/// Handler for UpdateBadgeImageCommand
/// Phase 6A.25: Updates a badge's image (uploads new image and deletes old one)
/// </summary>
public class UpdateBadgeImageCommandHandler : IRequestHandler<UpdateBadgeImageCommand, Result<BadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateBadgeImageCommandHandler> _logger;

    public UpdateBadgeImageCommandHandler(
        IBadgeRepository badgeRepository,
        IAzureBlobStorageService blobStorageService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateBadgeImageCommandHandler> logger)
    {
        _badgeRepository = badgeRepository;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BadgeDto>> Handle(UpdateBadgeImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateBadgeImage"))
        using (LogContext.PushProperty("EntityType", "Badge"))
        using (LogContext.PushProperty("BadgeId", request.BadgeId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateBadgeImage START: BadgeId={BadgeId}, FileName={FileName}, ImageSize={ImageSize}",
                request.BadgeId, request.FileName, request.ImageData?.Length ?? 0);

            try
            {
                // 1. Validate input
                if (request.BadgeId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBadgeImage FAILED: Invalid BadgeId - BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result<BadgeDto>.Failure("Badge ID is required");
                }

                if (request.ImageData == null || request.ImageData.Length == 0)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBadgeImage FAILED: Missing image data - BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result<BadgeDto>.Failure("Badge image is required");
                }

                if (string.IsNullOrWhiteSpace(request.FileName))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBadgeImage FAILED: Missing file name - BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result<BadgeDto>.Failure("File name is required");
                }

                // 2. Get existing badge
                var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);
                if (badge == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBadgeImage FAILED: Badge not found - BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result<BadgeDto>.Failure($"Badge with ID {request.BadgeId} not found");
                }

        // 3. Store old blob name for deletion
        var oldBlobName = badge.BlobName;

        // 4. Upload new image to Azure Blob Storage
        var uniqueFileName = $"badges/{Guid.NewGuid()}-{request.FileName}";
        using var stream = new MemoryStream(request.ImageData);

        var (blobName, blobUrl) = await _blobStorageService.UploadFileAsync(
            uniqueFileName,
            stream,
            GetContentType(request.FileName),
            "badges",
            cancellationToken);

        // 5. Update badge with new image
        var updateResult = badge.UpdateImage(blobUrl, blobName);
        if (!updateResult.IsSuccess)
        {
            // Rollback: Delete the newly uploaded blob
            await _blobStorageService.DeleteFileAsync(blobName, "badges", cancellationToken);
            return Result<BadgeDto>.Failure(updateResult.Errors);
        }

                // 6. Save changes
                _badgeRepository.Update(badge);
                await _unitOfWork.CommitAsync(cancellationToken);

                // 7. Delete old image (non-blocking, ignore errors)
                try
                {
                    await _blobStorageService.DeleteFileAsync(oldBlobName, "badges", cancellationToken);

                    _logger.LogInformation(
                        "UpdateBadgeImage: Old blob deleted - BadgeId={BadgeId}, OldBlobName={OldBlobName}",
                        request.BadgeId, oldBlobName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "UpdateBadgeImage: Old blob deletion failed (non-critical) - BadgeId={BadgeId}, OldBlobName={OldBlobName}, Error={ErrorMessage}",
                        request.BadgeId, oldBlobName, ex.Message);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateBadgeImage COMPLETE: BadgeId={BadgeId}, NewBlobUrl={BlobUrl}, OldBlobName={OldBlobName}, Duration={ElapsedMs}ms",
                    request.BadgeId, badge.ImageUrl, oldBlobName, stopwatch.ElapsedMilliseconds);

                // 8. Return updated DTO
                // Phase 6A.31a: Use ToBadgeDto() extension method which handles obsolete property mapping
                var dto = badge.ToBadgeDto();

                return Result<BadgeDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateBadgeImage FAILED: Exception occurred - BadgeId={BadgeId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.BadgeId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}
