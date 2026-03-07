using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.DeleteAlbumPhoto;

/// <summary>
/// Command to delete a photo from an event's photo album.
/// Only the photo uploader or event organizer can delete a photo.
/// Removes from aggregate and deletes all 3 blob sizes from Azure Blob Storage.
/// </summary>
public record DeleteAlbumPhotoCommand(
    Guid EventId,
    Guid PhotoId,
    Guid RequesterId
) : ICommand;

public class DeleteAlbumPhotoCommandHandler : ICommandHandler<DeleteAlbumPhotoCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IAlbumImageService _albumImageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAlbumPhotoCommandHandler> _logger;

    public DeleteAlbumPhotoCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAlbumImageService albumImageService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteAlbumPhotoCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _albumImageService = albumImageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteAlbumPhotoCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteAlbumPhoto"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PhotoId", request.PhotoId))
        using (LogContext.PushProperty("RequesterId", request.RequesterId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteAlbumPhoto START: EventId={EventId}, PhotoId={PhotoId}, RequesterId={RequesterId}",
                request.EventId, request.PhotoId, request.RequesterId);

            try
            {
                // 1. Get album by event ID with change tracking
                var album = await _photoAlbumRepository.GetByEventIdAsync(request.EventId, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteAlbumPhoto FAILED: Album not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found for this event");
                }

                // 2. Remove photo from aggregate (domain checks uploader/organizer permission)
                var removeResult = album.RemovePhoto(request.PhotoId, request.RequesterId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteAlbumPhoto FAILED: Domain validation failed - EventId={EventId}, PhotoId={PhotoId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.PhotoId, removeResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result.Failure(removeResult.Errors);
                }

                var removedPhoto = removeResult.Value;

                // 3. Delete all 3 blob sizes from Azure Blob Storage
                _logger.LogInformation(
                    "DeleteAlbumPhoto: Deleting blobs - PhotoId={PhotoId}, OriginalBlob={OriginalBlob}, ThumbnailBlob={ThumbnailBlob}, MediumBlob={MediumBlob}",
                    request.PhotoId, removedPhoto.OriginalBlobName, removedPhoto.ThumbnailBlobName, removedPhoto.MediumBlobName);

                var deleteResult = await _albumImageService.DeletePhotoAsync(
                    removedPhoto.OriginalBlobName,
                    removedPhoto.ThumbnailBlobName,
                    removedPhoto.MediumBlobName,
                    cancellationToken);

                if (deleteResult.IsFailure)
                {
                    // Log but continue — blobs can be cleaned up by background service
                    _logger.LogWarning(
                        "DeleteAlbumPhoto: Blob deletion failed (will be cleaned up later) - PhotoId={PhotoId}, Error={Error}",
                        request.PhotoId, deleteResult.Error);
                }

                // 4. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteAlbumPhoto COMPLETE: PhotoId={PhotoId}, AlbumId={AlbumId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.PhotoId, album.Id, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "DeleteAlbumPhoto FAILED: Exception occurred - EventId={EventId}, PhotoId={PhotoId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.PhotoId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
