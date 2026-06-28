using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Queries.PhotoAlbums.GetAlbumByEventId;

/// <summary>
/// Query to get all photo albums for an event.
/// Returns empty list if no albums exist (not an error).
/// </summary>
public record GetAlbumByEventIdQuery(Guid EventId) : IQuery<List<PhotoAlbumDto>>;

public class GetAlbumByEventIdQueryHandler : IQueryHandler<GetAlbumByEventIdQuery, List<PhotoAlbumDto>>
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

    public async Task<Result<List<PhotoAlbumDto>>> Handle(GetAlbumByEventIdQuery request, CancellationToken cancellationToken)
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
                    return Result<List<PhotoAlbumDto>>.Failure("Event ID is required");
                }

                // Get all albums for this event with no change tracking (read-only query)
                var albums = await _photoAlbumRepository.GetAllByEventIdAsync(
                    request.EventId, trackChanges: false, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetAlbumByEventId COMPLETE: EventId={EventId}, AlbumCount={AlbumCount}, Duration={ElapsedMs}ms",
                    request.EventId, albums.Count, stopwatch.ElapsedMilliseconds);

                var dtos = albums.Select(MapToDto).ToList();
                return Result<List<PhotoAlbumDto>>.Success(dtos);
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
            Name = album.Name,
            Status = album.Status,
            Description = album.Description,
            CoverPhotoUrl = album.CoverPhotoUrl,
            RetentionDays = album.RetentionDays,
            PhotoCount = album.PhotoCount,
            PublishedAt = album.PublishedAt,
            CreatedAt = album.CreatedAt,
            UpdatedAt = album.UpdatedAt
        };
    }
}
