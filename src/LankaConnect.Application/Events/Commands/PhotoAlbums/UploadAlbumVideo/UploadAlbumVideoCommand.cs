using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.UploadAlbumVideo;

/// <summary>
/// Command to upload a video to a photo album.
/// Validates upload permissions, processes the thumbnail (EXIF strip, resize),
/// uploads video + thumbnail to Azure Blob Storage, and adds metadata to the PhotoAlbum aggregate.
/// </summary>
public record UploadAlbumVideoCommand(
    Guid AlbumId,
    Guid UploaderId,
    string UploaderName,
    byte[] VideoData,
    string VideoFileName,
    byte[] ThumbnailData,
    string ThumbnailFileName,
    string? Caption = null,
    long? DurationSeconds = null
) : ICommand<AlbumPhotoDto>;

public class UploadAlbumVideoCommandHandler : ICommandHandler<UploadAlbumVideoCommand, AlbumPhotoDto>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IAlbumImageService _albumImageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadAlbumVideoCommandHandler> _logger;

    public UploadAlbumVideoCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAlbumImageService albumImageService,
        IUnitOfWork unitOfWork,
        ILogger<UploadAlbumVideoCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _albumImageService = albumImageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AlbumPhotoDto>> Handle(UploadAlbumVideoCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UploadAlbumVideo"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        using (LogContext.PushProperty("UploaderId", request.UploaderId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UploadAlbumVideo START: AlbumId={AlbumId}, UploaderId={UploaderId}, FileName={FileName}, DataLength={DataLength}",
                request.AlbumId, request.UploaderId, request.VideoFileName, request.VideoData.Length);

            try
            {
                // 1. Validate video file before processing
                var validationResult = _albumImageService.ValidateAlbumVideo(request.VideoData, request.VideoFileName);
                if (validationResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumVideo FAILED: Video validation failed - AlbumId={AlbumId}, FileName={FileName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.VideoFileName, validationResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(validationResult.Errors);
                }

                // 2. Get album by ID with change tracking
                var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: true, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumVideo FAILED: Album not found - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure("Photo album not found");
                }

                // 3. Verify the uploader is the organizer
                if (album.OrganizerId != request.UploaderId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumVideo FAILED: User is not organizer - AlbumId={AlbumId}, UploaderId={UploaderId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.UploaderId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure("Only the event organizer can upload videos to this album");
                }

                // 4. Process and upload video + thumbnail to Azure Blob Storage
                _logger.LogInformation(
                    "UploadAlbumVideo: Processing video - AlbumId={AlbumId}, FileName={FileName}",
                    request.AlbumId, request.VideoFileName);

                var uploadResult = await _albumImageService.ProcessAndUploadVideoAsync(
                    request.VideoData, request.VideoFileName,
                    request.ThumbnailData, request.ThumbnailFileName,
                    album.EventId, cancellationToken);

                if (uploadResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "UploadAlbumVideo FAILED: Video upload failed - AlbumId={AlbumId}, FileName={FileName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.VideoFileName, uploadResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(uploadResult.Errors);
                }

                var uploaded = uploadResult.Value;

                // 5. Add video to album aggregate
                var addVideoResult = album.AddVideo(
                    request.UploaderId,
                    request.UploaderName,
                    uploaded.OriginalUrl,
                    uploaded.OriginalBlobName,
                    uploaded.ThumbnailUrl,
                    uploaded.ThumbnailBlobName,
                    request.Caption,
                    uploaded.FileSizeBytes,
                    request.DurationSeconds);

                if (addVideoResult.IsFailure)
                {
                    // Rollback: Delete uploaded blobs if domain operation fails
                    _logger.LogWarning(
                        "UploadAlbumVideo: Domain validation failed, rolling back blob upload - AlbumId={AlbumId}, Error={Error}",
                        request.AlbumId, addVideoResult.Error);

                    await _albumImageService.DeletePhotoAsync(
                        uploaded.OriginalBlobName,
                        uploaded.ThumbnailBlobName,
                        mediumBlobName: null,
                        cancellationToken);

                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumVideo FAILED: Domain validation failed after upload - AlbumId={AlbumId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, addVideoResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(addVideoResult.Errors);
                }

                // 6. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                var video = addVideoResult.Value;
                stopwatch.Stop();

                _logger.LogInformation(
                    "UploadAlbumVideo COMPLETE: VideoId={VideoId}, AlbumId={AlbumId}, MediaType={MediaType}, FileSizeBytes={FileSizeBytes}, Duration={ElapsedMs}ms",
                    video.Id, album.Id, video.MediaType, video.FileSizeBytes, stopwatch.ElapsedMilliseconds);

                // 7. Map to DTO and return
                var dto = MapToDto(video);
                return Result<AlbumPhotoDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UploadAlbumVideo FAILED: Exception occurred - AlbumId={AlbumId}, FileName={FileName}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.AlbumId, request.VideoFileName, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private static AlbumPhotoDto MapToDto(Modules.Media.Domain.Entities.AlbumPhoto video)
    {
        return new AlbumPhotoDto
        {
            Id = video.Id,
            AlbumId = video.AlbumId,
            UploaderId = video.UploaderId,
            UploaderName = video.UploaderName,
            OriginalUrl = video.OriginalUrl,
            ThumbnailUrl = video.ThumbnailUrl,
            MediumUrl = video.MediumUrl ?? string.Empty,
            Caption = video.Caption,
            Status = video.Status,
            MediaType = video.MediaType,
            FileSizeBytes = video.FileSizeBytes,
            DurationSeconds = video.DurationSeconds,
            UploadedAt = video.UploadedAt,
            ExpiresAt = video.ExpiresAt,
            DisplayOrder = video.DisplayOrder
        };
    }
}
