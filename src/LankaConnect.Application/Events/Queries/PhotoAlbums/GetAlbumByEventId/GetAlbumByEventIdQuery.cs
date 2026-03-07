using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.PhotoAlbums.GetAlbumByEventId;

/// <summary>
/// Query to get a photo album by its event ID.
/// Returns null if no album exists for the event (not an error).
/// </summary>
public record GetAlbumByEventIdQuery(Guid EventId) : IQuery<PhotoAlbumDto?>;

public class GetAlbumByEventIdQueryHandler : IQueryHandler<GetAlbumByEventIdQuery, PhotoAlbumDto?>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly ILogger<GetAlbumByEventIdQueryHandler> _logger;

    public GetAlbumByEventIdQueryHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        ILogger<GetAlbumByEventIdQueryHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _logger = logger;
    }

    public async Task<Result<PhotoAlbumDto?>> Handle(GetAlbumByEventIdQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetAlbumByEventId"))
        using (LogContext.PushProperty("EntityType", "PhotoAlbum"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetAlbumByEventId START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetAlbumByEventId FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto?>.Failure("Event ID is required");
                }

                // Get album with no change tracking (read-only query)
                var album = await _photoAlbumRepository.GetByEventIdAsync(
                    request.EventId, trackChanges: false, cancellationToken);

                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "GetAlbumByEventId COMPLETE: No album found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto?>.Success(null);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetAlbumByEventId COMPLETE: AlbumId={AlbumId}, EventId={EventId}, Status={Status}, PhotoCount={PhotoCount}, Duration={ElapsedMs}ms",
                    album.Id, request.EventId, album.Status, album.PhotoCount, stopwatch.ElapsedMilliseconds);

                var dto = MapToDto(album);
                return Result<PhotoAlbumDto?>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetAlbumByEventId FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private static PhotoAlbumDto MapToDto(PhotoAlbum album)
    {
        return new PhotoAlbumDto
        {
            Id = album.Id,
            EventId = album.EventId,
            OrganizerId = album.OrganizerId,
            EventTitle = album.EventTitle,
            Status = album.Status,
            UploadPermission = album.UploadPermission,
            ModerationMode = album.ModerationMode,
            Description = album.Description,
            CoverPhotoUrl = album.CoverPhotoUrl,
            RetentionDays = album.RetentionDays,
            PhotoCount = album.PhotoCount,
            PublishedAt = album.PublishedAt,
            ClosedAt = album.ClosedAt,
            CreatedAt = album.CreatedAt,
            UpdatedAt = album.UpdatedAt
        };
    }
}
