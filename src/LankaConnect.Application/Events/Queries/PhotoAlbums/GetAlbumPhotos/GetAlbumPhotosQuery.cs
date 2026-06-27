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
using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.PhotoAlbums.GetAlbumPhotos;

/// <summary>
/// Query to get paginated approved photos from an album.
/// Uses cursor-based pagination for infinite scroll support.
/// </summary>
public record GetAlbumPhotosQuery(
    Guid AlbumId,
    int PageSize = 20,
    DateTime? Cursor = null
) : IQuery<PaginatedAlbumPhotosResponse>;

public class GetAlbumPhotosQueryHandler : IQueryHandler<GetAlbumPhotosQuery, PaginatedAlbumPhotosResponse>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly ILogger<GetAlbumPhotosQueryHandler> _logger;

    private const int MAX_PAGE_SIZE = 50;
    private const int DEFAULT_PAGE_SIZE = 20;

    public GetAlbumPhotosQueryHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        ILogger<GetAlbumPhotosQueryHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _logger = logger;
    }

    public async Task<Result<PaginatedAlbumPhotosResponse>> Handle(GetAlbumPhotosQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetAlbumPhotos"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetAlbumPhotos START: AlbumId={AlbumId}, PageSize={PageSize}, Cursor={Cursor}",
                request.AlbumId, request.PageSize, request.Cursor);

            try
            {
                // Validate request
                if (request.AlbumId == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetAlbumPhotos FAILED: Invalid AlbumId - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result<PaginatedAlbumPhotosResponse>.Failure("Album ID is required");
                }

                // Clamp page size
                var pageSize = Math.Clamp(request.PageSize, 1, MAX_PAGE_SIZE);

                // Get album to verify it exists and get photo count
                var album = await _photoAlbumRepository.GetByIdAsync(
                    request.AlbumId, trackChanges: false, cancellationToken);

                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetAlbumPhotos FAILED: Album not found - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result<PaginatedAlbumPhotosResponse>.Failure("Photo album not found");
                }

                // Get paginated approved photos
                var (photos, hasMore) = await _photoAlbumRepository.GetApprovedPhotosAsync(
                    album.Id, pageSize, request.Cursor, cancellationToken);

                // Determine next cursor (UploadedAt of last photo in this page)
                DateTime? nextCursor = null;
                if (hasMore && photos.Count > 0)
                {
                    nextCursor = photos[photos.Count - 1].UploadedAt;
                }

                // Map to DTOs
                var photoDtos = photos.Select(MapToDto).ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetAlbumPhotos COMPLETE: AlbumId={AlbumId}, ReturnedPhotos={ReturnedPhotos}, HasMore={HasMore}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    request.AlbumId, photoDtos.Count, hasMore, album.PhotoCount, stopwatch.ElapsedMilliseconds);

                var response = new PaginatedAlbumPhotosResponse
                {
                    Photos = photoDtos,
                    HasMore = hasMore,
                    NextCursor = nextCursor,
                    TotalCount = album.PhotoCount
                };

                return Result<PaginatedAlbumPhotosResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetAlbumPhotos FAILED: Exception occurred - AlbumId={AlbumId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.AlbumId, stopwatch.ElapsedMilliseconds, ex.Message);
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
