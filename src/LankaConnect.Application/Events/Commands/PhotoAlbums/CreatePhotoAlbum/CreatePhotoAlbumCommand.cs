using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.CreatePhotoAlbum;

/// <summary>
/// Command to create a photo album for an event.
/// Multiple albums per event allowed (unique constraint on EventId + Name).
/// Event must be in Published, Active, Completed, or Archived status.
/// </summary>
public record CreatePhotoAlbumCommand(
    Guid EventId,
    Guid UserId,
    string Name,
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
                "CreatePhotoAlbum START: EventId={EventId}, UserId={UserId}, Name={AlbumName}",
                request.EventId, request.UserId, request.Name);

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

                // 3. Verify event status allows album creation (Published, Active, Completed, or Archived)
                var allowedStatuses = new[] { EventStatus.Published, EventStatus.Active, EventStatus.Completed, EventStatus.Archived };
                if (!allowedStatuses.Contains(@event.Status))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreatePhotoAlbum FAILED: Invalid event status - EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                        request.EventId, @event.Status, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto>.Failure(
                        $"Cannot create a photo album for an event in {(int)@event.Status} status. Event must be Published, Active, Completed, or Archived.");
                }

                // 4. Verify no album with same name exists for this event
                var nameExists = await _photoAlbumRepository.ExistsByEventIdAndNameAsync(
                    request.EventId, request.Name, cancellationToken);
                if (nameExists)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreatePhotoAlbum FAILED: Album with name already exists - EventId={EventId}, Name={AlbumName}, Duration={ElapsedMs}ms",
                        request.EventId, request.Name, stopwatch.ElapsedMilliseconds);
                    return Result<PhotoAlbumDto>.Failure($"A photo album with the name '{request.Name}' already exists for this event");
                }

                // 5. Create album via domain factory method
                var createResult = PhotoAlbum.Create(
                    request.EventId,
                    request.UserId,
                    @event.Title.Value,
                    request.Name,
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
                    "CreatePhotoAlbum COMPLETE: AlbumId={AlbumId}, EventId={EventId}, Name={AlbumName}, Duration={ElapsedMs}ms",
                    album.Id, request.EventId, album.Name, stopwatch.ElapsedMilliseconds);

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
