using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.CreatePhotoAlbum;

/// <summary>
/// Command to create a photo album for an event.
/// Only one album per event (enforced by unique constraint).
/// Event must be in Active, Completed, or Archived status.
/// </summary>
public record CreatePhotoAlbumCommand(
    Guid EventId,
    Guid UserId,
    string? Description = null
) : ICommand<PhotoAlbumDto>;

public class CreatePhotoAlbumCommandHandler : ICommandHandler<CreatePhotoAlbumCommand, PhotoAlbumDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePhotoAlbumCommandHandler> _logger;

    public CreatePhotoAlbumCommandHandler(
        IEventRepository eventRepository,
        IPhotoAlbumRepository photoAlbumRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreatePhotoAlbumCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _photoAlbumRepository = photoAlbumRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PhotoAlbumDto>> Handle(CreatePhotoAlbumCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreatePhotoAlbum"))
        using (LogContext.PushProperty("EntityType", "PhotoAlbum"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreatePhotoAlbum START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // 1. Get event and verify it exists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: false, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreatePhotoAlbum FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto>.Failure("Event not found");
                }

                // 2. Verify the user is the organizer
                if (!@event.IsOrganizer(request.UserId))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreatePhotoAlbum FAILED: User is not organizer - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto>.Failure("Only the event organizer can create a photo album");
                }

                // 3. Verify event status allows album creation (Active, Completed, or Archived)
                var allowedStatuses = new[] { EventStatus.Active, EventStatus.Completed, EventStatus.Archived };
                if (!allowedStatuses.Contains(@event.Status))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreatePhotoAlbum FAILED: Invalid event status - EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                        request.EventId, @event.Status, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto>.Failure(
                        $"Cannot create a photo album for an event in {(int)@event.Status} status. Event must be Active, Completed, or Archived.");
                }

                // 4. Verify no album exists for this event
                var albumExists = await _photoAlbumRepository.AlbumExistsForEventAsync(request.EventId, cancellationToken);
                if (albumExists)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreatePhotoAlbum FAILED: Album already exists - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto>.Failure("A photo album already exists for this event");
                }

                // 5. Create album via domain factory method
                var createResult = PhotoAlbum.Create(
                    request.EventId,
                    request.UserId,
                    @event.Title.Value,
                    request.Description);

                if (createResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreatePhotoAlbum FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, createResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto>.Failure(createResult.Errors);
                }

                var album = createResult.Value;

                // 6. Add to repository and commit
                await _photoAlbumRepository.AddAsync(album, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreatePhotoAlbum COMPLETE: AlbumId={AlbumId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    album.Id, request.EventId, stopwatch.ElapsedMilliseconds);

                // 7. Map to DTO and return
                var dto = MapToDto(album);
                return Result<PhotoAlbumDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "CreatePhotoAlbum FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
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
