using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.ClosePhotoAlbum;

/// <summary>
/// Command to close a photo album, stopping new uploads.
/// Photos remain viewable until their individual expiry dates.
/// </summary>
public record ClosePhotoAlbumCommand(
    Guid EventId,
    Guid UserId
) : ICommand;

public class ClosePhotoAlbumCommandHandler : ICommandHandler<ClosePhotoAlbumCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClosePhotoAlbumCommandHandler> _logger;

    public ClosePhotoAlbumCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClosePhotoAlbumCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ClosePhotoAlbumCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ClosePhotoAlbum"))
        using (LogContext.PushProperty("EntityType", "PhotoAlbum"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ClosePhotoAlbum START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // 1. Get album by event ID with change tracking
                var album = await _photoAlbumRepository.GetByEventIdAsync(request.EventId, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ClosePhotoAlbum FAILED: Album not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found for this event");
                }

                // 2. Verify the user is the organizer
                if (album.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ClosePhotoAlbum FAILED: User is not organizer - EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Only the event organizer can close the photo album");
                }

                // 3. Close via domain method
                var closeResult = album.Close();
                if (closeResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ClosePhotoAlbum FAILED: Domain validation failed - EventId={EventId}, AlbumId={AlbumId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, album.Id, closeResult.Error, stopwatch.ElapsedMilliseconds);
                    return closeResult;
                }

                // 4. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "ClosePhotoAlbum COMPLETE: AlbumId={AlbumId}, EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                    album.Id, request.EventId, album.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ClosePhotoAlbum FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
