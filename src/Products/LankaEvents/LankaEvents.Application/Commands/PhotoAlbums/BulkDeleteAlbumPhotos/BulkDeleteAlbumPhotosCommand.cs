using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.BulkDeleteAlbumPhotos;

/// <summary>
/// Command to bulk delete photos from a photo album.
/// Only the event organizer can perform bulk delete.
/// Removes from aggregate and deletes all blob sizes from Azure Blob Storage.
/// </summary>
public record BulkDeleteAlbumPhotosCommand(
    Guid AlbumId,
    List<Guid> PhotoIds,
    Guid RequesterId
) : ICommand<int>;

public class BulkDeleteAlbumPhotosCommandHandler : ICommandHandler<BulkDeleteAlbumPhotosCommand, int>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IAlbumImageService _albumImageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkDeleteAlbumPhotosCommandHandler> _logger;

    public BulkDeleteAlbumPhotosCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAlbumImageService albumImageService,
        IUnitOfWork unitOfWork,
        ILogger<BulkDeleteAlbumPhotosCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _albumImageService = albumImageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(BulkDeleteAlbumPhotosCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "BulkDeleteAlbumPhotos"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        using (LogContext.PushProperty("RequesterId", request.RequesterId))
        using (LogContext.PushProperty("PhotoCount", request.PhotoIds.Count))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "BulkDeleteAlbumPhotos START: AlbumId={AlbumId}, PhotoCount={PhotoCount}, RequesterId={RequesterId}",
                request.AlbumId, request.PhotoIds.Count, request.RequesterId);

            try
            {
                var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: true, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "BulkDeleteAlbumPhotos FAILED: Album not found - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result<int>.Failure("Photo album not found");
                }

                var removeResult = album.RemovePhotos(request.PhotoIds, request.RequesterId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "BulkDeleteAlbumPhotos FAILED: Domain validation failed - AlbumId={AlbumId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, removeResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<int>.Failure(removeResult.Errors);
                }

                var removedPhotos = removeResult.Value;

                // Delete blobs in parallel — failures are non-fatal (background cleanup handles orphans)
                var blobTasks = removedPhotos.Select(photo =>
                    _albumImageService.DeletePhotoAsync(
                        photo.OriginalBlobName,
                        photo.ThumbnailBlobName,
                        photo.MediumBlobName,
                        cancellationToken));

                var blobResults = await Task.WhenAll(blobTasks);

                var blobFailures = blobResults.Count(r => r.IsFailure);
                if (blobFailures > 0)
                {
                    _logger.LogWarning(
                        "BulkDeleteAlbumPhotos: {BlobFailures} blob deletion(s) failed (will be cleaned up later) - AlbumId={AlbumId}",
                        blobFailures, request.AlbumId);
                }

                // Persist album mutation to MediaDbContext + dispatch domain events via AppDbContext.
                // Wave 9.h.10.6 F30a: same MediaDbContext-not-saved bug.
                await _photoAlbumRepository.UpdateAsync(album, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "BulkDeleteAlbumPhotos COMPLETE: AlbumId={AlbumId}, DeletedCount={DeletedCount}, Duration={ElapsedMs}ms",
                    request.AlbumId, removedPhotos.Count, stopwatch.ElapsedMilliseconds);

                return Result<int>.Success(removedPhotos.Count);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "BulkDeleteAlbumPhotos FAILED: Exception occurred - AlbumId={AlbumId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.AlbumId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
