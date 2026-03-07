using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.PhotoAlbums.GetPendingPhotos;

/// <summary>
/// Query to get all pending photos for organizer moderation queue.
/// Returns photos with AlbumPhotoStatus.PendingApproval.
/// </summary>
public record GetPendingPhotosQuery(
    Guid EventId,
    Guid UserId
) : IQuery<IReadOnlyList<AlbumPhotoDto>>;

public class GetPendingPhotosQueryHandler : IQueryHandler<GetPendingPhotosQuery, IReadOnlyList<AlbumPhotoDto>>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly ILogger<GetPendingPhotosQueryHandler> _logger;

    public GetPendingPhotosQueryHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        ILogger<GetPendingPhotosQueryHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AlbumPhotoDto>>> Handle(GetPendingPhotosQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetPendingPhotos"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetPendingPhotos START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetPendingPhotos FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<IReadOnlyList<AlbumPhotoDto>>.Failure("Event ID is required");
                }

                // Get album to verify it exists and check organizer permission
                var album = await _photoAlbumRepository.GetByEventIdAsync(
                    request.EventId, trackChanges: false, cancellationToken);

                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetPendingPhotos FAILED: Album not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<IReadOnlyList<AlbumPhotoDto>>.Failure("Photo album not found for this event");
                }

                // Verify the user is the organizer (only organizer can see pending photos)
                if (album.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetPendingPhotos FAILED: User is not organizer - EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result<IReadOnlyList<AlbumPhotoDto>>.Failure("Only the event organizer can view pending photos");
                }

                // Get pending photos
                var pendingPhotos = await _photoAlbumRepository.GetPendingPhotosAsync(
                    album.Id, cancellationToken);

                // Map to DTOs
                var photoDtos = pendingPhotos.Select(MapToDto).ToList().AsReadOnly();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetPendingPhotos COMPLETE: EventId={EventId}, AlbumId={AlbumId}, PendingCount={PendingCount}, Duration={ElapsedMs}ms",
                    request.EventId, album.Id, photoDtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<AlbumPhotoDto>>.Success(photoDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetPendingPhotos FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private static AlbumPhotoDto MapToDto(AlbumPhoto photo)
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
