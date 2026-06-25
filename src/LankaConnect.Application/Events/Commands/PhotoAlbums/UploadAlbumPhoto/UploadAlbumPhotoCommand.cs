using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.UploadAlbumPhoto;

/// <summary>
/// Command to upload a photo to a photo album.
/// Validates upload permissions, processes the image (EXIF strip, thumbnails),
/// uploads to Azure Blob Storage, and adds metadata to the PhotoAlbum aggregate.
/// </summary>
public record UploadAlbumPhotoCommand(
    Guid AlbumId,
    Guid UploaderId,
    string UploaderName,
    byte[] ImageData,
    string FileName,
    string? Caption = null
) : ICommand<AlbumPhotoDto>;

public class UploadAlbumPhotoCommandHandler : ICommandHandler<UploadAlbumPhotoCommand, AlbumPhotoDto>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IAlbumImageService _albumImageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadAlbumPhotoCommandHandler> _logger;

    public UploadAlbumPhotoCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAlbumImageService albumImageService,
        IUnitOfWork unitOfWork,
        ILogger<UploadAlbumPhotoCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _albumImageService = albumImageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AlbumPhotoDto>> Handle(UploadAlbumPhotoCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UploadAlbumPhoto"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        using (LogContext.PushProperty("UploaderId", request.UploaderId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UploadAlbumPhoto START: AlbumId={AlbumId}, UploaderId={UploaderId}, FileName={FileName}, DataLength={DataLength}",
                request.AlbumId, request.UploaderId, request.FileName, request.ImageData.Length);

            try
            {
                // 1. Validate image file before processing
                var validationResult = _albumImageService.ValidateAlbumPhoto(request.ImageData, request.FileName);
                if (validationResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumPhoto FAILED: Image validation failed - AlbumId={AlbumId}, FileName={FileName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.FileName, validationResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(validationResult.Errors);
                }

                // 2. Get album by ID with change tracking
                var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: true, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumPhoto FAILED: Album not found - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure("Photo album not found");
                }

                // 3. Verify the uploader is the organizer (organizer-only upload in simplified model)
                if (album.OrganizerId != request.UploaderId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumPhoto FAILED: User is not organizer - AlbumId={AlbumId}, UploaderId={UploaderId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.UploaderId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure("Only the event organizer can upload photos to this album");
                }

                // 4. Process and upload image to Azure Blob Storage (EXIF strip, thumbnails)
                _logger.LogInformation(
                    "UploadAlbumPhoto: Processing image - AlbumId={AlbumId}, FileName={FileName}",
                    request.AlbumId, request.FileName);

                var uploadResult = await _albumImageService.ProcessAndUploadAsync(
                    request.ImageData, request.FileName, album.EventId, cancellationToken);

                if (uploadResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "UploadAlbumPhoto FAILED: Image upload failed - AlbumId={AlbumId}, FileName={FileName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.FileName, uploadResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(uploadResult.Errors);
                }

                var uploaded = uploadResult.Value;

                // 5. Add photo to album aggregate (all photos auto-approved, no moderation)
                var addPhotoResult = album.AddPhoto(
                    request.UploaderId,
                    request.UploaderName,
                    uploaded.OriginalUrl,
                    uploaded.OriginalBlobName,
                    uploaded.ThumbnailUrl,
                    uploaded.ThumbnailBlobName,
                    uploaded.MediumUrl,
                    uploaded.MediumBlobName,
                    request.Caption,
                    uploaded.FileSizeBytes);

                if (addPhotoResult.IsFailure)
                {
                    // Rollback: Delete uploaded blobs if domain operation fails
                    _logger.LogWarning(
                        "UploadAlbumPhoto: Domain validation failed, rolling back blob upload - AlbumId={AlbumId}, Error={Error}",
                        request.AlbumId, addPhotoResult.Error);

                    await _albumImageService.DeletePhotoAsync(
                        uploaded.OriginalBlobName,
                        uploaded.ThumbnailBlobName,
                        uploaded.MediumBlobName,
                        cancellationToken);

                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumPhoto FAILED: Domain validation failed after upload - AlbumId={AlbumId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, addPhotoResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(addPhotoResult.Errors);
                }

                // 6. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                var photo = addPhotoResult.Value;
                stopwatch.Stop();

                _logger.LogInformation(
                    "UploadAlbumPhoto COMPLETE: PhotoId={PhotoId}, AlbumId={AlbumId}, Status={PhotoStatus}, FileSizeBytes={FileSizeBytes}, Duration={ElapsedMs}ms",
                    photo.Id, album.Id, photo.Status, photo.FileSizeBytes, stopwatch.ElapsedMilliseconds);

                // 7. Map to DTO and return
                var dto = MapToDto(photo);
                return Result<AlbumPhotoDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UploadAlbumPhoto FAILED: Exception occurred - AlbumId={AlbumId}, FileName={FileName}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.AlbumId, request.FileName, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private static AlbumPhotoDto MapToDto(Modules.Media.Domain.Entities.AlbumPhoto photo)
    {
        return new AlbumPhotoDto
        {
            Id = photo.Id,
            AlbumId = photo.AlbumId,
            UploaderId = photo.UploaderId,
            UploaderName = photo.UploaderName,
            OriginalUrl = photo.OriginalUrl,
            ThumbnailUrl = photo.ThumbnailUrl,
            MediumUrl = photo.MediumUrl ?? string.Empty,
            Caption = photo.Caption,
            Status = photo.Status,
            MediaType = photo.MediaType,
            FileSizeBytes = photo.FileSizeBytes,
            DurationSeconds = photo.DurationSeconds,
            UploadedAt = photo.UploadedAt,
            ExpiresAt = photo.ExpiresAt,
            DisplayOrder = photo.DisplayOrder
        };
    }
}
