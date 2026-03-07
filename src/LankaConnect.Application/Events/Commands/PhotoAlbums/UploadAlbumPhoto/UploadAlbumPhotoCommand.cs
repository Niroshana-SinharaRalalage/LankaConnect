using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.UploadAlbumPhoto;

/// <summary>
/// Command to upload a photo to an event's photo album.
/// Validates upload permissions, processes the image (EXIF strip, thumbnails),
/// uploads to Azure Blob Storage, and adds metadata to the PhotoAlbum aggregate.
/// </summary>
public record UploadAlbumPhotoCommand(
    Guid EventId,
    Guid UploaderId,
    string UploaderName,
    byte[] ImageData,
    string FileName,
    string? Caption = null
) : ICommand<AlbumPhotoDto>;

public class UploadAlbumPhotoCommandHandler : ICommandHandler<UploadAlbumPhotoCommand, AlbumPhotoDto>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IAlbumImageService _albumImageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadAlbumPhotoCommandHandler> _logger;

    public UploadAlbumPhotoCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IRegistrationRepository registrationRepository,
        IAlbumImageService albumImageService,
        IUnitOfWork unitOfWork,
        ILogger<UploadAlbumPhotoCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _registrationRepository = registrationRepository;
        _albumImageService = albumImageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AlbumPhotoDto>> Handle(UploadAlbumPhotoCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UploadAlbumPhoto"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UploaderId", request.UploaderId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UploadAlbumPhoto START: EventId={EventId}, UploaderId={UploaderId}, FileName={FileName}, DataLength={DataLength}",
                request.EventId, request.UploaderId, request.FileName, request.ImageData.Length);

            try
            {
                // 1. Validate image file before processing
                var validationResult = _albumImageService.ValidateAlbumPhoto(request.ImageData, request.FileName);
                if (validationResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumPhoto FAILED: Image validation failed - EventId={EventId}, FileName={FileName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.FileName, validationResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(validationResult.Errors);
                }

                // 2. Get album by event ID with change tracking
                var album = await _photoAlbumRepository.GetByEventIdAsync(request.EventId, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumPhoto FAILED: Album not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure("Photo album not found for this event");
                }

                // 3. Check upload permissions
                var permissionResult = await CheckUploadPermissionAsync(
                    album, request.UploaderId, request.EventId, cancellationToken);
                if (permissionResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumPhoto FAILED: Permission denied - EventId={EventId}, UploaderId={UploaderId}, Permission={Permission}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.UploaderId, album.UploadPermission, permissionResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(permissionResult.Errors);
                }

                // 4. Process and upload image to Azure Blob Storage (EXIF strip, thumbnails)
                _logger.LogInformation(
                    "UploadAlbumPhoto: Processing image - EventId={EventId}, FileName={FileName}",
                    request.EventId, request.FileName);

                var uploadResult = await _albumImageService.ProcessAndUploadAsync(
                    request.ImageData, request.FileName, request.EventId, cancellationToken);

                if (uploadResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "UploadAlbumPhoto FAILED: Image upload failed - EventId={EventId}, FileName={FileName}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.FileName, uploadResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(uploadResult.Errors);
                }

                var uploaded = uploadResult.Value;

                // 5. Add photo to album aggregate
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
                        "UploadAlbumPhoto: Domain validation failed, rolling back blob upload - EventId={EventId}, Error={Error}",
                        request.EventId, addPhotoResult.Error);

                    await _albumImageService.DeletePhotoAsync(
                        uploaded.OriginalBlobName,
                        uploaded.ThumbnailBlobName,
                        uploaded.MediumBlobName,
                        cancellationToken);

                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UploadAlbumPhoto FAILED: Domain validation failed after upload - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, addPhotoResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<AlbumPhotoDto>.Failure(addPhotoResult.Errors);
                }

                // 6. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                var photo = addPhotoResult.Value;
                stopwatch.Stop();

                _logger.LogInformation(
                    "UploadAlbumPhoto COMPLETE: PhotoId={PhotoId}, AlbumId={AlbumId}, EventId={EventId}, Status={PhotoStatus}, FileSizeBytes={FileSizeBytes}, Duration={ElapsedMs}ms",
                    photo.Id, album.Id, request.EventId, photo.Status, photo.FileSizeBytes, stopwatch.ElapsedMilliseconds);

                // 7. Map to DTO and return
                var dto = MapToDto(photo);
                return Result<AlbumPhotoDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UploadAlbumPhoto FAILED: Exception occurred - EventId={EventId}, FileName={FileName}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.FileName, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Checks whether the uploader has permission to upload photos to this album.
    /// - OrganizerOnly: Only the event organizer can upload.
    /// - RegisteredAttendees: User must have a registration for the event.
    /// - AnyAuthenticated: Any authenticated user can upload (uploader ID is not empty).
    /// </summary>
    private async Task<Result> CheckUploadPermissionAsync(
        PhotoAlbum album, Guid uploaderId, Guid eventId, CancellationToken cancellationToken)
    {
        switch (album.UploadPermission)
        {
            case AlbumUploadPermission.OrganizerOnly:
                if (album.OrganizerId != uploaderId)
                    return Result.Failure("Only the event organizer can upload photos to this album");
                break;

            case AlbumUploadPermission.RegisteredAttendees:
                // Organizer can always upload
                if (album.OrganizerId == uploaderId)
                    break;

                // Check if user has a registration for this event
                var registration = await _registrationRepository.GetByEventAndUserAsync(
                    eventId, uploaderId, cancellationToken);
                if (registration == null)
                    return Result.Failure("Only registered attendees can upload photos to this album");
                break;

            case AlbumUploadPermission.AnyAuthenticated:
                // Any authenticated user can upload (uploaderId must be valid)
                if (uploaderId == Guid.Empty)
                    return Result.Failure("Authentication is required to upload photos");
                break;

            default:
                return Result.Failure($"Unknown upload permission: {album.UploadPermission}");
        }

        return Result.Success();
    }

    private static AlbumPhotoDto MapToDto(Domain.Events.Entities.AlbumPhoto photo)
    {
        return new AlbumPhotoDto
        {
            Id = photo.Id,
            AlbumId = photo.AlbumId,
            UploaderId = photo.UploaderId,
            UploaderName = photo.UploaderName,
            OriginalUrl = photo.OriginalUrl,
            ThumbnailUrl = photo.ThumbnailUrl,
            MediumUrl = photo.MediumUrl,
            Caption = photo.Caption,
            Status = photo.Status,
            FileSizeBytes = photo.FileSizeBytes,
            UploadedAt = photo.UploadedAt,
            ExpiresAt = photo.ExpiresAt,
            DisplayOrder = photo.DisplayOrder
        };
    }
}
