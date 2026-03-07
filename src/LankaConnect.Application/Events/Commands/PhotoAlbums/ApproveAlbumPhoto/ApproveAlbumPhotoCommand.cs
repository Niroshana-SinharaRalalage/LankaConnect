using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.ApproveAlbumPhoto;

/// <summary>
/// Command to approve a pending photo in a pre-moderation album.
/// Only the event organizer can approve photos.
/// </summary>
public record ApproveAlbumPhotoCommand(
    Guid EventId,
    Guid PhotoId,
    Guid UserId
) : ICommand;

public class ApproveAlbumPhotoCommandHandler : ICommandHandler<ApproveAlbumPhotoCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveAlbumPhotoCommandHandler> _logger;

    public ApproveAlbumPhotoCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IUnitOfWork unitOfWork,
        ILogger<ApproveAlbumPhotoCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ApproveAlbumPhotoCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ApproveAlbumPhoto"))
        using (LogContext.PushProperty("EntityType", "AlbumPhoto"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PhotoId", request.PhotoId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ApproveAlbumPhoto START: EventId={EventId}, PhotoId={PhotoId}, UserId={UserId}",
                request.EventId, request.PhotoId, request.UserId);

            try
            {
                // 1. Get album by event ID with change tracking
                var album = await _photoAlbumRepository.GetByEventIdAsync(request.EventId, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ApproveAlbumPhoto FAILED: Album not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found for this event");
                }

                // 2. Verify the user is the organizer
                if (album.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ApproveAlbumPhoto FAILED: User is not organizer - EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Only the event organizer can approve photos");
                }

                // 3. Approve photo via domain method
                var approveResult = album.ApprovePhoto(request.PhotoId);
                if (approveResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ApproveAlbumPhoto FAILED: Domain validation failed - EventId={EventId}, PhotoId={PhotoId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.PhotoId, approveResult.Error, stopwatch.ElapsedMilliseconds);
                    return approveResult;
                }

                // 4. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "ApproveAlbumPhoto COMPLETE: PhotoId={PhotoId}, AlbumId={AlbumId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.PhotoId, album.Id, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ApproveAlbumPhoto FAILED: Exception occurred - EventId={EventId}, PhotoId={PhotoId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.PhotoId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
