using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Modules.Media.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.DeleteAlbumPhoto;

/// <summary>
/// Command to delete a photo from a photo album.
/// Only the photo uploader or event organizer can delete a photo.
/// Removes from aggregate and deletes all 3 blob sizes from Azure Blob Storage.
/// </summary>
public record DeleteAlbumPhotoCommand(
    Guid AlbumId,
    Guid PhotoId,
    Guid RequesterId
) : ICommand;

public class DeleteAlbumPhotoCommandHandler : ICommandHandler<DeleteAlbumPhotoCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IAlbumImageService _albumImageService;
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly MediaDbContext _mediaContext;
    private readonly ILogger<DeleteAlbumPhotoCommandHandler> _logger;

    public DeleteAlbumPhotoCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAlbumImageService albumImageService,
        IMultiContextUnitOfWork unitOfWork,
        MediaDbContext mediaContext,
        ILogger<DeleteAlbumPhotoCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _albumImageService = albumImageService;
        _unitOfWork = unitOfWork;
        _mediaContext = mediaContext;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteAlbumPhotoCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteAlbumPhoto"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        using (LogContext.PushProperty("PhotoId", request.PhotoId))
        using (LogContext.PushProperty("RequesterId", request.RequesterId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteAlbumPhoto START: AlbumId={AlbumId}, PhotoId={PhotoId}, RequesterId={RequesterId}",
                request.AlbumId, request.PhotoId, request.RequesterId);

            try
            {
                // 1. Get album by ID with change tracking
                var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: true, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteAlbumPhoto FAILED: Album not found - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found");
                }

                // 2. Remove photo from aggregate (domain checks uploader/organizer permission)
                var removeResult = album.RemovePhoto(request.PhotoId, request.RequesterId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteAlbumPhoto FAILED: Domain validation failed - AlbumId={AlbumId}, PhotoId={PhotoId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.PhotoId, removeResult.Error, stopwatch.ElapsedMilliseconds);
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
                    // Log but continue -- blobs can be cleaned up by background service
                    _logger.LogWarning(
                        "DeleteAlbumPhoto: Blob deletion failed (will be cleaned up later) - PhotoId={PhotoId}, Error={Error}",
                        request.PhotoId, deleteResult.Error);
                }

                // 4. Wave 6.5.b: atomic multi-context commit. Replaces the F30a workaround.
                await _unitOfWork.CommitAsync(new DbContext[] { _mediaContext }, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteAlbumPhoto COMPLETE: PhotoId={PhotoId}, AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                    request.PhotoId, album.Id, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "DeleteAlbumPhoto FAILED: Exception occurred - AlbumId={AlbumId}, PhotoId={PhotoId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.AlbumId, request.PhotoId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
