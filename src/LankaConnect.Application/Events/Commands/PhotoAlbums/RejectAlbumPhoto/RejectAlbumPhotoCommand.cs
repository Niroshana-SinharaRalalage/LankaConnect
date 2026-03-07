using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.RejectAlbumPhoto;

/// <summary>
/// Command to reject a pending photo in a pre-moderation album.
/// Only the event organizer can reject photos.
/// Rejected photos have their blobs deleted from Azure Blob Storage.
/// </summary>
public record RejectAlbumPhotoCommand(
    Guid EventId,
    Guid PhotoId,
    Guid UserId
) : ICommand;

public class RejectAlbumPhotoCommandHandler : ICommandHandler<RejectAlbumPhotoCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IAlbumImageService _albumImageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectAlbumPhotoCommandHandler> _logger;

    public RejectAlbumPhotoCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAlbumImageService albumImageService,
        IUnitOfWork unitOfWork,
        ILogger<RejectAlbumPhotoCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _albumImageService = albumImageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RejectAlbumPhotoCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RejectAlbumPhoto"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PhotoId", request.PhotoId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RejectAlbumPhoto START: EventId={EventId}, PhotoId={PhotoId}, UserId={UserId}",
                request.EventId, request.PhotoId, request.UserId);

            try
            {
                // 1. Get album by event ID with change tracking
                var album = await _photoAlbumRepository.GetByEventIdAsync(request.EventId, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RejectAlbumPhoto FAILED: Album not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found for this event");
                }

                // 2. Verify the user is the organizer
                if (album.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RejectAlbumPhoto FAILED: User is not organizer - EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Only the event organizer can reject photos");
                }

                // 3. Reject photo via domain method (returns the rejected photo for blob cleanup)
                var rejectResult = album.RejectPhoto(request.PhotoId);
                if (rejectResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RejectAlbumPhoto FAILED: Domain validation failed - EventId={EventId}, PhotoId={PhotoId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.PhotoId, rejectResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result.Failure(rejectResult.Errors);
                }

                var rejectedPhoto = rejectResult.Value;

                // 4. Delete blobs from Azure Blob Storage
                _logger.LogInformation(
                    "RejectAlbumPhoto: Deleting blobs for rejected photo - PhotoId={PhotoId}, OriginalBlob={OriginalBlob}",
                    request.PhotoId, rejectedPhoto.OriginalBlobName);

                var deleteResult = await _albumImageService.DeletePhotoAsync(
                    rejectedPhoto.OriginalBlobName,
                    rejectedPhoto.ThumbnailBlobName,
                    rejectedPhoto.MediumBlobName,
                    cancellationToken);

                if (deleteResult.IsFailure)
                {
                    // Log but continue — blobs can be cleaned up by background service
                    _logger.LogWarning(
                        "RejectAlbumPhoto: Blob deletion failed (will be cleaned up later) - PhotoId={PhotoId}, Error={Error}",
                        request.PhotoId, deleteResult.Error);
                }

                // 5. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RejectAlbumPhoto COMPLETE: PhotoId={PhotoId}, AlbumId={AlbumId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.PhotoId, album.Id, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "RejectAlbumPhoto FAILED: Exception occurred - EventId={EventId}, PhotoId={PhotoId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.PhotoId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
